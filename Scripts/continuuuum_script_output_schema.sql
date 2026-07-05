-- Script Output: suggestions + unified draft comments extensions.
-- Run after continuuuum_review_schema.sql and continuuuum_localization_workflow_schema.sql.

CREATE TABLE IF NOT EXISTS script_suggestions (
    id TEXT PRIMARY KEY,
    draft_episode_id TEXT NOT NULL REFERENCES draft_episodes(id) ON DELETE CASCADE,
    suggested_by TEXT NOT NULL,
    base_script_text TEXT NOT NULL,
    suggested_script_text TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    review_cycle INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    resolved_at TEXT,
    resolved_by TEXT
);
CREATE INDEX IF NOT EXISTS idx_script_suggestions_draft ON script_suggestions(draft_episode_id);
CREATE INDEX IF NOT EXISTS idx_script_suggestions_status ON script_suggestions(status);

CREATE TABLE IF NOT EXISTS script_suggestions_archive (
    id TEXT PRIMARY KEY,
    original_suggestion_id TEXT,
    draft_episode_id TEXT NOT NULL,
    suggested_by TEXT NOT NULL,
    base_script_text TEXT NOT NULL,
    suggested_script_text TEXT NOT NULL,
    status TEXT NOT NULL,
    review_cycle INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    resolved_at TEXT,
    resolved_by TEXT,
    archived_at TEXT NOT NULL,
    archived_reason TEXT NOT NULL DEFAULT 'resolved'
);
CREATE INDEX IF NOT EXISTS idx_script_suggestions_archive_draft ON script_suggestions_archive(draft_episode_id);
