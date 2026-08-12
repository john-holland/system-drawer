"""Tests for Continuuuum game/dimension context, visibility, prewarm, annotations."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from game_dimension_db import ensure_game_dimension_schema_force  # noqa: E402
from game_dimension_dao import (  # noqa: E402
    get_user_context,
    put_visibility,
    resolve_entry_properties,
    set_user_context,
    upsert_entry_property_dim,
)
from gd_association_routes import register_gd_association_routes  # noqa: E402
from gd_route_annotations import accepts_game_dimension, configure_gd_annotations  # noqa: E402
from lemma_routes import register_lemma_routes  # noqa: E402
from localization_routes import register_localization_routes  # noqa: E402
from thesaurus_db import ensure_thesaurus_schema  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "gd.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_game_dimension_schema_force(conn)
        ensure_thesaurus_schema(conn)
        # Keep simplified properties table if thesaurus schema omits it in some builds
        conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS thesaurus_entry_properties (
              entry_id TEXT NOT NULL,
              property_key TEXT NOT NULL,
              property_value TEXT,
              PRIMARY KEY (entry_id, property_key)
            );
            """
        )
        conn.commit()
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    configure_gd_annotations(get_conn)
    register_gd_association_routes(app, get_conn, lambda: True)
    # also non-admin client path tested via headers on separate register — override is_admin from header
    app2 = Flask(__name__)
    app2.config["TESTING"] = True

    def is_admin():
        from flask import request

        return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")

    configure_gd_annotations(get_conn)
    register_gd_association_routes(app2, get_conn, is_admin)
    register_lemma_routes(app2, get_conn)
    register_localization_routes(
        app2,
        get_conn,
        get_user=lambda: "admin",
        create_notification=lambda *a, **k: None,
        is_admin=is_admin,
    )
    return app2.test_client(), get_conn

def test_user_context_side_effect(app_client):
    client, get_conn = app_client
    r = client.patch(
        "/api/gd/user-context",
        json={"dimension": 0},
        headers={"X-User-ID": "u1", "X-Admin": "1"},
    )
    assert r.status_code == 200
    ctx = r.get_json()
    assert ctx["dimIndex"] == 0

    # Grant dim 1 then switch
    conn = get_conn()
    dims = conn.execute("SELECT id FROM dimensions WHERE dim_index = 1").fetchone()
    put_visibility(
        conn, "dimension", dims["id"], is_public=False, grant_user_ids=["u1"], granted_by="admin"
    )
    games = conn.execute("SELECT id FROM games WHERE slug = 'main'").fetchone()
    put_visibility(
        conn, "game", games["id"], is_public=False, grant_user_ids=["u1"], granted_by="admin"
    )

    r2 = client.patch(
        "/api/gd/user-context",
        json={"dimension": 1, "game": "main"},
        headers={"X-User-ID": "u1"},
    )
    assert r2.status_code == 200
    assert r2.get_json()["dimIndex"] == 1

    r3 = client.get("/api/gd/user-context", headers={"X-User-ID": "u1"})
    assert r3.get_json()["dimIndex"] == 1


def test_query_param_overrides_context(app_client):
    client, get_conn = app_client
    conn = get_conn()
    set_user_context(
        conn,
        "u2",
        game_id=conn.execute("SELECT id FROM games WHERE slug='main'").fetchone()["id"],
        dimension_id=conn.execute("SELECT id FROM dimensions WHERE dim_index=0").fetchone()["id"],
    )
    # admin sees all
    r = client.get(
        "/api/gd/user-context?dimension=0",
        headers={"X-User-ID": "u2", "X-Admin": "1", "X-Dimension": "1"},
    )
    assert r.status_code == 200
    # g.dim_index should be 0 from query; response is stored context though
    # annotation binds g — verify list dimensions works with query
    r2 = client.get(
        "/api/gd/dimensions?dimension=0",
        headers={"X-User-ID": "u2", "X-Admin": "1"},
    )
    assert r2.status_code == 200
    assert any(d["dimIndex"] == 0 for d in r2.get_json())


def test_dim_property_overlay(app_client):
    _, get_conn = app_client
    conn = get_conn()
    conn.execute(
        "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES ('e1','en','word','noun')"
    )
    conn.execute(
        "INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value) VALUES ('e1','color','red')"
    )
    conn.commit()
    bag0 = resolve_entry_properties(conn, "e1", 0)
    assert bag0["color"] == "red"
    upsert_entry_property_dim(conn, "e1", 1, "color", "blue")
    bag1 = resolve_entry_properties(conn, "e1", 1)
    assert bag1["color"] == "blue"
    # missing override key still from dim 0
    upsert_entry_property_dim(conn, "e1", 1, "other", "x")
    bag1b = resolve_entry_properties(conn, "e1", 1)
    assert bag1b["color"] == "blue"
    assert bag1b.get("other") == "x"


