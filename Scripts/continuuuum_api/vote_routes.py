"""Vote runs, named ballots, GameSession list, and Game Lobbies for Continuuuum."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable

from flask import jsonify, request, send_from_directory

GetConn = Callable[[], sqlite3.Connection]
STATIC_VOTES = Path(__file__).resolve().parent / "static" / "votes"
STATIC_LOBBIES = Path(__file__).resolve().parent / "static" / "game-lobbies"
STATIC_VOTING_PLACES = Path(__file__).resolve().parent / "static" / "voting-places"
STATIC_PLAYERS = Path(__file__).resolve().parent / "static" / "players"

BALLOT_KINDS = ("Question", "Measure", "Candidate")
BALLOT_KIND_ROLES = {
    "Measure": "law",
    "Question": "jurisdiction",
    "Candidate": "electoral",
}
TALLY_METHODS = ("plurality", "irv", "stv")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _add_columns(conn: sqlite3.Connection, table: str, columns: list[tuple[str, str]]) -> None:
    existing = {r[1] for r in conn.execute(f"PRAGMA table_info({table})").fetchall()}
    for name, decl in columns:
        if name not in existing:
            conn.execute(f"ALTER TABLE {table} ADD COLUMN {name} {decl}")


def ensure_vote_tables(conn: sqlite3.Connection) -> None:
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS game_lobby_configs (
            id TEXT PRIMARY KEY,
            name TEXT UNIQUE NOT NULL,
            lobby_type_id TEXT,
            content_kind TEXT DEFAULT 'game_mode',
            content_id TEXT,
            max_players INTEGER DEFAULT 8,
            min_players_to_start INTEGER DEFAULT 1,
            game_size INTEGER DEFAULT 8,
            mode TEXT DEFAULT 'SinglePlayer',
            require_password INTEGER DEFAULT 0,
            allow_spectators INTEGER DEFAULT 1,
            max_spectators INTEGER DEFAULT 4,
            properties_json TEXT DEFAULT '{}',
            created_utc TEXT NOT NULL,
            updated_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS game_lobbies (
            name TEXT PRIMARY KEY,
            display_name TEXT,
            lobby_type_id TEXT,
            content_kind TEXT DEFAULT 'game_mode',
            content_id TEXT,
            advertise_address TEXT,
            lobby_port INTEGER DEFAULT 7780,
            game_port INTEGER DEFAULT 7777,
            player_count INTEGER DEFAULT 0,
            max_players INTEGER DEFAULT 8,
            min_players_to_start INTEGER DEFAULT 1,
            game_size INTEGER DEFAULT 8,
            mode TEXT DEFAULT 'SinglePlayer',
            require_password INTEGER DEFAULT 0,
            allow_spectators INTEGER DEFAULT 1,
            max_spectators INTEGER DEFAULT 4,
            properties_json TEXT DEFAULT '{}',
            active INTEGER DEFAULT 0,
            last_heartbeat_utc TEXT,
            created_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS game_sessions (
            id TEXT PRIMARY KEY,
            lobby_session_name TEXT NOT NULL,
            display_name TEXT,
            created_utc TEXT NOT NULL,
            created_narrative_time REAL DEFAULT 0,
            active INTEGER DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS vote_ballots (
            name TEXT PRIMARY KEY,
            kind TEXT DEFAULT 'Question',
            title TEXT,
            prompt TEXT,
            spec_json TEXT DEFAULT '{}',
            created_utc TEXT NOT NULL,
            updated_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS vote_runs (
            run_id TEXT PRIMARY KEY,
            game_session_id TEXT NOT NULL,
            ballot_id TEXT,
            causality_leaf_id TEXT,
            certified INTEGER DEFAULT 0,
            tally_json TEXT,
            tally_hash INTEGER DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS vote_casts (
            id TEXT PRIMARY KEY,
            run_id TEXT NOT NULL,
            actor_id TEXT,
            option_id TEXT,
            demographic_slice_id TEXT,
            causality_leaf_id TEXT,
            spoiled INTEGER DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS game_session_players (
            id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            player_id TEXT NOT NULL,
            display_name TEXT,
            actor_id TEXT,
            local_client_json TEXT,
            updated_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS game_session_vote_config (
            session_id TEXT PRIMARY KEY,
            demographics_json TEXT DEFAULT '{}',
            certified_json TEXT DEFAULT '{}',
            last_ballot_id TEXT,
            last_kind TEXT,
            last_run_id TEXT,
            updated_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS voting_places (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            lobby_id TEXT,
            properties_json TEXT DEFAULT '{}',
            created_utc TEXT NOT NULL,
            updated_utc TEXT NOT NULL
        );
        """
    )
    _add_columns(
        conn,
        "game_sessions",
        [
            ("source", "TEXT DEFAULT 'web'"),
            ("live", "INTEGER DEFAULT 0"),
            ("last_heartbeat_utc", "TEXT"),
            ("parent_id", "TEXT"),
            ("pecking_order", "INTEGER DEFAULT 20"),
        ],
    )
    _add_columns(conn, "game_lobbies", [("config_id", "TEXT")])
    _add_columns(
        conn,
        "vote_ballots",
        [("demographics_json", "TEXT DEFAULT '{}'"), ("gov_mix_json", "TEXT DEFAULT '{}'")],
    )
    _add_columns(conn, "vote_casts", [("ranking_json", "TEXT DEFAULT '[]'")])
    _migrate_lobby_configs(conn)
    conn.commit()


def _lobby_dict(r) -> dict:
    props = r["properties_json"] if "properties_json" in r.keys() else "{}"
    try:
        parsed = json.loads(props or "{}")
    except json.JSONDecodeError:
        parsed = {}
    return {
        "name": r["name"],
        "displayName": r["display_name"],
        "lobbyTypeId": r["lobby_type_id"],
        "contentKind": r["content_kind"] or "game_mode",
        "contentId": r["content_id"] or "",
        "advertiseAddress": r["advertise_address"] or "",
        "lobbyPort": r["lobby_port"] or 7780,
        "gamePort": r["game_port"] or 7777,
        "playerCount": r["player_count"] or 0,
        "maxPlayers": r["max_players"] or 8,
        "minPlayersToStart": r["min_players_to_start"] or 1,
        "gameSize": r["game_size"] or r["max_players"] or 8,
        "mode": r["mode"] or "SinglePlayer",
        "requirePassword": bool(r["require_password"]),
        "allowSpectators": bool(r["allow_spectators"]),
        "maxSpectators": r["max_spectators"] or 4,
        "propertiesJson": parsed if isinstance(parsed, dict) else {},
        "active": bool(r["active"]),
        "lastHeartbeatUtc": r["last_heartbeat_utc"],
        "createdUtc": r["created_utc"],
        "configId": r["config_id"] if "config_id" in r.keys() else "",
    }


def _session_dict(r) -> dict:
    return {
        "id": r["id"],
        "lobbySessionName": r["lobby_session_name"],
        "displayName": r["display_name"],
        "createdUtc": r["created_utc"],
        "createdNarrativeTime": r["created_narrative_time"],
        "active": bool(r["active"]),
        "source": r["source"] if "source" in r.keys() else "web",
        "live": bool(r["live"]) if "live" in r.keys() else False,
        "lastHeartbeatUtc": r["last_heartbeat_utc"] if "last_heartbeat_utc" in r.keys() else None,
        "parentId": r["parent_id"] if "parent_id" in r.keys() else None,
        "peckingOrder": r["pecking_order"] if "pecking_order" in r.keys() else 20,
    }


def _session_dict_full(conn: sqlite3.Connection, r) -> dict:
    d = _session_dict(r)
    d["players"] = _session_players(conn, r["id"])
    d["voteConfig"] = _get_vote_config(conn, r["id"])
    runs = conn.execute(
        "SELECT run_id, ballot_id, certified, tally_json, tally_hash FROM vote_runs WHERE game_session_id=? ORDER BY rowid",
        (r["id"],),
    ).fetchall()
    d["runs"] = []
    for run in runs:
        item = {
            "runId": run["run_id"],
            "ballotId": run["ballot_id"],
            "ballotKind": _ballot_kind_for(conn, run["ballot_id"]),
            "certified": bool(run["certified"]),
            "tally": json.loads(run["tally_json"] or "{}"),
            "tallyHash": run["tally_hash"],
        }
        item.update(_run_debug(conn, run["run_id"]))
        d["runs"].append(item)
    return d


def _json_obj(value, fallback=None):
    if fallback is None:
        fallback = {}
    if isinstance(value, dict):
        return value
    if isinstance(value, str) and value.strip():
        try:
            parsed = json.loads(value)
            return parsed if isinstance(parsed, dict) else fallback
        except json.JSONDecodeError:
            return fallback
    return fallback


def _share_units(value) -> int:
    try:
        units = int(round(float(value) * 100))
    except (TypeError, ValueError):
        units = 0
    return max(0, min(100, units))


def reconcile_demographic_shares(shares: list, changed_index: int | None = None) -> list[float]:
    """Keep the changed share; split the remainder evenly. Leftover hundredths go to the last other slice."""
    n = len(shares or [])
    if n == 0:
        return []
    if n == 1:
        return [1.0]
    if changed_index is None or changed_index < 0 or changed_index >= n:
        even, extra = divmod(100, n)
        return [(even + (extra if i == n - 1 else 0)) / 100.0 for i in range(n)]
    changed = _share_units(shares[changed_index])
    remainder = 100 - changed
    others = n - 1
    even, extra = divmod(remainder, others)
    last_other = n - 2 if changed_index == n - 1 else n - 1
    out = []
    for i in range(n):
        if i == changed_index:
            out.append(changed / 100.0)
        else:
            out.append((even + (extra if i == last_other else 0)) / 100.0)
    return out


