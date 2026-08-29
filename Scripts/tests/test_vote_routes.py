import json
import uuid
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuuuum_api"))

from continuuuum_api.server import app
from continuuuum_api.vote_routes import reconcile_demographic_shares

NAV = (
    Path(__file__).resolve().parents[1]
    / "continuuuum_api"
    / "static"
    / "shared"
    / "continuuuum-nav"
    / "continuuuum-nav.js"
)


def _client():
    return app.test_client()


def _lobby_name():
    return "lobby-" + uuid.uuid4().hex[:10]


def test_votes_page_and_game_session_runs():
    client = _client()
    r = client.get("/votes")
    assert r.status_code == 200
    assert b"Lobbies" in r.data
    assert b"Ballot" in r.data
    assert b"votes-header" in r.data
    assert b"Remove ballot" in r.data
    assert b"votes-pane" in r.data
    assert b"votes-lobby-filter" in r.data
    assert b"Kind, name, or property value" in r.data

    lobby = _lobby_name()
    r = client.post("/api/game-sessions", json={"displayName": "Vote A", "lobbySessionName": lobby})
    assert r.status_code == 201
    body = r.get_json()
    sid = body["id"]
    assert sid
    assert body["lobbySessionName"] == lobby
    assert body["lobby"]["name"] == lobby
    assert body["lobby"]["active"] is True

    r = client.post(f"/api/game-sessions/{sid}/switch")
    assert r.status_code == 200
    assert r.get_json()["ok"] is True

    r = client.post(f"/api/game-sessions/{sid}/save-local")
    assert r.status_code == 200
    assert r.get_json()["lemma"] == "save-server-to-local"

    r = client.post("/api/votes/runs", json={"gameSessionId": sid, "ballotId": "gov"})
    assert r.status_code == 201
    run_id = r.get_json()["runId"]
    assert r.get_json()["gameSessionId"] == sid

    r = client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "a1", "optionId": "alice"})
    assert r.status_code == 201
    r = client.post(f"/api/votes/runs/{run_id}/certify")
    assert r.status_code == 200
    assert r.get_json()["certified"] is True
    assert r.get_json()["tally"]["alice"] == 1

    r = client.post(f"/api/votes/runs/{run_id}/recount")
    assert r.status_code == 200
    assert r.get_json()["lemma"] == "recount"
    clone_id = r.get_json()["runId"]
    assert clone_id != run_id

    r = client.get(f"/api/votes/runs/{clone_id}/results")
    assert r.status_code == 200
    assert r.get_json()["gameSessionId"] == sid

    r = client.post(f"/api/game-sessions/{sid}/close")
    assert r.status_code == 200
    assert r.get_json()["cleanup"] is True
    listed = client.get("/api/game-sessions").get_json()["items"]
    assert all(s["id"] != sid for s in listed)


def test_create_session_upserts_lobby_and_mints_name():
    client = _client()
    r = client.post("/api/game-sessions", json={"displayName": "Minted"})
    assert r.status_code == 201
    body = r.get_json()
    assert body["lobbySessionName"].startswith("Lobby-")
    assert body["lobbySessionName"] != "Drawer 2"
    name = body["lobbySessionName"]
    r = client.get(f"/api/game-lobbies/{name}")
    assert r.status_code == 200
    assert r.get_json()["active"] is True
    assert r.get_json()["gameSize"] == 8


def test_nested_sessions_adopt_and_umbrella_close():
    client = _client()
    lobby = _lobby_name()
    a = client.post("/api/game-sessions", json={"displayName": "A", "lobbySessionName": lobby}).get_json()
    b = client.post(
        "/api/game-sessions",
        json={"displayName": "B", "lobbySessionName": lobby, "parentId": a["id"], "peckingOrder": 1},
    ).get_json()
    c = client.post(
        "/api/game-sessions",
        json={"displayName": "C", "lobbySessionName": lobby, "parentId": b["id"], "peckingOrder": 2},
    ).get_json()
    assert b["parentId"] == a["id"]
    assert c["parentId"] == b["id"]

    r = client.post(f"/api/game-sessions/{b['id']}/close", json={"mode": "adopt"})
    assert r.status_code == 200
    page = client.get(f"/api/game-sessions?lobby={lobby}").get_json()
    ids = {s["id"]: s for s in page["items"]}
    assert a["id"] in ids
    assert b["id"] not in ids
    assert c["id"] in ids
    assert ids[c["id"]]["parentId"] == a["id"]

    d = client.post(
        "/api/game-sessions",
        json={"displayName": "D", "lobbySessionName": lobby, "parentId": c["id"]},
    ).get_json()
    r = client.post(f"/api/game-sessions/{c['id']}/close", json={"mode": "umbrella"})
    assert r.status_code == 200
    page = client.get(f"/api/game-sessions?lobby={lobby}").get_json()
    leftover = {s["id"] for s in page["items"]}
    assert c["id"] not in leftover
    assert d["id"] not in leftover
    assert a["id"] in leftover


