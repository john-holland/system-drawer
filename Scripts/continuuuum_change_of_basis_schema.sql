-- Change-of-basis rules and word overrides for translation.
-- Run against continuuuum.db (e.g. via USC CLI or sqlite3).
-- Requires: continuuuum_thesaurus_schema.sql (languages, thesaurus_entries).

-- change_of_basis_rules: rules apply when translating TO target_language_id.
-- clause_kind: main, subordinate, any. clause_position: start, mid, end, any.
-- before_pos_whitelist/blacklist, after_pos_whitelist/blacklist: comma-separated POS; NULL = any.
-- reorder_action: none, swap_with_prev, move_after_noun, etc.
CREATE TABLE IF NOT EXISTS change_of_basis_rules (
    id TEXT PRIMARY KEY,
    target_language_id TEXT NOT NULL REFERENCES languages(id) ON DELETE CASCADE,
    clause_kind TEXT,  -- main, subordinate, any
    clause_position TEXT,  -- start, mid, end, any
    before_pos_whitelist TEXT,  -- comma-separated; NULL = any
    before_pos_blacklist TEXT,
    after_pos_whitelist TEXT,
    after_pos_blacklist TEXT,
    reorder_action TEXT NOT NULL DEFAULT 'none',  -- none, swap_with_prev, move_after_noun, etc.
    priority INTEGER  -- lower = applied first
);
CREATE INDEX IF NOT EXISTS idx_cob_rules_target_language ON change_of_basis_rules(target_language_id);

-- change_of_basis_word_overrides: per-term, per-context overrides (e.g. proper nouns: place vs person).
-- term: surface form or lemma. context_type: place, person, default, etc.
-- target_form: override translation; NULL = do not translate / leave as-is.
CREATE TABLE IF NOT EXISTS change_of_basis_word_overrides (
    id TEXT PRIMARY KEY,
    target_language_id TEXT NOT NULL REFERENCES languages(id) ON DELETE CASCADE,
    term TEXT NOT NULL,
    context_type TEXT NOT NULL DEFAULT 'default',  -- place, person, default, etc.
    target_form TEXT,  -- NULL = leave as-is
    rule_id TEXT REFERENCES change_of_basis_rules(id) ON DELETE SET NULL  -- optional link to rule
);
CREATE INDEX IF NOT EXISTS idx_cob_overrides_target_language ON change_of_basis_word_overrides(target_language_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_cob_overrides_lang_term_context ON change_of_basis_word_overrides(target_language_id, term, context_type);
