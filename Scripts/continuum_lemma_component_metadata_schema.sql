-- Lemma component creation metadata (blueprints, runtime reports, search cache)
-- Run after continuum_lemma_library_schema.sql

CREATE TABLE IF NOT EXISTS lemma_component_blueprints (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL,
    prefab_ref TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    captured_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    UNIQUE(entry_id, prefab_ref, content_hash)
);
CREATE INDEX IF NOT EXISTS idx_lemma_blueprints_entry ON lemma_component_blueprints(entry_id);
CREATE INDEX IF NOT EXISTS idx_lemma_blueprints_prefab ON lemma_component_blueprints(prefab_ref);

CREATE TABLE IF NOT EXISTS lemma_component_reports (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL,
    run_id TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    captured_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_lemma_reports_entry ON lemma_component_reports(entry_id);
CREATE INDEX IF NOT EXISTS idx_lemma_reports_captured ON lemma_component_reports(entry_id, captured_at DESC);

CREATE TABLE IF NOT EXISTS lemma_component_metadata_cache (
    entry_id TEXT PRIMARY KEY,
    cache_key TEXT NOT NULL,
    prefab_ref TEXT,
    component_type_names_json TEXT NOT NULL DEFAULT '[]',
    bucket_ids_json TEXT NOT NULL DEFAULT '[]',
    causality_leaf_ids_json TEXT NOT NULL DEFAULT '[]',
    last_blueprint_at TEXT,
    last_report_at TEXT,
    report_count INTEGER NOT NULL DEFAULT 0,
    summary_json TEXT NOT NULL DEFAULT '{}',
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_lemma_metadata_cache_key ON lemma_component_metadata_cache(cache_key);
