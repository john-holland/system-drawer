# Continuum API

Flask server for episode script, thesaurus alternatives, AST update/rebalance, change-of-basis, and screenplay (speech/SFX). Used by the script-output React app.

## Audio ref format

`audio_ref` in `script_speech_audio`, `script_sound_effects`, and `script_audio_by_language` must be one of:

- **File path**: absolute or relative path to an audio file (e.g. `./audio/dub_fr_01.wav`).
- **Blob id**: reference to a stored blob, e.g. `blob:<id>` where `<id>` is a row id (e.g. `document_blobs.id`). Tools resolve via project blob storage.
- **URI**: `file:///path/to/file.wav` or any URI scheme your pipeline supports.

Tree inclusion for clause-level audio: an AST node belongs to a clause if the node’s Farey interval is contained in the row’s `(farey_left_num/farey_left_den, farey_right_num/farey_right_den]`.

## Endpoints

- `GET /api/episode-script/<episode_id>` – episode_script row (scriptText, language, etc.)
- `GET /api/thesaurus/alternatives?token=...&language=...` – lemma, POS, alternatives for a word
- `PATCH /api/thesaurus/ast-nodes/<node_id>` – update tokenOrPhrase, posTag
- `POST /api/thesaurus/rebalance` – body `{ episodeScriptId }` – recompute Farey intervals
- `POST /api/thesaurus/build-ast` – body `{ episodeScriptId, scriptText?, languageId? }` – build AST from script (detect quote blocks)
- `POST /api/thesaurus/change-of-basis` – body `{ episodeScriptId, targetLanguage }` – translate script; updates script_audio_by_language for target language
- `GET /api/episode-script/<id>/screenplay?language=...` – screenplay output (blocks with dialogue/SFX and audio refs)
- `POST /api/episodes/<episode_id>/extract-screenplay-work-orders` – body `{ episodeScriptId? }` – extract work orders from script_speech_audio and script_sound_effects (dialogue and SFX tasks)

## Society sim (gov-glove)

Political society simulation: planets, cities, zoning, building registry, telecom virtual networks.

```bash
cd Scripts/continuum_api && npm install
python -m continuum_api.server --db continuum.db --port 5050
```

Web UI: `/city-config`, `/society-dashboard`

- `GET /api/society/planets` – list planets
- `POST /api/society/planets/{planetId}/cities` – create city (+ virtual network + IPv6)
- `POST /api/society/cities/{cityId}/zoning/solve` – zoning / size / budget solver
- `POST /api/society/cities/{cityId}/tick` – advance political solver
- `GET /api/society/cities/{cityId}/spatial-map` – top-down zones + building pins

Requires `gov-glove` npm package (vendored under `vendor/gov-glove`).


Web UI: `/lemma-library` (browse, create, bulk import, localization search).

- `GET /api/thesaurus/entries` – merged built-in + custom lemma search (`q`, `language`, `pos`, `source`, `propertyKey`, `entryId`)
- `POST /api/thesaurus/entries` – create single lemma
- `POST /api/thesaurus/entries/import` – bulk CSV/TSV upload
- `GET /api/thesaurus/clauses` – global clause search
- `GET /api/thesaurus/localization-view` – localization-focused combined view
- `POST /api/thesaurus/parse-properties` – parse `{P:...}` default property strings

### XLIFF translations

Web UI: Lemma Library → **Translations** (`/lemma-library#translations`).

- `GET /api/thesaurus/languages` – language codes for export/import dropdowns
- `GET /api/thesaurus/export-xliff?sourceLang=en&targetLang=fr` – download XLIFF 2.0 XML
- `POST /api/thesaurus/import-xliff` – multipart `file` or JSON `{ "xliff": "…" }`
- `GET /api/thesaurus/language-audit` – missing translation report (optional UI link)

```bash
curl -o thesaurus-fr.xliff "http://127.0.0.1:5050/api/thesaurus/export-xliff?sourceLang=en&targetLang=fr"
curl -X POST -F file=@thesaurus-fr.xliff http://127.0.0.1:5050/api/thesaurus/import-xliff
```

Built-in vocabulary JSON: `continuum_api/data/builtin_vocabulary.json` (export via Unity **Continuum → Export Built-in Vocabulary JSON**).

Apply `continuum_lemma_library_schema.sql` for `prefab-id` property spec and indexes.

## Run

Set continuum.db path (or place `continuum.db` in repo root):

```bash
pip install -r requirements.txt
CONTINUUM_DB=/path/to/continuum.db python -m continuum_api.server --port 5050
```

Or: `python -m continuum_api.server --db /path/to/continuum.db`

Apply `continuum_episodes_schema.sql`, `continuum_thesaurus_schema.sql`, `continuum_screenplay_schema.sql`, `continuum_draft_schema.sql`, and `continuum_review_schema.sql` to the DB first.
