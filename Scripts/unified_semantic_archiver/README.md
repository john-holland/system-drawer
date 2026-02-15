# Unified Semantic Archiver

Flowering ring of semantic compressors (video, audio, library, data) with continuum DB, ETL, and research feed.

## Install

```bash
pip install -r Scripts/unified_semantic_archiver/requirements.txt
```

From project root, ensure Scripts is on `PYTHONPATH`:

```bash
$env:PYTHONPATH = "Scripts"   # PowerShell
python -m unified_semantic_archiver init --db Scripts/unified_semantic_archiver/continuum.db
```

## Usage

```bash
# Initialize DB
python -m unified_semantic_archiver init --db ./continuum.db

# Run ETL (Luigi)
python -m unified_semantic_archiver run-etl --source ./input/ --db ./continuum.db

# Compress media
python -m unified_semantic_archiver compress --media video.mp4 --type video --out ./out --db ./continuum.db

# Build Cursor research context
python -m unified_semantic_archiver cursor-research --db ./continuum.db --output ./context.json
```

## Unity Explorer

Window → Continuum → Continuum Explorer. Set DB path to `Scripts/unified_semantic_archiver/continuum.db` (absolute path). Requires Python on PATH.

## Structure

- `db/` — Schema + ContinuumDb micro ORM
- `etl/` — Luigi Extract/Transform/Load (identity stub)
- `compressors/` — Video, audio, library, data; ring orchestrator
- `research/` — Unique chunk/kernel store; improvement feed
- `services/` — Cursor call service
- `cli/` — Query DB (used by Unity explorer)
