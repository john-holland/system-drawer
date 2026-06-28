-- Composed lemmas: parent entries reference child lemmas (spatial graph expansion)
-- Run after continuum_thesaurus_schema.sql and continuum_spatial_4d_schema.sql

CREATE TABLE IF NOT EXISTS thesaurus_entry_compositions (
    id TEXT PRIMARY KEY,
    parent_entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id) ON DELETE CASCADE,
    child_entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id),
    sort_order INTEGER NOT NULL DEFAULT 0,
    spatial_4d_id TEXT REFERENCES spatial_4d(id) ON DELETE SET NULL,
    anchor_text TEXT,
    anchor_farey_json TEXT,
    draft_episode_id TEXT,
    UNIQUE(parent_entry_id, child_entry_id)
);

CREATE INDEX IF NOT EXISTS idx_comp_parent ON thesaurus_entry_compositions(parent_entry_id);
CREATE INDEX IF NOT EXISTS idx_comp_child ON thesaurus_entry_compositions(child_entry_id);

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES (
    'lemma-composition',
    'String',
    NULL,
    '',
    'JSON summary of composed child lemma ids (normalized rows live in thesaurus_entry_compositions).'
);
