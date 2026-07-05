-- Lemma-native quest sets, objectives, sessions, summaries, assets, pathing, behavior trees
-- Apply after continuuuum_spatial_4d_schema.sql and continuuuum_dialogue_schema.sql

CREATE TABLE IF NOT EXISTS quest_sets (
    id TEXT PRIMARY KEY,
    episode_id TEXT,
    lemma_entry_id TEXT,
    title TEXT NOT NULL,
    root_spatial_4d_id TEXT,
    compiled_json TEXT NOT NULL,
    default_map_profile_json TEXT,
    version INTEGER NOT NULL DEFAULT 1,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_quest_sets_lemma ON quest_sets(lemma_entry_id);
CREATE INDEX IF NOT EXISTS idx_quest_sets_episode ON quest_sets(episode_id);

CREATE TABLE IF NOT EXISTS quest_objectives (
    id TEXT PRIMARY KEY,
    quest_set_id TEXT NOT NULL REFERENCES quest_sets(id) ON DELETE CASCADE,
    objective_id TEXT NOT NULL,
    parent_id TEXT,
    spatial_4d_id TEXT,
    bounds_json TEXT,
    predicate4d TEXT,
    completion4d TEXT,
    sort_order INTEGER NOT NULL DEFAULT 0,
    summary_text TEXT,
    pathing_json TEXT,
    behavior_trees_json TEXT,
    UNIQUE (quest_set_id, objective_id)
);

CREATE INDEX IF NOT EXISTS idx_quest_objectives_set ON quest_objectives(quest_set_id);

CREATE TABLE IF NOT EXISTS quest_summaries (
    id TEXT PRIMARY KEY,
    objective_id TEXT NOT NULL REFERENCES quest_objectives(id) ON DELETE CASCADE,
    mode TEXT NOT NULL CHECK (mode IN ('bespoke', 'generated')),
    text TEXT,
    style_profile_json TEXT,
    suggestion_id TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS quest_suggestions (
    id TEXT PRIMARY KEY,
    quest_set_id TEXT NOT NULL REFERENCES quest_sets(id) ON DELETE CASCADE,
    objective_id TEXT,
    kind TEXT NOT NULL DEFAULT 'summary',
    prompt TEXT,
    style_hint TEXT,
    suggestion_json TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    created_at TEXT NOT NULL,
    accepted_at TEXT
);

CREATE TABLE IF NOT EXISTS quest_assets (
    id TEXT PRIMARY KEY,
    objective_id TEXT NOT NULL REFERENCES quest_objectives(id) ON DELETE CASCADE,
    kind TEXT NOT NULL CHECK (kind IN ('icon', 'map_marker', 'banner', 'ambient', 'stinger')),
    asset_ref TEXT,
    inpaint_mask_json TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS quest_pathing (
    id TEXT PRIMARY KEY,
    objective_id TEXT NOT NULL REFERENCES quest_objectives(id) ON DELETE CASCADE,
    preset_key TEXT NOT NULL,
    settings_json TEXT NOT NULL,
    authoring_rows_json TEXT,
    UNIQUE (objective_id, preset_key)
);

CREATE TABLE IF NOT EXISTS quest_behavior_trees (
    id TEXT PRIMARY KEY,
    objective_id TEXT NOT NULL REFERENCES quest_objectives(id) ON DELETE CASCADE,
    role TEXT NOT NULL CHECK (role IN ('ui', 'map_display', 'animation')),
    tree_ref TEXT,
    params_json TEXT,
    UNIQUE (objective_id, role)
);

CREATE TABLE IF NOT EXISTS quest_sessions (
    id TEXT PRIMARY KEY,
    quest_set_id TEXT NOT NULL REFERENCES quest_sets(id),
    tenant TEXT NOT NULL DEFAULT 'default',
    user_id TEXT,
    active_objective_id TEXT,
    state_json TEXT NOT NULL,
    trace_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_quest_sessions_set ON quest_sessions(quest_set_id);

CREATE TABLE IF NOT EXISTS quest_map_cache (
    id TEXT PRIMARY KEY,
    spatial_4d_id TEXT NOT NULL,
    narrative_t REAL NOT NULL,
    projection_axis TEXT NOT NULL DEFAULT 'xz',
    resolution INTEGER NOT NULL DEFAULT 256,
    blob_ref TEXT,
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_quest_map_cache_spatial ON quest_map_cache(spatial_4d_id, narrative_t);

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('quest-set', 'String', NULL, '', 'Open a named quest set block.'),
    ('objective', 'String', NULL, '', 'Stable objective id within quest set.'),
    ('spatial4d', 'String', NULL, '', 'Bind objective to spatial_4d.id.'),
    ('bounds3d', 'String', NULL, '', 'Override 3D AABB xMin..zMax comma form.'),
    ('bounds4d', 'String', NULL, '', 'Override 4D AABB xMin..tMax comma form.'),
    ('summary', 'String', NULL, '', 'Bespoke objective summary text.'),
    ('style', 'String', NULL, '', 'Style profile key or inline hint.'),
    ('style-suggest', 'String', NULL, '', 'Request style suggestion profile.'),
    ('travel-binding', 'String', NULL, '', 'Quest pathing preset key.'),
    ('map-layer', 'String', '["occupancy","causal","emergence","composite"]', 'composite', 'Map layer mix.'),
    ('ui-bt', 'String', NULL, '', 'UI behavior tree asset ref.'),
    ('map-bt', 'String', NULL, '', 'Map display behavior tree asset ref.'),
    ('anim-bt', 'String', NULL, '', 'Animation behavior tree asset ref.'),
    ('audio-cue', 'String', NULL, '', 'One-shot audio cue ref.'),
    ('ambient-loop', 'String', NULL, '', 'Ambient loop audio ref.');
