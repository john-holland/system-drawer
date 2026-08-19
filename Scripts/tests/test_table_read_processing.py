"""Table-read processing: Previous/retreat, quotes, Whisper match, Save/Sync, admin owner."""

from __future__ import annotations

import io
import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask, request

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from table_read_routes import ensure_table_read_tables, register_table_read_routes  # noqa: E402
from asset_owner_routes import register_asset_owner_routes  # noqa: E402
from usc_whisper import UscUnavailable, set_transcribe_impl  # noqa: E402
from webcam_anim_routes import register_webcam_anim_routes  # noqa: E402


def _bootstrap(conn: sqlite3.Connection) -> None:
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
            'ds-1', 'draft-tr', 'ALICE\nHello.\n\nBOB\nHi.', 'en', '2020-01-01', '2020-01-01'
        );
        CREATE TABLE reviewer_comments (
            id TEXT PRIMARY KEY, reviewer_id TEXT, draft_episode_id TEXT,
            comment_text TEXT, comment_type TEXT, created_at TEXT,
            text_selection_start INTEGER, text_selection_end INTEGER, script_ref TEXT,
            review_cycle INTEGER DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS notifications (
            id TEXT PRIMARY KEY, user_id TEXT, type TEXT, draft_id TEXT,
            review_id TEXT, message TEXT, read_at TEXT, created_at TEXT
        );
        CREATE TABLE IF NOT EXISTS user_presence (
            user_id TEXT PRIMARY KEY, last_seen_at TEXT
        );
        INSERT INTO user_presence VALUES ('reader-2', '2020-01-01');
        """
    )
    ensure_table_read_tables(conn)
    conn.commit()


def _mock_resaurce(route, payload):
    if route == "chat/room/ensure-for-table-read":
        return {"ok": True, "chat_room": {"id": "room_test_1"}}
    if route == "chat/room/sync-table-read-members":
        return {"ok": True, "chat_room": {"id": "room_test_1", "participants": payload.get("participants", [])}}
    if route == "chat/message/send":
        return {"ok": True, "message": {"id": "msg_1"}}
    return {"ok": False}


@pytest.fixture
def proc_client(tmp_path, monkeypatch):
    db = tmp_path / "tr.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        return conn

    conn = get_conn()
    _bootstrap(conn)
    conn.close()

    monkeypatch.setattr("table_read_chat.resaurce_route", _mock_resaurce)
    monkeypatch.setattr("table_read_routes.resaurce_route", _mock_resaurce)
    monkeypatch.setattr("table_read_routes.send_chat_message", lambda *a, **k: {"ok": True})
    try:
        monkeypatch.setattr("continuuuum_api.table_read_chat.resaurce_route", _mock_resaurce)
        monkeypatch.setattr("continuuuum_api.table_read_routes.resaurce_route", _mock_resaurce)
        monkeypatch.setattr("continuuuum_api.table_read_routes.send_chat_message", lambda *a, **k: {"ok": True})
    except Exception:
        pass

    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_user():
        return request.headers.get("X-User-ID", "anonymous")

    def is_admin():
        return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")

    register_table_read_routes(app, get_conn, get_user, None, "http://127.0.0.1:9")
    register_asset_owner_routes(app, get_conn, get_user, is_admin, "http://127.0.0.1:9")
    register_webcam_anim_routes(app, get_conn)
    client = app.test_client()
    yield client, get_conn, app
    set_transcribe_impl(None)
    try:
        from table_read_processing import set_usc_download_impl, set_usc_upload_impl
        set_usc_download_impl(None)
        set_usc_upload_impl(None)
    except Exception:
        pass


def _create_session(client, user="host-1"):
    r = client.post(
        "/api/table-read/sessions",
        json={"draftEpisodeId": "draft-tr"},
        headers={"X-User-ID": user},
    )
    assert r.status_code == 201, r.get_data(as_text=True)
    return r.get_json()["id"]


def _hello_span(script: str) -> tuple[int, int]:
    start = script.index("Hello.")
    return start, start + len("Hello.")


def test_previous_restores_prior_turn(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    r = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"})
    snap = r.get_json()
    assert snap["currentTurn"]["turnIndex"] == 0
    assert len(snap["turns"]) >= 2

    r = client.post(f"/api/table-read/sessions/{sid}/advance", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    assert r.get_json()["currentTurn"]["turnIndex"] >= 1

    r = client.post(f"/api/table-read/sessions/{sid}/retreat", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    snap = r.get_json()
    assert snap["currentTurn"]["turnIndex"] == 0
    assert snap["currentTurn"]["status"] == "active"
    nxt = [t for t in snap["turns"] if t["turnIndex"] == 1][0]
    assert nxt["status"] == "pending"


def test_quote_add_403_unless_host(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    client.post(
        f"/api/table-read/sessions/{sid}/join",
        json={"displayName": "reader-2"},
        headers={"X-User-ID": "reader-2"},
    )
    r = client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": 0, "charEnd": 5},
        headers={"X-User-ID": "reader-2"},
    )
    assert r.status_code == 403

    r = client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "dialogActorId": "alice-1", "charStart": 6, "charEnd": 12},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201
    body = r.get_json()
    assert body["quotes"]
    assert body["quoteMap"]
    snap = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "reader-2"}).get_json()
    assert snap.get("quotes")
    assert snap["quotes"][0]["characterName"] == "ALICE"


def test_character_map_starts_without_dummy_quote_spans(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    snap = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()
    names = [c["characterName"] for c in snap.get("quoteMap") or []]
    assert "ALICE" in names
    assert "BOB" in names
    for entry in snap["quoteMap"]:
        assert entry["quotes"] == []

    r = client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "CAROL", "charStart": 0, "charEnd": 1},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201
    carol = next(c for c in r.get_json()["quoteMap"] if c["characterName"] == "CAROL")
    assert carol["quotes"] == []
    assert not any(
        q.get("charStart") == 0 and q.get("charEnd") == 1 for q in r.get_json()["quotes"]
    )

    r = client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "CAROL"},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201
    carol = next(c for c in r.get_json()["quoteMap"] if c["characterName"] == "CAROL")
    assert carol["quotes"] == []

    script = snap["scriptText"]
    start, end = _hello_span(script)
    r = client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    alice = next(c for c in r.get_json()["quoteMap"] if c["characterName"] == "ALICE")
    assert alice["quotes"]
    assert alice["quotes"][0]["start"] == start
    assert alice["quotes"][0]["end"] == end
    assert alice["quotes"][0]["end"] > alice["quotes"][0]["start"]


def test_processing_include_default_matches_transcript(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    script = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()["scriptText"]
    start, end = _hello_span(script)
    client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "dialogActorId": "alice-1", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}).get_json()
    seg_id = proc["segments"][0]["id"]

    set_transcribe_impl(lambda path, base: {"text": "Hello."})
    r = client.post(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/upload",
        data={"file": (io.BytesIO(b"RIFF"), "take.wav")},
        headers={"X-User-ID": "host-1"},
        content_type="multipart/form-data",
    )
    assert r.status_code == 200
    seg = r.get_json()["segments"][0]
    assert seg["include"] is True
    assert seg["matchOk"] is True

    set_transcribe_impl(lambda path, base: {"text": "unrelated noise"})
    r = client.post(
        f"/api/table-read/sessions/{sid}/processing/whisper",
        json={"segmentId": seg_id},
        headers={"X-User-ID": "host-1"},
    )
    assert r.get_json()["segments"][0]["include"] is False
    assert r.get_json()["segments"][0]["matchOk"] is False


def test_pause_fields_round_trip_and_composition_order(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    script = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()["scriptText"]
    start, end = _hello_span(script)
    client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}).get_json()
    seg_id = proc["segments"][0]["id"]
    r = client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={
            "include": True,
            "pauseBefore": True,
            "pauseBeforeSec": 1.5,
            "pauseAfter": True,
            "pauseAfterSec": 0.25,
            "insertPause": True,
            "insertPausePos": 0.4,
            "insertPauseSec": 0.1,
        },
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 200
    seg = r.get_json()["segment"]
    assert seg["pauseBefore"] is True
    assert seg["pauseBeforeSec"] == 1.5
    assert seg["insertPausePos"] == 0.4
    kinds = [c["kind"] for c in r.get_json()["composition"]]
    assert kinds[0] == "silence"
    assert "clip" in kinds
    assert kinds[-1] == "silence"


def test_save_partial_then_sync_label(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    script = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()["scriptText"]
    start, end = _hello_span(script)
    client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    r = client.post(f"/api/table-read/sessions/{sid}/save", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    body = r.get_json()
    assert body["savedAt"]
    assert body["saveLabel"] == "Sync"
    assert body.get("partial") is True

    proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}).get_json()
    assert proc["saveLabel"] == "Sync"
    assert proc["savedAt"]

    r2 = client.post(f"/api/table-read/sessions/{sid}/sync", headers={"X-User-ID": "host-1"})
    assert r2.status_code == 200
    assert r2.get_json()["saveLabel"] == "Sync"


def test_update_script_writes_new_storage_row(proc_client):
    client, get_conn, _ = proc_client
    sid = _create_session(client)
    client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"})
    conn = get_conn()
    before = conn.execute(
        "SELECT COUNT(*) AS c FROM table_read_script_storage WHERE session_id = ?", (sid,)
    ).fetchone()["c"]
    conn.execute(
        "UPDATE draft_episode_script SET script_text = 'ALICE\nHello there.\n\nBOB\nHi.' WHERE id = 'ds-1'"
    )
    conn.commit()
    conn.close()
    r = client.post(f"/api/table-read/sessions/{sid}/update-script", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    conn = get_conn()
    after = conn.execute(
        "SELECT COUNT(*) AS c FROM table_read_script_storage WHERE session_id = ?", (sid,)
    ).fetchone()["c"]
    conn.close()
    assert after == before + 1


def test_whisper_missing_usc_fails_segment(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    script = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()["scriptText"]
    start, end = _hello_span(script)
    client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    seg_id = client.get(
        f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}
    ).get_json()["segments"][0]["id"]

    def boom(path, base):
        raise UscUnavailable("USC media unreachable")

    set_transcribe_impl(boom)
    r = client.post(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/upload",
        data={"file": (io.BytesIO(b"RIFF"), "take.wav")},
        headers={"X-User-ID": "host-1"},
        content_type="multipart/form-data",
    )
    assert r.status_code == 200
    seg = r.get_json()["segments"][0]
    assert seg["status"] == "failed"
    src = Path(__file__).resolve().parents[1] / "continuuuum_api" / "usc_whisper.py"
    text = src.read_text(encoding="utf-8")
    assert "import whisper" not in text
    assert "openai_whisper" not in text
    proc_src = (Path(__file__).resolve().parents[1] / "continuuuum_api" / "table_read_processing.py").read_text(
        encoding="utf-8"
    )
    assert "import whisper" not in proc_src
    assert "openai_whisper" not in proc_src


def test_admin_reassign_writes_history_and_warehouse(proc_client):
    client, get_conn, _ = proc_client
    sid = _create_session(client)
    r = client.post(
        "/api/admin/asset-owners",
        json={"assetKind": "continuuuum", "assetId": sid, "toOwner": "new-host", "reason": "transfer"},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 403

    r = client.post(
        "/api/admin/asset-owners",
        json={"assetKind": "continuuuum", "assetId": sid, "toOwner": "new-host", "reason": "transfer"},
        headers={"X-User-ID": "admin-1", "X-Admin": "1"},
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    body = r.get_json()
    assert body["toOwner"] == "new-host"
    assert body["warehouseId"]
    conn = get_conn()
    hist = conn.execute("SELECT * FROM asset_owner_history").fetchone()
    assert hist["to_owner"] == "new-host"
    wh = conn.execute(
        "SELECT * FROM credits_warehouse_history WHERE event_kind = 'asset_owner_reassigned'"
    ).fetchone()
    assert wh is not None
    host = conn.execute("SELECT host_user_id FROM table_read_sessions WHERE id = ?", (sid,)).fetchone()
    assert host["host_user_id"] == "new-host"
    conn.close()


def test_no_suggest_endpoints_on_processing(proc_client):
    _, _, app = proc_client
    rules = [str(rule) for rule in app.url_map.iter_rules()]
    proc_rules = [r for r in rules if "processing" in r]
    assert proc_rules
    assert not any("suggest" in r or "change-request" in r for r in proc_rules)
    quote_rules = [r for r in rules if "/quotes" in r]
    assert not any("suggest" in r for r in quote_rules)


def _add_hello_segment(client, sid):
    script = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()["scriptText"]
    start, end = _hello_span(script)
    client.post(
        f"/api/table-read/sessions/{sid}/quotes",
        json={"characterName": "ALICE", "charStart": start, "charEnd": end},
        headers={"X-User-ID": "host-1"},
    )
    proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}).get_json()
    return proc["segments"][0]["id"]


def test_segment_video_anim_checkbox_and_props_round_trip(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    r = client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={
            "processVideoAnimation": True,
            "detectorProfileId": "human-mediapipe-v1",
            "videoLibraryDocId": "77",
            "animProps": {
                "webcamAnimKind": "dance",
                "subsection": "chorus",
                "timelineStartMs": 10,
                "timelineEndMs": 90,
                "granularity": "centisecond",
                "species": "",
            },
        },
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 200
    seg = r.get_json()["segment"]
    assert seg["processVideoAnimation"] is True
    assert seg["detectorProfileId"] == "human-mediapipe-v1"
    assert seg["videoLibraryDocId"] == "77"
    assert seg["animProps"]["webcamAnimKind"] == "dance"
    assert seg["animProps"]["timelineEndMs"] == 90
    assert r.get_json()["detectorProfiles"]
    assert any(p["id"] == "human-mediapipe-v1" for p in r.get_json()["detectorProfiles"])


def test_unchecked_segment_skips_webcam_queue(proc_client):
    client, get_conn, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={"include": True, "processVideoAnimation": False, "videoLibraryDocId": "77"},
        headers={"X-User-ID": "host-1"},
    )
    r = client.post(f"/api/table-read/sessions/{sid}/save", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    conn = get_conn()
    rows = conn.execute("SELECT * FROM webcam_anim_upload_queue").fetchall()
    conn.close()
    assert len(rows) == 0


def test_checked_without_usc_video_is_400(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={"include": True, "processVideoAnimation": True},
        headers={"X-User-ID": "host-1"},
    )
    r = client.post(f"/api/table-read/sessions/{sid}/save", headers={"X-User-ID": "host-1"})
    assert r.status_code == 400
    assert "videoLibraryDocId" in r.get_json()["error"]
    r2 = client.post(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/process-anim",
        json={"processVideoAnimation": True},
        headers={"X-User-ID": "host-1"},
    )
    assert r2.status_code == 400


def test_mocap_profile_without_species_is_400(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    models = client.get("/api/webcam-animations/models").get_json()
    profiles = models["detectorProfiles"]
    animal = next(p for p in profiles if p["id"] == "animal-mocap-v2")
    animal["defaultSpecies"] = ""
    client.put("/api/webcam-animations/models", json={"detectorProfiles": profiles})
    client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={
            "include": True,
            "processVideoAnimation": True,
            "detectorProfileId": "animal-mocap-v2",
            "videoLibraryDocId": "12",
            "animProps": {"species": ""},
        },
        headers={"X-User-ID": "host-1"},
    )
    r = client.post(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/process-anim",
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 400
    assert "species" in r.get_json()["error"]


def test_save_lists_processed_assets_and_dummy_hop(proc_client, tmp_path):
    from pose_detectors import set_hop_runner
    from table_read_processing import set_usc_download_impl, set_usc_upload_impl

    client, get_conn, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    pose = tmp_path / "pose.json"

    def download(doc_id, base, dest):
        target = Path(dest) if dest else tmp_path / f"{doc_id}.bin"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(b"fake-video")
        return str(target)

    def hop(_path, _payload):
        pose.write_text('{"frames":[]}', encoding="utf-8")
        return {"pose_track_path": str(pose)}

    set_usc_download_impl(download)
    set_usc_upload_impl(lambda path, dtype, base: "42")
    set_hop_runner("mediapipe_holistic@v1", hop)
    set_transcribe_impl(lambda path, base: {"text": "Hello."})
    try:
        client.patch(
            f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
            json={
                "include": True,
                "processVideoAnimation": True,
                "detectorProfileId": "human-mediapipe-v1",
                "videoLibraryDocId": "88",
            },
            headers={"X-User-ID": "host-1"},
        )
        client.post(
            f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/upload",
            data={"file": (io.BytesIO(b"RIFF"), "take.wav")},
            headers={"X-User-ID": "host-1"},
            content_type="multipart/form-data",
        )
        r = client.post(f"/api/table-read/sessions/{sid}/save", headers={"X-User-ID": "host-1"})
        assert r.status_code == 200, r.get_data(as_text=True)
        assets = r.get_json()["processedAssets"]
        kinds = {a["kind"] for a in assets}
        assert "script" in kinds
        assert "dialogue" in kinds
        video = next(a for a in assets if a["kind"] == "video")
        assert video["libraryDocId"] == "88"
        audio = next(a for a in assets if a["kind"] == "audio")
        assert audio["audioRef"] == "usc://42"
        assert audio["libraryDocId"] == "42"
        anim = next(a for a in assets if a["kind"] == "animation")
        assert anim["poseTrackPath"]
        assert anim["modelSpec"] == "mediapipe_holistic@v1"
        proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "reader-2"}).get_json()
        assert proc["processedAssets"]
        seg = next(s for s in proc["segments"] if s["id"] == seg_id)
        assert seg["animStatus"] == "done"
        assert seg["webcamRecordingId"]
        conn = get_conn()
        rec = conn.execute(
            "SELECT model_spec FROM webcam_anim_recordings WHERE id = ?",
            (seg["webcamRecordingId"],),
        ).fetchone()
        conn.close()
        assert rec["model_spec"] == "mediapipe_holistic@v1"
    finally:
        set_hop_runner("mediapipe_holistic@v1", None)
        set_usc_download_impl(None)
        set_usc_upload_impl(None)


def test_missing_usc_video_download_fails_segment_not_local_only(proc_client):
    from table_read_processing import set_usc_download_impl

    client, get_conn, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)

    def boom(doc_id, base, dest):
        raise RuntimeError("USC library unreachable")

    set_usc_download_impl(boom)
    try:
        client.patch(
            f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
            json={
                "include": True,
                "processVideoAnimation": True,
                "detectorProfileId": "human-mediapipe-v1",
                "videoLibraryDocId": "99",
            },
            headers={"X-User-ID": "host-1"},
        )
        r = client.post(f"/api/table-read/sessions/{sid}/save", headers={"X-User-ID": "host-1"})
        assert r.status_code == 200
        proc = client.get(f"/api/table-read/sessions/{sid}/processing", headers={"X-User-ID": "host-1"}).get_json()
        seg = next(s for s in proc["segments"] if s["id"] == seg_id)
        assert seg["animStatus"] == "failed"
        assert seg["videoLibraryDocId"] == "99"
        conn = get_conn()
        recs = conn.execute("SELECT library_doc_id FROM webcam_anim_recordings").fetchall()
        conn.close()
        assert recs == []
    finally:
        set_usc_download_impl(None)


def test_non_host_cannot_patch_or_process_anim(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    seg_id = _add_hello_segment(client, sid)
    client.post(
        f"/api/table-read/sessions/{sid}/join",
        json={"displayName": "reader-2"},
        headers={"X-User-ID": "reader-2"},
    )
    r = client.patch(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}",
        json={"processVideoAnimation": True},
        headers={"X-User-ID": "reader-2"},
    )
    assert r.status_code == 403
    r = client.post(
        f"/api/table-read/sessions/{sid}/processing/segments/{seg_id}/process-anim",
        headers={"X-User-ID": "reader-2"},
    )
    assert r.status_code == 403


def test_discard_recording_hides_from_snapshot(proc_client):
    client, _, _ = proc_client
    sid = _create_session(client)
    r = client.post(
        f"/api/table-read/sessions/{sid}/recordings",
        json={"mediaKind": "video"},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201, r.get_data(as_text=True)
    rec_id = r.get_json()["id"]
    snap = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()
    assert any(rec["id"] == rec_id for rec in snap["recordings"])

    r = client.post(
        f"/api/table-read/sessions/{sid}/recordings/{rec_id}/discard",
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 200
    assert r.get_json()["status"] == "discarded"
    snap = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"}).get_json()
    assert all(rec["id"] != rec_id for rec in snap["recordings"])
