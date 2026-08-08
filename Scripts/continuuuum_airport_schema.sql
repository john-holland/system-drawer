-- Airport airplane schedules + staff hours (cron).
-- Applied by airport_routes.ensure_airport_tables.

CREATE TABLE IF NOT EXISTS airport_airplane_schedule (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    airplane_id TEXT NOT NULL,
    flight_id TEXT NOT NULL,
    cron_expr TEXT NOT NULL DEFAULT '* 6-22 * * *',
    schedule_kind TEXT NOT NULL DEFAULT 'service',
    enabled INTEGER NOT NULL DEFAULT 1,
    label TEXT,
    airplane_crew_json TEXT,
    gate_crew_json TEXT,
    ground_crew_json TEXT,
    notes TEXT,
    UNIQUE(airplane_id, flight_id, schedule_kind, cron_expr)
);

CREATE TABLE IF NOT EXISTS airport_staff_hours (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    building_id TEXT NOT NULL,
    role TEXT NOT NULL,
    open_cron TEXT NOT NULL DEFAULT '* 5-23 * * *',
    close_cron TEXT NOT NULL DEFAULT '',
    enabled INTEGER NOT NULL DEFAULT 1,
    notes TEXT,
    UNIQUE(building_id, role, open_cron)
);

CREATE INDEX IF NOT EXISTS idx_airport_airplane_sched_plane
    ON airport_airplane_schedule(airplane_id);
CREATE INDEX IF NOT EXISTS idx_airport_staff_hours_building
    ON airport_staff_hours(building_id);
