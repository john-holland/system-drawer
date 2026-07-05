-- Optional seed: materialize Unity built-in thesaurus URNs into continuuuum.db after core schema.
-- Operational rule: continuuuum ETL and API must NOT mint thesaurus_entries.id values with prefix
--   urn:unity:continuuuum:builtin:v1:
-- (see C# VocabularyLanguageEncoding.BuiltInUrnPrefix in Drawer 2 / Assets/Continuuuum).
--
-- Requires: languages row for code 'en' (or change :lang below). Run after continuuuum_thesaurus_schema.sql.
-- Dictionary source 'unity_builtin' labels rows from this seed (contrast with 'internal', 'efigs', etc.).
--
-- This file does NOT list every built-in; generate full INSERTs from Unity or a small ETL that reads
-- VocabularyBuiltInRegistry. Example pattern:

-- INSERT OR IGNORE INTO thesaurus_entries (id, language_id, term, pos_tag, version)
-- SELECT
--   'urn:unity:continuuuum:builtin:v1:en/det/the',
--   (SELECT id FROM languages WHERE code = 'en' LIMIT 1),
--   'the',
--   'determiner',
--   '1.0'
-- WHERE EXISTS (SELECT 1 FROM languages WHERE code = 'en');

-- INSERT OR IGNORE INTO dictionary_definitions (id, entry_id, language_id, definition, source, version, created_at)
-- SELECT
--   lower(hex(randomblob(16))),
--   'urn:unity:continuuuum:builtin:v1:en/det/the',
--   (SELECT id FROM languages WHERE code = 'en' LIMIT 1),
--   'Definite article (Unity built-in).',
--   'unity_builtin',
--   '1.0',
--   datetime('now')
-- WHERE EXISTS (SELECT 1 FROM thesaurus_entries WHERE id = 'urn:unity:continuuuum:builtin:v1:en/det/the');