def test_named_ballot_persist_and_build():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby}).get_json()["id"]
    r = client.post(
        "/api/votes/ballots",
        json={"name": "mayor-q", "title": "Mayor", "prompt": "Yes?", "spec": {"options": [{"optionId": "yes"}]}},
    )
    assert r.status_code in (200, 201)
    r = client.get("/api/votes/ballots/mayor-q")
    assert r.status_code == 200
    assert r.get_json()["title"] == "Mayor"
    r = client.post("/api/votes/ballots/mayor-q/build", json={"gameSessionId": sid})
    assert r.status_code == 201
    run = r.get_json()
    assert run["ballotId"] == "mayor-q"
    assert run["gameSessionId"] == sid
    r = client.post(f"/api/game-sessions/{sid}/close")
    assert r.status_code == 200
    r = client.get("/api/votes/ballots/mayor-q")
    assert r.status_code == 200


def test_heartbeat_sets_live_and_prefab():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby, "displayName": "Live"}).get_json()["id"]
    r = client.post(
        "/api/game-lobbies",
        json={
            "name": lobby,
            "gameSize": 6,
            "mode": "ClassicLockstep",
            "minPlayersToStart": 2,
            "lobbyTypeId": "vanilla",
            "contentKind": "game_mode",
            "sessions": [{"id": sid, "displayName": "Live", "active": True, "parentId": None, "peckingOrder": 0}],
        },
    )
    assert r.status_code == 200
    body = r.get_json()
    assert body["gameSize"] == 6
    assert body["mode"] == "ClassicLockstep"
    live = [s for s in body["sessions"] if s["id"] == sid][0]
    assert live["live"] is True
    r = client.post("/api/game-lobbies", json={"name": lobby, "sessions": []})
    assert r.status_code == 200
    dead = [s for s in r.get_json()["sessions"] if s["id"] == sid][0]
    assert dead["live"] is False
    r = client.put(f"/api/game-lobbies/{lobby}/prefab", json={"gameSize": 12, "mode": "AuthoritativePeerToPeer", "propertiesJson": {"x": 1}})
    assert r.status_code == 200
    assert r.get_json()["gameSize"] == 12
    r = client.put(f"/api/game-lobbies/{lobby}/prefab", json={"propertiesJson": "not-json"})
    assert r.status_code == 400
    r = client.post(f"/api/game-lobbies/{lobby}/close")
    assert r.status_code == 200


def test_game_sessions_pagination_and_graph():
    client = _client()
    lobby = _lobby_name()
    a = client.post("/api/game-sessions", json={"displayName": "RootA", "lobbySessionName": lobby}).get_json()
    b = client.post(
        "/api/game-sessions",
        json={"displayName": "ChildB", "lobbySessionName": lobby, "parentId": a["id"]},
    ).get_json()
    r = client.get(f"/api/game-sessions?lobby={lobby}&limit=1&offset=0")
    assert r.status_code == 200
    page = r.get_json()
    assert page["limit"] == 1
    assert page["offset"] == 0
    assert page["total"] >= 2
    assert len(page["items"]) == 1
    r = client.get(f"/api/game-sessions?q=RootA&lobby={lobby}")
    assert any(s["id"] == a["id"] for s in r.get_json()["items"])
    r = client.get(f"/api/game-sessions/graph?lobby={lobby}&limit=50&offset=0")
    graph = r.get_json()
    assert any(n["id"] == a["id"] for n in graph["nodes"])
    assert any(n["id"] == b["id"] for n in graph["nodes"])
    assert any(l["source"] == a["id"] and l["target"] == b["id"] for l in graph["links"])


