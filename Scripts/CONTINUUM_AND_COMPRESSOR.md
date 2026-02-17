# Continuum and Unified Semantic Compressor

The continuum DB and semantic archiver have been moved into separate repositories:

- **USC (Unified Semantic Compressor)** — repo `unified-semantic-compressor`; Python package `unified_semantic_archiver`.  
  Core library: compressors (video, audio, library, data), continuum DB schema, ETL, research feed, CLI (query_db).  
  Install: `pip install -e /path/to/unified-semantic-compressor`

- **Continuum** — `C:\Users\John\continuum`  
  Library server (web UI + API): upload, search, map, geocode. Depends on unified-semantic-compressor.  
  Run: install compressor, then `python serve_library.py` from the continuum repo.  
  Open http://localhost:5050.

## Tenant per game (SaaS and local dev)

Each game or Unity project should use a **continuum tenant** so library data is scoped per team. The server and CLI filter all library_documents by `tenant_id`.

- **Local development**: Use tenant **`default`** (the default when nothing is set). This keeps local and SaaS behavior consistent. Edit `Scripts/continuum_tenant.txt` and set its content to `default`, or leave the Tenant field empty in the Continuum windows.
- **Production**: Use stable lowercase slugs (e.g. `game-slug`, `team-id`) so multiple game teams can use the same continuum stack with isolated data.
- **Setting the tenant in Drawer 2**:  
  - Set **Scripts/continuum_tenant.txt** to one line (the tenant id). Commit this file so the whole team uses the same tenant.  
  - Or override per-window in Continuum Library / Continuum Explorer with the "Tenant" field.

## Unity (Drawer 2)

**Python environment:** Continuum Explorer and the CLI fallback in Continuum Library use the **system** (or first-on-PATH) Python. That Python must have **USC (unified_semantic_archiver)** installed, e.g. `pip install -e /path/to/unified-semantic-compressor`. There is no project venv or configurable interpreter; use the same environment you use to run the continuum server. Optional: set **Python path** in Explorer to a specific executable if needed.

- **Continuum Explorer** (Window → Continuum → Continuum Explorer)  
  Set DB path to a continuum.db (e.g. from the continuum repo after `python -m unified_semantic_archiver init --db ./continuum.db`, or any path to a continuum DB). Working directory for the CLI is the project `Scripts` folder; the CLI runs as an installed module, so the DB path should be absolute or relative to your current working directory as needed. When browsing the `library_documents` table, the **Tenant** field (or `Scripts/continuum_tenant.txt`) is used for `--tenant`.

- **Continuum Library** (Window → Continuum → Continuum Library)  
  Set Base URL to the continuum server (e.g. http://localhost:5050) or leave empty and set DB path to use the Python CLI only. All requests to the server send **X-Tenant-ID** (and download URLs use `?tenant=`) from the Tenant field or `Scripts/continuum_tenant.txt`.
