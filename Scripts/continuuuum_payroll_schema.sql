-- Continuuuum company income payroll (HWM + seats + flexible retainers).

CREATE TABLE IF NOT EXISTS payroll_meta (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS payroll_companies (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  saurce_product_id TEXT,
  high_water_mark_usd REAL NOT NULL DEFAULT 100000,
  hwm_retainer_pct REAL NOT NULL DEFAULT 0.10,
  lifetime_net_usd REAL NOT NULL DEFAULT 0,
  phase TEXT NOT NULL DEFAULT 'pre_hwm',
  currency TEXT NOT NULL DEFAULT 'USD',
  unity_enterprise_override_usd REAL,
  tenant_id TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS payroll_beneficiary_balances (
  id TEXT PRIMARY KEY,
  company_id TEXT NOT NULL,
  beneficiary TEXT NOT NULL,
  ops_usd REAL NOT NULL DEFAULT 0,
  retainer_usd REAL NOT NULL DEFAULT 0,
  distributed_usd REAL NOT NULL DEFAULT 0,
  UNIQUE (company_id, beneficiary),
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_income_events (
  id TEXT PRIMARY KEY,
  company_id TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  source TEXT,
  gross_usd REAL,
  net_usd REAL NOT NULL,
  phase_applied TEXT NOT NULL,
  pre_hwm_portion_usd REAL NOT NULL DEFAULT 0,
  post_hwm_portion_usd REAL NOT NULL DEFAULT 0,
  meta_json TEXT,
  created_at TEXT NOT NULL,
  UNIQUE (company_id, idempotency_key),
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_allocations (
  id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL,
  company_id TEXT NOT NULL,
  beneficiary TEXT NOT NULL,
  bucket TEXT NOT NULL,
  amount_usd REAL NOT NULL,
  rate REAL NOT NULL,
  phase TEXT NOT NULL,
  retainer_id TEXT,
  FOREIGN KEY (event_id) REFERENCES payroll_income_events(id),
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_retainer_draws (
  id TEXT PRIMARY KEY,
  company_id TEXT NOT NULL,
  beneficiary TEXT NOT NULL,
  amount_usd REAL NOT NULL,
  reason TEXT,
  created_at TEXT NOT NULL,
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_team_members (
  id TEXT PRIMARY KEY,
  company_id TEXT NOT NULL,
  display_name TEXT NOT NULL,
  email TEXT,
  resaurce_employee_id TEXT,
  role TEXT NOT NULL DEFAULT 'other',
  is_designer INTEGER NOT NULL DEFAULT 0,
  is_engineer INTEGER NOT NULL DEFAULT 0,
  gameplay INTEGER NOT NULL DEFAULT 0,
  technical INTEGER NOT NULL DEFAULT 0,
  active INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_service_pricing (
  id TEXT PRIMARY KEY,
  company_id TEXT,
  cursor_monthly_usd REAL NOT NULL DEFAULT 40,
  unity_bands_json TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  UNIQUE (company_id)
);

CREATE TABLE IF NOT EXISTS payroll_retainers (
  id TEXT PRIMARY KEY,
  company_id TEXT NOT NULL,
  name TEXT NOT NULL,
  kind TEXT NOT NULL,
  mode TEXT NOT NULL,
  percent REAL,
  amount_usd REAL,
  amount_locked INTEGER NOT NULL DEFAULT 0,
  cron_expr TEXT,
  forward_company_id TEXT,
  forward_label TEXT,
  user_ids_json TEXT,
  auto_track TEXT,
  enabled INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE TABLE IF NOT EXISTS payroll_retainer_runs (
  id TEXT PRIMARY KEY,
  retainer_id TEXT NOT NULL,
  company_id TEXT NOT NULL,
  fire_key TEXT NOT NULL,
  amount_usd REAL NOT NULL,
  created_at TEXT NOT NULL,
  UNIQUE (retainer_id, fire_key),
  FOREIGN KEY (retainer_id) REFERENCES payroll_retainers(id),
  FOREIGN KEY (company_id) REFERENCES payroll_companies(id)
);

CREATE INDEX IF NOT EXISTS idx_payroll_events_company ON payroll_income_events(company_id);
CREATE INDEX IF NOT EXISTS idx_payroll_alloc_event ON payroll_allocations(event_id);
CREATE INDEX IF NOT EXISTS idx_payroll_companies_product ON payroll_companies(saurce_product_id);
CREATE INDEX IF NOT EXISTS idx_payroll_members_company ON payroll_team_members(company_id);
CREATE INDEX IF NOT EXISTS idx_payroll_retainers_company ON payroll_retainers(company_id);