def test_game_lobbies_page_and_nav():
    client = _client()
    r = client.get("/game-lobbies")
    assert r.status_code == 200
    assert b"Game Lobbies" in r.data
    assert b"Configure" in r.data
    assert b"gl-header" in r.data
    src = NAV.read_text(encoding="utf-8")
    assert "path: '/game-lobbies'" in src
    assert "id: 'game-lobbies'" in src


def test_lobby_config_multi_instance_and_snapshot():
    client = _client()
    name = "cfg-" + uuid.uuid4().hex[:8]
    r = client.post(
        "/api/game-lobby-configs",
        json={
            "name": name,
            "lobbyTypeId": "mayor-dog",
            "contentKind": "mod",
            "contentId": "mayor-dog",
            "gameSize": 6,
            "mode": "ClassicLockstep",
            "minPlayersToStart": 2,
        },
    )
    assert r.status_code == 201
    cfg = r.get_json()
    cid = cfg["id"]
    assert cfg["gameSize"] == 6

    a = client.post("/api/game-lobbies", json={"configId": cid})
    b = client.post("/api/game-lobbies", json={"configId": cid})
    assert a.status_code == 201
    assert b.status_code == 201
    inst_a = a.get_json()
    inst_b = b.get_json()
    assert inst_a["name"] != inst_b["name"]
    assert inst_a["configId"] == cid
    assert inst_b["configId"] == cid
    assert inst_a["active"] is True and inst_b["active"] is True
    assert inst_a["gameSize"] == 6
    assert inst_a["mode"] == "ClassicLockstep"
    assert inst_a["sessions"] == []

    listed = client.get(f"/api/game-lobbies?configId={cid}").get_json()
    names = {row["name"] for row in listed}
    assert inst_a["name"] in names and inst_b["name"] in names

    r = client.put(
        f"/api/game-lobby-configs/{cid}",
        json={**cfg, "gameSize": 12, "mode": "AuthoritativePeerToPeer"},
    )
    assert r.status_code == 200
    assert r.get_json()["gameSize"] == 12
    still = client.get(f"/api/game-lobbies/{inst_a['name']}").get_json()
    assert still["gameSize"] == 6
    assert still["mode"] == "ClassicLockstep"

    fresh = client.post("/api/game-lobbies", json={"configId": cid}).get_json()
    assert fresh["gameSize"] == 12


def test_create_session_nested_under_instance():
    client = _client()
    name = "nest-" + uuid.uuid4().hex[:8]
    cid = client.post("/api/game-lobby-configs", json={"name": name, "gameSize": 4}).get_json()["id"]
    lobby = client.post("/api/game-lobbies", json={"configId": cid}).get_json()["name"]
    root = client.post(
        "/api/game-sessions",
        json={"lobbySessionName": lobby, "displayName": "Root", "peckingOrder": 0},
    ).get_json()
    child = client.post(
        "/api/game-sessions",
        json={
            "lobbySessionName": lobby,
            "displayName": "Quest",
            "parentId": root["id"],
            "peckingOrder": 1,
        },
    ).get_json()
    assert child["parentId"] == root["id"]
    after = client.get(f"/api/game-lobbies/{lobby}").get_json()
    assert after["gameSize"] == 4
    ids = {s["id"]: s for s in after["sessions"]}
    assert ids[child["id"]]["parentId"] == root["id"]
    assert ids[child["id"]]["peckingOrder"] == 1


def test_heartbeat_live_is_per_instance_not_per_config():
    client = _client()
    name = "hb-" + uuid.uuid4().hex[:8]
    cid = client.post("/api/game-lobby-configs", json={"name": name}).get_json()["id"]
    a = client.post("/api/game-lobbies", json={"configId": cid}).get_json()["name"]
    b = client.post("/api/game-lobbies", json={"configId": cid}).get_json()["name"]
    sa = client.post("/api/game-sessions", json={"lobbySessionName": a, "displayName": "A"}).get_json()["id"]
    sb = client.post("/api/game-sessions", json={"lobbySessionName": b, "displayName": "B"}).get_json()["id"]
    client.post(
        "/api/game-lobbies",
        json={"name": a, "configId": cid, "sessions": [{"id": sa, "displayName": "A", "active": True}]},
    )
    client.post(
        "/api/game-lobbies",
        json={"name": b, "configId": cid, "sessions": [{"id": sb, "displayName": "B", "active": True}]},
    )
    la = client.get(f"/api/game-lobbies/{a}").get_json()
    lb = client.get(f"/api/game-lobbies/{b}").get_json()
    assert [s for s in la["sessions"] if s["id"] == sa][0]["live"] is True
    assert [s for s in lb["sessions"] if s["id"] == sb][0]["live"] is True
    client.post("/api/game-lobbies", json={"name": a, "configId": cid, "sessions": []})
    la = client.get(f"/api/game-lobbies/{a}").get_json()
    lb = client.get(f"/api/game-lobbies/{b}").get_json()
    assert [s for s in la["sessions"] if s["id"] == sa][0]["live"] is False
    assert [s for s in lb["sessions"] if s["id"] == sb][0]["live"] is True


