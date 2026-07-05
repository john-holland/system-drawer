# Webtop ↔ Unity bridge

Extends [continuuuum-script-editor BRIDGE.md](../../continuuuum_api/static/shared/continuuuum-script-editor/BRIDGE.md).

## Message envelope

```json
{ "action": "...", "requestId": "optional", "payload": {}, "method": "GET", "path": "/networks", "body": {} }
```

## webtop → Unity

| action | purpose |
|--------|---------|
| `api` | Proxy CRUD to `/api/telecom/*` |
| `ring` | Call event → Ears / ringtone |
| `cctvFrame` | CCTV tile URL or texture handle |
| `notifyVisual` | Sensor / UI cue |

## Unity → webtop

| action | purpose |
|--------|---------|
| `deviceContext` | IP, phone, network_id, PAM session |
| `spatialBinding` | causality_leaf_id, geohash |
| `bridgeResponse` | `api` reply with `requestId` |

## JS API

```javascript
window.continuuuumTelecomPump.apiRequest('GET', '/networks');
window.continuuuumTelecomPump.ring({ direction: 'incoming', phone: '1-1-555-0100' });
```

Shell-agnostic: works under OS.js or future daedalOS.
