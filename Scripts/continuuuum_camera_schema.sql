-- Camera pathing scenes, ratings, votes, and threaded comments.
-- Run after core continuuuum schemas.

CREATE TABLE IF NOT EXISTS camera_scenes (
    id TEXT PRIMARY KEY,
    episode_id TEXT,
    shot_id TEXT,
    focus_mode TEXT NOT NULL,
    topology_json TEXT,
    rig_pose_json TEXT,
    memorability_ml REAL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_camera_scenes_episode ON camera_scenes(episode_id);

CREATE TABLE IF NOT EXISTS camera_scene_ratings (
    scene_id TEXT NOT NULL REFERENCES camera_scenes(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL,
    score INTEGER NOT NULL CHECK(score >= 1 AND score <= 5),
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    PRIMARY KEY (scene_id, user_id)
);

CREATE TABLE IF NOT EXISTS camera_scene_votes (
    scene_id TEXT NOT NULL REFERENCES camera_scenes(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL,
    vote INTEGER NOT NULL CHECK(vote IN (-1, 1)),
    created_at TEXT NOT NULL,
    PRIMARY KEY (scene_id, user_id)
);

CREATE TABLE IF NOT EXISTS continuuuum_threaded_comments (
    id TEXT PRIMARY KEY,
    domain TEXT NOT NULL,
    anchor_type TEXT NOT NULL,
    anchor_id TEXT NOT NULL,
    parent_comment_id TEXT,
    author_user_id TEXT NOT NULL,
    body_text TEXT NOT NULL,
    mentions_json TEXT,
    direct_link TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT
);
CREATE INDEX IF NOT EXISTS idx_threaded_comments_anchor ON continuuuum_threaded_comments(domain, anchor_id);
CREATE INDEX IF NOT EXISTS idx_threaded_comments_parent ON continuuuum_threaded_comments(parent_comment_id);
