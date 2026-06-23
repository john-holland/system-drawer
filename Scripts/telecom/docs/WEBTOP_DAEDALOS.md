# daedalOS alternate shell path

daedalOS is **not shipped in v1**. Use this guide to swap shells later without changing Unity or the message pump.

## Prerequisites

- Same entry: `webtop-host.html`
- Same bridge: [`telecom-message-pump.js`](../../../apps/telecom-webtop/src/telecom-message-pump.js)
- Protocol: [`WEBTOP_BRIDGE.md`](WEBTOP_BRIDGE.md)

## OS.js Application → daedalOS Process

| OS.js app | daedalOS process |
|-----------|------------------|
| TelecomDialer | `Dialer.exe` |
| TelecomTerminal | `Terminal.exe` |
| TelecomNetwork | `Network.exe` |
| TelecomBrowser | `Browser.exe` |

Mount each process iframe or React component that imports `window.continuumTelecomPump`.

## Layout

- daedalOS: static assets under `public/`
- OS.js: packages under `src/apps/` + `vendor/os-js/`

## WebView constraints

SimpleUnity3DWebView loads local `file://` or bundled static URLs. Avoid external CDN scripts for offline CCTV terminals.

## Parity checklist

- [ ] PAM login stub from `deviceContext`
- [ ] Discovery UI (Dialer)
- [ ] Network CRUD viewer
- [ ] Representational-net browser
- [ ] Message pump never bypassed

## When to prefer daedalOS

Heavier desktop UX, Windows 98 aesthetic, built-in file manager — vs lighter Continuum OS.js-style shell.

## Shell env

`TELECOM_WEBTOP_SHELL=daedalos` — document only until runtime swap is implemented.
