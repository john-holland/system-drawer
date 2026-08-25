CREATE TABLE IF NOT EXISTS phone_poles (
    pole_id TEXT PRIMARY KEY,
    display_name TEXT,
    asset_guid TEXT,
    city_id TEXT,
    world_json TEXT,
    updated_by TEXT,
    updated_at TEXT
);

CREATE TABLE IF NOT EXISTS phone_wires (
    wire_id TEXT PRIMARY KEY,
    from_pole_id TEXT,
    to_pole_id TEXT,
    asset_guid TEXT,
    rope_json TEXT,
    updated_by TEXT,
    updated_at TEXT,
    FOREIGN KEY (from_pole_id) REFERENCES phone_poles(pole_id),
    FOREIGN KEY (to_pole_id) REFERENCES phone_poles(pole_id)
);

CREATE TABLE IF NOT EXISTS phone_wire_associations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pole_id TEXT,
    wire_id TEXT,
    intersection_lot_id TEXT,
    asset_guid TEXT,
    wire_end_kind TEXT,
    t01 REAL,
    updated_by TEXT,
    updated_at TEXT,
    UNIQUE (pole_id, wire_id, intersection_lot_id, wire_end_kind)
);
