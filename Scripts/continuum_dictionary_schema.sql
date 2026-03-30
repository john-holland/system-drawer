-- Dictionary definitions for thesaurus entries (tooltips, word asset definitions).
-- EFIGS fallback: en, fr, it, de, es. source: internal, efigs, fallback.
-- Run after continuum_thesaurus_schema.sql.

CREATE TABLE IF NOT EXISTS dictionary_definitions (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id) ON DELETE CASCADE,
    language_id TEXT NOT NULL REFERENCES languages(id),
    definition TEXT NOT NULL,
    source TEXT,  -- 'internal', 'audit', 'efigs', 'fallback'
    version TEXT,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_dictionary_definitions_entry ON dictionary_definitions(entry_id);
CREATE INDEX IF NOT EXISTS idx_dictionary_definitions_language ON dictionary_definitions(language_id);
