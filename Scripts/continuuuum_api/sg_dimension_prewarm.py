"""Build / cache SG2D/SG3D/SG4D warm snapshots for dimension switch-over."""

from __future__ import annotations

import hashlib
import json
import sqlite3
import time
from typing import Any, Optional

try:
    from continuuuum_api import game_dimension_dao as dao
except ImportError:
    import game_dimension_dao as dao  # type: ignore

SG_KINDS = ("sg2d", "sg3d", "sg4d")

# In-process TTL cache: key -> (expires_at, payload_dict)
_ttl_cache: dict[str, tuple[float, dict[str, Any]]] = {}
_TTL_SECONDS = 60.0


def _cache_key(game_id: str, dimension_id: str, sg_kind: str) -> str:
    return f"{game_id}:{dimension_id}:{sg_kind}"


def _ttl_get(game_id: str, dimension_id: str, sg_kind: str) -> Optional[dict[str, Any]]:
    key = _cache_key(game_id, dimension_id, sg_kind)
    hit = _ttl_cache.get(key)
    if not hit:
        return None
    exp, payload = hit
    if time.time() > exp:
        _ttl_cache.pop(key, None)
        return None
    return payload


def _ttl_put(game_id: str, dimension_id: str, sg_kind: str, payload: dict[str, Any]) -> None:
    key = _cache_key(game_id, dimension_id, sg_kind)
    _ttl_cache[key] = (time.time() + _TTL_SECONDS, payload)


def invalidate_ttl(
    game_id: Optional[str] = None, dimension_id: Optional[str] = None
) -> None:
    if game_id is None and dimension_id is None:
        _ttl_cache.clear()
        return
    dead = []
    for k in _ttl_cache:
        parts = k.split(":")
        if game_id and parts[0] != game_id:
            continue
        if dimension_id and len(parts) > 1 and parts[1] != dimension_id:
            continue
        if game_id or dimension_id:
            dead.append(k)
    for k in dead:
        _ttl_cache.pop(k, None)


def _collect_spatial_entries(conn: sqlite3.Connection, game_id: str, dim_index: int) -> list[dict[str, Any]]:
    """Best-effort: entries with spatial-gen properties, filtered by assoc when present."""
    out: list[dict[str, Any]] = []
    try:
        rows = conn.execute(
            """
            SELECT DISTINCT entry_id FROM thesaurus_entry_properties
            WHERE property_key LIKE 'spatial%' OR property_key = 'spatial-generator-definitions'
            LIMIT 200
            """
        ).fetchall()
    except sqlite3.OperationalError:
        return out

    dim = dao.get_dimension_by_index(conn, dim_index)
    dim_id = dim["id"] if dim else None
    for r in rows:
        eid = r["entry_id"]
        if dim_id and not dao.entity_visible_for_context(
            conn, "thesaurus_entries", eid, game_id, dim_id
        ):
            continue
        props = dao.resolve_entry_properties(conn, eid, dim_index)
        view: dict[str, Any] = {"id": eid, "properties": props}
        try:
            from continuuuum_api.spatial_generator_specs import enrich_entry_spatial_generators
        except ImportError:
            try:
                from spatial_generator_specs import enrich_entry_spatial_generators  # type: ignore
            except ImportError:
                enrich_entry_spatial_generators = None  # type: ignore
        if enrich_entry_spatial_generators:
            enrich_entry_spatial_generators(view)
        out.append(
            {
                "entryId": eid,
                "spatialGeneratorDefinitions": view.get("spatialGeneratorDefinitions") or [],
                "spatialGen2d": view.get("spatialGen2d"),
                "spatialGen3d": view.get("spatialGen3d"),
                "spatialGen4d": view.get("spatialGen4d"),
                "prefabId": props.get("prefab-id") or props.get("prefab_id"),
                "stylesheetPrefabIds": props.get("stylesheet-prefab-ids"),
            }
        )
    return out


