-- Continuum Living Thesaurus Schema
-- Languages, thesaurus entries with POS alternatives, Farey nested-interval AST, translations
-- Run against continuum.db (e.g. via USC CLI or sqlite3)

-- Languages: code (en, zh-Hant, ko, fr), script_direction, optional morphology_rules_ref
CREATE TABLE IF NOT EXISTS languages (
    id TEXT PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,  -- e.g. en, zh-Hant, ko, fr
    name TEXT NOT NULL,
    script_direction TEXT NOT NULL DEFAULT 'ltr',  -- ltr or rtl
    morphology_rules_ref TEXT
);
CREATE INDEX IF NOT EXISTS idx_languages_code ON languages(code);

-- Thesaurus entries: one per term per language, with POS
CREATE TABLE IF NOT EXISTS thesaurus_entries (
    id TEXT PRIMARY KEY,
    language_id TEXT NOT NULL REFERENCES languages(id),
    term TEXT NOT NULL,  -- lemma / headword
    pos_tag TEXT NOT NULL,  -- noun, verb, adverb, adjective, etc.
    UNIQUE(language_id, term, pos_tag)
);
CREATE INDEX IF NOT EXISTS idx_thesaurus_entries_language ON thesaurus_entries(language_id);
CREATE INDEX IF NOT EXISTS idx_thesaurus_entries_term ON thesaurus_entries(term);

-- Thesaurus alternatives: synonyms, inflections, honorifics per entry
CREATE TABLE IF NOT EXISTS thesaurus_alternatives (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id) ON DELETE CASCADE,
    pos_tag TEXT,
    form TEXT NOT NULL,  -- the alternative form
    role TEXT  -- e.g. synonym, inflection, honorific, conjugation
);
CREATE INDEX IF NOT EXISTS idx_thesaurus_alternatives_entry ON thesaurus_alternatives(entry_id);

-- AST nodes: Farey nested-interval encoding for script/phrase trees
-- Mediant insertion allows reorder without full renumbering
-- (node_kind, quote_id added by continuum_screenplay_schema.sql for quote blocks.)
CREATE TABLE IF NOT EXISTS thesaurus_ast_nodes (
    id TEXT PRIMARY KEY,
    parent_id TEXT REFERENCES thesaurus_ast_nodes(id) ON DELETE CASCADE,
    farey_left_num INTEGER NOT NULL,
    farey_left_den INTEGER NOT NULL,
    farey_right_num INTEGER NOT NULL,
    farey_right_den INTEGER NOT NULL,
    token_or_phrase TEXT NOT NULL,
    pos_tag TEXT,
    language_id TEXT NOT NULL REFERENCES languages(id),
    episode_script_id TEXT,  -- optional link to episode_script
    sort_key REAL  -- derived or cached for ordering
);
CREATE INDEX IF NOT EXISTS idx_ast_nodes_parent ON thesaurus_ast_nodes(parent_id);
CREATE INDEX IF NOT EXISTS idx_ast_nodes_episode_script ON thesaurus_ast_nodes(episode_script_id);
CREATE INDEX IF NOT EXISTS idx_ast_nodes_farey ON thesaurus_ast_nodes(farey_left_num, farey_left_den, farey_right_num, farey_right_den);

-- Thesaurus translations: change of basis (entry in one language -> form in another)
CREATE TABLE IF NOT EXISTS thesaurus_translations (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL REFERENCES thesaurus_entries(id) ON DELETE CASCADE,
    language_id TEXT NOT NULL REFERENCES languages(id),
    form TEXT NOT NULL,
    UNIQUE(entry_id, language_id)
);
CREATE INDEX IF NOT EXISTS idx_thesaurus_translations_entry ON thesaurus_translations(entry_id);
CREATE INDEX IF NOT EXISTS idx_thesaurus_translations_language ON thesaurus_translations(language_id);
