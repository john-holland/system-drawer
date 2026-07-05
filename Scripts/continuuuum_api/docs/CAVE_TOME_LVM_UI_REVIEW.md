# Cave → Tome → LVM UI Review

Architecture review of Continuuuum, resaurce, and log-view-machine UI mechanisms.  
**Date:** 2026-06-30 · **Scope:** full stack (Continuuuum + resaurce + log-view-machine canonical model)

Related: [CAVE_TOME_LVM_RFC.md](./CAVE_TOME_LVM_RFC.md) (policy ADR)

---

## 1. UI mechanisms inventory

### 1.1 Continuuuum static surfaces

| Surface | Route | Stack | Nav app | User session | Tome bootstrap | Data path | State | Trace |
|---------|-------|-------|---------|--------------|----------------|-----------|-------|-------|
| **Library** | `/library` | Vanilla + Leaflet/D3 | `library` / `import` | Late load (after main IIFE) | `library-tome` | Direct `/api/library/*`, `/api/spatial/*` | Module `let` vars | `api_audit_log` only |
| **USC Import** | `/library?panel=upload` | Same | `import` | Same | Same | `/api/library/upload` | Same | Same |
| **Lemma Library** | `/lemma-library` | Vanilla + shared widgets | `lemma` + hash subnav | `ContinuuuumUserSession` | `lemma-tome` | Direct `/api/thesaurus/*` | `browseItems`, hash router | Audit only |
| **Episodic hub** | `/ui` | Vanilla + tome bootstrap | `hub` + hash subnav | Session | `hub-tome` | Direct `/api/episodes`, `/api/drafts`, `/api/reviews` | Hash sections | Audit only |
| **Script Output** | `/script-output` (Vite `:5174`) | Vanilla (Vite host) | `hub` (mislabeled) | Session | None | `ContinuuuumScriptOutput.api()` → `/api/*` | `_state` object | Audit only |
| **Story Board** | `/story-board` | Vanilla | `story-board` | **None** | **None** | `/api/stories`, `/api/work-orders` | Locals + modal | None |
| **Project Calendar** | `/project-calendar` | Vanilla | `project-calendar` | **None** | **None** | `/api/production/*`, `/api/stories`, `/api/calendar/*` | `state` object | None |
| **Budget Dashboard** | `/budget-dashboard` | Vanilla | `budget-dashboard` | **None** | **None** | `/api/production/budget/*` | DOM-only | None |
| **Legal Tracker** | `/legal-tracker` | Vanilla | `legal-tracker` | Raw `localStorage` | **None** | `/api/legal/*` | Modal + case state | None |
| **Network Definitions** | `/network-definitions` | Vanilla | `network` | Optional | `network-tome` | `lemmaApiBase` + `/api/telecom/*` | Tab `active` id | Audit only |
| **Society Dashboard** | `/society-dashboard` | Vanilla | `society` | Optional | `society-tome` | `/api/society/*` | Render-on-load | Audit only |
| **City Config** | `/city-config` | Vanilla + spatial map | `cities` | Optional | `society-tome` (shared) | PATCH `/api/society/cities/*` | Debounced save timer | Audit only |
| **Camera Pathing** | `/camera-pathing` | Vanilla | `camera` | Session | `camera-tome` | `/api/camera/*` | `activeSceneId` | Audit only |
| **Table Read** | `/table-read` | Vanilla + Socket.IO | `table-read` | Session | `table-read-tome` | **Hybrid:** REST + tome messages + socket | `state` in `continuuuum-table-read.js` | Partial |
| **SQL Viewer** | `/sql-viewer` | Vanilla + tome class | `sql-viewer` | Session (required) | `sql-viewer-tome` | **All via** `robotCopy.sendMessage` | `SqlViewerTome.model` | Audit + user gate |
| **Mayor Dog Mods** | `/mayor-dog-mods` | Vanilla | **Custom header** | Manual user input | **None** (YAML exists) | `/api/mods/*` | `view`, `registry`, `loadload` | None |

**Agile apps not in `cave.yaml` childCaves:** story-board, project-calendar, budget-dashboard, legal-tracker, mayor-dog-mods.

