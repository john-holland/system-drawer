-- Cave / resaurce / saurce integration tables (Continuuuum :5050)

CREATE TABLE IF NOT EXISTS platform_feature_gates (
  feature_key TEXT PRIMARY KEY,
  legal_case_id TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'blocked',
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS legal_cases (
  id TEXT PRIMARY KEY,
  slug TEXT UNIQUE,
  title TEXT NOT NULL,
  category TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'open',
  severity TEXT NOT NULL DEFAULT 'medium',
  is_built_in INTEGER NOT NULL DEFAULT 0,
  feature_key TEXT,
  description TEXT,
  patent_refs_json TEXT,
  saurce_product_id TEXT,
  media_rights_asset_id TEXT,
  lemma_package_id TEXT,
  continuuuum_asset_id INTEGER,
  assigned_to TEXT,
  opened_at TEXT NOT NULL,
  closed_at TEXT,
  case_kind TEXT NOT NULL DEFAULT 'internal_agile',
  external_metadata_json TEXT
);

CREATE TABLE IF NOT EXISTS legal_resolutions (
  id TEXT PRIMARY KEY,
  case_id TEXT NOT NULL,
  summary TEXT NOT NULL,
  resolution_type TEXT NOT NULL,
  resolved_at TEXT NOT NULL,
  resolved_by TEXT,
  effective_date TEXT,
  document_refs_json TEXT,
  FOREIGN KEY (case_id) REFERENCES legal_cases(id)
);

CREATE TABLE IF NOT EXISTS legal_code_line_refs (
  id TEXT PRIMARY KEY,
  case_id TEXT,
  resolution_id TEXT,
  repo TEXT NOT NULL,
  file_path TEXT NOT NULL,
  start_line INTEGER NOT NULL,
  end_line INTEGER NOT NULL,
  commit_sha TEXT,
  branch TEXT,
  tag TEXT,
  blame_author TEXT,
  note TEXT,
  verified_at TEXT,
  FOREIGN KEY (case_id) REFERENCES legal_cases(id),
  FOREIGN KEY (resolution_id) REFERENCES legal_resolutions(id)
);

CREATE TABLE IF NOT EXISTS legal_docket_entries (
  id TEXT PRIMARY KEY,
  case_id TEXT NOT NULL,
  filed_at TEXT,
  entry_kind TEXT NOT NULL DEFAULT 'manual',
  title TEXT NOT NULL,
  summary TEXT,
  source_url TEXT,
  created_at TEXT NOT NULL,
  FOREIGN KEY (case_id) REFERENCES legal_cases(id)
);

CREATE TABLE IF NOT EXISTS legal_watchlist_items (
  id TEXT PRIMARY KEY,
  slug TEXT UNIQUE,
  title TEXT NOT NULL,
  jurisdiction TEXT,
  agency TEXT,
  status TEXT NOT NULL DEFAULT 'watching',
  related_case_id TEXT,
  notes TEXT,
  source_url TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS chat_tos_versions (
  id TEXT PRIMARY KEY,
  semver TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  body TEXT NOT NULL,
  published_at TEXT NOT NULL,
  published_by TEXT
);

CREATE TABLE IF NOT EXISTS chat_profit_wallets (
  holder_kind TEXT NOT NULL,
  holder_id TEXT NOT NULL,
  balance_usd REAL NOT NULL DEFAULT 0,
  currency TEXT NOT NULL DEFAULT 'USD',
  updated_at TEXT NOT NULL,
  PRIMARY KEY (holder_kind, holder_id)
);

CREATE TABLE IF NOT EXISTS chat_entitlements (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  product_id TEXT NOT NULL,
  tos_version_id TEXT NOT NULL,
  signed_at TEXT NOT NULL,
  signer_ip TEXT,
  sole_user_attested INTEGER NOT NULL DEFAULT 0,
  legal_age_attested INTEGER NOT NULL DEFAULT 0,
  payer_kind TEXT NOT NULL,
  payer_user_id TEXT,
  payer_legal_entity TEXT,
  fee_ledger_id TEXT,
  profit_ledger_id TEXT,
  fee_usd REAL NOT NULL DEFAULT 1.0,
  status TEXT NOT NULL DEFAULT 'active',
  UNIQUE (user_id, product_id)
);

CREATE TABLE IF NOT EXISTS chat_invites (
  id TEXT PRIMARY KEY,
  token TEXT UNIQUE NOT NULL,
  email TEXT NOT NULL,
  user_id TEXT,
  product_id TEXT NOT NULL,
  created_by_admin TEXT NOT NULL,
  pay_for_them INTEGER NOT NULL DEFAULT 0,
  payer_legal_entity TEXT,
  expires_at TEXT,
  accepted_at TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS chat_profit_withdrawals (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  amount_usd REAL NOT NULL,
  rail TEXT NOT NULL DEFAULT 'stub',
  status TEXT NOT NULL DEFAULT 'posted',
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS chat_session_messages (
  id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  product_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  tokens_json TEXT,
  text TEXT NOT NULL,
  byte_len INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_chat_session_product ON chat_session_messages(product_id, created_at);
CREATE INDEX IF NOT EXISTS idx_chat_session_id ON chat_session_messages(session_id, created_at);

CREATE TABLE IF NOT EXISTS media_rights (
  id TEXT PRIMARY KEY,
  asset_id TEXT NOT NULL,
  platform TEXT NOT NULL,
  territory TEXT,
  effective_from TEXT,
  effective_to TEXT,
  agreement_ref TEXT,
  status TEXT NOT NULL DEFAULT 'published',
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS lemma_packages (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  lemma_entry_ids_json TEXT NOT NULL,
  premium_cost REAL,
  currency TEXT DEFAULT 'USD',
  vat_rate REAL,
  state_tax_jurisdiction TEXT,
  saurce_product_id TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS saurce_products (
  id TEXT PRIMARY KEY,
  slug TEXT UNIQUE NOT NULL,
  name TEXT NOT NULL,
  type TEXT NOT NULL,
  description TEXT,
  continuuuum_asset_id INTEGER,
  lemma_package_id TEXT,
  media_rights_asset_id TEXT,
  primary_legal_case_id TEXT,
  price_tag_json TEXT,
  subscription_json TEXT,
  preorder_json TEXT,
  investment_json TEXT,
  game_profile_json TEXT,
  publish_status TEXT NOT NULL DEFAULT 'draft',
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS saurce_investor_accounts (
  id TEXT PRIMARY KEY,
  owner_user_id TEXT NOT NULL,
  rail TEXT NOT NULL,
  crypto_wallet_address TEXT,
  ach_account_last4 TEXT,
  square_customer_id TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS saurce_investment_positions (
  id TEXT PRIMARY KEY,
  product_id TEXT NOT NULL,
  investor_account_id TEXT NOT NULL,
  position_type TEXT NOT NULL DEFAULT 'standard',
  ownership_percent REAL NOT NULL DEFAULT 0,
  committed_amount REAL NOT NULL DEFAULT 0,
  currency TEXT NOT NULL DEFAULT 'USD',
  status TEXT NOT NULL DEFAULT 'pending',
  created_at TEXT NOT NULL,
  FOREIGN KEY (product_id) REFERENCES saurce_products(id)
);

CREATE TABLE IF NOT EXISTS saurce_ledger_entries (
  id TEXT PRIMARY KEY,
  entry_type TEXT NOT NULL,
  product_id TEXT,
  position_id TEXT,
  gross_amount REAL,
  net_amount REAL,
  investor_pool_amount REAL,
  currency TEXT DEFAULT 'USD',
  idempotency_key TEXT UNIQUE,
  meta_json TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS saurce_preorder_reservations (
  id TEXT PRIMARY KEY,
  product_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  tier TEXT NOT NULL DEFAULT 'standard',
  deposit_paid REAL NOT NULL DEFAULT 0,
  investment_amount REAL,
  discount_applied REAL DEFAULT 0,
  investment_position_id TEXT,
  status TEXT NOT NULL DEFAULT 'reserved',
  created_at TEXT NOT NULL,
  FOREIGN KEY (product_id) REFERENCES saurce_products(id)
);

CREATE TABLE IF NOT EXISTS saurce_safe_crypto_foundations (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  wallet_id TEXT NOT NULL,
  asset TEXT NOT NULL DEFAULT 'USDC',
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS editor_presence (
  cave_or_tome_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  location TEXT,
  updated_at TEXT NOT NULL,
  PRIMARY KEY (cave_or_tome_id, user_id)
);

CREATE TABLE IF NOT EXISTS cave_sessions (
  session_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  permission_level TEXT NOT NULL DEFAULT 'user',
  created_at TEXT NOT NULL
);