def test_ballot_kinds_fold_into_gov_and_error_elsewhere():
    client = _client()
    measure = client.post(
        "/api/votes/ballots",
        json={"name": "law-1", "kind": "Measure", "title": "Ballot", "govMix": {"republic01": 0.8}},
    ).get_json()
    assert measure["kind"] == "Measure"
    assert measure["role"] == "law"
    assert measure["label"] == "Ballot"
    assert measure["listLabel"] == "measures"
    assert {o["optionId"] for o in measure["options"]} == {"yes", "no"}
    assert measure["errors"] == []

    junta_elect = client.post(
        "/api/votes/ballots",
        json={"name": "elect-junta", "kind": "Candidate", "govMix": {"junta01": 1.0}},
    ).get_json()
    assert junta_elect["role"] == "electoral"
    assert junta_elect["errors"]
    assert "junta" in junta_elect["errors"][0]

    q = client.post(
        "/api/votes/ballots",
        json={"name": "juri-1", "kind": "Question", "govMix": {"republic01": 0.7}},
    ).get_json()
    assert q["role"] == "jurisdiction"
    assert q["errors"] == []


def test_followon_vote_config_and_session_debug():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby}).get_json()["id"]
    client.post(
        "/api/votes/ballots",
        json={
            "name": "follow-q",
            "kind": "Question",
            "demographics": {
                "slices": [
                    {"sliceId": "dem", "groupProperty": "party", "groupValue": "democrat", "share01": 0.6, "yesTilt01": 0.7},
                    {"sliceId": "rep", "groupProperty": "party", "groupValue": "republican", "share01": 0.4, "yesTilt01": 0.3},
                ]
            },
        },
    )
    built = client.post("/api/votes/ballots/follow-q/build", json={"gameSessionId": sid}).get_json()
    run_id = built["runId"]
    assert built["spec"]["demographics"]["slices"]
    client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "a1", "optionId": "yes", "demographicSliceId": "dem"})
    client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "a2", "optionId": "no", "demographicSliceId": "rep"})
    cert = client.post(f"/api/votes/runs/{run_id}/certify").get_json()
    assert cert["votesPerPlayer"]
    assert cert["votesPerDemographic"]
    assert cert["actorVotes"]
    assert cert["followOn"]["certified"].get("jurisdiction.follow-q") == "true"
    second = client.post(
        "/api/votes/ballots",
        json={"name": "follow-2", "kind": "Measure"},
    ).get_json()
    built2 = client.post("/api/votes/ballots/follow-2/build", json={"gameSessionId": sid}).get_json()
    assert built2["spec"]["demographics"]["slices"]
    assert built2["spec"]["priorCertified"].get("jurisdiction.follow-q") == "true"
    debug = client.get(f"/api/game-lobbies/{lobby}").get_json()
    sess = [s for s in debug["sessions"] if s["id"] == sid][0]
    assert sess["runs"]
    assert any(run.get("actorVotes") for run in sess["runs"])