def build_kind_payload(
    conn: sqlite3.Connection,
    game: dict[str, Any],
    dim: dict[str, Any],
    sg_kind: str,
) -> dict[str, Any]:
    entries = _collect_spatial_entries(conn, game["id"], int(dim["dimIndex"]))
    mode = "TwoDimensional" if sg_kind == "sg2d" else "ThreeDimensional" if sg_kind == "sg3d" else "FourDimensional"
    payload = {
        "sgKind": sg_kind,
        "gameSlug": game["slug"],
        "dimIndex": dim["dimIndex"],
        "mode": mode,
        "enable4d": sg_kind == "sg4d",
        "entries": entries,
        "placementHints": {
            "seed": f"{game['slug']}:{dim['dimIndex']}:{sg_kind}",
            "treeParamOverrides": {},
        },
    }
    raw = json.dumps(payload, sort_keys=True, default=str)
    etag = hashlib.sha256(raw.encode("utf-8")).hexdigest()[:16]
    payload["etag"] = etag
    payload["sourceRevision"] = etag
    return payload


def build_or_refresh(
    conn: sqlite3.Connection,
    game_ref: Any,
    dim_ref: Any,
    kinds: Optional[list[str]] = None,
) -> dict[str, Any]:
    game = dao.resolve_game_ref(conn, game_ref) or dao.get_game_by_slug(conn, "main")
    dim = dao.resolve_dimension_ref(conn, dim_ref) or dao.get_dimension_by_index(conn, 0)
    if not game or not dim:
        raise ValueError("unknown game or dimension")
    use_kinds = [k for k in (kinds or list(SG_KINDS)) if k in SG_KINDS]
    etags: dict[str, str] = {}
    built: dict[str, Any] = {}
    for kind in use_kinds:
        payload = build_kind_payload(conn, game, dim, kind)
        snap = dao.put_warm_snapshot(
            conn,
            game["id"],
            dim["id"],
            kind,
            payload,
            payload["etag"],
            payload.get("sourceRevision"),
        )
        _ttl_put(game["id"], dim["id"], kind, snap)
        etags[kind] = payload["etag"]
        built[kind] = snap
    return {
        "game": game,
        "dimension": dim,
        "etags": etags,
        "builtAt": next(iter(built.values()))["builtAt"] if built else None,
        "kinds": use_kinds,
        "snapshots": built,
    }


def get_prewarm(
    conn: sqlite3.Connection,
    game_ref: Any,
    dim_ref: Any,
    kind: Optional[str] = None,
) -> dict[str, Any]:
    game = dao.resolve_game_ref(conn, game_ref) or dao.get_game_by_slug(conn, "main")
    dim = dao.resolve_dimension_ref(conn, dim_ref) or dao.get_dimension_by_index(conn, 0)
    if not game or not dim:
        return {"snapshots": {}, "missing": True}
    kinds = [kind] if kind else list(SG_KINDS)
    snaps: dict[str, Any] = {}
    missing = []
    for k in kinds:
        if k not in SG_KINDS:
            continue
        cached = _ttl_get(game["id"], dim["id"], k)
        if cached:
            snaps[k] = cached
            continue
        snap = dao.get_warm_snapshot(conn, game["id"], dim["id"], k)
        if snap:
            _ttl_put(game["id"], dim["id"], k, snap)
            snaps[k] = snap
        else:
            missing.append(k)
    return {
        "game": game,
        "dimension": dim,
        "snapshots": snaps,
        "missing": missing,
    }


def invalidate(
    conn: sqlite3.Connection,
    game_ref: Any = None,
    dim_ref: Any = None,
) -> int:
    game_id = None
    dim_id = None
    if game_ref is not None:
        g = dao.resolve_game_ref(conn, game_ref)
        game_id = g["id"] if g else None
    if dim_ref is not None:
        d = dao.resolve_dimension_ref(conn, dim_ref)
        dim_id = d["id"] if d else None
    invalidate_ttl(game_id, dim_id)
    return dao.invalidate_warm_snapshots(conn, game_id, dim_id)
