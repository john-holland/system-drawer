"""Project access management for telecom devices and filesystem grants."""

from __future__ import annotations

import sqlite3
from typing import Iterable


def user_has_permission(conn: sqlite3.Connection, user_id: str, permission: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM telecom_pam_permissions WHERE user_id = ? AND permission_key = ?",
        (user_id, permission),
    )
    if cur.fetchone():
        return True
    cur = conn.execute(
        "SELECT 1 FROM telecom_pam_permissions WHERE user_id = ? AND permission_key = 'admin'",
        (user_id,),
    )
    return cur.fetchone() is not None


def user_can_access_device(conn: sqlite3.Connection, user_id: str, device_id: str) -> bool:
    if user_has_permission(conn, user_id, "admin"):
        return True
    cur = conn.execute(
        "SELECT 1 FROM telecom_pam_user_devices WHERE user_id = ? AND device_id = ?",
        (user_id, device_id),
    )
    return cur.fetchone() is not None


def user_can_access_path(
    conn: sqlite3.Connection,
    user_id: str,
    playbook_path: str,
    fs_path: str,
    *,
    require_write: bool = False,
) -> bool:
    if user_has_permission(conn, user_id, "admin"):
        return True
    cur = conn.execute(
        """SELECT rw FROM telecom_pam_filesystem_grants
           WHERE user_id = ? AND playbook_path = ? AND fs_path = ?""",
        (user_id, playbook_path, fs_path),
    )
    row = cur.fetchone()
    if not row:
        return False
    if require_write:
        return bool(row[0])
    return True


def list_user_permissions(conn: sqlite3.Connection, user_id: str) -> list[str]:
    cur = conn.execute(
        "SELECT permission_key FROM telecom_pam_permissions WHERE user_id = ? ORDER BY permission_key",
        (user_id,),
    )
    return [r[0] for r in cur.fetchall()]


def grant_permissions(conn: sqlite3.Connection, user_id: str, permissions: Iterable[str]) -> None:
    for key in permissions:
        pid = f"{user_id}:{key}"
        conn.execute(
            "INSERT OR IGNORE INTO telecom_pam_permissions (id, user_id, permission_key) VALUES (?, ?, ?)",
            (pid, user_id, key),
        )
