-- Continuuuum end-credits lists (cast/crew) + warehouse history for guild review

CREATE TABLE IF NOT EXISTS credits_lists (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    title TEXT NOT NULL,
    episode_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_credits_lists_tenant ON credits_lists(tenant_id);
CREATE INDEX IF NOT EXISTS idx_credits_lists_episode ON credits_lists(episode_id);

CREATE TABLE IF NOT EXISTS credits_sections (
    id TEXT PRIMARY KEY,
    list_id TEXT NOT NULL REFERENCES credits_lists(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    scroll_speed REAL NOT NULL DEFAULT 40,
    is_special_ui INTEGER NOT NULL DEFAULT 0,
    quadrant_path TEXT NOT NULL DEFAULT 'R.0',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_credits_sections_list ON credits_sections(list_id, sort_order);

CREATE TABLE IF NOT EXISTS credits_entries (
    id TEXT PRIMARY KEY,
    list_id TEXT NOT NULL REFERENCES credits_lists(id) ON DELETE CASCADE,
    section_id TEXT NOT NULL REFERENCES credits_sections(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL DEFAULT '',
    nick_name TEXT NOT NULL DEFAULT '',
    show_nickname INTEGER NOT NULL DEFAULT 0,
    show_full_name INTEGER NOT NULL DEFAULT 1,
    sort_order INTEGER NOT NULL DEFAULT 0,
    quote TEXT NOT NULL DEFAULT '',
    images_json TEXT NOT NULL DEFAULT '[]',
    company TEXT NOT NULL DEFAULT '',
    rights_marks TEXT NOT NULL DEFAULT '',
    years TEXT NOT NULL DEFAULT '',
    scroll_speed REAL,
    source_user_id TEXT,
    source_kind TEXT NOT NULL DEFAULT 'manual',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_credits_entries_list ON credits_entries(list_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_credits_entries_section ON credits_entries(section_id, sort_order);
CREATE INDEX IF NOT EXISTS idx_credits_entries_source ON credits_entries(list_id, source_user_id);

CREATE TABLE IF NOT EXISTS credits_warehouse_history (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    list_id TEXT,
    event_kind TEXT NOT NULL,
    source TEXT NOT NULL DEFAULT 'manual',
    actor_user_id TEXT,
    payload_json TEXT NOT NULL DEFAULT '{}',
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_credits_wh_list ON credits_warehouse_history(list_id, created_at);
CREATE INDEX IF NOT EXISTS idx_credits_wh_tenant ON credits_warehouse_history(tenant_id, created_at);
CREATE INDEX IF NOT EXISTS idx_credits_wh_kind ON credits_warehouse_history(event_kind);
