-- Review workflow for draft episodes: reviewer, reviewer_comments, approved_user, notifications.
-- Run after continuuuum_draft_schema.sql and continuuuum_audit_schema.sql.

-- Add committed_at to draft_episodes (when non-null, reviewer edits are locked)
ALTER TABLE draft_episodes ADD COLUMN committed_at TEXT;

-- reviewer: one row per draft-reviewer pair
CREATE TABLE IF NOT EXISTS reviewer (
    id TEXT PRIMARY KEY,
    draft_episode_id TEXT NOT NULL REFERENCES draft_episodes(id) ON DELETE CASCADE,
    reviewer_user_id TEXT NOT NULL,
    reviewee_user_id TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',  -- 'pending' | 'approved' | 'request_changes'
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_reviewer_reviewer_user ON reviewer(reviewer_user_id);
CREATE INDEX IF NOT EXISTS idx_reviewer_draft ON reviewer(draft_episode_id);
CREATE INDEX IF NOT EXISTS idx_reviewer_reviewee ON reviewer(reviewee_user_id);

-- reviewer_comments: inline comments with text selection
CREATE TABLE IF NOT EXISTS reviewer_comments (
    id TEXT PRIMARY KEY,
    reviewer_id TEXT NOT NULL REFERENCES reviewer(id) ON DELETE CASCADE,
    script_ref TEXT,  -- draft_episode_script.id or episode_script.id
    text_selection_start INTEGER,
    text_selection_end INTEGER,
    comment_text TEXT NOT NULL,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_reviewer_comments_reviewer ON reviewer_comments(reviewer_id);

-- approved_user: users who may commit approved drafts (admin-editable)
CREATE TABLE IF NOT EXISTS approved_user (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL UNIQUE,
    added_by TEXT NOT NULL,
    added_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_approved_user_user ON approved_user(user_id);

-- notifications: draft review requests, comments, approvals, committed
CREATE TABLE IF NOT EXISTS notifications (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    type TEXT NOT NULL,  -- 'review_request' | 'comment' | 'approved' | 'committed'
    draft_id TEXT,
    review_id TEXT,
    message TEXT NOT NULL,
    read_at TEXT,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_notifications_user ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_read ON notifications(read_at);
