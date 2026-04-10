# Episodic Content Generator

Pipeline for creating, parsing, and outputting episodic narrative content for the Spatial 4D Generator in Unity. Uses the **Continuum** DB (episodes, episode_script, work_orders, causality) and **USC (Unified Semantic Compressor)** for assets.

---

## What is our input?

- **Episodes** — time windows, scene/engine, plot description, and optional script reference
- **Episode script** — raw script text or a reference to stored content (document_blobs, semantic_chunks)
- **USC assets** — library documents, semantic chunks, or unique kernels linked to episodes via `episode_assets`
- **Thesaurus** — languages, entries, alternatives, and AST nodes for translation and reordering

Apply schemas in order: `continuum_episodes_schema.sql`, `continuum_spatial_4d_schema.sql` (4D volumes + gateway leaf triplet + optional history for Continuum Explorer), `continuum_thesaurus_schema.sql`, `continuum_screenplay_schema.sql`, `continuum_thesaurus_version_schema.sql`, `continuum_draft_schema.sql`, `continuum_dictionary_schema.sql`, `continuum_audit_schema.sql`. See [CONTINUUM_AND_COMPRESSOR.md](CONTINUUM_AND_COMPRESSOR.md).

---

## How do I add an episode?

**Current state:** Episode creation is not fully automated. Use one of these:

1. **Direct SQL** — Insert into `episodes` and `episode_script`:
   ```sql
   INSERT INTO episodes (id, tenant_id, title, created_at, engine, scene_path, t_start, t_end, plot_description)
   VALUES ('ep-001', 'default', 'Episode 1', datetime('now'), 'unity', 'Assets/Scenes/Episode1.unity', 0, 3600, '...');

   INSERT INTO episode_script (id, episode_id, script_text, language, created_at)
   VALUES ('script-ep-001', 'ep-001', 'Your script text here...', 'en', datetime('now'));
   ```

2. **Unity** — **Window → Continuum → Continuum Episodes**. Set DB path, then use "Browse Episodes" to explore. "New Episode" is a stub; creation via USC API or direct SQLite is planned.

3. **USC / continuum repo** — If you use the continuum library server and migrations, episodes can be created via that pipeline.

---

## How do I upload a USC asset for episodic coverage?

USC (Unified Semantic Compressor) lives in a separate repo. Assets are stored in `library_documents`, `semantic_chunks`, or `unique_kernels`.

1. **Continuum Library Server** — From the continuum repo: `python serve_library.py`. Open http://localhost:5050 to upload and manage assets.
2. **Install USC** — `pip install -e /path/to/unified-semantic-compressor`
3. **Link to episodes** — Insert into `episode_assets`:
   ```sql
   INSERT INTO episode_assets (id, episode_id, usc_asset_id, asset_type, role, causality_leaf_id)
   VALUES ('ea-001', 'ep-001', '<document_id>', 'document', 'causality_source', NULL);
   ```
   `asset_type`: `'document'`, `'chunk'`, or `'kernel'`. `causality_leaf_id` links to a quad/oct tree leaf (e.g. `S3.O2.1.7`).

4. **Unity** — **Window → Continuum → Continuum Library** for search; **Continuum Explorer** to browse `library_documents` and `episode_assets`. Tenancy is scoped by `Scripts/continuum_tenant.txt` or the Tenant field.

---

## How do I parse an episode to create a script?

Parsing builds the **thesaurus AST** (tokens, quote blocks, Farey intervals) from raw script text.

1. **Ensure episode_script exists** with `script_text` (or `script_ref` for USC-stored content).
2. **Build AST** via API:
   ```bash
   curl -X POST http://localhost:5050/api/thesaurus/build-ast \
     -H "Content-Type: application/json" \
     -d '{"episodeScriptId": "script-ep-001", "scriptText": "optional override", "languageId": "en"}'
   ```
   Or from the **script-output** app: Load script by episode ID, then use Build AST (if exposed in UI).
3. **Result** — `thesaurus_ast_nodes` is populated with tokens and quote blocks; `node_kind` is `'token'` or `'quote'`.

---

## How do I output episodic content to the Spatial 4D Generator for Unity?

The Spatial 4D system uses **(x, y, z, t)** volumes: 3D regions plus narrative time windows.

