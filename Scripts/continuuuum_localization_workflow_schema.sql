-- Localization change-list workflow + review comment extensions
-- Run after continuuuum_review_schema.sql and continuuuum_localization_schema.sql

CREATE TABLE IF NOT EXISTS comment_topics (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS localization_change_lists (
    id TEXT PRIMARY KEY,
    episode_script_id TEXT,
    draft_episode_id TEXT,
    comment_topic_id TEXT NOT NULL REFERENCES comment_topics(id),
    workflow_status TEXT NOT NULL DEFAULT 'new',
    revision INTEGER NOT NULL DEFAULT 0,
    review_cycle INTEGER NOT NULL DEFAULT 0,
    last_saved_at TEXT,
    submit_schedule_cron TEXT,
    submit_window_opens_at TEXT,
    submit_window_closes_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    submitted_at TEXT,
    merged_at TEXT
);
CREATE INDEX IF NOT EXISTS idx_change_lists_draft ON localization_change_lists(draft_episode_id);
CREATE INDEX IF NOT EXISTS idx_change_lists_status ON localization_change_lists(workflow_status);

CREATE TABLE IF NOT EXISTS localization_change_list_reviewers (
    change_list_id TEXT NOT NULL REFERENCES localization_change_lists(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL,
    role TEXT NOT NULL DEFAULT 'reviewer',
    approved_at TEXT,
    rejected_at TEXT,
    PRIMARY KEY (change_list_id, user_id)
);

CREATE TABLE IF NOT EXISTS localization_change_list_items (
    id TEXT PRIMARY KEY,
    change_list_id TEXT NOT NULL REFERENCES localization_change_lists(id) ON DELETE CASCADE,
    sort_order INTEGER NOT NULL,
    severity TEXT NOT NULL,
    item_type TEXT NOT NULL,
    binding_id TEXT REFERENCES localization_clause_bindings(id),
    description TEXT NOT NULL,
    old_char_start INTEGER,
    old_char_end INTEGER,
    new_char_start INTEGER,
    new_char_end INTEGER,
    auto_applied INTEGER NOT NULL DEFAULT 0,
    user_acknowledged INTEGER NOT NULL DEFAULT 0,
    superseded_at TEXT,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_change_list_items_list ON localization_change_list_items(change_list_id);

CREATE TABLE IF NOT EXISTS reviewer_comments_archive (
    id TEXT PRIMARY KEY,
    reviewer_id TEXT NOT NULL REFERENCES reviewer(id) ON DELETE CASCADE,
    original_comment_id TEXT,
    comment_text TEXT NOT NULL,
    previously_on TEXT NOT NULL,
    text_selection_start INTEGER,
    text_selection_end INTEGER,
    property_key TEXT,
    review_cycle INTEGER NOT NULL,
    archived_at TEXT NOT NULL,
    archived_reason TEXT NOT NULL DEFAULT 'review_cycle_reset'
);
CREATE INDEX IF NOT EXISTS idx_reviewer_comments_archive_reviewer ON reviewer_comments_archive(reviewer_id);

-- Safe ALTERs for existing review tables (ignore errors if columns exist)
-- reviewer.review_cycle
-- reviewer_comments extensions applied via migration helper in API