def _normalize_demographics(demo) -> dict:
    data = demo if isinstance(demo, dict) else {}
    slices = [s for s in (data.get("slices") or []) if isinstance(s, dict)]
    shares = [s.get("share01") for s in slices]
    if slices and sum(_share_units(s) for s in shares) != 100:
        for sl, sh in zip(slices, reconcile_demographic_shares(shares, None)):
            sl["share01"] = sh
    return {**data, "slices": slices}


def _ballot_kind_for(conn: sqlite3.Connection, ballot_id: str | None) -> str:
    if not ballot_id:
        return ""
    row = conn.execute("SELECT kind FROM vote_ballots WHERE name=?", (ballot_id,)).fetchone()
    return _normalize_ballot_kind(row["kind"]) if row is not None else ""


def _normalize_ballot_kind(kind: str | None) -> str:
    raw = (kind or "Question").strip()
    lower = raw.lower()
    for name in BALLOT_KINDS:
        if lower == name.lower():
            return name
    return "Question"


def _default_ballot_options(kind: str) -> list:
    if kind == "Candidate":
        return []
    return [
        {"optionId": "yes", "displayName": "Yes"},
        {"optionId": "no", "displayName": "No"},
    ]


def _gov_mix_share(mix: dict) -> dict:
    keys = (
        "republic01",
        "parliamentary01",
        "theocracy01",
        "monarchyCeremonial01",
        "monarchyReal01",
        "junta01",
    )
    vals = {k: float(mix.get(k) or 0) for k in keys}
    total = sum(vals.values())
    if total <= 1e-5:
        return {k: 0.0 for k in keys} | {"sum": 0.0}
    return {k: vals[k] / total for k in keys} | {"sum": total}


def _normalize_tally_method(method, kind: str | None = None, seats=None) -> tuple[str, int]:
    raw = (method or "plurality").strip().lower()
    aliases = {
        "ranked": "irv",
        "ranked-choice": "irv",
        "rankedchoice": "irv",
        "rcv": "irv",
        "instant-runoff": "irv",
        "instant_runoff": "irv",
    }
    raw = aliases.get(raw, raw)
    if raw not in TALLY_METHODS:
        raw = "plurality"
    if raw == "stv":
        n = max(2, int(seats or 2))
    elif raw == "irv":
        n = 1
    else:
        n = 1
    return raw, n


def ballot_gov_errors(kind: str, gov_mix: dict | None, tally_method: str | None = None) -> list[str]:
    """Fold ballot kinds into government mix; error when the kind does not apply."""
    kind = _normalize_ballot_kind(kind)
    role = BALLOT_KIND_ROLES.get(kind, "jurisdiction")
    method, _seats = _normalize_tally_method(tally_method, kind)
    errors = []
    if method in ("irv", "stv") and kind != "Candidate":
        errors.append("ranked choice (IRV/STV) is only used on candidate ballots")
    if not gov_mix:
        return errors
    share = _gov_mix_share(gov_mix)
    if share["sum"] <= 1e-5:
        return errors
    senate_theocracy = bool(gov_mix.get("parliamentarySenateEnablesTheocracy"))
    civic = share["republic01"] + share["parliamentary01"] + share["monarchyCeremonial01"]
    if share["junta01"] >= 0.45 and role != "law":
        errors.append(f"{role} ballots are not used under junta government; use measures for laws")
    if share["monarchyReal01"] >= 0.45 and role == "electoral":
        errors.append("electoral ballots are not used under real monarchy")
    if share["theocracy01"] >= 0.45 and not senate_theocracy and role == "electoral":
        errors.append("electoral ballots are not used under theocracy without a parliamentary senate")
    if civic < 0.2 and role == "jurisdiction":
        errors.append("jurisdictional questions require a civic government mix")
    return errors


def _clean_ranking(ranking) -> list[str]:
    out = []
    seen = set()
    if not isinstance(ranking, list):
        return out
    for item in ranking:
        oid = str(item or "").strip()
        if not oid or oid in seen:
            continue
        seen.add(oid)
        out.append(oid)
    return out


def _ranking_from_cast(row) -> list[str]:
    ranking = []
    if "ranking_json" in row.keys() and row["ranking_json"]:
        try:
            parsed = json.loads(row["ranking_json"])
            ranking = _clean_ranking(parsed)
        except json.JSONDecodeError:
            ranking = []
    if not ranking and row["option_id"]:
        ranking = [row["option_id"]]
    return ranking


def _first_preferences(rankings: list[list[str]]) -> dict:
    first = {}
    for ranking in rankings:
        if ranking:
            first[ranking[0]] = first.get(ranking[0], 0) + 1
    return first


def _active_first_counts(ballots: list[list[str]], remaining: set[str]) -> tuple[dict, int]:
    counts = {c: 0 for c in remaining}
    active = 0
    for ranking in ballots:
        for oid in ranking:
            if oid in remaining:
                counts[oid] += 1
                active += 1
                break
    return counts, active


def _tally_irv(ballots: list[list[str]], remaining: set[str], first: dict) -> dict:
    rounds = []
    winners = []
    live = set(remaining)
    while live:
        counts, active = _active_first_counts(ballots, live)
        rec = {"counts": counts, "active": active, "eliminated": None, "elected": None}
        if active <= 0:
            rounds.append(rec)
            break
        ordered = sorted(live, key=lambda c: (-counts.get(c, 0), c))
        leader = ordered[0]
        if counts.get(leader, 0) > active / 2.0 or len(live) <= 2:
            winners = [leader]
            rec["elected"] = leader
            rounds.append(rec)
            break
        loser = min(live, key=lambda c: (counts.get(c, 0), c))
        live.remove(loser)
        rec["eliminated"] = loser
        rounds.append(rec)
    return {"method": "irv", "seats": 1, "firstPreferences": first, "winners": winners, "rounds": rounds}


def _tally_stv(ballots: list[list[str]], remaining: set[str], first: dict, seats: int) -> dict:
    papers = [{"ranking": list(b), "value": 1.0} for b in ballots]
    live = set(remaining)
    elected: list[str] = []
    rounds = []
    quota = int(len(papers) / (seats + 1)) + 1

    def counts_now() -> dict:
        counts = {c: 0.0 for c in live}
        for p in papers:
            if p["value"] <= 0:
                continue
            for oid in p["ranking"]:
                if oid in live:
                    counts[oid] += p["value"]
                    break
        return counts

    def transfer_surplus(pick: str, factor: float) -> None:
        for p in papers:
            for oid in p["ranking"]:
                if oid == pick:
                    p["value"] *= factor
                    break
                if oid in live:
                    break

    while len(elected) < seats and live:
        counts = counts_now()
        rec = {
            "counts": {k: round(v, 6) for k, v in counts.items()},
            "quota": quota,
            "elected": None,
            "eliminated": None,
            "surplus": 0,
        }
        at_quota = sorted([c for c in live if counts.get(c, 0) >= quota], key=lambda c: (-counts[c], c))
        if at_quota:
            pick = at_quota[0]
            votes = counts[pick]
            surplus = max(0.0, votes - quota)
            live.remove(pick)
            elected.append(pick)
            rec["elected"] = pick
            rec["surplus"] = round(surplus, 6)
            if votes > 0 and surplus > 0:
                transfer_surplus(pick, surplus / votes)
            else:
                transfer_surplus(pick, 0.0)
            rounds.append(rec)
            continue
        leftover = seats - len(elected)
        if len(live) <= leftover:
            for c in sorted(live, key=lambda x: (-counts.get(x, 0), x)):
                if len(elected) >= seats:
                    break
                elected.append(c)
                rec["elected"] = c
            live.clear()
            rounds.append(rec)
            break
        loser = min(live, key=lambda c: (counts.get(c, 0), c))
        live.remove(loser)
        rec["eliminated"] = loser
        rounds.append(rec)
    return {
        "method": "stv",
        "seats": seats,
        "firstPreferences": first,
        "winners": elected[:seats],
        "quota": quota,
        "rounds": rounds,
    }


def ranked_tally(rankings: list[list[str]], method: str, seats: int = 1, candidates: list[str] | None = None) -> dict:
    method, seats = _normalize_tally_method(method, seats=seats)
    ballots = [_clean_ranking(r) for r in rankings]
    ballots = [b for b in ballots if b]
    first = _first_preferences(ballots)
    if method == "plurality":
        ordered = sorted(first.items(), key=lambda kv: -kv[1])
        winners = [k for k, _ in ordered[:1]]
        return {
            "method": "plurality",
            "seats": 1,
            "firstPreferences": first,
            "winners": winners,
            "rounds": [],
        }
    cand = set(candidates or [])
    if not cand:
        for b in ballots:
            cand.update(b)
    if method == "irv":
        return _tally_irv(ballots, cand, first)
    return _tally_stv(ballots, cand, first, seats)


def _tally_hash(first: dict, winners: list) -> int:
    h = 17
    for k, v in sorted((first or {}).items()):
        h = (h * 31 + hash(str(k)) + int(v)) & 0x7FFFFFFF
    for w in winners or []:
        h = (h * 31 + hash(str(w))) & 0x7FFFFFFF
    return h


