-- Loadouts / inventory items for Continuuuum + Unity InventoryManager
CREATE TABLE IF NOT EXISTS loadouts (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  icon_asset TEXT,
  prefab_id TEXT,
  use_takeout_animation INTEGER NOT NULL DEFAULT 0,
  use_putaway_animation INTEGER NOT NULL DEFAULT 0,
  ownedby_actor_id TEXT,
  heldby_actor_id TEXT,
  onground_x REAL,
  onground_y REAL,
  onground_z REAL,
  loadout_set_id TEXT NOT NULL DEFAULT 'default'
);

CREATE INDEX IF NOT EXISTS idx_loadouts_set ON loadouts(loadout_set_id);
CREATE INDEX IF NOT EXISTS idx_loadouts_owned ON loadouts(ownedby_actor_id);
CREATE INDEX IF NOT EXISTS idx_loadouts_held ON loadouts(heldby_actor_id);
CREATE INDEX IF NOT EXISTS idx_loadouts_name ON loadouts(name);
