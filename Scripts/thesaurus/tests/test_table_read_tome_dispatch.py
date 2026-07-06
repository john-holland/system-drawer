"""Table read tome dispatch tests."""





from __future__ import annotations





import os


import sqlite3


import sys


import tempfile


from pathlib import Path


from unittest.mock import patch





import pytest





_scripts = Path(__file__).resolve().parents[2]


_api = _scripts / "continuuuum_api"


if str(_api) not in sys.path:


    sys.path.insert(0, str(_api))


if str(_scripts) not in sys.path:


    sys.path.insert(0, str(_scripts))





from continuuuum_api import server as srv


from continuuuum_api.cave_loader import build_routes_overview


from continuuuum_api.table_read_routes import ensure_table_read_tables








def _bootstrap_db(path: str) -> None:


    conn = sqlite3.connect(path)


    conn.executescript(


        """


        CREATE TABLE draft_episodes (


            id TEXT PRIMARY KEY, title TEXT, created_by TEXT, committed_at TEXT,


            created_at TEXT, updated_at TEXT, episode_id TEXT


        );


        INSERT INTO draft_episodes VALUES (


            'draft-tr', 'Table Read Draft', 'host-1', NULL, '2020-01-01', '2020-01-01', NULL


        );


        CREATE TABLE draft_episode_script (


            id TEXT PRIMARY KEY, draft_episode_id TEXT, script_text TEXT, language TEXT,


            created_at TEXT, updated_at TEXT


        );


        INSERT INTO draft_episode_script VALUES (


            'ds-1', 'draft-tr', 'ALICE\nHello.', 'en', '2020-01-01', '2020-01-01'


        );


        """


    )


    ensure_table_read_tables(conn)


    conn.commit()


    conn.close()








@pytest.fixture


def tome_client(monkeypatch):


    fd, path = tempfile.mkstemp(suffix=".db")


    os.close(fd)


    _bootstrap_db(path)


    monkeypatch.setenv("CONTINUUUUM_DB", path)


    srv._schema_initialized = True





    def mock_resaurce(route, payload):


        if route == "chat/room/ensure-for-table-read":


            return {"ok": True, "chat_room": {"id": "room_mock_1"}}


        if route == "chat/room/sync-table-read-members":


            return {"ok": True, "chat_room": {"id": "room_mock_1"}}


        if route == "chat/message/send":


            return {"ok": True, "message": {"id": "msg_1"}}


        return {"ok": False}





    with patch("continuuuum_api.table_read_chat.resaurce_route", side_effect=mock_resaurce), patch(


        "continuuuum_api.table_read_routes.resaurce_route", side_effect=mock_resaurce


    ), patch("continuuuum_api.table_read_routes.send_chat_message", return_value={"ok": True}):


        yield srv.app.test_client(), path


    try:


        os.unlink(path)


    except OSError:


        pass








def test_routes_overview_includes_table_read_tome_machines():


    overview = build_routes_overview()


    paths = {r["path"] for r in overview.get("tomes", []) if r.get("tomeId") == "table-read-tome"}


    assert "/api/tomes/table-read-tome/machines/sessionMachine/message" in paths


    assert "/api/tomes/table-read-tome/machines/chatEnsureMachine/message" in paths


    assert "/api/tomes/table-read-tome/machines/inviteMachine/message" in paths








def test_session_machine_open(tome_client):


    client, _ = tome_client


    r = client.post(


        "/api/table-read/sessions",


        json={"draftEpisodeId": "draft-tr"},


        headers={"X-User-ID": "host-1"},


    )


    sid = r.get_json()["id"]


    r = client.post(


        "/api/tomes/table-read-tome/machines/sessionMachine/message",


        json={"event": "SESSION_OPEN", "data": {"sessionId": sid, "displayName": "host-1"}},


        headers={"X-User-ID": "host-1"},


    )


    assert r.status_code == 200


    body = r.get_json()


    assert body.get("ok") is True


    snap = body.get("result") or {}


    assert snap.get("session", {}).get("chatRoomId") == "room_mock_1"








def test_invite_machine(tome_client):


    client, _ = tome_client


    r = client.post(


        "/api/table-read/sessions",


        json={"draftEpisodeId": "draft-tr"},


        headers={"X-User-ID": "host-1"},


    )


    sid = r.get_json()["id"]


    client.post(


        f"/api/tomes/table-read-tome/machines/sessionMachine/message",


        json={"event": "SESSION_OPEN", "data": {"sessionId": sid}},


        headers={"X-User-ID": "host-1"},


    )


    r = client.post(


        "/api/tomes/table-read-tome/machines/inviteMachine/message",


        json={"event": "INVITE_USER", "data": {"sessionId": sid, "userId": "reader-2"}},


        headers={"X-User-ID": "host-1"},


    )


    assert r.status_code == 200


    result = (r.get_json().get("result") or {})


    assert result.get("ok") is True