def _yes_no_property(kind: str, ballot_id: str, option_id: str) -> tuple[str, str]:
    prop = ("law." if kind == "Measure" else "jurisdiction.") + (ballot_id or "item")
    return prop, "true" if option_id == "yes" else "false"


def _fold_option_assignments(kind: str, ballot_id: str, options: list) -> list:
    out = []
    for opt in options or []:
        if not isinstance(opt, dict):
            continue
        row = dict(opt)
        oid = row.get("optionId") or row.get("id") or ""
        if kind != "Candidate" and not row.get("win"):
            name, value = _yes_no_property(kind, ballot_id, oid)
            row["win"] = [{"propertyName": name, "propertyValue": value}]
        if kind == "Candidate" and not row.get("win"):
            row["win"] = [{"propertyName": "governor", "propertyValue": oid}]
        out.append(row)
    return out


def _player_dict(r) -> dict:
    payload = _json_obj(r["local_client_json"] if "local_client_json" in r.keys() else "{}")
    return {
        "id": r["id"],
        "sessionId": r["session_id"],
        "playerId": r["player_id"],
        "displayName": r["display_name"] or r["player_id"],
        "actorId": r["actor_id"] or r["player_id"],
        "hasLocalClient": bool(payload),
        "updatedUtc": r["updated_utc"],
    }


def _upsert_players(conn: sqlite3.Connection, session_id: str, players: list) -> None:
    now = _now()
    for p in players or []:
        if not isinstance(p, dict):
            continue
        pid = (p.get("playerId") or p.get("id") or "").strip()
        if not pid:
            continue
        actor = p.get("actorId") or pid
        display = p.get("displayName") or pid
        local = p.get("localClient") if isinstance(p.get("localClient"), dict) else None
        existing = conn.execute(
            "SELECT * FROM game_session_players WHERE session_id=? AND player_id=?",
            (session_id, pid),
        ).fetchone()
        local_json = json.dumps(local) if local is not None else (existing["local_client_json"] if existing else "{}")
        if existing is None:
            conn.execute(
                """INSERT INTO game_session_players (
                    id, session_id, player_id, display_name, actor_id, local_client_json, updated_utc
                ) VALUES (?,?,?,?,?,?,?)""",
                (uuid.uuid4().hex, session_id, pid, display, actor, local_json, now),
            )
        else:
            conn.execute(
                """UPDATE game_session_players SET display_name=?, actor_id=?, local_client_json=?, updated_utc=?
                   WHERE id=?""",
                (display, actor, local_json, now, existing["id"]),
            )


def _session_players(conn: sqlite3.Connection, session_id: str) -> list:
    rows = conn.execute(
        "SELECT * FROM game_session_players WHERE session_id=? ORDER BY display_name, player_id",
        (session_id,),
    ).fetchall()
    return [_player_dict(r) for r in rows]


def _local_client_payload(conn: sqlite3.Connection, session_row, player_row=None) -> dict:
    lobby_name = session_row["lobby_session_name"]
    lobby = conn.execute("SELECT * FROM game_lobbies WHERE name=?", (lobby_name,)).fetchone()
    player = _player_dict(player_row) if player_row is not None else None
    stored = _json_obj(player_row["local_client_json"] if player_row is not None else "{}")
    payload = {
        "lemma": "save-server-to-local",
        "perspective": "player",
        "id": session_row["id"],
        "displayName": session_row["display_name"],
        "lobbySessionName": lobby_name,
        "parentId": session_row["parent_id"] if "parent_id" in session_row.keys() else None,
        "peckingOrder": session_row["pecking_order"] if "pecking_order" in session_row.keys() else 20,
        "prefab": _lobby_dict(lobby) if lobby is not None else {},
        "player": player,
        "source": "unity-local-client" if stored else "continuuuum-review",
    }
    if stored:
        payload["localClient"] = stored
    return payload


def _vote_config_dict(r) -> dict:
    return {
        "sessionId": r["session_id"],
        "demographics": _json_obj(r["demographics_json"]),
        "certified": _json_obj(r["certified_json"]),
        "lastBallotId": r["last_ballot_id"] or "",
        "lastKind": r["last_kind"] or "",
        "lastRunId": r["last_run_id"] or "",
        "updatedUtc": r["updated_utc"],
    }


def _get_vote_config(conn: sqlite3.Connection, session_id: str) -> dict:
    row = conn.execute(
        "SELECT * FROM game_session_vote_config WHERE session_id=?", (session_id,)
    ).fetchone()
    if row is None:
        return {
            "sessionId": session_id,
            "demographics": {},
            "certified": {},
            "lastBallotId": "",
            "lastKind": "",
            "lastRunId": "",
            "updatedUtc": None,
        }
    return _vote_config_dict(row)


def _merge_followon_spec(conn: sqlite3.Connection, session_id: str, spec: dict) -> dict:
    cfg = _get_vote_config(conn, session_id)
    merged = dict(spec or {})
    demo = merged.get("demographics") if isinstance(merged.get("demographics"), dict) else {}
    prior = cfg.get("demographics") or {}
    if not demo.get("slices") and prior.get("slices"):
        merged["demographics"] = prior
    certified = cfg.get("certified") or {}
    if certified:
        merged["priorCertified"] = certified
    if cfg.get("lastRunId"):
        gates = list(merged.get("causalityGates") or [])
        leaf = f"vote.{session_id}.{cfg['lastRunId']}"
        if not any(g.get("requiredEventId") == leaf for g in gates if isinstance(g, dict)):
            gates.append({"requiredEventId": leaf, "fromPriorVote": True})
        merged["causalityGates"] = gates
    return merged


def _store_vote_config(conn: sqlite3.Connection, session_id: str, **fields) -> dict:
    now = _now()
    existing = conn.execute(
        "SELECT * FROM game_session_vote_config WHERE session_id=?", (session_id,)
    ).fetchone()
    demo = fields.get("demographics")
    certified = fields.get("certified")
    if existing is None:
        conn.execute(
            """INSERT INTO game_session_vote_config (
                session_id, demographics_json, certified_json, last_ballot_id, last_kind, last_run_id, updated_utc
            ) VALUES (?,?,?,?,?,?,?)""",
            (
                session_id,
                json.dumps(demo if isinstance(demo, dict) else {}),
                json.dumps(certified if isinstance(certified, dict) else {}),
                fields.get("lastBallotId") or "",
                fields.get("lastKind") or "",
                fields.get("lastRunId") or "",
                now,
            ),
        )
    else:
        prior = _vote_config_dict(existing)
        bag = dict(prior["certified"] or {})
        if isinstance(certified, dict):
            bag.update(certified)
        conn.execute(
            """UPDATE game_session_vote_config SET demographics_json=?, certified_json=?, last_ballot_id=?,
               last_kind=?, last_run_id=?, updated_utc=? WHERE session_id=?""",
            (
                json.dumps(demo if isinstance(demo, dict) else prior["demographics"]),
                json.dumps(bag),
                fields.get("lastBallotId") if "lastBallotId" in fields else prior["lastBallotId"],
                fields.get("lastKind") if "lastKind" in fields else prior["lastKind"],
                fields.get("lastRunId") if "lastRunId" in fields else prior["lastRunId"],
                now,
                session_id,
            ),
        )
    return _get_vote_config(conn, session_id)


def _ballot_dict(r, gov_mix=None) -> dict:
    spec = _json_obj(r["spec_json"])
    kind = _normalize_ballot_kind(r["kind"])
    mix = gov_mix if gov_mix is not None else _json_obj(r["gov_mix_json"] if "gov_mix_json" in r.keys() else "{}")
    demographics = spec.get("demographics") if isinstance(spec.get("demographics"), dict) else {}
    if "demographics_json" in r.keys() and r["demographics_json"]:
        col = _json_obj(r["demographics_json"])
        if col.get("slices") and not demographics.get("slices"):
            demographics = col
    options = spec.get("options") if isinstance(spec.get("options"), list) else []
    if not options:
        options = _default_ballot_options(kind)
    options = _fold_option_assignments(kind, r["name"], options)
    method, seats = _normalize_tally_method(spec.get("tallyMethod") or spec.get("method"), kind, spec.get("seats"))
    list_label = {"Measure": "measures", "Candidate": "candidates"}.get(kind, "questions")
    return {
        "name": r["name"],
        "kind": kind,
        "role": BALLOT_KIND_ROLES.get(kind, "jurisdiction"),
        "title": r["title"] or "Ballot",
        "prompt": r["prompt"] or "",
        "label": "Ballot",
        "listLabel": list_label,
        "tallyMethod": method,
        "seats": seats,
        "options": options,
        "demographics": demographics,
        "govMix": mix,
        "errors": ballot_gov_errors(kind, mix, method),
        "spec": {**spec, "options": options, "demographics": demographics, "tallyMethod": method, "seats": seats},
    }


