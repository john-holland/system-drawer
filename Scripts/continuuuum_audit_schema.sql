-- API audit log and user presence for contractor work proof.
-- Visibility: contractors see own user_id rows; admins see all.
-- Use X-User-ID and X-Admin headers (or auth token) for middleware.

CREATE TABLE IF NOT EXISTS api_audit_log (
    id TEXT PRIMARY KEY,
    timestamp TEXT NOT NULL,
    user_id TEXT NOT NULL,
    api_path TEXT NOT NULL,
    method TEXT NOT NULL,
    remark TEXT,
    request_id TEXT,
    episode_id TEXT,
    status_code INTEGER
);
CREATE INDEX IF NOT EXISTS idx_audit_user ON api_audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON api_audit_log(timestamp);

CREATE TABLE IF NOT EXISTS user_presence (
    user_id TEXT PRIMARY KEY,
    last_seen_at TEXT NOT NULL,
    session_id TEXT
);