### 1.2 Edge surfaces

| Surface | Location | Stack | Nav | Data path | Tier (see §4) |
|---------|----------|-------|-----|-----------|---------------|
| **Unity Script Editor** | `Assets/Continuuuum/Editor/ScriptEditor/WebView/` | Vanilla + Unity bridge | None | C# `ContinuuuumEditorApiClient` | D |
| **Continuuuum WebGL editor** | `continuuuum/library/continuuuum_editor_webgl/` | Unity WebGL | Unity | `?apiBase=` | D |
| **resaurce chat-remote** | `D:/Development/resaurce/chat-remote/` | React 18 + Module Federation | Host-provided | `/api/chat/*` or host `cavePost` | A |
| **inventory frontend** | `D:/Development/inventory/frontend/` | React + pact tests | App shell | `POST /cave/route` (resaurce/saurce) | A |

### 1.3 Shared infrastructure

| Module | Path | Role |
|--------|------|------|
| `continuuuum-nav.js` | `static/shared/continuuuum-nav/` | Cross-app header, chat toggle, dev user switcher |
| `continuuuum-user-session.js` | `static/shared/continuuuum-user-session/` | `X-User-ID`, `X-Admin` headers |
| `continuuuum-cave-shell.js` | `static/shared/continuuuum-cave-shell/` | RobotCopy, presence, preorder gate |
| `continuuuum-tome-bootstrap.js` | same | Thin wrapper: `init({ tomeId })` + optional `onReady` |
| Shared widgets | `continuuuum-script-editor`, `continuuuum-lemma-*`, `continuuuum-table-read`, `continuuuum-chat-panel` | Reused across hub, lemma, table-read, script-output |

### 1.4 Cross-cutting issues

1. **`detectApp()` gaps** — `budget-dashboard` and `legal-tracker` not auto-detected (pages pass `app` explicitly today).
2. **~10 duplicated `fetch`/`api()` wrappers** — no shared `ContinuuuumApiClient`.
3. **Tome cargo-cult** — 8+ pages mount `ContinuuuumTomeBootstrap` but never call `RobotCopy` (presence/gate side effects only).
4. **Dead tome wiring** — YAML tomes with no `_dispatch_tome_message` handler and no client `sendMessage`:

| Tome ID | In `cave.yaml` | In `_dispatch_tome_message` | Client uses RobotCopy |
|---------|----------------|----------------------------|------------------------|
| `library-tome` | Yes | No | No |
| `hub-tome` | Yes | No | No |
| `lemma-tome` | Yes | Partial (`browseMachine` only) | No |
| `network-tome` | Yes | No | No |
| `society-tome` | Yes | No | No |
| `camera-tome` | Yes | No | No |
| `features-tome` | Yes | No | No |
| `mayor-dog-mods-tome` | YAML only | No | No |
| `sql-viewer-tome` | Yes | Yes | **Yes** |
| `table-read-tome` | Yes | Yes | **Partial** |
| `saurce-tome` | No (robit only) | Yes | No |
| `resaurce-tome` | No | Partial | No |
| `drawer-game-tome` | Yes | Yes | API stub only |

---

## 2. Gap analysis: three layers

### 2.1 Canonical model (log-view-machine)

From `D:/Development/log-view-machine/docs/ARCHITECTURE_AND_CAVE.md`:

```
Cave (spelunk route tree)
  → Tome (machines + RobotCopy)
    → ViewStateMachine (XState + render())
      → LVM tracing / observeViewKey hooks
```

Key APIs: `getRenderTarget(path)`, `getTransportForTarget`, `useCave` / `useTome` / `useViewStateMachineInstance`.

### 2.2 Capability matrix

