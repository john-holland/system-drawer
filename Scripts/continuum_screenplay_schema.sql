-- Screenplay: quote blocks, speech audio, sound effects, aggregation by language.
-- Run after continuum_thesaurus_schema.sql and continuum_episodes_schema.sql (episode_script, languages).

-- AST: add node_kind and quote_id for quote blocks (for existing DBs; new DBs get these from thesaurus schema).
-- If columns already exist, these ALTERs will fail; run manually once or ignore.
ALTER TABLE thesaurus_ast_nodes ADD COLUMN node_kind TEXT NOT NULL DEFAULT 'token';
ALTER TABLE thesaurus_ast_nodes ADD COLUMN quote_id TEXT;

-- script_speech_audio: one row per (episode_script, language, Farey clause) for spoken dialogue.
-- Remarks: audio_ref = file path, blob id (e.g. document_blobs.id), or URI (file://... blob:<id>).
-- Tree inclusion: any AST node whose Farey interval is contained in (ln/ld, rn/rd] belongs to this clause.
CREATE TABLE IF NOT EXISTS script_speech_audio (
    id TEXT PRIMARY KEY,
    episode_script_id TEXT NOT NULL REFERENCES episode_script(id) ON DELETE CASCADE,
    language_id TEXT NOT NULL REFERENCES languages(id),
    farey_left_num INTEGER NOT NULL,
    farey_left_den INTEGER NOT NULL,
    farey_right_num INTEGER NOT NULL,
    farey_right_den INTEGER NOT NULL,
    audio_ref TEXT NOT NULL,
    ast_node_id TEXT REFERENCES thesaurus_ast_nodes(id) ON DELETE SET NULL,
    created_at TEXT
);
CREATE INDEX IF NOT EXISTS idx_speech_audio_script_lang ON script_speech_audio(episode_script_id, language_id);
CREATE INDEX IF NOT EXISTS idx_speech_audio_farey ON script_speech_audio(farey_left_num, farey_left_den, farey_right_num, farey_right_den);

-- script_sound_effects: clause-level Farey; same encoding as speech. language_id NULL = language-agnostic.
CREATE TABLE IF NOT EXISTS script_sound_effects (
    id TEXT PRIMARY KEY,
    episode_script_id TEXT NOT NULL REFERENCES episode_script(id) ON DELETE CASCADE,
    farey_left_num INTEGER NOT NULL,
    farey_left_den INTEGER NOT NULL,
    farey_right_num INTEGER NOT NULL,
    farey_right_den INTEGER NOT NULL,
    audio_ref TEXT NOT NULL,
    language_id TEXT REFERENCES languages(id),
    effect_kind TEXT
);
CREATE INDEX IF NOT EXISTS idx_sfx_script ON script_sound_effects(episode_script_id);
CREATE INDEX IF NOT EXISTS idx_sfx_farey ON script_sound_effects(farey_left_num, farey_left_den, farey_right_num, farey_right_den);

-- script_audio_by_language: aggregates speech + SFX by (episode_script_id, language_id, Farey clause, kind).
-- Updated by change-of-basis when target language is set. kind = 'speech' | 'sfx'.
CREATE TABLE IF NOT EXISTS script_audio_by_language (
    id TEXT PRIMARY KEY,
    episode_script_id TEXT NOT NULL REFERENCES episode_script(id) ON DELETE CASCADE,
    language_id TEXT NOT NULL REFERENCES languages(id),
    farey_left_num INTEGER NOT NULL,
    farey_left_den INTEGER NOT NULL,
    farey_right_num INTEGER NOT NULL,
    farey_right_den INTEGER NOT NULL,
    kind TEXT NOT NULL,
    audio_ref TEXT NOT NULL,
    source_speech_id TEXT REFERENCES script_speech_audio(id) ON DELETE SET NULL,
    source_sfx_id TEXT REFERENCES script_sound_effects(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS idx_audio_by_lang_script ON script_audio_by_language(episode_script_id);
CREATE INDEX IF NOT EXISTS idx_audio_by_lang_lang ON script_audio_by_language(language_id);
