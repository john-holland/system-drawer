-- Continuum Episodes Schema for 4D Spatial Generator
-- Run against continuum.db (e.g. via USC CLI or sqlite3)
-- Tables: episodes, episode_assets, narrative_type_detections, causality_structure, work_orders

-- Episodes: time info, scene/engine, tokenized script ref, plot description
CREATE TABLE IF NOT EXISTS episodes (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    title TEXT NOT NULL,
    created_at TEXT NOT NULL,
    engine TEXT NOT NULL DEFAULT 'unity',  -- 'unity' or 'unreal'
    scene_path TEXT,  -- Unity: Assets/Scenes/Episode1.unity; Unreal: map path
    t_start REAL NOT NULL,
    t_end REAL NOT NULL,
    tokenized_script_ref TEXT,  -- FK to document_blobs or semantic_chunks
    plot_description TEXT
);

-- Episode assets: USC assets linked to episodes with causality leaf IDs
CREATE TABLE IF NOT EXISTS episode_assets (
    id TEXT PRIMARY KEY,
    episode_id TEXT NOT NULL REFERENCES episodes(id),
    usc_asset_id TEXT NOT NULL,  -- library_documents, semantic_chunks, or unique_kernels
    asset_type TEXT NOT NULL,  -- 'document', 'chunk', 'kernel'
    role TEXT,  -- 'causality_source', 'scene_prop'
    causality_leaf_id TEXT  -- Link to quad/oct tree leaf (e.g. S3.O2.1.7)
);

CREATE INDEX IF NOT EXISTS idx_episode_assets_episode ON episode_assets(episode_id);

-- Narrative type detection: linear, non-linear, hub_and_spoke per asset/word
CREATE TABLE IF NOT EXISTS narrative_type_detections (
    id TEXT PRIMARY KEY,
    episode_id TEXT NOT NULL REFERENCES episodes(id),
    asset_id TEXT NOT NULL,  -- USC asset ref or causality_leaf_id
    asset_word TEXT NOT NULL,  -- Vocabulary term (e.g. 'ladder', 'choice_point')
    narrative_type TEXT NOT NULL,  -- 'linear', 'non-linear', 'hub_and_spoke'
    detection_source TEXT NOT NULL  -- 'chatgpt' or 'procedural'
);

CREATE INDEX IF NOT EXISTS idx_ntd_episode ON narrative_type_detections(episode_id);

-- Causality structure: hub-and-spoke vs linear with quote refs and cycle IDs
CREATE TABLE IF NOT EXISTS causality_structure (
    id TEXT PRIMARY KEY,
    episode_id TEXT NOT NULL REFERENCES episodes(id),
    structure_type TEXT NOT NULL,  -- 'linear' or 'hub_and_spoke'
    description TEXT,
    chatgpt_quote_refs TEXT,  -- JSON array of {quote, startIndex, endIndex, lineNum}
    procedural_cycle_ids TEXT,  -- JSON array of cycle/hub node IDs
    detection_source TEXT NOT NULL  -- 'chatgpt' or 'procedural'
);

CREATE INDEX IF NOT EXISTS idx_cs_episode ON causality_structure(episode_id);

-- Work orders: fungible dev studio tasks from causality tree
CREATE TABLE IF NOT EXISTS work_orders (
    id TEXT PRIMARY KEY,
    episode_id TEXT NOT NULL REFERENCES episodes(id),
    causality_leaf_id TEXT,
    asset_id TEXT,
    narrative_type TEXT NOT NULL,  -- 'linear' or 'hub_and_spoke'
    depends_on TEXT,  -- JSON array of work order IDs (for linear chains)
    prompt_description TEXT,
    status TEXT NOT NULL DEFAULT 'pending',  -- 'pending', 'assigned', 'in_progress', 'done'
    assigned_to TEXT
);

CREATE INDEX IF NOT EXISTS idx_wo_episode ON work_orders(episode_id);
CREATE INDEX IF NOT EXISTS idx_wo_status ON work_orders(status);
