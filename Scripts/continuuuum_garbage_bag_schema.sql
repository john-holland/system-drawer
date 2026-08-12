-- Garbage bags: composite identity (id, dim). Dim 0 defines existence.
CREATE TABLE IF NOT EXISTS garbage_bags (
  id TEXT NOT NULL,
  dim INTEGER NOT NULL DEFAULT 0,
  title TEXT NOT NULL,
  commodities_json TEXT,
  default_mass_kg REAL NOT NULL DEFAULT 8,
  is_default INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  PRIMARY KEY (id, dim)
);

CREATE INDEX IF NOT EXISTS idx_garbage_bags_dim ON garbage_bags(dim);
