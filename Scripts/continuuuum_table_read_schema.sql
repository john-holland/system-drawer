-- Table read sessions (multi-user script reading rooms)

CREATE TABLE IF NOT EXISTS table_read_sessions (
    id TEXT PRIMARY KEY,
    draft_episode_id TEXT NOT NULL,
    host_user_id TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'active',
    content_source TEXT NOT NULL DEFAULT 'draft',
    suggestion_id TEXT,
    segment_mode TEXT NOT NULL DEFAULT 'script',
    comment_mode TEXT NOT NULL DEFAULT 'all',
    current_turn_index INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    ended_at TEXT,
    resaurce_chat_room_id TEXT
);

CREATE TABLE IF NOT EXISTS table_read_participants (
    session_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    display_name TEXT,
    join_order INTEGER NOT NULL DEFAULT 0,
    role TEXT NOT NULL DEFAULT 'reader',
    joined_at TEXT NOT NULL,
    left_at TEXT,
    PRIMARY KEY (session_id, user_id)
);

CREATE TABLE IF NOT EXISTS table_read_turns (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    turn_index INTEGER NOT NULL,
    segment_type TEXT NOT NULL,
    segment_ref TEXT,
    assigned_user_id TEXT,
    text_snapshot TEXT,
    status TEXT NOT NULL DEFAULT 'pending',
    char_start INTEGER,
    char_end INTEGER
);

CREATE INDEX IF NOT EXISTS idx_table_read_turns_session ON table_read_turns(session_id, turn_index);

CREATE TABLE IF NOT EXISTS table_read_recordings (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    media_kind TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'recording',
    part_count INTEGER NOT NULL DEFAULT 0,
    library_doc_ids_json TEXT NOT NULL DEFAULT '[]',
    created_at TEXT NOT NULL,
    finalized_at TEXT
);

CREATE INDEX IF NOT EXISTS idx_table_read_recordings_session ON table_read_recordings(session_id, user_id);
