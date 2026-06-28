# Agile Production — Continuum + resaurce

Production schedule and budget live in **resaurce** (JSON store under `data/resaurce-store.json`). Stories, work orders, causality validation, narrative overlay, and calendar subscriptions live in **continuum.db**. Chat is hosted in **resaurce** and proxied through **continuum_api**.

## Water-reservoir budget (Pond metaphor)

- `capacity_usd` — budget ceiling (reservoir size)
- `water_level_usd` — current pool (cash_pool balance from journal)
- Inflows: Saurce `preorder_deposit`, `investment`, etc. → debit `cash_pool`
- Outflows: `story_spend`, `story_allocation` → credit `cash_pool`
- UI: `/budget-dashboard`, water gauge on `/project-calendar`

Double-entry journal: `budget_journal_entries` with `debit_account` / `credit_account` per transaction.

## Schema apply order

1. `continuum_episodes_schema.sql`
2. `continuum_work_orders_screenplay_schema.sql`
3. `continuum_cave_saurce_schema.sql` (legal cases, feature gates)
4. `continuum_stories_schema.sql` (auto-applied on first API boot via `story_db.ensure_stories_schema`)

## Story workflow

`new` → `grooming` → `in_progress` → `in_review` → `submitted` → `completed`

- Stories **do not reopen** after `completed` (HTTP 409).
- Status regression is rejected.
- Transition to `submitted` / `completed` requires all linked work orders `done` and passing causality validation (HTTP 422 + `buildErrors`).

## Work orders

- Link to stories via `POST /api/stories/<id>/work-orders`.
- Asset kinds: `continuum`, `unity`, `legal`, `usc`, `spatial_4d`, `lemma`, `prefab` (`asset_ref_json`).
- Causality test: `POST /api/work-orders/<id>/run-causality-test`.
- Generate from causality: `POST /api/episodes/<episode_id>/generate-causality-work-orders`.
- SQL Viewer recipes: `stories_by_status`, `work_orders_with_assets`, `story_schedule_crossref`, `causality_structure_for_episode`.

## resaurce Cave routes

| Route | Purpose |
|-------|---------|
| `production/budget/create` | Create budget plan + water level |
| `production/budget/journal/post` | Post double-entry line |
| `production/budget/journal/from-saurce` | Mirror Saurce ledger |
| `production/budget/water-level` | Current reservoir level + alerts |
| `production/budget/allocate-story` | Reserve water for story |
| `production/schedule/create` | Create schedule + milestones |
| `chat/room/ensure-for-story` | Idempotent story chat room |
| `chat/room/sync-members` | Sync assignees/watchers |

Continuum proxies: `/api/production/*`, `/api/chat/*`.

## Chat in Continuum header

Toggle **Chat** in `continuum-nav` (persists `localStorage.continuumChatOpen`). Story rooms auto-created on story POST and when assignees/watchers change.

## UI

| URL | Purpose |
|-----|---------|
| `/story-board` | Kanban + story detail, WO assets, GitHub/Jira fields |
| `/project-calendar` | Calendar + narrative overlay + milestones |
| `/budget-dashboard` | Water-level gauge + journal |
| `/legal-tracker` | Open legal cases for agile |

## Calendar sync (cron)

```bash
cd Scripts
set CONTINUUM_DB=path\to\continuum.db
python continuum_api/scripts/calendar_sync.py --all
python continuum_api/scripts/calendar_sync.py --provider ical --output-dir ./calendar_export
python continuum_api/scripts/calendar_sync.py --provider google
python continuum_api/scripts/calendar_sync.py --provider outlook
```

## Google Sheets publish (cron / on-demand)

```bash
cd Scripts
set CONTINUUM_DB=path\to\continuum.db
set GOOGLE_SHEETS_CREDENTIALS=path\to\service-account.json
set GOOGLE_SHEETS_SPREADSHEET_ID=your-sheet-id
python continuum_api/scripts/sheets_publish.py --budget-plan budget_xxx --all-linked
python continuum_api/scripts/sheets_publish.py --budget-plan budget_xxx --dry-run
```

API: `POST /api/production/budget/<id>/publish-sheets`

Without credentials, exports JSON tabs to `SHEETS_EXPORT_DIR` (default `./sheets_export`).

### Windows Task Scheduler

- Every 15 min: `calendar_sync.py --all`
- Hourly: `sheets_publish.py --budget-plan <id> --all-linked`

### Linux cron

```
*/15 * * * * cd /path/Scripts && CONTINUUM_DB=/path/continuum.db python continuum_api/scripts/calendar_sync.py --all
0 * * * * cd /path/Scripts && python continuum_api/scripts/sheets_publish.py --budget-plan budget_xxx --all-linked
```

API: `POST /api/calendar/sync-now`, `GET/POST /api/calendar/subscriptions`.

## Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `RESAURCE_CAVE_URL` | `http://127.0.0.1:3456` | Continuum → resaurce proxy |
| `RESAURCE_DATA_DIR` | `resaurce/data` | Budget/schedule/chat persistence |
| `CONTINUUM_DB` | `Scripts/continuum.db` | Stories + work orders |
| `GOOGLE_SHEETS_CREDENTIALS` | — | Service account JSON for Sheets/Calendar |
| `GOOGLE_SHEETS_SPREADSHEET_ID` | — | Target spreadsheet |
| `MS_GRAPH_TOKEN` | — | Outlook calendar sync |
| `GOOGLE_CALENDAR_CREDENTIALS` | — | Google Calendar sync |

## Legal agile tracker

On story/work-order save and `in_progress` transition, continuum checks `legal_cases` and `platform_feature_gates`. Critical collisions block `in_progress`. UI at `/legal-tracker`.