1. **Calendar events** — `NarrativeCalendarAsset` holds events with optional `spatiotemporalVolume` (Bounds4).
2. **Narrative4DPlacer** — Inserts event volumes into `SpatialGenerator4D`. Assign calendar and 4D generator (or resolve from `SpatialGenerator4DOrchestrator`).
3. **Export to file** — Use `Spatial4DExportUtility` or the in-game Spatial 4D editor to export to JSON/YAML/XML (`Spatial4DExpressionsDto`). Configure path in `SpatialGenerator4DOrchestrator` (e.g. `inGameUIOutputFilePath`).
4. **Continuum → Unity** — The `spatial_4d` table (see `continuum_spatial_4d_schema.sql`) holds Bounds4-style fields plus **causality_leaf_back / _pause / _forward** (gateway termini aligned with `SpatialGenerator4D`) and optional **causality_history_json** (append-only rows). Browse via **Continuum Explorer**. ETL from continuum (work_orders, causality_structure, episodes) into `NarrativeCalendarAsset` events is pipeline-specific; the schema supports `episodes.t_start`, `episodes.t_end`, `episodes.scene_path`, and `work_orders.causality_leaf_id` for mapping.

See [Assets/BedogaGenerator/SpatialGenerator4D_Setup.md](../Assets/BedogaGenerator/SpatialGenerator4D_Setup.md).

---

## How do I see a selection of episodes?

- **Unity** — **Window → Continuum → Continuum Episodes** → "Browse Episodes (SELECT * FROM episodes)". Opens Continuum Explorer with that query.
- **Unity** — **Window → Continuum → Continuum Explorer** → Table: `episodes` (or `episode_script`, `episode_assets`).
- **API** — There is no `/api/episodes` list endpoint yet. You need the episode ID from the DB or Explorer.

---

## Is there a way to go from episodes to a tool using that episode?

**Not yet.** You must **copy/paste the episode ID** into each tool:

- **Script-output app** — Enter episode ID, click Load script.
- **Continuum Work Orders** — Set DB path; filter by episode (work orders are keyed by `episode_id`). No click-through from episode list to Work Orders with that episode pre-selected.
- **Continuum Explorer** — Run `SELECT * FROM work_orders WHERE episode_id = 'ep-001'` etc.

A future improvement: episode picker or deep links (e.g. `?episodeId=ep-001`) so tools open with the episode pre-selected.

---

## Do we have a base-level System Drawer / Continuum / USC index page?

**No.** Today:

- **Script-output** — http://localhost:5174 (React app; enter episode ID)
- **Continuum API** — http://localhost:5050 (Flask; used by script-output via proxy)
- **Continuum Library Server** — http://localhost:5050 (from continuum repo; separate from system-drawer’s continuum API when both run)
- **Unity** — Window → Continuum → Continuum Explorer / Episodes / Library / Work Orders

There is no unified index page linking episodes, script-output, work orders, and USC. A single hub (e.g. `/` or `/continuum`) listing episodes and linking to tools would improve discoverability.

---

## New APIs (episodic completion)

- `GET /api/episodes` – list episodes (tenant_id, limit, offset, engine, scene_path)
- `POST /api/episodes` – create episode
- `POST /api/episode-script` – create episode_script
- `POST /api/episode-assets` – link USC asset to episode
- `GET /api/drafts/episodes`, `POST /api/drafts/episodes`, `PATCH`, `POST .../publish`, `GET/PUT .../script` – draft workflow
- `GET /api/thesaurus/language-audit` – discrepancies (missing translations, version mismatch, no EFIGS fallback)
- `GET /api/thesaurus/definition` – dictionary tooltip with EFIGS fallback
- `GET /api/audit` – audit log (contractors: own rows; admins: all)
- `POST /api/deeplink` – write deeplink file for Unity DeepLinkHandler

## Quick reference: tools and run commands

| Tool | How to run | Purpose |
|------|------------|---------|
| Continuum API | `cd Scripts && CONTINUUM_DB=... python -m continuum_api.server --port 5050` | Episode script, thesaurus, AST, screenplay, work orders, drafts, audit |
| Continuum UI | http://localhost:5050/ or http://localhost:5050/ui | Web UI: create episode, list, link to script-output |
| Script-output | `cd apps/script-output && npm run dev` | React UI: load script by episode ID, click words, reorder, change-of-basis |
| Continuum Explorer | Unity: Window → Continuum → Continuum Explorer | Browse DB tables, run SQL |
| Continuum Episodes | Unity: Window → Continuum → Continuum Episodes | Browse episodes, episode_assets |
| Continuum Work Orders | Unity: Window → Continuum → Continuum Work Orders | Browse/filter work orders |
| Continuum Library | Unity: Window → Continuum → Continuum Library | Search USC assets by location |
