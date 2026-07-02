-- Galactic body registry (stars, planets, planetoids, moons) and night-sky caches

CREATE TABLE IF NOT EXISTS galactic_bodies (
    body_id TEXT PRIMARY KEY,
    kind TEXT NOT NULL CHECK (kind IN ('star', 'planet', 'planetoid', 'moon')),
    display_name TEXT NOT NULL,
    galactic_x REAL NOT NULL DEFAULT 0,
    galactic_y REAL NOT NULL DEFAULT 0,
    galactic_z REAL NOT NULL DEFAULT 0,
    mass_kg REAL NOT NULL DEFAULT 0,
    radius_m REAL NOT NULL DEFAULT 0,
    radiation_level REAL NOT NULL DEFAULT 0,
    gravity_well_strength REAL NOT NULL DEFAULT 1,
    society_planet_id TEXT REFERENCES society_planets(planet_id),
    usc_asset_id TEXT,
    scene_prefab_ref TEXT,
    lemma_color_id TEXT,
    lemma_visibility_id TEXT,
    immovable INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS galactic_night_sky_caches (
    cache_id TEXT PRIMARY KEY,
    observer_body_id TEXT NOT NULL REFERENCES galactic_bodies(body_id),
    anchor_lat REAL NOT NULL DEFAULT 0,
    anchor_lon REAL NOT NULL DEFAULT 0,
    anchor_alt_m REAL NOT NULL DEFAULT 0,
    cubemap_usc_id TEXT,
    local_path TEXT,
    star_count INTEGER NOT NULL DEFAULT 0,
    bake_version INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS galactic_sky_lattice_cells (
    cell_id TEXT PRIMARY KEY,
    centroid_x REAL NOT NULL,
    centroid_y REAL NOT NULL,
    centroid_z REAL NOT NULL,
    egg_radii_json TEXT NOT NULL DEFAULT '{}',
    blended_cache_ids_json TEXT NOT NULL DEFAULT '[]',
    weights_json TEXT NOT NULL DEFAULT '[]',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_galactic_bodies_kind ON galactic_bodies(kind);
CREATE INDEX IF NOT EXISTS idx_galactic_night_sky_observer ON galactic_night_sky_caches(observer_body_id);
CREATE INDEX IF NOT EXISTS idx_galactic_lattice_centroid ON galactic_sky_lattice_cells(centroid_x, centroid_y, centroid_z);
