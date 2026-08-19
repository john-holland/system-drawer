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

-- Frozen script at table-read start (later draft edits do not mutate the take)
CREATE TABLE IF NOT EXISTS table_read_script_storage (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    draft_episode_id TEXT NOT NULL,
    script_text TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_tr_script_storage_session ON table_read_script_storage(session_id, created_at);

-- Organizer character → actor → quote spans
CREATE TABLE IF NOT EXISTS table_read_character_quotes (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    character_name TEXT NOT NULL,
    dialog_actor_id TEXT,
    char_start INTEGER NOT NULL,
    char_end INTEGER NOT NULL,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_tr_quotes_session ON table_read_character_quotes(session_id);

-- Organizer character roster (name → actor) without requiring quote spans
CREATE TABLE IF NOT EXISTS table_read_characters (
    session_id TEXT NOT NULL,
    character_name TEXT NOT NULL,
    dialog_actor_id TEXT,
    created_at TEXT NOT NULL,
    PRIMARY KEY (session_id, character_name)
);

CREATE TABLE IF NOT EXISTS table_read_processing_segments (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    quote_id TEXT NOT NULL,
    recording_id TEXT,
    usc_job_id TEXT,
    whisper_json TEXT,
    include INTEGER NOT NULL DEFAULT 1,
    pause_before INTEGER NOT NULL DEFAULT 0,
    pause_before_sec REAL NOT NULL DEFAULT 0,
    pause_after INTEGER NOT NULL DEFAULT 0,
    pause_after_sec REAL NOT NULL DEFAULT 0,
    insert_pause INTEGER NOT NULL DEFAULT 0,
    insert_pause_pos REAL NOT NULL DEFAULT 0,
    insert_pause_sec REAL NOT NULL DEFAULT 0,
    upload_library_doc_id TEXT,
    match_ok INTEGER,
    status TEXT NOT NULL DEFAULT 'pending',
    audio_url TEXT,
    process_video_animation INTEGER NOT NULL DEFAULT 0,
    detector_profile_id TEXT,
    anim_props_json TEXT,
    video_library_doc_id TEXT,
    webcam_recording_id TEXT,
    pose_track_path TEXT,
    anim_status TEXT NOT NULL DEFAULT 'idle',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_tr_proc_seg_session ON table_read_processing_segments(session_id);

CREATE TABLE IF NOT EXISTS table_read_processing_choices (
    session_id TEXT PRIMARY KEY,
    composition_json TEXT NOT NULL DEFAULT '{}',
    saved_at TEXT,
    dialogue_set_id TEXT,
    processed_assets_json TEXT NOT NULL DEFAULT '[]',
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS asset_owner_history (
    id TEXT PRIMARY KEY,
    asset_kind TEXT NOT NULL,
    asset_id TEXT NOT NULL,
    from_owner TEXT,
    to_owner TEXT NOT NULL,
    admin_user_id TEXT NOT NULL,
    created_at TEXT NOT NULL,
    reason TEXT
);
CREATE INDEX IF NOT EXISTS idx_asset_owner_history_asset ON asset_owner_history(asset_kind, asset_id, created_at);
