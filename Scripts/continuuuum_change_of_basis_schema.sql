-- Change-of-basis rules and word overrides for translation.
-- Run against continuuuum.db (e.g. via USC CLI or sqlite3).
-- Requires: continuuuum_thesaurus_schema.sql (languages, thesaurus_entries).
-- Runtime ensure also adds activation/replacement columns via nsm_wiring_db.ensure_nsm_schema.

-- change_of_basis_rules: rules apply when translating source_language_id → target_language_id.
-- clause_kind: main, subordinate, any. clause_position: start, mid, end, any.
-- before_pos_whitelist/blacklist, after_pos_whitelist/blacklist: comma-separated POS; NULL = any.
-- reorder_action: none, swap_with_prev, move_after_noun, etc.
-- activation_json / replacement_json: multi-word / word-id clause rewrite (see change_of_basis_engine.py).
CREATE TABLE IF NOT EXISTS change_of_basis_rules (
    id TEXT PRIMARY KEY,
    target_language_id TEXT NOT NULL REFERENCES languages(id) ON DELETE CASCADE,
    source_language_id TEXT REFERENCES languages(id) ON DELETE CASCADE,
    clause_kind TEXT,
    clause_position TEXT,
    before_pos_whitelist TEXT,
    before_pos_blacklist TEXT,
    after_pos_whitelist TEXT,
    after_pos_blacklist TEXT,
    reorder_action TEXT NOT NULL DEFAULT 'none',
    priority INTEGER,
    source_pos TEXT,
    conjugation_mood TEXT,
    conjugation_tense TEXT,
    conjugation_person TEXT,
    conjugation_number TEXT,
    activation_json TEXT,
    replacement_json TEXT,
    match_mode TEXT DEFAULT 'sequence',
    max_applications INTEGER,
    enabled INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IF NOT EXISTS idx_cob_rules_target_language ON change_of_basis_rules(target_language_id);

CREATE TABLE IF NOT EXISTS change_of_basis_word_overrides (
    id TEXT PRIMARY KEY,
    target_language_id TEXT NOT NULL REFERENCES languages(id) ON DELETE CASCADE,
    term TEXT NOT NULL,
    context_type TEXT NOT NULL DEFAULT 'default',
    target_form TEXT,
    rule_id TEXT REFERENCES change_of_basis_rules(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS idx_cob_overrides_target_language ON change_of_basis_word_overrides(target_language_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_cob_overrides_lang_term_context ON change_of_basis_word_overrides(target_language_id, term, context_type);

CREATE TABLE IF NOT EXISTS change_of_basis_conjugations (
    id TEXT PRIMARY KEY,
    target_language_id TEXT NOT NULL REFERENCES languages(id) ON DELETE CASCADE,
    entry_id TEXT,
    lemma_term TEXT NOT NULL,
    pos_tag TEXT NOT NULL,
    mood TEXT NOT NULL DEFAULT 'indicative',
    tense TEXT NOT NULL DEFAULT 'present',
    person TEXT NOT NULL DEFAULT '3',
    number TEXT NOT NULL DEFAULT 'singular',
    aspect TEXT NOT NULL DEFAULT 'none',
    politeness TEXT NOT NULL DEFAULT 'plain',
    polarity TEXT NOT NULL DEFAULT 'affirmative',
    voice TEXT NOT NULL DEFAULT 'active',
    formality TEXT NOT NULL DEFAULT 'plain',
    target_form TEXT NOT NULL,
    UNIQUE(target_language_id, lemma_term, pos_tag, mood, tense, person, number,
           aspect, politeness, polarity, voice, formality)
);

CREATE TABLE IF NOT EXISTS change_of_basis_engine_defaults (
    id TEXT PRIMARY KEY DEFAULT 'global',
    max_global_passes INTEGER NOT NULL DEFAULT 32,
    max_rule_applications INTEGER NOT NULL DEFAULT 8,
    max_clause_expansions INTEGER NOT NULL DEFAULT 64,
    warn_on_loop INTEGER NOT NULL DEFAULT 1,
    fail_on_loop INTEGER NOT NULL DEFAULT 0,
    require_validation INTEGER NOT NULL DEFAULT 0
);
