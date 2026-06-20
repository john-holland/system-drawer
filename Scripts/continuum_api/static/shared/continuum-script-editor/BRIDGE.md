# Continuum Script Editor — Unity Bridge

JSON messages between bundled `editor-host.html` and C# (`ContinuumEditorBridge`).

## Request (WebView → C#)

```json
{ "action": "api", "requestId": "req-1", "method": "POST", "path": "/api/scripts/{draftId}/apply-edit", "body": "{\"oldText\":\"...\",\"newText\":\"...\"}" }
{ "action": "openReview", "reviewId": "...", "draftId": "..." }
{ "action": "notificationRead", "notificationId": "..." }
```

## Response (C# → WebView)

```json
{ "requestId": "req-1", "ok": true, "data": "{...}" }
{ "requestId": "req-1", "ok": false, "error": "message" }
```

## Security

- WebView loads **local** `Assets/Continuum/Editor/ScriptEditor/WebView/editor-host.html` only.
- No login forms, cookies, or external navigation.
- All HTTP uses `ContinuumEditorApiClient` with `X-User-ID` and `X-Tenant-ID` from `ContinuumEditorSession`.

## JS entry points

- `ContinuumScriptEditor.mountWithBridge(el, bridge, options)` — shared module with bridge delegation.
- `window.continuumHost.mount(options)` — host page helper used by Unity spike.
