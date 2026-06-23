# ChatGPT representational net

Generate fictional intranet sites for the telecom virtual network. **HTML shape and links must validate**; prose may be nonsense.

## Workflow

1. **Phase 1** — emit `manifest.json` (see `telecom/playbooks/prompts/representational-net/site-manifest.schema.json`)
2. **Phase 2** — emit JSON array of `{ slug, html }` using prompts in `telecom/playbooks/prompts/representational-net/`
3. **Validate** — `python Scripts/telecom/representational_net/validate.py telecom/playbooks/resources/sites/{siteId}`
4. **Register** — add playbook resource with `source: representational_net`

## ChatGPT setup

1. System prompt: `system.md`
2. User prompt: `page-batch.user.md` with `{siteId}`, `{theme}`, `{pageCount}`, `{deviceLinks}`, `{manifestJson}`
3. OpenAI structured outputs: bind page-batch JSON schema; temperature 0.3–0.5
4. Custom GPT: upload manifest schema + skeleton example
5. Repair pass: pipe `validate.py --repair-hints` errors back to the model

## LM Studio

Same JSON contract as [`chatgpt_critique.py`](../../video_storage_tool/chatgpt_critique.py) — localhost OpenAI-compatible API.

## Link rules

- Relative slugs from manifest only (+ `#`, `tel:`, `telecom://device/`)
- `index.html` links to all pages; every page links home
- No `http`/`https` in v1

## Example

[`telecom/playbooks/resources/sites/corp-intranet/`](../../../telecom/playbooks/resources/sites/corp-intranet/)

## Serving

- Flask: `GET /api/telecom/sites/{siteId}/index.html`
- OS.js TelecomBrowser app
