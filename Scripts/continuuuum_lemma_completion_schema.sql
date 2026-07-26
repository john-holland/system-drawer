-- Lemma completion tracking for NSM primes + top-N common words.
-- Run after continuuuum_thesaurus_schema.sql (entry_id is soft-linked text, not FK).

CREATE TABLE IF NOT EXISTS lemma_completion (
    id TEXT PRIMARY KEY,
    language_code TEXT NOT NULL DEFAULT 'en',
    term TEXT NOT NULL,
    rank INTEGER,
    entry_id TEXT,
    is_prime INTEGER NOT NULL DEFAULT 0,
    is_builtin INTEGER NOT NULL DEFAULT 0,
    is_implemented INTEGER NOT NULL DEFAULT 0,
    benefits_from_asset_store INTEGER NOT NULL DEFAULT 0,
    nsm_definition TEXT,
    composition_json TEXT,
    descriptor_json TEXT,
    updated_at TEXT NOT NULL,
    UNIQUE(language_code, term)
);

CREATE INDEX IF NOT EXISTS idx_lemma_completion_rank ON lemma_completion(rank);
CREATE INDEX IF NOT EXISTS idx_lemma_completion_flags
    ON lemma_completion(is_builtin, is_implemented, benefits_from_asset_store);