def _run_debug(conn: sqlite3.Connection, run_id: str) -> dict:
    casts = conn.execute("SELECT * FROM vote_casts WHERE run_id=?", (run_id,)).fetchall()
    by_player = {}
    by_slice = {}
    actors = []
    total = 0
    for c in casts:
        if c["spoiled"]:
            continue
        total += 1
        actor = c["actor_id"] or ""
        opt = c["option_id"] or ""
        slice_id = c["demographic_slice_id"] or ""
        actors.append(
            {
                "actorId": actor,
                "optionId": opt,
                "ranking": _ranking_from_cast(c),
                "demographicSliceId": slice_id,
            }
        )
        by_player.setdefault(actor, {})
        by_player[actor][opt] = by_player[actor].get(opt, 0) + 1
        by_slice.setdefault(slice_id or "(none)", {"count": 0, "options": {}})
        by_slice[slice_id or "(none)"]["count"] += 1
        by_slice[slice_id or "(none)"]["options"][opt] = by_slice[slice_id or "(none)"]["options"].get(opt, 0) + 1
    demo_pct = []
    for slice_id, info in sorted(by_slice.items()):
        share = (info["count"] / total) if total else 0
        demo_pct.append(
            {
                "demographicSliceId": slice_id,
                "count": info["count"],
                "percent": round(share * 100, 2),
                "options": info["options"],
            }
        )
    return {
        "votesPerPlayer": [{"playerId": k, "votes": v} for k, v in sorted(by_player.items())],
        "votesPerDemographic": demo_pct,
        "actorVotes": actors,
        "castCount": total,
    }


def _voting_place_dict(r) -> dict:
    return {
        "id": r["id"],
        "name": r["name"],
        "lobbyId": r["lobby_id"] or "",
        "propertiesJson": _json_obj(r["properties_json"]),
        "createdUtc": r["created_utc"],
        "updatedUtc": r["updated_utc"],
    }


def _props_json(value) -> str:
    if isinstance(value, dict):
        return json.dumps(value)
    if isinstance(value, str) and value.strip():
        try:
            json.loads(value)
            return value
        except json.JSONDecodeError:
            return "{}"
    return "{}"


def _slug(text: str) -> str:
    raw = "".join(ch.lower() if ch.isalnum() else "-" for ch in (text or "").strip())
    while "--" in raw:
        raw = raw.replace("--", "-")
    raw = raw.strip("-")
    return raw or uuid.uuid4().hex[:10]


def _config_dict(r) -> dict:
    props = r["properties_json"] if "properties_json" in r.keys() else "{}"
    try:
        parsed = json.loads(props or "{}")
    except json.JSONDecodeError:
        parsed = {}
    return {
        "id": r["id"],
        "name": r["name"],
        "lobbyTypeId": r["lobby_type_id"] or "",
        "contentKind": r["content_kind"] or "game_mode",
        "contentId": r["content_id"] or "",
        "maxPlayers": r["max_players"] or 8,
        "minPlayersToStart": r["min_players_to_start"] or 1,
        "gameSize": r["game_size"] or r["max_players"] or 8,
        "mode": r["mode"] or "SinglePlayer",
        "requirePassword": bool(r["require_password"]),
        "allowSpectators": bool(r["allow_spectators"]),
        "maxSpectators": r["max_spectators"] or 4,
        "propertiesJson": parsed if isinstance(parsed, dict) else {},
        "createdUtc": r["created_utc"],
        "updatedUtc": r["updated_utc"],
    }