def test_players_local_client_download_and_voting_places():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby, "displayName": "P"}).get_json()["id"]
    r = client.post(
        "/api/game-lobbies",
        json={
            "name": lobby,
            "sessions": [
                {
                    "id": sid,
                    "displayName": "P",
                    "active": True,
                    "players": [{"playerId": "p1", "displayName": "Ada", "actorId": "actor-ada"}],
                }
            ],
        },
    )
    assert r.status_code == 200
    players = client.get(f"/api/game-sessions/{sid}/players").get_json()
    assert players[0]["playerId"] == "p1"
    local = client.get(f"/api/game-sessions/{sid}/players/p1/local-client").get_json()
    assert local["lemma"] == "save-server-to-local"
    assert local["player"]["playerId"] == "p1"
    assert local["lobbySessionName"] == lobby
    saved = client.post(f"/api/game-sessions/{sid}/save-local", json={"playerId": "p1"}).get_json()
    assert saved["localSave"] is True
    assert saved["unityPath"].endswith(".json")
    place = client.post(
        "/api/voting-places",
        json={
            "name": "polls-" + lobby,
            "lobbyId": lobby,
            "propertiesJson": {
                "feederPolicy": "addressOrRandom",
                "boothLayout": "fourSectionDivided",
                "feederCount": 2,
            },
        },
    ).get_json()
    assert place["lobbyId"] == lobby
    assert place["propertiesJson"]["boothLayout"] == "fourSectionDivided"
    assert place["propertiesJson"]["feederPolicy"] == "addressOrRandom"
    listed = client.get(f"/api/voting-places?lobbyId={lobby}").get_json()
    assert any(p["id"] == place["id"] for p in listed)
    page = client.get("/voting-places")
    assert page.status_code == 200
    assert b"Voting places" in page.data
    assert b"addressOrRandom" in page.data
    assert b"fourSectionDivided" in page.data
    assert b"twoSectionBackToBack" in page.data
    assert b"feederPolicy" in page.data or b"Feeder policy" in page.data
    tbd = client.get("/players")
    assert tbd.status_code == 200
    assert b"To be determined" in tbd.data
    nav = NAV.read_text(encoding="utf-8")
    assert "path: '/voting-places'" in nav
    assert "path: '/players'" in nav
    gl_js = (
        Path(__file__).resolve().parents[1]
        / "continuuuum_api"
        / "static"
        / "game-lobbies"
        / "game-lobbies.js"
    ).read_text(encoding="utf-8")
    assert "ContinuuuumNav.mount" in gl_js
    votes_js = (
        Path(__file__).resolve().parents[1] / "continuuuum_api" / "static" / "votes" / "votes.js"
    ).read_text(encoding="utf-8")
    assert "ContinuuuumNav.mount" in votes_js
    assert "data-edit-lobby" in (
        Path(__file__).resolve().parents[1]
        / "continuuuum_api"
        / "static"
        / "shared"
        / "game-lobby-list"
        / "game-lobby-list.js"
    ).read_text(encoding="utf-8")


def test_irv_eliminates_last_and_transfers():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby}).get_json()["id"]
    client.post(
        "/api/votes/ballots",
        json={
            "name": "irv-mayor",
            "kind": "Candidate",
            "tallyMethod": "irv",
            "options": [
                {"optionId": "a", "displayName": "A"},
                {"optionId": "b", "displayName": "B"},
                {"optionId": "c", "displayName": "C"},
            ],
        },
    )
    built = client.post("/api/votes/ballots/irv-mayor/build", json={"gameSessionId": sid}).get_json()
    run_id = built["runId"]
    for _ in range(4):
        client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "a" + str(_), "ranking": ["a", "b"]})
    for _ in range(3):
        client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "b" + str(_), "ranking": ["b", "c"]})
    for _ in range(2):
        client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "c" + str(_), "ranking": ["c", "b"]})
    cert = client.post(f"/api/votes/runs/{run_id}/certify").get_json()
    assert cert["tally"]["a"] == 4
    assert cert["tallyDetail"]["method"] == "irv"
    assert cert["winners"] == ["b"]
    assert cert["winner"] == "b"
    assert any(r.get("eliminated") == "c" for r in cert["tallyDetail"]["rounds"])


def test_stv_two_seats_droop_surplus():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby}).get_json()["id"]
    client.post(
        "/api/votes/ballots",
        json={
            "name": "stv-council",
            "kind": "Candidate",
            "tallyMethod": "stv",
            "seats": 2,
            "options": [
                {"optionId": "a", "displayName": "A"},
                {"optionId": "b", "displayName": "B"},
                {"optionId": "c", "displayName": "C"},
            ],
        },
    )
    built = client.post("/api/votes/ballots/stv-council/build", json={"gameSessionId": sid}).get_json()
    run_id = built["runId"]
    for i in range(3):
        client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "aa" + str(i), "ranking": ["a", "b"]})
    client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "bb", "ranking": ["b", "c"]})
    client.post(f"/api/votes/runs/{run_id}/cast", json={"actorId": "cc", "ranking": ["c", "b"]})
    cert = client.post(f"/api/votes/runs/{run_id}/certify").get_json()
    assert cert["tallyDetail"]["method"] == "stv"
    assert cert["tallyDetail"]["seats"] == 2
    assert cert["tallyDetail"]["quota"] == 2
    assert set(cert["winners"]) == {"a", "b"}
    assert cert["followOn"]["certified"].get("governor") in ("a", "b")