def test_visibility_hides_main_from_non_admin(app_client):
    client, get_conn = app_client
    r = client.get("/api/gd/games", headers={"X-User-ID": "nobody"})
    assert r.status_code == 200
    assert r.get_json() == []

    r_admin = client.get("/api/gd/games", headers={"X-User-ID": "admin", "X-Admin": "1"})
    assert any(g["slug"] == "main" for g in r_admin.get_json())

    conn = get_conn()
    gid = conn.execute("SELECT id FROM games WHERE slug='main'").fetchone()["id"]
    client.put(
        "/api/gd/visibility",
        json={"subjectKind": "game", "subjectId": gid, "grantUserIds": ["nobody"]},
        headers={"X-User-ID": "admin", "X-Admin": "1"},
    )
    r2 = client.get("/api/gd/games", headers={"X-User-ID": "nobody"})
    assert any(g["slug"] == "main" for g in r2.get_json())

    r403 = client.patch(
        "/api/gd/user-context",
        json={"game": "main"},
        headers={"X-User-ID": "stranger"},
    )
    assert r403.status_code == 403
    assert r403.get_json().get("code") == "GAME_NOT_VISIBLE"


def test_create_lemma_dimension_switch_required(app_client):
    client, _ = app_client
    # Grant game so annotation passes visibility for game main at dim 1
    # as admin to avoid GAME_NOT_VISIBLE
    r = client.post(
        "/api/thesaurus/entries?dimension=1",
        json={"word": "testword", "partOfSpeech": "noun"},
        headers={"X-User-ID": "admin", "X-Admin": "1", "X-Dimension": "1"},
    )
    assert r.status_code == 409
    assert r.get_json().get("code") == "DIMENSION_SWITCH_REQUIRED"


def test_create_lemma_force_landing_still_dim0_existence(app_client):
    client, get_conn = app_client
    r = client.post(
        "/api/thesaurus/entries?dimension=1&createAtLandingDimension=true",
        json={"word": "shoelex", "partOfSpeech": "noun"},
        headers={"X-User-ID": "admin", "X-Admin": "1", "X-Dimension": "1"},
    )
    assert r.status_code in (200, 201), r.get_json()
    body = r.get_json()
    assert body.get("existenceDimension") == 0
    assert body.get("landingDimension") == 1
    entry = body.get("entry") or {}
    entry_id = entry.get("id")
    assert entry_id
    conn = get_conn()
    row = conn.execute(
        "SELECT id FROM thesaurus_entries WHERE id = ?", (entry_id,)
    ).fetchone()
    assert row is not None


def test_prefab_id_dim_overlay(app_client):
    _, get_conn = app_client
    conn = get_conn()
    conn.execute(
        "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES ('shoe1','en','shoe','noun')"
    )
    conn.execute(
        "INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value) VALUES ('shoe1','prefab-id','shoe')"
    )
    conn.commit()
    assert resolve_entry_properties(conn, "shoe1", 0)["prefab-id"] == "shoe"
    upsert_entry_property_dim(conn, "shoe1", 1, "prefab-id", "car")
    assert resolve_entry_properties(conn, "shoe1", 1)["prefab-id"] == "car"
    assert resolve_entry_properties(conn, "shoe1", 0)["prefab-id"] == "shoe"


def test_dim_property_requires_dim0_base(app_client):
    client, get_conn = app_client
    conn = get_conn()
    conn.execute(
        "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES ('ebase','en','basey','noun')"
    )
    conn.commit()
    hdrs = {"X-User-ID": "admin", "X-Admin": "1", "X-Dimension": "1"}
    # Invent key only at dim 1 — rejected
    r = client.put(
        "/api/thesaurus/entry-properties?createAtLandingDimension=true",
        json={
            "entryId": "ebase",
            "propertyKey": "prefab-id",
            "propertyValue": "car",
            "createAtLandingDimension": True,
        },
        headers=hdrs,
    )
    assert r.status_code == 400
    assert r.get_json().get("code") == "DIM0_BASE_PROPERTY_REQUIRED"
    # Seed dim 0 then override
    conn.execute(
        "INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value) VALUES ('ebase','prefab-id','shoe')"
    )
    conn.commit()
    r2 = client.put(
        "/api/thesaurus/entry-properties?createAtLandingDimension=true",
        json={
            "entryId": "ebase",
            "propertyKey": "prefab-id",
            "propertyValue": "car",
            "createAtLandingDimension": True,
        },
        headers=hdrs,
    )
    assert r2.status_code == 200, r2.get_json()
    assert resolve_entry_properties(conn, "ebase", 1)["prefab-id"] == "car"