def _upsert_config(conn: sqlite3.Connection, body: dict, existing_id: str | None = None) -> dict:
    now = _now()
    name = (body.get("name") or "").strip()
    if not name:
        raise ValueError("name required")
    cid = existing_id or body.get("id") or _slug(name)
    game_size = int(body.get("gameSize") or body.get("maxPlayers") or 8)
    fields = (
        name,
        body.get("lobbyTypeId") or "",
        body.get("contentKind") or "game_mode",
        body.get("contentId") or "",
        int(body.get("maxPlayers") or game_size),
        int(body.get("minPlayersToStart") or 1),
        game_size,
        body.get("mode") or "SinglePlayer",
        1 if body.get("requirePassword") else 0,
        0 if body.get("allowSpectators") is False else 1,
        int(body.get("maxSpectators") or 4),
        _props_json(body.get("propertiesJson")),
        now,
    )
    row = conn.execute("SELECT * FROM game_lobby_configs WHERE id=?", (cid,)).fetchone()
    if row is None:
        conn.execute(
            """INSERT INTO game_lobby_configs (
                id, name, lobby_type_id, content_kind, content_id, max_players, min_players_to_start,
                game_size, mode, require_password, allow_spectators, max_spectators, properties_json,
                created_utc, updated_utc
            ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (cid, *fields[:12], now, now),
        )
    else:
        conn.execute(
            """UPDATE game_lobby_configs SET
                name=?, lobby_type_id=?, content_kind=?, content_id=?, max_players=?, min_players_to_start=?,
                game_size=?, mode=?, require_password=?, allow_spectators=?, max_spectators=?,
                properties_json=?, updated_utc=?
            WHERE id=?""",
            (*fields, cid),
        )
    return _config_dict(conn.execute("SELECT * FROM game_lobby_configs WHERE id=?", (cid,)).fetchone())


def _migrate_lobby_configs(conn: sqlite3.Connection) -> None:
    for lb in conn.execute("SELECT * FROM game_lobbies").fetchall():
        if "config_id" in lb.keys() and lb["config_id"]:
            continue
        kind = lb["content_kind"] or "game_mode"
        mode = lb["mode"] or "SinglePlayer"
        type_id = lb["lobby_type_id"] or ""
        content_id = lb["content_id"] or ""
        if type_id or content_id:
            key = f"{type_id}|{kind}|{content_id}|{mode}"
            cfg_name = type_id or content_id or lb["name"]
        else:
            key = lb["name"]
            cfg_name = lb["name"]
        cid = _slug(key)
        existing = conn.execute("SELECT id FROM game_lobby_configs WHERE id=?", (cid,)).fetchone()
        if existing is None:
            named = conn.execute("SELECT id FROM game_lobby_configs WHERE name=?", (cfg_name,)).fetchone()
            if named is None:
                conn.execute(
                    """INSERT INTO game_lobby_configs (
                        id, name, lobby_type_id, content_kind, content_id, max_players, min_players_to_start,
                        game_size, mode, require_password, allow_spectators, max_spectators, properties_json,
                        created_utc, updated_utc
                    ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                    (
                        cid,
                        cfg_name,
                        type_id,
                        kind,
                        content_id,
                        lb["max_players"] or 8,
                        lb["min_players_to_start"] or 1,
                        lb["game_size"] or lb["max_players"] or 8,
                        mode,
                        lb["require_password"] or 0,
                        0 if not lb["allow_spectators"] else 1,
                        lb["max_spectators"] or 4,
                        lb["properties_json"] or "{}",
                        lb["created_utc"] or _now(),
                        _now(),
                    ),
                )
            else:
                cid = named["id"]
        conn.execute("UPDATE game_lobbies SET config_id=? WHERE name=?", (cid, lb["name"]))


def _spawn_instance(conn: sqlite3.Connection, config_id: str, display_name: str | None = None) -> dict:
    cfg = conn.execute("SELECT * FROM game_lobby_configs WHERE id=?", (config_id,)).fetchone()
    if cfg is None:
        raise KeyError("config not found")
    slug = _slug(cfg["name"] or config_id)
    name = f"{slug}-{uuid.uuid4().hex[:8]}"
    while conn.execute("SELECT name FROM game_lobbies WHERE name=?", (name,)).fetchone():
        name = f"{slug}-{uuid.uuid4().hex[:8]}"
    try:
        props_obj = json.loads(cfg["properties_json"] or "{}")
    except json.JSONDecodeError:
        props_obj = {}
    body = {
        "displayName": display_name or cfg["name"],
        "lobbyTypeId": cfg["lobby_type_id"] or "",
        "contentKind": cfg["content_kind"] or "game_mode",
        "contentId": cfg["content_id"] or "",
        "maxPlayers": cfg["max_players"] or 8,
        "minPlayersToStart": cfg["min_players_to_start"] or 1,
        "gameSize": cfg["game_size"] or cfg["max_players"] or 8,
        "mode": cfg["mode"] or "SinglePlayer",
        "requirePassword": bool(cfg["require_password"]),
        "allowSpectators": bool(cfg["allow_spectators"]),
        "maxSpectators": cfg["max_spectators"] or 4,
        "propertiesJson": props_obj,
        "configId": config_id,
        "playerCount": 0,
    }
    lobby = _upsert_lobby(conn, name, body, active=1)
    lobby["sessions"] = []
    return lobby


def _upsert_lobby(conn: sqlite3.Connection, name: str, body: dict, active: int = 1) -> dict:
    now = _now()
    game_size = int(body.get("gameSize") or body.get("maxPlayers") or 8)
    max_players = int(body.get("maxPlayers") or game_size)
    props = body.get("propertiesJson")
    if isinstance(props, dict):
        props_s = json.dumps(props)
    elif isinstance(props, str) and props.strip():
        try:
            json.loads(props)
            props_s = props
        except json.JSONDecodeError:
            props_s = "{}"
    else:
        props_s = "{}"
    row = conn.execute("SELECT * FROM game_lobbies WHERE name=?", (name,)).fetchone()
    fields = dict(
        display_name=body.get("displayName") or name,
        lobby_type_id=body.get("lobbyTypeId") or "",
        content_kind=body.get("contentKind") or "game_mode",
        content_id=body.get("contentId") or "",
        advertise_address=body.get("advertiseAddress") or "",
        lobby_port=int(body.get("lobbyPort") or 7780),
        game_port=int(body.get("gamePort") or 7777),
        player_count=int(body.get("playerCount") or 0),
        max_players=max_players,
        min_players_to_start=int(body.get("minPlayersToStart") or 1),
        game_size=game_size,
        mode=body.get("mode") or "SinglePlayer",
        require_password=1 if body.get("requirePassword") else 0,
        allow_spectators=0 if body.get("allowSpectators") is False else 1,
        max_spectators=int(body.get("maxSpectators") or 4),
        properties_json=props_s,
        active=active,
        last_heartbeat_utc=now,
    )
    cfg_id = body.get("configId")
    if not cfg_id and row is not None and "config_id" in row.keys():
        cfg_id = row["config_id"] or ""
    fields["config_id"] = cfg_id or ""
    if row is None:
        conn.execute(
            """INSERT INTO game_lobbies (
                name, display_name, lobby_type_id, content_kind, content_id, advertise_address,
                lobby_port, game_port, player_count, max_players, min_players_to_start, game_size,
                mode, require_password, allow_spectators, max_spectators, properties_json,
                active, last_heartbeat_utc, created_utc, config_id
            ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (
                name,
                fields["display_name"],
                fields["lobby_type_id"],
                fields["content_kind"],
                fields["content_id"],
                fields["advertise_address"],
                fields["lobby_port"],
                fields["game_port"],
                fields["player_count"],
                fields["max_players"],
                fields["min_players_to_start"],
                fields["game_size"],
                fields["mode"],
                fields["require_password"],
                fields["allow_spectators"],
                fields["max_spectators"],
                fields["properties_json"],
                fields["active"],
                fields["last_heartbeat_utc"],
                now,
                fields["config_id"],
            ),
        )
    else:
        conn.execute(
            """UPDATE game_lobbies SET
                display_name=?, lobby_type_id=?, content_kind=?, content_id=?, advertise_address=?,
                lobby_port=?, game_port=?, player_count=?, max_players=?, min_players_to_start=?,
                game_size=?, mode=?, require_password=?, allow_spectators=?, max_spectators=?,
                properties_json=?, active=?, last_heartbeat_utc=?, config_id=?
            WHERE name=?""",
            (
                fields["display_name"] or row["display_name"],
                fields["lobby_type_id"] or row["lobby_type_id"],
                fields["content_kind"],
                fields["content_id"] or row["content_id"],
                fields["advertise_address"] or row["advertise_address"],
                fields["lobby_port"],
                fields["game_port"],
                fields["player_count"],
                fields["max_players"],
                fields["min_players_to_start"],
                fields["game_size"],
                fields["mode"],
                fields["require_password"] if "requirePassword" in body else row["require_password"],
                fields["allow_spectators"],
                fields["max_spectators"],
                fields["properties_json"] if props is not None else row["properties_json"],
                fields["active"],
                fields["last_heartbeat_utc"],
                fields["config_id"] or (row["config_id"] if "config_id" in row.keys() else ""),
                name,
            ),
        )
    return _lobby_dict(conn.execute("SELECT * FROM game_lobbies WHERE name=?", (name,)).fetchone())


def _filter_sessions(conn: sqlite3.Connection):
    q = (request.args.get("q") or "").strip().lower()
    lobby = request.args.get("lobby") or ""
    live = request.args.get("live")
    parent_id = request.args.get("parentId")
    content_kind = request.args.get("contentKind") or ""
    config_id = request.args.get("configId") or ""
    sql = """
        SELECT s.* FROM game_sessions s
        LEFT JOIN game_lobbies l ON l.name = s.lobby_session_name
        WHERE 1=1
    """
    args: list = []
    if lobby:
        sql += " AND s.lobby_session_name=?"
        args.append(lobby)
    if config_id:
        sql += " AND l.config_id=?"
        args.append(config_id)
    if live in ("1", "true", "True"):
        sql += " AND s.live=1"
    elif live in ("0", "false", "False"):
        sql += " AND (s.live=0 OR s.live IS NULL)"
    if parent_id:
        sql += " AND s.parent_id=?"
        args.append(parent_id)
    if content_kind:
        sql += " AND l.content_kind=?"
        args.append(content_kind)
    if q:
        sql += " AND (lower(s.id) LIKE ? OR lower(ifnull(s.display_name,'')) LIKE ? OR lower(s.lobby_session_name) LIKE ?)"
        like = f"%{q}%"
        args.extend([like, like, like])
    sql += " ORDER BY s.pecking_order ASC, s.created_utc ASC"
    return conn.execute(sql, args).fetchall()


def _descendants(conn: sqlite3.Connection, sid: str) -> list[str]:
    ids = [sid]
    seen = {sid}
    queue = [sid]
    while queue:
        cur = queue.pop(0)
        for r in conn.execute("SELECT id FROM game_sessions WHERE parent_id=?", (cur,)).fetchall():
            if r["id"] not in seen:
                seen.add(r["id"])
                ids.append(r["id"])
                queue.append(r["id"])
    return ids


def _delete_session_votes(conn: sqlite3.Connection, sid: str) -> None:
    conn.execute(
        "DELETE FROM vote_casts WHERE run_id IN (SELECT run_id FROM vote_runs WHERE game_session_id=?)",
        (sid,),
    )
    conn.execute("DELETE FROM vote_runs WHERE game_session_id=?", (sid,))
    conn.execute("DELETE FROM game_session_players WHERE session_id=?", (sid,))
    conn.execute("DELETE FROM game_session_vote_config WHERE session_id=?", (sid,))
    conn.execute("DELETE FROM game_sessions WHERE id=?", (sid,))


def register_vote_routes(app, get_conn: GetConn) -> None:
    @app.route("/votes")
    @app.route("/votes/<path:subpath>")
    def serve_votes(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_VOTES, subpath)
        return send_from_directory(STATIC_VOTES, "index.html")

    @app.route("/game-lobbies")
    @app.route("/game-lobbies/<path:subpath>")
    def serve_game_lobbies(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_LOBBIES, subpath)
        return send_from_directory(STATIC_LOBBIES, "index.html")

    @app.route("/voting-places")
    @app.route("/voting-places/<path:subpath>")
    def serve_voting_places(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_VOTING_PLACES, subpath)
        return send_from_directory(STATIC_VOTING_PLACES, "index.html")

    @app.route("/players")
    @app.route("/players/<path:subpath>")
    def serve_players(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_PLAYERS, subpath)
        return send_from_directory(STATIC_PLAYERS, "index.html")

    @app.route("/api/game-lobby-configs", methods=["GET", "POST"])
    def game_lobby_configs():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            rows = conn.execute("SELECT * FROM game_lobby_configs ORDER BY name").fetchall()
            return jsonify([_config_dict(r) for r in rows])
        body = request.get_json(silent=True) or {}
        props = body.get("propertiesJson")
        if isinstance(props, str) and props.strip():
            try:
                json.loads(props)
            except json.JSONDecodeError:
                return jsonify({"error": "propertiesJson must be object JSON"}), 400
        try:
            cfg = _upsert_config(conn, body)
        except ValueError as exc:
            return jsonify({"error": str(exc)}), 400
        conn.commit()
        return jsonify(cfg), 201

    @app.route("/api/game-lobby-configs/<cid>", methods=["GET", "PUT"])
    def game_lobby_config_one(cid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_lobby_configs WHERE id=?", (cid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        if request.method == "GET":
            return jsonify(_config_dict(row))
        body = request.get_json(silent=True) or {}
        props = body.get("propertiesJson")
        if isinstance(props, str) and props.strip():
            try:
                json.loads(props)
            except json.JSONDecodeError:
                return jsonify({"error": "propertiesJson must be object JSON"}), 400
        merged = {**_config_dict(row), **body}
        if "propertiesJson" not in body:
            merged["propertiesJson"] = _config_dict(row)["propertiesJson"]
        try:
            cfg = _upsert_config(conn, merged, existing_id=cid)
        except ValueError as exc:
            return jsonify({"error": str(exc)}), 400
        conn.commit()
        return jsonify(cfg)

    @app.route("/api/game-lobbies", methods=["GET", "POST"])
    def game_lobbies():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            config_id = request.args.get("configId") or ""
            if config_id:
                lobbies = conn.execute(
                    "SELECT * FROM game_lobbies WHERE config_id=? ORDER BY name", (config_id,)
                ).fetchall()
            else:
                lobbies = conn.execute("SELECT * FROM game_lobbies ORDER BY name").fetchall()
            out = []
            for lb in lobbies:
                d = _lobby_dict(lb)
                sess = conn.execute(
                    "SELECT * FROM game_sessions WHERE lobby_session_name=? ORDER BY pecking_order ASC, created_utc ASC",
                    (lb["name"],),
                ).fetchall()
                d["sessions"] = [_session_dict_full(conn, s) for s in sess]
                out.append(d)
            return jsonify(out)
        body = request.get_json(silent=True) or {}
        config_id = body.get("configId") or ""
        name = body.get("name") or body.get("lobbySessionName")
        is_heartbeat = "sessions" in body
        if config_id and not name and not is_heartbeat:
            try:
                lobby = _spawn_instance(conn, config_id, body.get("displayName"))
            except KeyError:
                return jsonify({"error": "config not found"}), 404
            conn.commit()
            return jsonify(lobby), 201
        if not name:
            return jsonify({"error": "name required"}), 400
        if config_id:
            body = {**body, "configId": config_id}
        lobby = _upsert_lobby(conn, name, body, active=1)
        listed = body.get("sessions") or []
        now = _now()
        live_ids = []
        for s in listed:
            sid = s.get("id") or uuid.uuid4().hex
            live_ids.append(sid)
            existing = conn.execute("SELECT id FROM game_sessions WHERE id=?", (sid,)).fetchone()
            if existing is None:
                conn.execute(
                    """INSERT INTO game_sessions (
                        id, lobby_session_name, display_name, created_utc, created_narrative_time,
                        active, source, live, last_heartbeat_utc, parent_id, pecking_order
                    ) VALUES (?,?,?,?,?,?,?,?,?,?,?)""",
                    (
                        sid,
                        name,
                        s.get("displayName") or "Session",
                        now,
                        float(s.get("createdNarrativeTime") or 0),
                        1 if s.get("active") else 0,
                        "unity",
                        1,
                        now,
                        s.get("parentId") or None,
                        int(s.get("peckingOrder") or 20),
                    ),
                )
            else:
                conn.execute(
                    """UPDATE game_sessions SET display_name=?, active=?, live=1, source='unity',
                       last_heartbeat_utc=?, parent_id=?, pecking_order=? WHERE id=?""",
                    (
                        s.get("displayName") or "Session",
                        1 if s.get("active") else 0,
                        now,
                        s.get("parentId") or None,
                        int(s.get("peckingOrder") or 20),
                        sid,
                    ),
                )
            _upsert_players(conn, sid, s.get("players") or [])
        top_players = body.get("players") or []
        if top_players:
            target = next((i for i in live_ids), None)
            for s in listed:
                if s.get("active") and s.get("id"):
                    target = s.get("id")
                    break
            if target:
                _upsert_players(conn, target, top_players)
        if is_heartbeat:
            if live_ids:
                placeholders = ",".join("?" * len(live_ids))
                conn.execute(
                    f"UPDATE game_sessions SET live=0 WHERE lobby_session_name=? AND id NOT IN ({placeholders})",
                    [name, *live_ids],
                )
            else:
                conn.execute("UPDATE game_sessions SET live=0 WHERE lobby_session_name=?", (name,))
        conn.commit()
        sess = conn.execute(
            "SELECT * FROM game_sessions WHERE lobby_session_name=?", (name,)
        ).fetchall()
        lobby["sessions"] = [_session_dict_full(conn, s) for s in sess]
        return jsonify(lobby)

    @app.route("/api/game-lobbies/<name>", methods=["GET"])
    def game_lobby_one(name):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_lobbies WHERE name=?", (name,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        d = _lobby_dict(row)
        sess = conn.execute(
            "SELECT * FROM game_sessions WHERE lobby_session_name=? ORDER BY pecking_order ASC",
            (name,),
        ).fetchall()
        d["sessions"] = [_session_dict_full(conn, s) for s in sess]
        return jsonify(d)

    @app.route("/api/game-lobbies/<name>/prefab", methods=["PUT"])
    def game_lobby_prefab(name):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_lobbies WHERE name=?", (name,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        body = request.get_json(silent=True) or {}
        props = body.get("propertiesJson")
        if isinstance(props, str) and props.strip():
            try:
                json.loads(props)
            except json.JSONDecodeError:
                return jsonify({"error": "propertiesJson must be object JSON"}), 400
        merged = {**_lobby_dict(row), **body}
        if "propertiesJson" not in body:
            merged["propertiesJson"] = _lobby_dict(row)["propertiesJson"]
        lobby = _upsert_lobby(conn, name, merged, active=row["active"])
        conn.commit()
        return jsonify(lobby)

    @app.route("/api/game-lobbies/<name>/close", methods=["POST"])
    def game_lobby_close(name):
        conn = get_conn()
        ensure_vote_tables(conn)
        conn.execute("UPDATE game_lobbies SET active=0 WHERE name=?", (name,))
        conn.execute("UPDATE game_sessions SET live=0, active=0 WHERE lobby_session_name=?", (name,))
        conn.commit()
        return jsonify({"ok": True, "name": name})

    @app.route("/api/game-sessions/graph", methods=["GET"])
    def game_sessions_graph():
        conn = get_conn()
        ensure_vote_tables(conn)
        rows = _filter_sessions(conn)
        by_id = {r["id"]: r for r in rows}
        all_rows = conn.execute("SELECT * FROM game_sessions").fetchall()
        all_by = {r["id"]: r for r in all_rows}
        roots = [r for r in rows if not r["parent_id"] or r["parent_id"] not in by_id]
        try:
            limit = min(200, max(1, int(request.args.get("limit") or 50)))
            offset = max(0, int(request.args.get("offset") or 0))
        except ValueError:
            limit, offset = 50, 0
        page_roots = roots[offset : offset + limit]
        included = set()
        queue = [r["id"] for r in page_roots]
        while queue:
            cur = queue.pop(0)
            if cur in included:
                continue
            included.add(cur)
            for r in all_rows:
                if r["parent_id"] == cur and r["id"] not in included:
                    queue.append(r["id"])
        nodes = [_session_dict(all_by[i]) for i in included if i in all_by]
        links = []
        for n in nodes:
            pid = n.get("parentId")
            if pid and pid in included:
                links.append({"source": pid, "target": n["id"]})
        return jsonify({"nodes": nodes, "links": links, "total": len(roots), "offset": offset, "limit": limit})

    @app.route("/api/game-sessions", methods=["GET", "POST"])
    def game_sessions():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            rows = _filter_sessions(conn)
            try:
                limit = min(200, max(1, int(request.args.get("limit") or 50)))
                offset = max(0, int(request.args.get("offset") or 0))
            except ValueError:
                limit, offset = 50, 0
            page = rows[offset : offset + limit]
            return jsonify(
                {
                    "items": [_session_dict(r) for r in page],
                    "total": len(rows),
                    "offset": offset,
                    "limit": limit,
                }
            )
        body = request.get_json(silent=True) or {}
        sid = body.get("id") or uuid.uuid4().hex
        lobby = (body.get("lobbySessionName") or "").strip()
        config_id = body.get("configId") or ""
        if not lobby:
            if config_id:
                try:
                    spawned = _spawn_instance(conn, config_id, body.get("lobbyDisplayName"))
                except KeyError:
                    return jsonify({"error": "config not found"}), 404
                lobby = spawned["name"]
            else:
                lobby = "Lobby-" + uuid.uuid4().hex[:8]
        name = body.get("displayName") or "Session"
        parent_id = body.get("parentId") or None
        if parent_id:
            parent = conn.execute("SELECT * FROM game_sessions WHERE id=?", (parent_id,)).fetchone()
            if parent is None or parent["lobby_session_name"] != lobby:
                return jsonify({"error": "parentId must be a session in the same lobby"}), 400
        existing_lobby = conn.execute("SELECT * FROM game_lobbies WHERE name=?", (lobby,)).fetchone()
        if existing_lobby is None:
            if config_id:
                body = {**body, "configId": config_id}
            lobby_row = _upsert_lobby(conn, lobby, body, active=1)
        else:
            conn.execute("UPDATE game_lobbies SET active=1 WHERE name=?", (lobby,))
            lobby_row = _lobby_dict(existing_lobby)
        if "peckingOrder" in body and body.get("peckingOrder") is not None:
            pecking = int(body.get("peckingOrder"))
        else:
            if parent_id:
                mx = conn.execute(
                    "SELECT MAX(pecking_order) AS m FROM game_sessions WHERE parent_id=?",
                    (parent_id,),
                ).fetchone()
            else:
                mx = conn.execute(
                    "SELECT MAX(pecking_order) AS m FROM game_sessions WHERE lobby_session_name=? AND (parent_id IS NULL OR parent_id='')",
                    (lobby,),
                ).fetchone()
            pecking = int(mx["m"] if mx is not None and mx["m"] is not None else -1) + 1
        conn.execute(
            """INSERT INTO game_sessions (
                id, lobby_session_name, display_name, created_utc, created_narrative_time,
                active, source, live, last_heartbeat_utc, parent_id, pecking_order
            ) VALUES (?,?,?,?,?,?,?,?,?,?,?)""",
            (
                sid,
                lobby,
                name,
                _now(),
                float(body.get("createdNarrativeTime") or 0),
                0,
                body.get("source") or "web",
                0,
                None,
                parent_id,
                pecking,
            ),
        )
        conn.commit()
        return (
            jsonify(
                {
                    "id": sid,
                    "lobbySessionName": lobby,
                    "displayName": name,
                    "parentId": parent_id,
                    "peckingOrder": pecking,
                    "lobby": {
                        "name": lobby_row["name"],
                        "active": lobby_row["active"],
                        "gameSize": lobby_row["gameSize"],
                        "mode": lobby_row["mode"],
                        "propertiesJson": lobby_row["propertiesJson"],
                    },
                }
            ),
            201,
        )

    @app.route("/api/game-sessions/<sid>/switch", methods=["POST"])
    def game_session_switch(sid):
        conn = get_conn()
        ensure_vote_tables(conn)
        conn.execute("UPDATE game_sessions SET active=0")
        conn.execute("UPDATE game_sessions SET active=1 WHERE id=?", (sid,))
        conn.commit()
        return jsonify({"ok": True, "id": sid})

    @app.route("/api/game-sessions/<sid>/close", methods=["POST"])
    def game_session_close(sid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_sessions WHERE id=?", (sid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        body = request.get_json(silent=True) or {}
        mode = (body.get("mode") or "adopt").lower()
        if mode == "umbrella":
            for did in reversed(_descendants(conn, sid)):
                _delete_session_votes(conn, did)
        else:
            parent = row["parent_id"]
            conn.execute("UPDATE game_sessions SET parent_id=? WHERE parent_id=?", (parent, sid))
            _delete_session_votes(conn, sid)
        conn.commit()
        return jsonify({"ok": True, "cleanup": True, "mode": mode})

    @app.route("/api/game-sessions/<sid>/save-local", methods=["POST"])
    def game_session_save_local(sid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_sessions WHERE id=?", (sid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        body = request.get_json(silent=True) or {}
        player_id = (body.get("playerId") or "").strip()
        player_row = None
        if player_id:
            player_row = conn.execute(
                "SELECT * FROM game_session_players WHERE session_id=? AND player_id=?",
                (sid, player_id),
            ).fetchone()
            local = body.get("localClient") if isinstance(body.get("localClient"), dict) else None
            if local is not None:
                _upsert_players(
                    conn,
                    sid,
                    [{"playerId": player_id, "displayName": body.get("displayName"), "actorId": body.get("actorId"), "localClient": local}],
                )
                conn.commit()
                player_row = conn.execute(
                    "SELECT * FROM game_session_players WHERE session_id=? AND player_id=?",
                    (sid, player_id),
                ).fetchone()
        payload = _local_client_payload(conn, row, player_row)
        payload["localSave"] = True
        payload["unityPath"] = f"game-sessions/{row['lobby_session_name']}/{row['id']}.json"
        return jsonify(payload)

    @app.route("/api/game-sessions/<sid>/players", methods=["GET", "POST"])
    def game_session_players(sid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_sessions WHERE id=?", (sid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            listed = body.get("players") if isinstance(body.get("players"), list) else [body]
            _upsert_players(conn, sid, listed)
            conn.commit()
        return jsonify(_session_players(conn, sid))

    @app.route("/api/game-sessions/<sid>/players/<player_id>/local-client", methods=["GET"])
    def game_session_player_local_client(sid, player_id):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM game_sessions WHERE id=?", (sid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        player_row = conn.execute(
            "SELECT * FROM game_session_players WHERE session_id=? AND player_id=?",
            (sid, player_id),
        ).fetchone()
        if player_row is None:
            return jsonify({"error": "player not found"}), 404
        return jsonify(_local_client_payload(conn, row, player_row))

    @app.route("/api/game-sessions/<sid>/vote-config", methods=["GET", "PUT"])
    def game_session_vote_config(sid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT id FROM game_sessions WHERE id=?", (sid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        if request.method == "PUT":
            body = request.get_json(silent=True) or {}
            cfg = _store_vote_config(
                conn,
                sid,
                demographics=body.get("demographics") if isinstance(body.get("demographics"), dict) else None,
                certified=body.get("certified") if isinstance(body.get("certified"), dict) else None,
                lastBallotId=body.get("lastBallotId"),
                lastKind=body.get("lastKind"),
                lastRunId=body.get("lastRunId"),
            )
            conn.commit()
            return jsonify(cfg)
        return jsonify(_get_vote_config(conn, sid))

    @app.route("/api/votes/ballots", methods=["GET", "POST"])
    def vote_ballots():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            rows = conn.execute("SELECT * FROM vote_ballots ORDER BY name").fetchall()
            return jsonify([_ballot_dict(r) for r in rows])
        body = request.get_json(silent=True) or {}
        name = body.get("name") or body.get("ballotId")
        if not name:
            return jsonify({"error": "name required"}), 400
        kind = _normalize_ballot_kind(body.get("kind"))
        now = _now()
        spec = body.get("spec") if isinstance(body.get("spec"), dict) else {}
        options = body.get("options") if isinstance(body.get("options"), list) else spec.get("options")
        if not isinstance(options, list) or not options:
            options = _default_ballot_options(kind)
        options = _fold_option_assignments(kind, name, options)
        demographics = _normalize_demographics(
            body.get("demographics") if isinstance(body.get("demographics"), dict) else spec.get("demographics") or {}
        )
        gov_mix = body.get("govMix") if isinstance(body.get("govMix"), dict) else spec.get("govMix") or {}
        method, seats = _normalize_tally_method(
            body.get("tallyMethod") or spec.get("tallyMethod"), kind, body.get("seats") if "seats" in body else spec.get("seats")
        )
        spec = {
            **spec,
            "options": options,
            "demographics": demographics,
            "govMix": gov_mix,
            "tallyMethod": method,
            "seats": seats,
        }
        errors = ballot_gov_errors(kind, gov_mix, method)
        existing = conn.execute("SELECT name FROM vote_ballots WHERE name=?", (name,)).fetchone()
        if existing is None:
            conn.execute(
                """INSERT INTO vote_ballots (
                    name, kind, title, prompt, spec_json, demographics_json, gov_mix_json, created_utc, updated_utc
                ) VALUES (?,?,?,?,?,?,?,?,?)""",
                (
                    name,
                    kind,
                    body.get("title") or "Ballot",
                    body.get("prompt") or "",
                    json.dumps(spec),
                    json.dumps(demographics),
                    json.dumps(gov_mix),
                    now,
                    now,
                ),
            )
        else:
            conn.execute(
                """UPDATE vote_ballots SET kind=?, title=?, prompt=?, spec_json=?, demographics_json=?,
                   gov_mix_json=?, updated_utc=? WHERE name=?""",
                (
                    kind,
                    body.get("title") or "Ballot",
                    body.get("prompt") or "",
                    json.dumps(spec),
                    json.dumps(demographics),
                    json.dumps(gov_mix),
                    now,
                    name,
                ),
            )
        conn.commit()
        row = conn.execute("SELECT * FROM vote_ballots WHERE name=?", (name,)).fetchone()
        out = _ballot_dict(row)
        out["errors"] = errors
        return jsonify(out), 201 if existing is None else 200

    @app.route("/api/votes/ballots/<name>", methods=["GET", "DELETE"])
    def vote_ballot_one(name):
        conn = get_conn()
        ensure_vote_tables(conn)
        r = conn.execute("SELECT * FROM vote_ballots WHERE name=?", (name,)).fetchone()
        if r is None:
            return jsonify({"error": "not found"}), 404
        if request.method == "DELETE":
            conn.execute("DELETE FROM vote_ballots WHERE name=?", (name,))
            conn.commit()
            return jsonify({"ok": True, "removed": name})
        return jsonify(_ballot_dict(r))

    @app.route("/api/votes/ballots/<name>/build", methods=["POST"])
    def vote_ballot_build(name):
        conn = get_conn()
        ensure_vote_tables(conn)
        ballot = conn.execute("SELECT * FROM vote_ballots WHERE name=?", (name,)).fetchone()
        if ballot is None:
            return jsonify({"error": "ballot not found"}), 404
        body = request.get_json(silent=True) or {}
        gs = body.get("gameSessionId") or ""
        sess = conn.execute("SELECT * FROM game_sessions WHERE id=?", (gs,)).fetchone() if gs else None
        if sess is None:
            return jsonify({"error": "session not found"}), 404
        parsed = _ballot_dict(ballot)
        followon = _merge_followon_spec(conn, gs, parsed["spec"])
        errors = list(parsed.get("errors") or [])
        errors.extend(
            ballot_gov_errors(
                parsed["kind"],
                followon.get("govMix") or parsed.get("govMix"),
                followon.get("tallyMethod") or parsed.get("tallyMethod"),
            )
        )
        run_id = uuid.uuid4().hex
        leaf = f"vote.{gs}.{run_id}"
        conn.execute(
            "INSERT INTO vote_runs (run_id, game_session_id, ballot_id, causality_leaf_id, certified, tally_json, tally_hash) VALUES (?,?,?,?,0,'{}',0)",
            (run_id, gs, name, leaf),
        )
        _store_vote_config(
            conn,
            gs,
            demographics=followon.get("demographics") if isinstance(followon.get("demographics"), dict) else None,
            lastBallotId=name,
            lastKind=parsed["kind"],
            lastRunId=run_id,
        )
        conn.commit()
        return jsonify(
            {
                "runId": run_id,
                "gameSessionId": gs,
                "ballotId": name,
                "kind": parsed["kind"],
                "role": parsed["role"],
                "causalityLeafId": leaf,
                "spec": followon,
                "errors": errors,
                "voteConfig": _get_vote_config(conn, gs),
            }
        ), 201

    @app.route("/api/votes/runs", methods=["GET", "POST"])
    def vote_runs():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            rows = conn.execute(
                "SELECT run_id, game_session_id, ballot_id, causality_leaf_id, certified, tally_json, tally_hash FROM vote_runs"
            ).fetchall()
            return jsonify(
                [
                    {
                        "runId": r["run_id"],
                        "gameSessionId": r["game_session_id"],
                        "ballotId": r["ballot_id"],
                        "ballotKind": _ballot_kind_for(conn, r["ballot_id"]),
                        "causalityLeafId": r["causality_leaf_id"],
                        "certified": bool(r["certified"]),
                        "tally": json.loads(r["tally_json"] or "{}"),
                        "tallyHash": r["tally_hash"],
                        **_run_debug(conn, r["run_id"]),
                    }
                    for r in rows
                ]
            )
        body = request.get_json(silent=True) or {}
        run_id = body.get("runId") or uuid.uuid4().hex
        gs = body.get("gameSessionId") or ""
        ballot = body.get("ballotId") or "ballot"
        leaf = body.get("causalityLeafId") or f"vote.{gs}.{run_id}"
        conn.execute(
            "INSERT INTO vote_runs (run_id, game_session_id, ballot_id, causality_leaf_id, certified, tally_json, tally_hash) VALUES (?,?,?,?,0,'{}',0)",
            (run_id, gs, ballot, leaf),
        )
        conn.commit()
        return jsonify({"runId": run_id, "gameSessionId": gs, "causalityLeafId": leaf}), 201

    @app.route("/api/votes/runs/<run_id>/cast", methods=["POST"])
    def vote_cast(run_id):
        conn = get_conn()
        ensure_vote_tables(conn)
        body = request.get_json(silent=True) or {}
        cid = uuid.uuid4().hex
        ranking = _clean_ranking(body.get("ranking"))
        option_id = body.get("optionId") or (ranking[0] if ranking else "")
        if option_id and not ranking:
            ranking = [option_id]
        conn.execute(
            """INSERT INTO vote_casts (
                id, run_id, actor_id, option_id, demographic_slice_id, causality_leaf_id, spoiled, ranking_json
            ) VALUES (?,?,?,?,?,?,?,?)""",
            (
                cid,
                run_id,
                body.get("actorId") or "",
                option_id,
                body.get("demographicSliceId") or "",
                body.get("causalityLeafId") or f"vote.{run_id}.{cid}",
                1 if body.get("spoiled") else 0,
                json.dumps(ranking),
            ),
        )
        conn.commit()
        return jsonify({"id": cid, "runId": run_id, "optionId": option_id, "ranking": ranking}), 201

    @app.route("/api/votes/runs/<run_id>/certify", methods=["POST"])
    def vote_certify(run_id):
        conn = get_conn()
        ensure_vote_tables(conn)
        run = conn.execute("SELECT * FROM vote_runs WHERE run_id=?", (run_id,)).fetchone()
        if run is None:
            return jsonify({"error": "not found"}), 404
        casts = conn.execute("SELECT * FROM vote_casts WHERE run_id=? AND spoiled=0", (run_id,)).fetchall()
        rankings = [_ranking_from_cast(c) for c in casts]
        parsed = None
        method, seats = "plurality", 1
        candidates = []
        if run["ballot_id"]:
            ballot = conn.execute("SELECT * FROM vote_ballots WHERE name=?", (run["ballot_id"],)).fetchone()
            if ballot is not None:
                parsed = _ballot_dict(ballot)
                method = parsed.get("tallyMethod") or "plurality"
                seats = parsed.get("seats") or 1
                candidates = [o.get("optionId") for o in (parsed.get("options") or []) if o.get("optionId")]
        detail = ranked_tally(rankings, method, seats, candidates)
        first = detail.get("firstPreferences") or {}
        winners = detail.get("winners") or []
        h = _tally_hash(first, winners)
        conn.execute(
            "UPDATE vote_runs SET certified=1, tally_json=?, tally_hash=? WHERE run_id=?",
            (json.dumps(detail), h, run_id),
        )
        winner = winners[0] if winners else None
        certified = {}
        demographics = {}
        kind = ""
        if parsed is not None:
            kind = parsed["kind"]
            demographics = parsed.get("demographics") or {}
            win_set = set(winners)
            for opt in parsed.get("options") or []:
                if opt.get("optionId") in win_set:
                    for assign in opt.get("win") or []:
                        if isinstance(assign, dict) and assign.get("propertyName"):
                            certified[assign["propertyName"]] = assign.get("propertyValue") or ""
        _store_vote_config(
            conn,
            run["game_session_id"],
            demographics=demographics or None,
            certified=certified or None,
            lastBallotId=run["ballot_id"],
            lastKind=kind,
            lastRunId=run_id,
        )
        conn.commit()
        debug = _run_debug(conn, run_id)
        return jsonify(
            {
                "runId": run_id,
                "certified": True,
                "tally": first,
                "tallyDetail": detail,
                "tallyHash": h,
                "winner": winner,
                "winners": winners,
                "followOn": _get_vote_config(conn, run["game_session_id"]),
                **debug,
            }
        )

    @app.route("/api/votes/runs/<run_id>/recount", methods=["POST"])
    def vote_recount(run_id):
        conn = get_conn()
        ensure_vote_tables(conn)
        src = conn.execute("SELECT * FROM vote_runs WHERE run_id=?", (run_id,)).fetchone()
        if src is None:
            return jsonify({"error": "not found"}), 404
        new_id = uuid.uuid4().hex
        leaf = (src["causality_leaf_id"] or "") + ".recount"
        conn.execute(
            "INSERT INTO vote_runs (run_id, game_session_id, ballot_id, causality_leaf_id, certified, tally_json, tally_hash) VALUES (?,?,?,?,0,'{}',0)",
            (new_id, src["game_session_id"], src["ballot_id"], leaf),
        )
        for c in conn.execute("SELECT * FROM vote_casts WHERE run_id=?", (run_id,)).fetchall():
            ranking = c["ranking_json"] if "ranking_json" in c.keys() and c["ranking_json"] else json.dumps(_ranking_from_cast(c))
            conn.execute(
                """INSERT INTO vote_casts (
                    id, run_id, actor_id, option_id, demographic_slice_id, causality_leaf_id, spoiled, ranking_json
                ) VALUES (?,?,?,?,?,?,?,?)""",
                (
                    uuid.uuid4().hex,
                    new_id,
                    c["actor_id"],
                    c["option_id"],
                    c["demographic_slice_id"],
                    c["causality_leaf_id"],
                    c["spoiled"],
                    ranking,
                ),
            )
        conn.commit()
        return jsonify({"runId": new_id, "lemma": "recount", "from": run_id})

    @app.route("/api/votes/runs/<run_id>/results", methods=["GET"])
    @app.route("/api/votes/results", methods=["GET"])
    def vote_results(run_id=None):
        conn = get_conn()
        ensure_vote_tables(conn)
        rid = run_id or request.args.get("runId")
        if not rid:
            rows = conn.execute("SELECT run_id, tally_json, tally_hash, certified, game_session_id FROM vote_runs").fetchall()
            return jsonify(
                [
                    {
                        "runId": r["run_id"],
                        "gameSessionId": r["game_session_id"],
                        "tally": json.loads(r["tally_json"] or "{}"),
                        "tallyHash": r["tally_hash"],
                        "certified": bool(r["certified"]),
                        **_run_debug(conn, r["run_id"]),
                    }
                    for r in rows
                ]
            )
        r = conn.execute(
            "SELECT run_id, tally_json, tally_hash, certified, game_session_id FROM vote_runs WHERE run_id=?",
            (rid,),
        ).fetchone()
        if r is None:
            return jsonify({"error": "not found"}), 404
        return jsonify(
            {
                "runId": r["run_id"],
                "gameSessionId": r["game_session_id"],
                "tally": json.loads(r["tally_json"] or "{}"),
                "tallyHash": r["tally_hash"],
                "certified": bool(r["certified"]),
                **_run_debug(conn, r["run_id"]),
            }
        )

    @app.route("/api/voting-places", methods=["GET", "POST"])
    def voting_places():
        conn = get_conn()
        ensure_vote_tables(conn)
        if request.method == "GET":
            lobby_id = request.args.get("lobbyId") or ""
            if lobby_id:
                rows = conn.execute(
                    "SELECT * FROM voting_places WHERE lobby_id=? ORDER BY name", (lobby_id,)
                ).fetchall()
            else:
                rows = conn.execute("SELECT * FROM voting_places ORDER BY name").fetchall()
            return jsonify([_voting_place_dict(r) for r in rows])
        body = request.get_json(silent=True) or {}
        name = (body.get("name") or "").strip()
        if not name:
            return jsonify({"error": "name required"}), 400
        now = _now()
        vid = body.get("id") or _slug(name) + "-" + uuid.uuid4().hex[:6]
        conn.execute(
            """INSERT INTO voting_places (id, name, lobby_id, properties_json, created_utc, updated_utc)
               VALUES (?,?,?,?,?,?)""",
            (vid, name, body.get("lobbyId") or "", json.dumps(_json_obj(body.get("propertiesJson"))), now, now),
        )
        conn.commit()
        row = conn.execute("SELECT * FROM voting_places WHERE id=?", (vid,)).fetchone()
        return jsonify(_voting_place_dict(row)), 201

    @app.route("/api/voting-places/<vid>", methods=["GET", "PUT"])
    def voting_place_one(vid):
        conn = get_conn()
        ensure_vote_tables(conn)
        row = conn.execute("SELECT * FROM voting_places WHERE id=?", (vid,)).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        if request.method == "GET":
            return jsonify(_voting_place_dict(row))
        body = request.get_json(silent=True) or {}
        name = (body.get("name") or row["name"]).strip()
        lobby_id = body.get("lobbyId") if "lobbyId" in body else row["lobby_id"]
        props = body.get("propertiesJson") if "propertiesJson" in body else _json_obj(row["properties_json"])
        conn.execute(
            "UPDATE voting_places SET name=?, lobby_id=?, properties_json=?, updated_utc=? WHERE id=?",
            (name, lobby_id or "", json.dumps(_json_obj(props)), _now(), vid),
        )
        conn.commit()
        return jsonify(_voting_place_dict(conn.execute("SELECT * FROM voting_places WHERE id=?", (vid,)).fetchone()))
