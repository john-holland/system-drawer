-- Recursive lemma prompts, web overlays (built-in URNs), spatial/timing property specs
-- Run after continuuuum_localization_schema.sql and continuuuum_lemma_composition_schema.sql

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('lemma-prompt', 'String', NULL, '', 'Recursive prompt template with {P:name|key=value} placeholders.'),
    ('spatial-t-min', 'Float', NULL, '0', 'Default narrative time start (seconds) for lemma spatial volume.'),
    ('spatial-t-max', 'Float', NULL, '3600', 'Default narrative time end (seconds) for lemma spatial volume.'),
    ('spatial-center-x', 'Float', NULL, '0', 'Lemma spatial volume center X.'),
    ('spatial-center-y', 'Float', NULL, '0', 'Lemma spatial volume center Y.'),
    ('spatial-center-z', 'Float', NULL, '0', 'Lemma spatial volume center Z.'),
    ('spatial-size-x', 'Float', NULL, '1', 'Lemma spatial volume size X.'),
    ('spatial-size-y', 'Float', NULL, '1', 'Lemma spatial volume size Y.'),
    ('spatial-size-z', 'Float', NULL, '1', 'Lemma spatial volume size Z.'),
    ('spatial-4d-id', 'String', NULL, '', 'Linked spatial_4d row id for this lemma.');

CREATE TABLE IF NOT EXISTS thesaurus_lemma_overlays (
    target_entry_id TEXT PRIMARY KEY,
    lemma_prompt TEXT,
    spatial_4d_id TEXT,
    default_timing_json TEXT,
    patch_properties_json TEXT,
    composition_json TEXT,
    updated_at TEXT NOT NULL
);
