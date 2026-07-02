-- Lemma-native dialogue sets, server sessions, LLM suggestions, goal registry
-- Apply after continuum_lemma_prompt_schema.sql

CREATE TABLE IF NOT EXISTS dialogue_sets (
    id TEXT PRIMARY KEY,
    lemma_entry_id TEXT,
    name TEXT NOT NULL,
    compiled_json TEXT NOT NULL,
    version INTEGER NOT NULL DEFAULT 1,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_dialogue_sets_lemma ON dialogue_sets(lemma_entry_id);

CREATE TABLE IF NOT EXISTS dialogue_sessions (
    id TEXT PRIMARY KEY,
    set_id TEXT NOT NULL,
    tenant TEXT NOT NULL DEFAULT 'default',
    user_id TEXT,
    state_json TEXT NOT NULL,
    trace_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (set_id) REFERENCES dialogue_sets(id)
);

CREATE INDEX IF NOT EXISTS idx_dialogue_sessions_set ON dialogue_sessions(set_id);

CREATE TABLE IF NOT EXISTS dialogue_suggestions (
    id TEXT PRIMARY KEY,
    set_id TEXT NOT NULL,
    parent_node_id TEXT,
    suggestion_json TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    source TEXT NOT NULL DEFAULT 'llm',
    created_at TEXT NOT NULL,
    FOREIGN KEY (set_id) REFERENCES dialogue_sets(id)
);

CREATE TABLE IF NOT EXISTS dialogue_goals (
    goal_key TEXT PRIMARY KEY,
    description TEXT,
    predicate_json TEXT
);

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('dialogue-set', 'String', NULL, '', 'Open a named dialogue set block.'),
    ('answer', 'String', NULL, '', 'Branch option id for player choice.'),
    ('predicate4d', 'String', NULL, '', '4D spatial predicate goal key.'),
    ('completion4d', 'String', NULL, '', '4D node id that unlocks branch when complete.'),
    ('presentation', 'String', '["text","ui","audio"]', 'text', 'Line presentation mode.'),
    ('speaker', 'String', NULL, '', 'NarrativeBindings key for actor speech playback.'),
    ('vis', 'String', '["auto","jaw","wobble","bobble","none"]', 'auto', 'Speech viseme backend override.');
