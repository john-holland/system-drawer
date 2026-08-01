-- Station placards hierarchy (cooking / train / bus / computer)

CREATE TABLE IF NOT EXISTS station_placards (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    city_id TEXT NOT NULL,
    stable_id TEXT NOT NULL,
    name TEXT NOT NULL,
    kind TEXT NOT NULL DEFAULT 'generic',
    config_json TEXT DEFAULT '{}',
    causality_leaf_id TEXT,
    building_stable_id TEXT,
    vehicle_id TEXT,
    parent_station_id TEXT,
    level_id TEXT DEFAULT '',
    staffing_weight REAL DEFAULT 1,
    updated_at TEXT DEFAULT (datetime('now')),
    UNIQUE(city_id, stable_id)
);

CREATE TABLE IF NOT EXISTS station_commodities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_id INTEGER NOT NULL,
    commodity_key TEXT NOT NULL,
    cron_expr TEXT,
    one_shot_at TEXT,
    surge_mult REAL DEFAULT 1,
    quantity REAL DEFAULT 1,
    price REAL DEFAULT 0,
    availability INTEGER DEFAULT 1,
    FOREIGN KEY (station_id) REFERENCES station_placards(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS station_assignments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_id INTEGER NOT NULL,
    assign_type TEXT NOT NULL,
    ref_id TEXT NOT NULL,
    role TEXT DEFAULT '',
    pecking_order INTEGER DEFAULT 100,
    FOREIGN KEY (station_id) REFERENCES station_placards(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS station_level_stats (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    level_id TEXT NOT NULL,
    city_id TEXT NOT NULL,
    payload_json TEXT NOT NULL DEFAULT '{}',
    uploaded_at TEXT DEFAULT (datetime('now')),
    UNIQUE(level_id, city_id)
);

CREATE INDEX IF NOT EXISTS idx_station_placards_city ON station_placards(city_id);
CREATE INDEX IF NOT EXISTS idx_station_placards_kind ON station_placards(kind);
CREATE INDEX IF NOT EXISTS idx_station_commodities_station ON station_commodities(station_id);
CREATE INDEX IF NOT EXISTS idx_station_assignments_station ON station_assignments(station_id);
