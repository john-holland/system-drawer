-- Political Society Sim schema (planets, cities, zoning, buildings, timelines)

CREATE TABLE IF NOT EXISTS society_planets (
    planet_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL,
    galactic_prefix_json TEXT NOT NULL,
    planet_body_usc_id TEXT,
    default_network_id TEXT NOT NULL,
    commodity_indices_json TEXT DEFAULT '{}',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS society_cities (
    city_id TEXT PRIMARY KEY,
    planet_id TEXT NOT NULL REFERENCES society_planets(planet_id),
    display_name TEXT NOT NULL,
    city_grid INTEGER NOT NULL,
    network_id TEXT NOT NULL,
    ipv6_city_prefix TEXT,
    phone_planetary_code INTEGER DEFAULT 1,
    geohash TEXT,
    sg4d_causality_leaf_id TEXT,
    solver_cadence_narrative_seconds REAL DEFAULT 3600,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    UNIQUE(planet_id, city_grid)
);

CREATE TABLE IF NOT EXISTS city_config (
    city_id TEXT PRIMARY KEY REFERENCES society_cities(city_id),
    city_size_sqm REAL DEFAULT 1000000,
    annual_budget_usd REAL DEFAULT 10000000,
    allow_debt INTEGER DEFAULT 0,
    commodity_indices_json TEXT DEFAULT '{}',
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS city_zone_documents (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    version INTEGER NOT NULL DEFAULT 1,
    document_json TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS city_scape_profiles (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    version INTEGER NOT NULL,
    profile_json TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS city_network_bindings (
    city_id TEXT PRIMARY KEY REFERENCES society_cities(city_id),
    network_id TEXT NOT NULL,
    ipv6_city_prefix TEXT NOT NULL,
    gateway_device_id TEXT,
    planet_network_id TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS city_debt_ledger (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    amount_usd REAL NOT NULL,
    reason TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS society_feature_flags (
    city_id TEXT NOT NULL,
    feature_key TEXT NOT NULL,
    enabled INTEGER DEFAULT 1,
    PRIMARY KEY (city_id, feature_key)
);

CREATE TABLE IF NOT EXISTS society_snapshots (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    tick_index INTEGER NOT NULL,
    snapshot_json TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS building_type_maps (
    building_type_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL,
    property_class TEXT NOT NULL,
    allowed_zone_ids_json TEXT,
    lemma_entry_id TEXT,
    prefab_id TEXT,
    default_opex_usd REAL DEFAULT 0,
    service_profile_json TEXT DEFAULT '{}',
    priority INTEGER DEFAULT 0,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS building_registry (
    stable_id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    building_type_id TEXT,
    zone_id TEXT,
    property_class TEXT,
    display_name TEXT,
    pin_local_x REAL,
    pin_local_z REAL,
    opex_usd REAL DEFAULT 0,
    service_profile_json TEXT DEFAULT '{}',
    causality_leaf_id TEXT,
    telecom_device_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS building_budget_ledger (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL,
    stable_id TEXT,
    line_item TEXT NOT NULL,
    amount_usd REAL NOT NULL,
    tick_index INTEGER,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS prebaked_timelines (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    frame_index INTEGER NOT NULL,
    frame_json TEXT NOT NULL,
    created_at TEXT NOT NULL,
    UNIQUE(city_id, frame_index)
);

CREATE TABLE IF NOT EXISTS political_solver_runs (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL REFERENCES society_cities(city_id),
    tick_index INTEGER NOT NULL,
    status TEXT NOT NULL,
    detail_json TEXT,
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_society_cities_planet ON society_cities(planet_id);
CREATE INDEX IF NOT EXISTS idx_building_registry_city ON building_registry(city_id);
CREATE INDEX IF NOT EXISTS idx_society_snapshots_city ON society_snapshots(city_id, tick_index);
CREATE INDEX IF NOT EXISTS idx_prebaked_timelines_city ON prebaked_timelines(city_id, frame_index);
