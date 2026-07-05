# OS.js v3 Webtop Setup

Continuuuum telecom webtop lives in [`apps/telecom-webtop`](../../../apps/telecom-webtop/).

## Dev

```bash
cd apps/telecom-webtop
npm install
npm run dev
```

Open http://localhost:5175 — API proxied to continuuuum_api :5050.

## Unity / SimpleUnity3DWebView

Load `public/webtop-host.html` (or Vite build `dist/webtop-host.html`) from `TelecomWebViewDisplay`.

## Apps

| App | Role |
|-----|------|
| TelecomDialer | Galactic phone UI |
| TelecomTerminal | Device context |
| TelecomNetwork | CRUD browser |
| TelecomBrowser | Representational-net iframe |

## Full OS.js v3

Install `@os-js/client` (already in package.json). Replace `desktop-shell.js` with OS.js `Core` bootstrap when pinning a full OS.js dist under `vendor/os-js/`.

Register packages via `metadata.json` per app under `src/apps/`.

## Env

- `TELECOM_WEBTOP_SHELL=osjs` (default)
- `TELECOM_WEBTOP_SHELL=daedalos` — documented only; returns 501 until implemented
