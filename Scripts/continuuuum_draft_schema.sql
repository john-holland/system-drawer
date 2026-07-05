-- Draft episodes and draft episode script for work-in-progress edits before publishing.
-- Run after continuuuum_episodes_schema.sql and continuuuum_thesaurus_schema.sql.
-- Requires min_thesaurus_version on episode_script (continuuuum_thesaurus_version_schema.sql).

-- draft_episodes: work-in-progress episode edits (before committing to episodes)
CREATE TABLE IF NOT EXISTS draft_episodes (
    id TEXT PRIMARY KEY,
    episode_id TEXT REFERENCES episodes(id) ON DELETE SET NULL,  -- NULL = new episode, not yet published
    tenant_id TEXT NOT NULL DEFAULT 'default',
    title TEXT NOT NULL,
    engine TEXT NOT NULL DEFAULT 'unity',
    scene_path TEXT,
    t_start REAL NOT NULL,
    t_end REAL NOT NULL,
    plot_description TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    created_by TEXT
);
CREATE INDEX IF NOT EXISTS idx_draft_episodes_episode ON draft_episodes(episode_id);
CREATE INDEX IF NOT EXISTS idx_draft_episodes_created_by ON draft_episodes(created_by);

-- draft_episode_script: draft script text per draft, with language and min_thesaurus_version
CREATE TABLE IF NOT EXISTS draft_episode_script (
    id TEXT PRIMARY KEY,
    draft_episode_id TEXT NOT NULL REFERENCES draft_episodes(id) ON DELETE CASCADE,
    episode_script_id TEXT REFERENCES episode_script(id) ON DELETE SET NULL,  -- links to canonical when publishing
    script_text TEXT,
    language TEXT NOT NULL DEFAULT 'en',
    min_thesaurus_version TEXT,  -- minimum version required for asset thesaurus definitions (semver e.g. 1.0, 1.1)
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_draft_episode_script_draft ON draft_episode_script(draft_episode_id);
