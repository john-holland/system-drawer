-- Mayor Dog Mods: registry, moddable targets, packages, overrides, portal skin, player loadout

CREATE TABLE IF NOT EXISTS mayor_dog_mods (
    id TEXT PRIMARY KEY,
    slug TEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    author_user_id TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'draft',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_mayor_dog_mods_status ON mayor_dog_mods(status);
CREATE INDEX IF NOT EXISTS idx_mayor_dog_mods_author ON mayor_dog_mods(author_user_id);

CREATE TABLE IF NOT EXISTS moddable_targets (
    id TEXT PRIMARY KEY,
    target_kind TEXT NOT NULL,
    entry_id TEXT,
    draft_episode_id TEXT,
    composition_child_index INTEGER,
    char_start INTEGER NOT NULL DEFAULT 0,
    char_end INTEGER NOT NULL DEFAULT 0,
    farey_left REAL,
    farey_right REAL,
    slot_key TEXT NOT NULL UNIQUE,
    label TEXT,
    description TEXT,
    source_hash TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_moddable_targets_entry ON moddable_targets(entry_id);
CREATE INDEX IF NOT EXISTS idx_moddable_targets_draft ON moddable_targets(draft_episode_id);
CREATE INDEX IF NOT EXISTS idx_moddable_targets_kind ON moddable_targets(target_kind);

CREATE TABLE IF NOT EXISTS mod_packages (
    id TEXT PRIMARY KEY,
    mod_id TEXT NOT NULL,
    version TEXT NOT NULL DEFAULT '1.0.0',
    payload_json TEXT,
    status TEXT NOT NULL DEFAULT 'draft',
    uploaded_by_user_id TEXT NOT NULL,
    published_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (mod_id) REFERENCES mayor_dog_mods(id)
);

CREATE INDEX IF NOT EXISTS idx_mod_packages_mod ON mod_packages(mod_id);
CREATE INDEX IF NOT EXISTS idx_mod_packages_status ON mod_packages(status);

CREATE TABLE IF NOT EXISTS mod_lemma_overrides (
    id TEXT PRIMARY KEY,
    package_id TEXT NOT NULL,
    target_id TEXT NOT NULL,
    override_text TEXT NOT NULL DEFAULT '',
    patch_properties_json TEXT,
    composition_patch_json TEXT,
    created_at TEXT NOT NULL,
    FOREIGN KEY (package_id) REFERENCES mod_packages(id),
    FOREIGN KEY (target_id) REFERENCES moddable_targets(id)
);

CREATE INDEX IF NOT EXISTS idx_mod_lemma_overrides_package ON mod_lemma_overrides(package_id);
CREATE INDEX IF NOT EXISTS idx_mod_lemma_overrides_target ON mod_lemma_overrides(target_id);

CREATE TABLE IF NOT EXISTS mod_episode_overrides (
    id TEXT PRIMARY KEY,
    package_id TEXT NOT NULL,
    target_id TEXT NOT NULL,
    override_text TEXT NOT NULL DEFAULT '',
    section_metadata_json TEXT,
    created_at TEXT NOT NULL,
    FOREIGN KEY (package_id) REFERENCES mod_packages(id),
    FOREIGN KEY (target_id) REFERENCES moddable_targets(id)
);

CREATE INDEX IF NOT EXISTS idx_mod_episode_overrides_package ON mod_episode_overrides(package_id);
CREATE INDEX IF NOT EXISTS idx_mod_episode_overrides_target ON mod_episode_overrides(target_id);

CREATE TABLE IF NOT EXISTS mod_portal_usc_sets (
    mod_id TEXT PRIMARY KEY,
    library_document_ids_json TEXT NOT NULL DEFAULT '[]',
    settings_json TEXT,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (mod_id) REFERENCES mayor_dog_mods(id)
);

CREATE TABLE IF NOT EXISTS user_enabled_mods (
    user_id TEXT NOT NULL,
    mod_package_id TEXT NOT NULL,
    priority INTEGER NOT NULL DEFAULT 0,
    enabled_at TEXT NOT NULL,
    PRIMARY KEY (user_id, mod_package_id),
    FOREIGN KEY (mod_package_id) REFERENCES mod_packages(id)
);

CREATE INDEX IF NOT EXISTS idx_user_enabled_mods_user ON user_enabled_mods(user_id);
