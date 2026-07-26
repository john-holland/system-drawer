# Web Lemma Build

Mobile-reactive Lemma Builder at `/lemma-build/` on the Drawer API (default port 5050). It mirrors the Unity Lemma Build form + Codestral chat, with a **target engine** that selects the system prompt appendix.

## Engines

| Id | Label | Enabled | Prompt |
|----|-------|---------|--------|
| `unity` | Unity | yes (default) | `LemmaBuildSystemPreface.md` + `LemmaBuildEngineUnity.md` |
| `haxe` | Haxe | yes | base + `LemmaBuildEngineHaxe.md` |
| `unreal` | Unreal | no | reserved; API returns `400` |

Preface files live under `Scripts/continuuuum_api/data/lemma_build_preface/`.

Query hydrate: `/lemma-build/?engine=haxe&lemma=open&partOfSpeech=verb`.

## Settings (admin)

Settings → **Lemma Library** (admin):

- Model base URL (LM Studio / OpenAI-compatible)
- Default model id (+ refresh models)
- Max concurrent builds
- Batch output directory
- Default target engine (`unity` \| `haxe`)

`PUT /api/lemma-build/settings` requires `X-Admin: 1`. Non-admin `GET` omits the base URL.

## Batch layout

`{batch_output_dir}/{session_id}/`:

- `chat.txt` — full turn transcript
- `engine.txt`, `meta.json`
- `descriptor.json` — when a lemma-mechanism-descriptor fence is present
- `generated/` — files from `write_file` tool calls or fenced code blocks

## API (summary)

| Method | Route | Notes |
|--------|-------|-------|
| GET/PUT | `/api/lemma-build/settings` | PUT admin-only |
| GET | `/api/lemma-build/engines` | catalog |
| GET | `/api/lemma-build/models` | admin; proxies `{base}/models` |
| POST | `/api/lemma-build/sessions` | create batch folder |
| GET | `/api/lemma-build/sessions/<id>` | meta + file list |
| GET | `/api/lemma-build/sessions/<id>/files/<name>` | download |
| POST | `/api/lemma-build/chat` | proxy + extract; `429` over concurrency |
| POST | `/api/lemma-build/parse-descriptor` | assistant text → descriptor |

## Deep link contract

`POST /api/deeplink`:

```json
{
  "window": "System Drawer/Lemmas/Lemma Build",
  "form": {
    "lemma": "open",
    "partOfSpeech": "verb",
    "posTag": "verb",
    "mechanicalRole": "AtomicAction",
    "outputTier": 0,
    "functionalDescription": "",
    "mechanismPrompt": "",
    "synonyms": ["unlock"],
    "compositionChildren": [{ "entryId": "urn:…", "sortOrder": 0 }],
    "properties": [{ "propertyKey": "k", "propertyValue": "v" }],
    "engine": "unity"
  }
}
```

Unity `DeepLinkHandler` opens the Lemma Build tab and applies `form` via `OpenOnLemmaBuildTabWithForm`. `engine` is persisted on the chat session JSON for round-trip; Unity codegen remains Unity-oriented until a Haxe pipeline exists.

`entryId` alone still opens the Properties tab (`window` containing `Lemma` but not `Lemma Build`).

## NSM semantic primes

The 65 English NSM primes are Continuuuum composition atoms. Canonical list + glosses live under `Scripts/continuuuum_api/data/` — see [`nsm_semantic_primes_README.md`](../../../../Scripts/continuuuum_api/data/nsm_semantic_primes_README.md). Completion tracking: `/lemma-completion` (scope **Primes**). Seed via `POST /api/lemma-completion/seed`.