def test_measure_rejects_irv_and_ranking_roundtrips():
    client = _client()
    lobby = _lobby_name()
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby}).get_json()["id"]
    bad = client.post(
        "/api/votes/ballots",
        json={"name": "law-irv", "kind": "Measure", "tallyMethod": "irv"},
    ).get_json()
    assert bad["errors"]
    assert "ranked choice" in bad["errors"][0]
    client.post(
        "/api/votes/ballots",
        json={
            "name": "rank-store",
            "kind": "Candidate",
            "tallyMethod": "irv",
            "options": [{"optionId": "x"}, {"optionId": "y"}],
        },
    )
    run_id = client.post("/api/votes/ballots/rank-store/build", json={"gameSessionId": sid}).get_json()["runId"]
    cast = client.post(
        f"/api/votes/runs/{run_id}/cast",
        json={"actorId": "p1", "ranking": ["y", "x"]},
    ).get_json()
    assert cast["optionId"] == "y"
    assert cast["ranking"] == ["y", "x"]
    recount = client.post(f"/api/votes/runs/{run_id}/recount").get_json()
    clone = recount["runId"]
    debug = client.get(f"/api/votes/runs/{clone}/results").get_json()
    actors = debug.get("actorVotes") or []
    assert actors[0]["ranking"] == ["y", "x"]
    assert actors[0]["optionId"] == "y"


def test_measure_run_exposes_ballot_kind():
    client = _client()
    lobby = "measure-hall-" + uuid.uuid4().hex[:6]
    sid = client.post("/api/game-sessions", json={"lobbySessionName": lobby, "displayName": "Town measure"}).get_json()["id"]
    client.post("/api/votes/ballots", json={"name": "park-bond", "kind": "Measure"})
    run_id = client.post("/api/votes/ballots/park-bond/build", json={"gameSessionId": sid}).get_json()["runId"]
    runs = client.get("/api/votes/runs").get_json()
    mine = next(r for r in runs if r["runId"] == run_id)
    assert mine["ballotKind"] == "Measure"
    assert mine["ballotId"] == "park-bond"
    listed = client.get("/api/game-lobbies").get_json()
    room = next(lb for lb in listed if lb["name"] == lobby)
    sess = next(s for s in room["sessions"] if s["id"] == sid)
    assert any(run.get("ballotKind") == "Measure" for run in sess["runs"])


def test_reconcile_demographic_shares_even_remainder():
    assert reconcile_demographic_shares([0.6, 0.3, 0.2], 0) == [0.6, 0.2, 0.2]
    assert reconcile_demographic_shares([0.33, 0.4, 0.4], 0) == [0.33, 0.33, 0.34]
    assert reconcile_demographic_shares([0.5], 0) == [1.0]
    assert reconcile_demographic_shares([0.4, 0.4, 0.4], None) == [0.33, 0.33, 0.34]


def test_remove_ballot_and_unbalanced_shares_normalize():
    client = _client()
    name = "rm-" + uuid.uuid4().hex[:8]
    r = client.post(
        "/api/votes/ballots",
        json={
            "name": name,
            "kind": "Question",
            "demographics": {
                "slices": [
                    {"sliceId": "a", "share01": 0.5},
                    {"sliceId": "b", "share01": 0.5},
                    {"sliceId": "c", "share01": 0.5},
                ]
            },
        },
    )
    assert r.status_code in (200, 201)
    shares = [s["share01"] for s in r.get_json()["demographics"]["slices"]]
    assert shares == [0.33, 0.33, 0.34]
    gone = client.delete(f"/api/votes/ballots/{name}")
    assert gone.status_code == 200
    assert gone.get_json()["removed"] == name
    assert client.get(f"/api/votes/ballots/{name}").status_code == 404
    assert client.delete(f"/api/votes/ballots/{name}").status_code == 404
