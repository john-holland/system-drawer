-- Transit Authority vehicle + building schedules (cron).
-- Applied by transit_routes.ensure_transit_tables.

CREATE TABLE IF NOT EXISTS transit_authority_vehicle_schedule (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    vehicle_id TEXT NOT NULL,
    route_id TEXT NOT NULL,
    cron_expr TEXT NOT NULL DEFAULT '* 6-22 * * 1-5',
    schedule_kind TEXT NOT NULL DEFAULT 'service',
    enabled INTEGER NOT NULL DEFAULT 1,
    label TEXT,
    notes TEXT,
    UNIQUE(vehicle_id, route_id, schedule_kind, cron_expr)
);

CREATE TABLE IF NOT EXISTS transit_building_schedule (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_id TEXT NOT NULL,
    building_id TEXT,
    cron_expr TEXT NOT NULL DEFAULT '* 5-23 * * *',
    kind TEXT NOT NULL DEFAULT 'opening',
    enabled INTEGER NOT NULL DEFAULT 1,
    notes TEXT,
    UNIQUE(station_id, kind, cron_expr)
);

CREATE INDEX IF NOT EXISTS idx_ta_vehicle_sched_vehicle
    ON transit_authority_vehicle_schedule(vehicle_id);
CREATE INDEX IF NOT EXISTS idx_ta_building_sched_station
    ON transit_building_schedule(station_id);