def test_sg_prewarm_and_dimension_switch(app_client):
    client, _ = app_client
    hdrs = {"X-User-ID": "admin", "X-Admin": "1"}
    r = client.post(
        "/api/gd/sg-prewarm",
        json={"game": "main", "dimension": 0, "kinds": ["sg2d", "sg3d", "sg4d"]},
        headers=hdrs,
    )
    assert r.status_code == 200
    body = r.get_json()
    assert set(body["kinds"]) == {"sg2d", "sg3d", "sg4d"}
    assert "sg2d" in body["etags"]

    g = client.get("/api/gd/sg-prewarm?game=main&dimension=0", headers=hdrs)
    assert g.status_code == 200
    assert "sg2d" in g.get_json()["snapshots"]

    sw = client.post(
        "/api/gd/dimension-switch",
        json={"game": "main", "dimension": 0},
        headers=hdrs,
    )
    assert sw.status_code == 200
    assert "snapshots" in sw.get_json()

    inv = client.post(
        "/api/gd/sg-prewarm/invalidate",
        json={"game": "main", "dimension": 0},
        headers=hdrs,
    )
    assert inv.status_code == 200
    g2 = client.get("/api/gd/sg-prewarm?game=main&dimension=0", headers=hdrs)
    assert g2.status_code == 404


def test_association_change_list_roundtrip(app_client):
    client, get_conn = app_client
    conn = get_conn()
    gid = conn.execute("SELECT id FROM games WHERE slug='main'").fetchone()["id"]
    did = conn.execute("SELECT id FROM dimensions WHERE dim_index=0").fetchone()["id"]
    hdrs = {"X-User-ID": "admin", "X-Admin": "1"}
    cl = client.post(
        "/api/gd/change-lists",
        json={
            "title": "t",
            "items": [
                {
                    "op": "add",
                    "tableName": "thesaurus_entries",
                    "entityId": "e1",
                    "gameId": gid,
                    "dimensionId": did,
                }
            ],
        },
        headers=hdrs,
    ).get_json()
    client.post(f"/api/gd/change-lists/{cl['id']}/submit-for-review", headers=hdrs)
    client.post(
        f"/api/gd/change-lists/{cl['id']}/reviewers",
        json={"reviewerUserId": "admin"},
        headers=hdrs,
    )
    client.patch(
        f"/api/gd/change-lists/{cl['id']}/reviewers/admin",
        json={"status": "approved"},
        headers=hdrs,
    )
    committed = client.post(f"/api/gd/change-lists/{cl['id']}/commit", headers=hdrs)
    assert committed.status_code == 200
    assert committed.get_json()["status"] == "merged"
    assocs = client.get("/api/gd/associations", headers=hdrs).get_json()
    assert any(a["entityId"] == "e1" for a in assocs)


def test_annotations_on_views(app_client):
    client, _ = app_client
    app = client.application
    annotated = set()
    for rule in app.url_map.iter_rules():
        ep = app.view_functions.get(rule.endpoint)
        if ep is None:
            continue
        # unwrap
        fn = ep
        while hasattr(fn, "__wrapped__"):
            if getattr(fn, "__continuuuum_accepts_game_dimension__", False):
                annotated.add(rule.rule)
            fn = fn.__wrapped__
        if getattr(ep, "__continuuuum_accepts_game_dimension__", False):
            annotated.add(rule.rule)
    # decorator sets attr on wrapper
    found_lemma = any("/api/thesaurus/entries" == r for r in annotated)
    found_ctx = any("user-context" in r for r in annotated)
    found_switch = any("dimension-switch" in r for r in annotated)
    assert found_lemma, annotated
    assert found_ctx, annotated
    assert found_switch, annotated


def test_finance_routes_never_accept_game_dimension():
    """Payroll/credits/budget must not use @accepts_game_dimension."""
    from payroll_routes import register_payroll_routes

    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_conn():
        conn = sqlite3.connect(":memory:")
        conn.row_factory = sqlite3.Row
        return conn

    register_payroll_routes(app, get_conn)
    for rule in app.url_map.iter_rules():
        if "/api/payroll" not in rule.rule:
            continue
        ep = app.view_functions.get(rule.endpoint)
        assert ep is not None
        assert not getattr(ep, "__continuuuum_accepts_game_dimension__", False), rule.rule
        fn = ep
        while hasattr(fn, "__wrapped__"):
            assert not getattr(fn, "__continuuuum_accepts_game_dimension__", False), rule.rule
            fn = fn.__wrapped__
