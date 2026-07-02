-- Dream cycle day/night sessions and dream memory recalls

CREATE TABLE IF NOT EXISTS dream_day_sessions (
    session_id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL,
    day_prompt TEXT,
    lemma_ids_json TEXT DEFAULT '[]',
    aspect_states_json TEXT DEFAULT '[]',
    day_collapse_seed INTEGER DEFAULT 0,
    quad_digest_json TEXT DEFAULT '{}',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS dream_sleep_sessions (
    sleep_session_id TEXT PRIMARY KEY,
    day_session_id TEXT NOT NULL REFERENCES dream_day_sessions(session_id),
    wave_json TEXT DEFAULT '[]',
    phase_markers_json TEXT DEFAULT '[]',
    sleep_seed INTEGER DEFAULT 0,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS dream_memory_recalls (
    recall_id TEXT PRIMARY KEY,
    sleep_session_id TEXT NOT NULL REFERENCES dream_sleep_sessions(sleep_session_id),
    actor_id TEXT,
    lstm_output_json TEXT DEFAULT '{}',
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_dream_day_city ON dream_day_sessions(city_id);
CREATE INDEX IF NOT EXISTS idx_dream_sleep_day ON dream_sleep_sessions(day_session_id);
