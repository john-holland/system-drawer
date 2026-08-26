-- Scribe document configs, page TEXT bodies, and bookmark-style anchors.
-- Format enum is ODF/OOXML-compatible metadata; v1 does not parse full .odt/.docx.
-- Apply after continuuuum_dialogue_schema.sql (localization_property_specs).

CREATE TABLE IF NOT EXISTS scribe_document_configs (
    id TEXT PRIMARY KEY,
    library_doc_id TEXT,
    title TEXT NOT NULL,
    format TEXT NOT NULL DEFAULT 'plain',
    format_options_json TEXT,
    pecking_order INTEGER NOT NULL DEFAULT 20,
    tenant TEXT NOT NULL DEFAULT 'default',
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_scribe_configs_tenant ON scribe_document_configs(tenant);
CREATE INDEX IF NOT EXISTS idx_scribe_configs_library ON scribe_document_configs(library_doc_id);

CREATE TABLE IF NOT EXISTS scribe_pages (
    id TEXT PRIMARY KEY,
    config_id TEXT NOT NULL,
    page_index INTEGER NOT NULL DEFAULT 0,
    body_text TEXT,
    body_blob_id TEXT,
    body_library_doc_id TEXT,
    surface_kind TEXT,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (config_id) REFERENCES scribe_document_configs(id),
    UNIQUE (config_id, page_index)
);

CREATE INDEX IF NOT EXISTS idx_scribe_pages_config ON scribe_pages(config_id);

CREATE TABLE IF NOT EXISTS scribe_page_anchors (
    id TEXT PRIMARY KEY,
    page_id TEXT NOT NULL,
    anchor_key TEXT NOT NULL,
    char_start INTEGER,
    char_end INTEGER,
    kind TEXT NOT NULL DEFAULT 'bookmark',
    payload_json TEXT,
    FOREIGN KEY (page_id) REFERENCES scribe_pages(id),
    UNIQUE (page_id, anchor_key)
);

CREATE INDEX IF NOT EXISTS idx_scribe_anchors_page ON scribe_page_anchors(page_id);

INSERT OR IGNORE INTO localization_property_specs (key, value_type, allowed_values_json, default_value, description)
VALUES
    ('scribe-set', 'String', NULL, '', 'Scribe document config / set id.'),
    ('page', 'Integer', NULL, '0', 'Scribe page index.'),
    ('anchor', 'String', NULL, '', 'Page anchor key (lemma|dialogue|comment|bookmark).'),
    ('format', 'String', '["plain","markdown","odt","docx","pdf","lemma"]', 'plain', 'Document format metadata (ODF/OOXML-compatible).'),
    ('pecking-order', 'Integer', NULL, '20', 'Scribe / document pecking order.');
