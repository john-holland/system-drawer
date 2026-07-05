-- Continuuuum telecommunications schema.
-- Run after core continuuuum schemas.

CREATE TABLE IF NOT EXISTS telecom_networks (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    virtual INTEGER NOT NULL DEFAULT 0,
    discovery_cross_route INTEGER NOT NULL DEFAULT 1,
    playbook_path TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS telecom_network_connections (
    id TEXT PRIMARY KEY,
    from_network_id TEXT NOT NULL REFERENCES telecom_networks(id) ON DELETE CASCADE,
    to_network_id TEXT NOT NULL REFERENCES telecom_networks(id) ON DELETE CASCADE,
    gateway_device_id TEXT,
    enabled INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_telecom_net_conn_from ON telecom_network_connections(from_network_id);

CREATE TABLE IF NOT EXISTS telecom_devices (
    id TEXT PRIMARY KEY,
    network_id TEXT NOT NULL REFERENCES telecom_networks(id) ON DELETE CASCADE,
    display_name TEXT NOT NULL,
    phone_e164 TEXT,
    ipv6_galactic TEXT,
    ipv6_terrestrial TEXT,
    ipv6_full TEXT,
    causality_leaf_id TEXT,
    usc_asset_id TEXT,
    spatial_geohash TEXT,
    metadata_json TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_telecom_devices_network ON telecom_devices(network_id);
CREATE INDEX IF NOT EXISTS idx_telecom_devices_phone ON telecom_devices(phone_e164);

CREATE TABLE IF NOT EXISTS telecom_routes (
    id TEXT PRIMARY KEY,
    network_id TEXT NOT NULL REFERENCES telecom_networks(id) ON DELETE CASCADE,
    prefix TEXT NOT NULL,
    next_hop TEXT,
    metric INTEGER NOT NULL DEFAULT 100,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_telecom_routes_network ON telecom_routes(network_id);

CREATE TABLE IF NOT EXISTS telecom_pam_users (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    password_hash TEXT,
    metadata_json TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS telecom_pam_permissions (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES telecom_pam_users(id) ON DELETE CASCADE,
    permission_key TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_telecom_pam_perm_user ON telecom_pam_permissions(user_id);

CREATE TABLE IF NOT EXISTS telecom_pam_user_devices (
    user_id TEXT NOT NULL REFERENCES telecom_pam_users(id) ON DELETE CASCADE,
    device_id TEXT NOT NULL REFERENCES telecom_devices(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, device_id)
);

CREATE TABLE IF NOT EXISTS telecom_pam_filesystem_grants (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES telecom_pam_users(id) ON DELETE CASCADE,
    playbook_path TEXT NOT NULL,
    fs_path TEXT NOT NULL,
    rw INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS telecom_device_ips (
    id TEXT PRIMARY KEY,
    device_id TEXT NOT NULL REFERENCES telecom_devices(id) ON DELETE CASCADE,
    ip_full TEXT NOT NULL,
    assigned_at TEXT NOT NULL,
    assignment_source TEXT NOT NULL DEFAULT 'auto'
);

CREATE TABLE IF NOT EXISTS telecom_playbook_exports (
    id TEXT PRIMARY KEY,
    episode_id TEXT,
    usc_selection_json TEXT NOT NULL,
    exported_tree_path TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS telecom_frame_processor_config (
    id TEXT PRIMARY KEY DEFAULT 'default',
    backend TEXT NOT NULL DEFAULT 'flask',
    base_url TEXT,
    updated_at TEXT NOT NULL
);

INSERT OR IGNORE INTO telecom_frame_processor_config (id, backend, base_url, updated_at)
VALUES ('default', 'flask', NULL, datetime('now'));

INSERT OR IGNORE INTO telecom_networks (id, name, virtual, discovery_cross_route, playbook_path, created_at, updated_at)
VALUES ('ubiquitous', 'Ubiquitous Virtual Network', 1, 1, 'base/ubiquitous-net.playbook.yaml', datetime('now'), datetime('now'));