| Capability | log-view-machine | resaurce | Continuuuum (Drawer 2) |
|------------|------------------|----------|----------------------|
| Declarative route tree | Spelunk TS factory | `cave.manifest.yaml` + domain YAML in `tomes/` | `cave/cave.yaml` |
| Client transport | RobotCopy / CaveRobit | `POST /cave/route` envelope v2 | `POST /api/tomes/{id}/machines/{m}/message` |
| View binding | `ViewStateMachine.render()` | Manifest `views` + `lvm.machines` map | None in static JS |
| State machines | XState in package | XState route registry (`resaurceRouteRegistry`) | None (string state in sql-viewer only) |
| Audit / trace | Package tracing | LVM2.0 `appendStore` via `caveRouteHooks` | `api_audit_log` + `user_presence` |
| Config → code | Adapters (node-example) | `manifestLoader.js` | **Manual** `_dispatch_tome_message()` in `cave_routes.py` |
| Federation | Cave childCaves / hosted apps | UI tome + Module Federation | Not implemented |
| Single-server target | Documented | Separate `:3456` Cave | Flask `:5050` (see `CONTINUUUUM_SINGLE_SERVER.md`) |

### 2.3 Dual Cave problem

```mermaid
flowchart TB
  subgraph continuuuum [Continuuuum Flask :5050]
    Nav[continuuuum-nav static apps]
    TomeMsg["POST /api/tomes/.../message"]
    Dispatch[_dispatch_tome_message manual map]
    REST["GET/POST /api/*"]
  end
  subgraph resaurce [resaurce Node :3456]
    CaveRoute["POST /cave/route"]
    Manifest[cave.manifest.yaml]
    LVM2[LVM2 appendStore]
    XState[resaurceRouteRegistry]
  end
  subgraph lvm_pkg [log-view-machine npm]
    VSM[ViewStateMachine React]
    CaveTS[Cave factory TS]
  end
  Nav --> REST
  Nav --> TomeMsg
  TomeMsg --> Dispatch --> REST
  CaveRoute --> Manifest --> XState --> LVM2
  CaveTS -.->|"reference only"| continuuuum
  VSM -.->|"inventory MF future"| resaurce
```

**Convergence gap:** Continuuuum YAML tomes describe routing that Python does not enforce. Resaurce manifest is authoritative for domain + LVM2. log-view-machine is not bundled in any Continuuuum static page.

### 2.4 `_dispatch_tome_message` drift risk

File: `Scripts/continuuuum_api/cave_routes.py` (lines 114–241).

- Hand-maintained branches for 6 tome IDs.
- Returns `{ ack: true }` for unknown machine/event pairs — **silent no-op** masks config drift.
- Tome YAML files under `cave/tomes/` are loaded for `/api/tomes` listing but **not** used for dispatch.

---

## 3. Pattern deep-dives

### Pattern A — Plain REST (story-board, legal-tracker, budget)

| Aspect | Detail |
|--------|--------|
| Request path | Browser → `fetch('/api/stories')` → Flask route → SQLite |
| State ownership | Page IIFE locals / small `state` object |
| Testability | High — pytest + curl; no envelope |
| Pros | Fast to build; Network tab friendly; no dispatch table |
| Cons | No `trace_id`; inconsistent `X-User-ID`; duplicated fetch helpers; no editor presence |

### Pattern B — Tome messages without VSM (sql-viewer)

| Aspect | Detail |
|--------|--------|
| Request path | Browser → `robotCopy.sendMessage(tome, machine, event)` → `/api/tomes/.../message` → `_dispatch_tome_message` → internal Flask `test_client` → REST handler |
| State ownership | `ContinuuuumSqlViewerTome` class (`model`, `state` string) |
| Testability | Medium — must test message endpoint + dispatch branches |
| Pros | Uniform client boundary; tomeId in YAML; admin gate centralized |
| Cons | Extra hop; manual dispatch; YAML not source of truth |

### Pattern C — resaurce Cave + LVM2 (production proxy, inventory pacts)

| Aspect | Detail |
|--------|--------|
| Request path | Client → `POST /cave/route` `{ route, trace_id, payload }` → `router.js` → XState → domain handler → `caveRouteHooks` → LVM2 append |
| State ownership | Server-side machines; UI is thin |
| Testability | High for contracts — pact tests in inventory |
| Pros | Manifest-driven; trace loop; domain tome `lvm_events`; pact-friendly |
| Cons | Separate service; envelope learning curve; not wired to most Continuuuum pages |

