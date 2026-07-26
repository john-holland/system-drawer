-- NSM semantic prime lemma property specs + association / fuzzy tables.
-- Extends continuuuum_localization_schema.sql (localization_property_specs).

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('nsm-prime', 'Bool', '["true","false"]', 'false', 'Entry is an NSM semantic prime'),
    ('nsm-group', 'String', '["substantive","relational","determiner","quantifier","evaluator","descriptor","mental","speech","action","existence","life","time","space","logical","intensifier","similarity"]', '', 'NSM prime group'),
    ('nsm-definition', 'String', NULL, '', 'Gloss / ostensive note for the prime'),
    ('nsm-logical-form', 'Json', NULL, '{}', 'Math/predicate AST for the lemma'),
    ('nsm-causality-role', 'String', '["none","causal","conditional","negation","temporal","modal"]', 'none', 'Discourse causality role'),
    ('nsm-temporal-role', 'String', '["none","when","now","before","after","duration","moment","place_time"]', 'none', 'Temporal role among time primes'),
    ('nsm-fuzzy-hedge', 'String', NULL, '', 'Hedge id / phrase key (somewhat, mostly, ...)'),
    ('nsm-fuzzy-curve', 'Json', NULL, '', 'Override membership curve params for this lemma'),
    ('causality-tree', 'String', NULL, '', 'Causality tree / composition note used by lemma-build samples'),
    ('cob-pos', 'String', NULL, '', 'Default POS filter for change-of-basis authoring'),
    ('cob-conjugation', 'Json', NULL, '', 'Default conjugation bag {mood,tense,person,number}');

CREATE TABLE IF NOT EXISTS nsm_prime_associations (
    id TEXT PRIMARY KEY,
    language_code TEXT NOT NULL DEFAULT 'en',
    source_term TEXT NOT NULL,
    target_term TEXT NOT NULL,
    relation_kind TEXT NOT NULL,
    directed INTEGER NOT NULL DEFAULT 1,
    math_form_json TEXT,
    notes TEXT,
    UNIQUE(language_code, source_term, target_term, relation_kind)
);
CREATE INDEX IF NOT EXISTS idx_nsm_assoc_source ON nsm_prime_associations(language_code, source_term);
CREATE INDEX IF NOT EXISTS idx_nsm_assoc_kind ON nsm_prime_associations(relation_kind);

CREATE TABLE IF NOT EXISTS nsm_fuzzy_hedges (
    id TEXT PRIMARY KEY,
    language_code TEXT NOT NULL DEFAULT 'en',
    phrase TEXT NOT NULL,
    aliases_json TEXT,
    band TEXT NOT NULL,
    curve_json TEXT NOT NULL,
    linked_primes_json TEXT,
    updated_at TEXT NOT NULL,
    UNIQUE(language_code, phrase)
);

CREATE TABLE IF NOT EXISTS nsm_fuzzy_variable_cache (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    language_code TEXT NOT NULL DEFAULT 'en',
    var_key TEXT NOT NULL,
    var_kind TEXT NOT NULL,
    grade REAL,
    payload_json TEXT,
    source_span TEXT,
    updated_at TEXT NOT NULL,
    UNIQUE(session_id, language_code, var_key)
);
CREATE INDEX IF NOT EXISTS idx_nsm_fuzzy_vars_session ON nsm_fuzzy_variable_cache(session_id);
