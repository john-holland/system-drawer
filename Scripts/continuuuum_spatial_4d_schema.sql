-- Spatial 4D rows for Continuuuum Explorer / ETL (Unity Drawer 2 ↔ continuuuum.db)
-- Apply after core episode schemas (e.g. continuuuum_episodes_schema.sql).
-- Gateway triplet: Back / Pause / Forward = oct-tree leaf ids sampled at volume tMin, centerT, tMax.

CREATE TABLE IF NOT EXISTS spatial_4d (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL DEFAULT 'default',
    episode_id TEXT REFERENCES episodes(id),
    created_at TEXT NOT NULL,
    center_x REAL NOT NULL,
    center_y REAL NOT NULL,
    center_z REAL NOT NULL,
    size_x REAL NOT NULL,
    size_y REAL NOT NULL,
    size_z REAL NOT NULL,
    t_min REAL NOT NULL,
    t_max REAL NOT NULL,
    payload_label TEXT,
    causality_leaf_base TEXT,
    causality_leaf_back TEXT,
    causality_leaf_pause TEXT,
    causality_leaf_forward TEXT,
    causality_history_json TEXT,
    road_segment_id TEXT,
    road_control_points_json TEXT,
    road_width REAL,
    road_gateway_back_json TEXT,
    road_gateway_pause_json TEXT,
    road_gateway_forward_json TEXT
);

CREATE INDEX IF NOT EXISTS idx_spatial_4d_episode ON spatial_4d(episode_id);
CREATE INDEX IF NOT EXISTS idx_spatial_4d_tenant ON spatial_4d(tenant_id);

-- Optional normalized history (alternative to causality_history_json blob on spatial_4d)
CREATE TABLE IF NOT EXISTS spatial_4d_history (
    id TEXT PRIMARY KEY,
    spatial_4d_id TEXT NOT NULL REFERENCES spatial_4d(id) ON DELETE CASCADE,
    step_index INTEGER NOT NULL,
    leaf_back TEXT,
    leaf_pause TEXT,
    leaf_forward TEXT,
    flags INTEGER NOT NULL DEFAULT 0,
    flags_json TEXT,
    narrative_t REAL,
    px REAL, py REAL, pz REAL,
    event_type TEXT
);

CREATE INDEX IF NOT EXISTS idx_s4dh_spatial ON spatial_4d_history(spatial_4d_id);
