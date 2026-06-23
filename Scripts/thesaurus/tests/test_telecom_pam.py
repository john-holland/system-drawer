import sqlite3
import sys
import tempfile
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from telecom.pam import grant_permissions, user_can_access_device, user_has_permission
from telecom.topology_loader import ensure_telecom_tables


def test_pam_admin():
    db = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
    db.close()
    conn = sqlite3.connect(db.name)
    ensure_telecom_tables(conn)
    uid = str(uuid.uuid4())
    conn.execute(
        "INSERT INTO telecom_pam_users (id, name, created_at, updated_at) VALUES (?, ?, datetime('now'), datetime('now'))",
        (uid, "admin-user"),
    )
    grant_permissions(conn, uid, ["admin"])
    conn.commit()
    assert user_has_permission(conn, uid, "call")
    conn.close()
