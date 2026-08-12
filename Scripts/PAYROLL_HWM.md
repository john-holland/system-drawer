# Continuuuum company payroll (HWM %, seats, retainers)

Company **income allocation** for Resaurce-linked products — not employee W-2 payroll, not film credits, and not production budget “water level.”

## Naming

| Term | Meaning |
|------|---------|
| **Company HWM** | Cumulative net income posted through Continuuuum payroll. Default `$100,000`, overridable per company. |
| **HWM retainer %** | Company skim (`hwm_retainer_pct`, default `0.10`) applied only to the **over-HWM** portion of each income event. Under HWM is free (100% ops). |
| **Service budget** | Monthly Unity + Cursor seat cost derived from team tags and lifetime net. |
| **Production water level** | Resaurce pond metaphor (`water_level_usd` / `capacity_usd`) for story budgets. Unrelated field. |

## Income phases

**Pre-HWM** (`lifetime_net_usd` &lt; HWM): **free** — 100% of the event goes to **company ops** (no HWM retainer skim).

**Post-HWM**: skim `hwm_retainer_pct` (default **10%**) of the event into the **company** retainer bucket; the remainder goes to **company ops**.

**Crossing:** if one event straddles the mark, under-mark dollars stay free → ops; over-mark dollars take the HWM retainer skim, rest → ops.

**Migration `free_until_100k_v1`** (pre-prod, one-shot via `payroll_meta`): sets every company to `$100,000` / `10%`, clears income/allocation/draw history booked under the old 12%-pre-HWM model, resets balances and lifetime to zero, and drops legacy `payroll_saving_indexes` / `payroll_post_hwm_shares`. Team members and retainer definitions are kept. After migration, admin may override HWM/% per company as usual.

Retainer draws (`POST .../retainer/draw`) move company retainer → company ops for transition / budget coverage.

## Team roster & seats

Team members live in Continuuuum payroll (`payroll_team_members`). Resaurce HR stays mock.

- **`gameplay` and `technical`** are independent booleans on every role (designer, engineer, or other). Not gated by job title.
- Soft create defaults only: new designer → `gameplay=1`; new engineer → `technical=1`; other → both off unless set.

**Unity seats** = count of active members with `gameplay=1`, priced by lifetime net band:

| Lifetime net | Band | $/seat/month |
|--------------|------|--------------|
| &lt; `$200,000` | free | `$0` |
| `$200,000`–`$25,000,000` | pro | `$210` |
| ≥ `$25,000,000` | enterprise | custom (`unity_enterprise_override_usd`) |

At enterprise, the API/UI show: *“Lifetime at or above $25,000,000 — contact Unity Finance and Business departments for custom pricing.”* (`unityEnterpriseContactLabel`).

**Cursor seats** = count of active members with `technical=1` × `$40`/user/month.

Auto-association: gameplay members fill the Unity service retainer user list; technical members fill the Cursor service retainer.

## Retainers

| Kind | Role |
|------|------|
| `service_unity` / `service_cursor` | Auto-maintained monthly `fixed_cron` retainers; amounts sync from service budget |
| `custom` | `%` of income (applied on each income post, after HWM skim) or `$` on a cron; optional forward company + user list |
| `hwm_pct` | Informational / system; income skim uses company `hwm_retainer_pct` on post-HWM dollars only |

`tick_retainers` (wired into the Continuuuum submit-cron loop) accrues due `fixed_cron` retainers into company retainer (or forward company), idempotent per fire window.

### Cron plain English

Shared SPA helper [`static/shared/cron/cron-humanize.js`](continuuuum_api/static/shared/cron/cron-humanize.js) (`CronHumanize` / `PayrollCronHumanize` alias). Also wired into transit, airplanes, staff hours, restaurants, stations, and project-calendar. Unity twin: `CronHumanize` + `[CronExpr]` Inspector drawer.

- **5-field cron**; compound schedules with `;` (or newlines). Narrative joins with “and”; fire counts **sum** (e.g. every 15 hours + weekly Monday 08:00 → 48 + 4 = **52** on an avg 30-day month). Two pure intervals on the same clock combine via **GCD** of periods.
- **Money mode** (`amountUsd > 0`): `$40 per month and every second Tuesday ($80 / month avg 30 day)`.
- **Occurrence mode** (no amount): `every 15 hours and once a week on Monday at 08:00 (52 occurrences per month avg 30 day)`.
- **Hours windows** (e.g. `* 6-22 * * 1-5`): plain English + “(active hours window)” (no occurrence count).
- **Month lenses**: payroll UI can select Avg 30 (default), 28, 29, or 31; totals row always shows all four.
- Example chips in the retainer editor cover monthly, nth weekday, weekly@time, every N hours, daily, and compound presets.

## Admin edits (HWM & retainer amounts)

Changing **HWM USD**, **HWM retainer %**, or a retainer’s **amountUsd / percent** requires `X-Admin: 1` (Continuuuum Dev panel → **admin** preset). Non-admin PATCH that includes those fields returns `403`. Other company fields (name, Unity enterprise override) remain open. The payroll SPA disables HWM fields and per-retainer Save controls until admin mode is on.

Admin **Save $** on `service_unity` / `service_cursor` sets `amount_locked` so seat-band sync does not wipe the override back to `$0` (free band). **Use seat formula** clears the lock and recalculates from gameplay/technical seats.

## Surfaces

- Schema: `Scripts/continuuuum_payroll_schema.sql`
- Engine: `continuuuum_api/payroll_engine.py`
- API + SPA: `/api/payroll/*`, `/payroll`
- Events feed (`GET .../events`): income posts plus retainer accruals; retainer allocations include `retainerName` (HWM skim → `"HWM retainer"`; cron runs → the retainer’s display name)
- Saurce hook: income ledger types (`preorder_deposit`, `preorder_backer_stake`, `investment`, `foundation_allocation`) call `maybe_post_income_for_product` when `payroll_companies.saurce_product_id` matches
- Resaurce mirror: `SAURCE_ENTRY_MAP` entries `payroll_retainer_accrual`, `payroll_ops_retain`, `payroll_distribution`, `payroll_retainer_draw`
