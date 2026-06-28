-- Agile stories, assignees, watchers, narrative overlay, calendar sync (continuum.db)
-- Apply after continuum_episodes_schema.sql and continuum_cave_saurce_schema.sql

CREATE TABLE IF NOT EXISTS stories (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    project_id TEXT,
    resaurce_schedule_id TEXT,
    resaurce_budget_plan_id TEXT,
    resaurce_chat_room_id TEXT,
    external_provider TEXT NOT NULL DEFAULT 'none',
    external_key TEXT,
    external_url TEXT,
    github_project_number INTEGER,
    jira_project_key TEXT,
    jira_issue_type TEXT,
    summary TEXT NOT NULL,
    description TEXT,
    story_value REAL NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'new'
        CHECK (status IN ('new', 'grooming', 'in_progress', 'in_review', 'submitted', 'completed')),
    episode_id TEXT REFERENCES episodes(id),
    narrative_t_start REAL,
    narrative_t_end REAL,
    calendar_start_date TEXT,
    calendar_end_date TEXT,
    build_errors_json TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    completed_at TEXT
);

CREATE INDEX IF NOT EXISTS idx_stories_tenant ON stories(tenant_id);
CREATE INDEX IF NOT EXISTS idx_stories_status ON stories(status);
CREATE INDEX IF NOT EXISTS idx_stories_schedule ON stories(resaurce_schedule_id);
CREATE INDEX IF NOT EXISTS idx_stories_episode ON stories(episode_id);

CREATE TABLE IF NOT EXISTS story_assignees (
    story_id TEXT NOT NULL REFERENCES stories(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT 'assignee',
    created_at TEXT NOT NULL,
    PRIMARY KEY (story_id, user_id)
);

CREATE TABLE IF NOT EXISTS story_watchers (
    story_id TEXT NOT NULL REFERENCES stories(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL,
    created_at TEXT NOT NULL,
    PRIMARY KEY (story_id, user_id)
);

CREATE TABLE IF NOT EXISTS story_work_orders (
    story_id TEXT NOT NULL REFERENCES stories(id) ON DELETE CASCADE,
    work_order_id TEXT NOT NULL REFERENCES work_orders(id) ON DELETE CASCADE,
    created_at TEXT NOT NULL,
    PRIMARY KEY (story_id, work_order_id)
);

CREATE TABLE IF NOT EXISTS narrative_timeline_overlay (
    id TEXT PRIMARY KEY,
    project_id TEXT,
    resaurce_schedule_id TEXT,
    spatial_4d_episode_id TEXT REFERENCES episodes(id),
    custom_start_date TEXT NOT NULL,
    narrative_start_offset_days REAL NOT NULL DEFAULT 0,
    scale_label TEXT,
    source TEXT NOT NULL DEFAULT 'manual',
    events_json TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS calendar_sync_subscriptions (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    story_id TEXT REFERENCES stories(id),
    resaurce_schedule_id TEXT,
    provider TEXT NOT NULL CHECK (provider IN ('google', 'ical', 'outlook')),
    target_url TEXT,
    oauth_token_ref TEXT,
    cron_expr TEXT NOT NULL DEFAULT '*/15 * * * *',
    last_sync_at TEXT,
    last_sync_status TEXT,
    last_sync_log TEXT,
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_cal_sync_story ON calendar_sync_subscriptions(story_id);
