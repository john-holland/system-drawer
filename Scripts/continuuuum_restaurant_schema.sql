-- Restaurant / kitchen sim schema (non-Toast: no payments)

CREATE TABLE IF NOT EXISTS restaurants (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    city_id TEXT,
    building_stable_id TEXT,
    name TEXT NOT NULL,
    cuisine TEXT DEFAULT '',
    hours_json TEXT DEFAULT '{}',
    open_state TEXT DEFAULT 'closed',
    service_profile_json TEXT DEFAULT '{}',
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS menu_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    sku TEXT,
    name TEXT NOT NULL,
    category TEXT DEFAULT 'entree',
    description TEXT DEFAULT '',
    price REAL DEFAULT 0,
    available INTEGER DEFAULT 1,
    ingredient_refs_json TEXT DEFAULT '[]',
    chef_card_hints_json TEXT DEFAULT '[]',
    sort_order INTEGER DEFAULT 0,
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS menu_modifiers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    menu_item_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    price_delta REAL DEFAULT 0,
    available INTEGER DEFAULT 1,
    FOREIGN KEY (menu_item_id) REFERENCES menu_items(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ingredients (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    unit TEXT DEFAULT 'ea',
    on_hand REAL DEFAULT 0,
    reorder_at REAL DEFAULT 0,
    commodity_key TEXT DEFAULT '',
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS supply_links (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    building_stable_id TEXT,
    ingredient_id INTEGER,
    bin_strategy TEXT DEFAULT 'batch_bin',
    notes TEXT DEFAULT '',
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE,
    FOREIGN KEY (ingredient_id) REFERENCES ingredients(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    status TEXT DEFAULT 'queued',
    ticket_label TEXT DEFAULT '',
    notes TEXT DEFAULT '',
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS order_lines (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    menu_item_id INTEGER,
    name TEXT NOT NULL,
    qty REAL DEFAULT 1,
    modifiers_json TEXT DEFAULT '[]',
    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
    FOREIGN KEY (menu_item_id) REFERENCES menu_items(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS commodity_schedules (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    commodity_key TEXT NOT NULL,
    cron_expr TEXT,
    one_shot_at TEXT,
    surge_mult REAL DEFAULT 1,
    quantity REAL DEFAULT 1,
    price REAL DEFAULT 0,
    availability INTEGER DEFAULT 1,
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS retinue_members (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    restaurant_id INTEGER NOT NULL,
    persona_key TEXT NOT NULL,
    role TEXT DEFAULT 'line-chef',
    pay_rate REAL DEFAULT 0,
    pecking_order INTEGER DEFAULT 100,
    duty_cron TEXT,
    shift_window_json TEXT DEFAULT '{}',
    waypoint_group TEXT DEFAULT '',
    FOREIGN KEY (restaurant_id) REFERENCES restaurants(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_menu_items_restaurant ON menu_items(restaurant_id);
CREATE INDEX IF NOT EXISTS idx_orders_restaurant ON orders(restaurant_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_retinue_restaurant ON retinue_members(restaurant_id);
CREATE INDEX IF NOT EXISTS idx_commodity_schedules_restaurant ON commodity_schedules(restaurant_id);
