-- Add version fields to thesaurus for change-of-basis and draft compatibility.
-- Run after continuum_thesaurus_schema.sql.
-- version: semver e.g. 1.0, 1.1; min_thesaurus_version: max version required for episode script tokens.
-- Note: ALTER will fail if column already exists; run once per DB or skip failed statements.

-- Thesaurus entries: add version (default 1.0)
ALTER TABLE thesaurus_entries ADD COLUMN version TEXT DEFAULT '1.0';

-- Thesaurus alternatives: add optional version (inherits from entry if null)
ALTER TABLE thesaurus_alternatives ADD COLUMN version TEXT;

-- Episode script: min version required for asset thesaurus
ALTER TABLE episode_script ADD COLUMN min_thesaurus_version TEXT;
