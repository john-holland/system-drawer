-- Lemma Build web settings + sessions

CREATE TABLE IF NOT EXISTS lemma_build_settings (
    tenant_id TEXT PRIMARY KEY DEFAULT 'default',
    lm_studio_base_url TEXT NOT NULL DEFAULT 'http://localhost:1234/v1',
    default_model_id TEXT NOT NULL DEFAULT 'mistralai/codestral-22b-v0.1',
    max_concurrent_builds INTEGER NOT NULL DEFAULT 1,
    batch_output_dir TEXT NOT NULL DEFAULT 'Library/LemmaBuild/batches',
    default_engine TEXT NOT NULL DEFAULT 'unity',
    updated_at TEXT NOT NULL,
    updated_by TEXT
);

CREATE TABLE IF NOT EXISTS lemma_build_sessions (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    lemma_phrase TEXT NOT NULL DEFAULT '',
    model_id TEXT NOT NULL DEFAULT '',
    engine TEXT NOT NULL DEFAULT 'unity',
    batch_dir TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_lemma_build_sessions_tenant
    ON lemma_build_sessions(tenant_id, updated_at);