### Pattern D — Full log-view-machine (target)

| Aspect | Detail |
|--------|--------|
| Request path | React → hooks → RobotCopy → CaveRobit transport → Tome machines → VSM `render()` |
| State ownership | ViewStateMachine + XState |
| Testability | Machine unit tests + component tests |
| Pros | Single render contract; codegen potential; matches long-term docs |
| Cons | React required; high migration cost for 15+ vanilla SPAs |

**Review question applied:** Moving story-board from D→C (add nav + session) pays off in one cycle. Moving story-board D→B does not. Table-read B→single-path (REST *or* tome, not both) pays off in one cycle.

---

## 4. Scored apps and tier assignments

**Scoring:** 1–5 per criterion (7 criteria, max 35). Thresholds: ≥18 → Tier A/B; 10–17 → Tier C; &lt;10 → Tier D.

| Criterion | 1 = favors REST | 5 = favors Cave/Tome/LVM |
|-----------|-----------------|---------------------------|
| Workflow complexity | Simple CRUD | Multi-step branching |
| Realtime / collab | Read-only | Live session / chat |
| Trace / audit | Internal low-stakes | Pact / legal / LVM2 |
| Cross-service | Same-origin only | Federation / resaurce proxy |
| UI framework fit | Vanilla / Unity | React / MF host |
| Migration cost | Stable app (inverse) | Greenfield / already on tome |
| Config drift risk | Few endpoints | Growing machine map |

### 4.1 Representative scores

| App | Wf | RT | Tr | XS | UI | Mig | Dr | **Total** | **Tier** |
|-----|----|----|----|----|----|-----|-----|-----------|----------|
| SQL Viewer | 4 | 2 | 4 | 2 | 3 | 5 | 5 | **25** | **B** |
| Table Read | 5 | 5 | 4 | 3 | 3 | 4 | 4 | **28** | **B** |
| chat-remote (resaurce) | 3 | 5 | 4 | 5 | 5 | 3 | 4 | **29** | **A** |
| inventory surfaces | 4 | 3 | 5 | 5 | 5 | 2 | 5 | **29** | **A** |
| Lemma Library | 4 | 2 | 3 | 2 | 4 | 4 | 3 | **22** | **C** |
| Episodic hub (`/ui`) | 4 | 3 | 3 | 2 | 4 | 4 | 2 | **22** | **C** |
| Story Board | 3 | 2 | 2 | 1 | 4 | 5 | 2 | **19** | **C** |
| Legal Tracker | 3 | 1 | 4 | 2 | 4 | 5 | 2 | **21** | **C** |
| Library / USC Import | 3 | 1 | 2 | 3 | 4 | 5 | 2 | **20** | **C** |
| Project Calendar | 3 | 2 | 2 | 3 | 4 | 5 | 2 | **21** | **C** |
| Budget Dashboard | 2 | 1 | 2 | 2 | 4 | 5 | 1 | **17** | **C** |
| Mayor Dog Mods | 2 | 1 | 1 | 1 | 3 | 5 | 1 | **14** | **D** |
| Unity Script Editor | 4 | 2 | 3 | 1 | 1 | 5 | 1 | **17** | **D** |

### 4.2 Full tier map (all surfaces)

| Tier | Surfaces |
|------|----------|
| **A** — Cave + Tome + VSM / MF | resaurce `chat-remote`, inventory federation modules (tax, legal, wallet per manifest), **new** React workflows |
| **B** — Cave shell + Tome machines | SQL Viewer, Table Read (session/chat orchestration), resaurce-tome/saurce-tome API adapters, drawer-game stub |
| **C** — Shared shell + REST | Library, Lemma Library, Episodic hub, Script Output, Story Board, Project Calendar, Budget, Legal, Network, Society, City Config, Camera |
| **D** — Plain REST at edge | Unity WebView editor, Mayor Dog Mods portal, internal scripts |

---

## 5. Review session conclusions

_Simulated 90-minute review outcomes (2026-06-30)._

### 5.1 Agreed recommendation (summary)

