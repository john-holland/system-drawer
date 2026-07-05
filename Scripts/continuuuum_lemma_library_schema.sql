-- Lemma library extensions (prefab-id property spec + indexes)
-- Run after continuuuum_localization_schema.sql

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES (
    'prefab-id',
    'String',
    NULL,
    '',
    'USC library document id or Unity prefab path linked to this lemma.'
);

CREATE INDEX IF NOT EXISTS idx_thesaurus_entry_properties_key ON thesaurus_entry_properties(property_key);
CREATE INDEX IF NOT EXISTS idx_thesaurus_entries_term_lang ON thesaurus_entries(language_id, term);
