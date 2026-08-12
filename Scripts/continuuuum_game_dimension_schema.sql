-- Continuuuum game / dimension registries, visibility, associations, dim property overrides, SG warm, reviews.

CREATE TABLE IF NOT EXISTS games (
  id TEXT PRIMARY KEY,
  slug TEXT NOT NULL UNIQUE,
  display_name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  is_public INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS dimensions (
  id TEXT PRIMARY KEY,
  dim_index INTEGER NOT NULL UNIQUE,
  slug TEXT,
  display_name TEXT NOT NULL,
  is_public INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS gd_visibility_grants (
  id TEXT PRIMARY KEY,
  subject_kind TEXT NOT NULL,
  subject_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  granted_by TEXT,
  created_at TEXT NOT NULL,
  UNIQUE (subject_kind, subject_id, user_id)
);

CREATE TABLE IF NOT EXISTS user_game_access (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  game_id TEXT NOT NULL,
  access_level TEXT NOT NULL DEFAULT 'play',
  created_at TEXT NOT NULL,
  UNIQUE (user_id, game_id),
  FOREIGN KEY (game_id) REFERENCES games(id)
);

CREATE TABLE IF NOT EXISTS user_dimension_access (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  dimension_id TEXT NOT NULL,
  access_level TEXT NOT NULL DEFAULT 'play',
  created_at TEXT NOT NULL,
  UNIQUE (user_id, dimension_id),
  FOREIGN KEY (dimension_id) REFERENCES dimensions(id)
);

CREATE TABLE IF NOT EXISTS user_context (
  user_id TEXT PRIMARY KEY,
  game_id TEXT,
  dimension_id TEXT,
  updated_at TEXT NOT NULL,
  FOREIGN KEY (game_id) REFERENCES games(id),
  FOREIGN KEY (dimension_id) REFERENCES dimensions(id)
);

CREATE TABLE IF NOT EXISTS entity_gd_assoc (
  id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  game_id TEXT NOT NULL,
  dimension_id TEXT NOT NULL,
  created_at TEXT NOT NULL,
  UNIQUE (table_name, entity_id, game_id, dimension_id),
  FOREIGN KEY (game_id) REFERENCES games(id),
  FOREIGN KEY (dimension_id) REFERENCES dimensions(id)
);

CREATE TABLE IF NOT EXISTS thesaurus_entry_property_dims (
  id TEXT PRIMARY KEY,
  entry_id TEXT NOT NULL,
  dim_index INTEGER NOT NULL,
  property_key TEXT NOT NULL,
  property_value TEXT,
  updated_at TEXT NOT NULL,
  UNIQUE (entry_id, dim_index, property_key)
);

CREATE TABLE IF NOT EXISTS sg_dimension_warm_snapshots (
  id TEXT PRIMARY KEY,
  game_id TEXT NOT NULL,
  dimension_id TEXT NOT NULL,
  sg_kind TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  etag TEXT NOT NULL,
  built_at TEXT NOT NULL,
  source_revision TEXT,
  UNIQUE (game_id, dimension_id, sg_kind),
  FOREIGN KEY (game_id) REFERENCES games(id),
  FOREIGN KEY (dimension_id) REFERENCES dimensions(id)
);

CREATE TABLE IF NOT EXISTS gd_change_lists (
  id TEXT PRIMARY KEY,
  title TEXT,
  status TEXT NOT NULL DEFAULT 'new',
  owner_user_id TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS gd_change_list_items (
  id TEXT PRIMARY KEY,
  change_list_id TEXT NOT NULL,
  op TEXT NOT NULL,
  table_name TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  game_id TEXT,
  dimension_id TEXT,
  ack INTEGER NOT NULL DEFAULT 0,
  payload_json TEXT,
  created_at TEXT NOT NULL,
  FOREIGN KEY (change_list_id) REFERENCES gd_change_lists(id)
);

CREATE TABLE IF NOT EXISTS gd_change_list_reviewers (
  id TEXT PRIMARY KEY,
  change_list_id TEXT NOT NULL,
  reviewer_user_id TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'pending',
  updated_at TEXT NOT NULL,
  UNIQUE (change_list_id, reviewer_user_id),
  FOREIGN KEY (change_list_id) REFERENCES gd_change_lists(id)
);

CREATE TABLE IF NOT EXISTS gd_review_comments (
  id TEXT PRIMARY KEY,
  change_list_id TEXT NOT NULL,
  item_id TEXT,
  author_user_id TEXT NOT NULL,
  body TEXT NOT NULL,
  created_at TEXT NOT NULL,
  FOREIGN KEY (change_list_id) REFERENCES gd_change_lists(id)
);

CREATE TABLE IF NOT EXISTS gd_suggestions (
  id TEXT PRIMARY KEY,
  change_list_id TEXT NOT NULL,
  author_user_id TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'open',
  created_at TEXT NOT NULL,
  FOREIGN KEY (change_list_id) REFERENCES gd_change_lists(id)
);
