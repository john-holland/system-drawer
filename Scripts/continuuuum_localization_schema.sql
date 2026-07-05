-- Localization property specs and clause bindings (extends continuuuum_thesaurus_schema.sql)

CREATE TABLE IF NOT EXISTS localization_property_specs (
    key TEXT PRIMARY KEY,
    value_type TEXT NOT NULL,
    allowed_values_json TEXT,
    default_value TEXT,
    description TEXT
);

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES (
    'non-ik-animation',
    'Bool',
    '["true","false"]',
    'false',
    'When true, ragdoll playback uses kinematic Non-IK sampling instead of physics cards.'
);

CREATE TABLE IF NOT EXISTS thesaurus_entry_properties (
    entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id) ON DELETE CASCADE,
    property_key TEXT NOT NULL,
    property_value TEXT NOT NULL,
    PRIMARY KEY (entry_id, property_key)
);

CREATE TABLE IF NOT EXISTS localization_clause_bindings (
    id TEXT PRIMARY KEY,
    episode_script_id TEXT,
    draft_script_id TEXT,
    farey_left_num INTEGER NOT NULL,
    farey_left_den INTEGER NOT NULL,
    farey_right_num INTEGER NOT NULL,
    farey_right_den INTEGER NOT NULL,
    char_start INTEGER NOT NULL,
    char_end INTEGER NOT NULL,
    selection_text TEXT NOT NULL,
    property_key TEXT NOT NULL,
    property_value TEXT NOT NULL,
    binding_kind TEXT NOT NULL DEFAULT 'lemma',
    ast_node_id TEXT REFERENCES thesaurus_ast_nodes(id) ON DELETE SET NULL,
    prompt_placeholder_name TEXT,
    entry_id TEXT REFERENCES thesaurus_entries(id) ON DELETE SET NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_clause_bindings_entry ON localization_clause_bindings(entry_id);
CREATE INDEX IF NOT EXISTS idx_clause_bindings_script ON localization_clause_bindings(episode_script_id);
CREATE INDEX IF NOT EXISTS idx_clause_bindings_draft ON localization_clause_bindings(draft_script_id);
CREATE INDEX IF NOT EXISTS idx_clause_bindings_farey ON localization_clause_bindings(
    episode_script_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den);