**Cave→Tome→LVM is recommended for trace-sensitive, multi-step, cross-service flows — not as a blanket wrapper around every Continuuuum vanilla SPA.**

- Adopt **Tier A** for new React/MF and inventory federation.
- Keep **Tier B** for sql-viewer and table-read; fix hybrid duplication on table-read.
- Standardize **Tier C** baseline for all production Continuuuum agile/editor UIs.
- Leave **Tier D** at Unity and one-off portals.

### 5.2 Stop / start list

| Stop | Start |
|------|-------|
| Mounting `ContinuuuumTomeBootstrap` without any `RobotCopy` usage | Tier C: `ContinuuuumNav` + `ContinuuuumUserSession` only |
| Growing `_dispatch_tome_message` by hand | Tome YAML → dispatch registration (Spike 1) |
| Per-app `fetch` wrappers | Shared `ContinuuuumApiClient` (Spike 2) |
| Dual chat paths in table-read (REST + tome for same concern) | Single orchestration path (Spike 3) |
| Linking library to `:5051` in localStorage | Same-origin `/library` (already patched in nav) |

### 5.3 Approved implementation spikes

| Spike | Scope | Success criteria |
|-------|-------|------------------|
| **Spike 1: Tome dispatch codegen** | One tome (`sql-viewer-tome`) | YAML routes generate or register dispatch; remove hand branch; tests for each machine/event |
| **Spike 2: Tier C baseline** | story-board + legal-tracker | Nav + session on both; `X-User-ID` on all mutations; `detectApp()` fixed |
| **Spike 3: Table-read path unify** | `continuuuum-table-read.js` | Session open + chat use either REST or tome, not both; document chosen path in RFC |

### 5.4 Open questions (defer to follow-up)

1. LVM2 bridge on Continuuuum Flask mutations — local append vs forward to resaurce `:3456`?
2. When to merge resaurce Cave into single-server `:5050` per `CONTINUUUUM_SINGLE_SERVER.md`?
3. Bundle log-view-machine in Continuuuum for one greenfield Tier A pilot?

---

## 6. Consolidation backlog (ranked by ROI)

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| P0 | Shared `ContinuuuumApiClient` | S | Removes duplication; enables trace headers everywhere |
| P0 | Tier C baseline on agile apps (story, legal, calendar, budget) | S | Consistent identity + audit |
| P1 | Tome dispatch from YAML | M | Stops config drift; unblocks Tier B growth |
| P1 | Remove cargo-cult tome bootstrap on Tier C pages | S | Less confusion; faster page load |
| P1 | Add agile routes to `cave.yaml` OR document intentional omission | S | Config matches reality |
| P2 | Table-read single orchestration path | M | Lower maintenance |
| P2 | LVM2 bridge for Tier B Continuuuum mutations | L | Pact/trace parity with resaurce |
| P2 | log-view-machine pilot (one MF surface) | L | Validates Tier A path |
| P3 | Full single-server merge (Continuuuum + serve_library + resaurce proxy) | XL | Ops simplicity |

---

## Appendix A — File reference

| Area | Path |
|------|------|
| Cave config | `Scripts/continuuuum_api/cave/cave.yaml` |
| Tome YAML | `Scripts/continuuuum_api/cave/tomes/*.yaml` |
| CaveRobit | `Scripts/continuuuum_api/cave/cave-robit.yaml` |
| Loader | `Scripts/continuuuum_api/cave_loader.py` |
| Dispatch | `Scripts/continuuuum_api/cave_routes.py` |
| Nav | `Scripts/continuuuum_api/static/shared/continuuuum-nav/continuuuum-nav.js` |
| Shell | `Scripts/continuuuum_api/static/shared/continuuuum-cave-shell/` |
| resaurce manifest | `D:/Development/resaurce/cave.manifest.yaml` |
| LVM2 hooks | `D:/Development/resaurce/src/lvm/caveRouteHooks.js` |
| LVM architecture | `D:/Development/log-view-machine/docs/ARCHITECTURE_AND_CAVE.md` |
| Single-server target | `continuuuum/CONTINUUUUM_SINGLE_SERVER.md` |
