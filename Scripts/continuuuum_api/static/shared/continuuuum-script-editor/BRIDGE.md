# Continuuuum Script Editor — Unity Bridge

JSON messages between bundled `editor-host.html` and C# (`ContinuuuumEditorBridge`).

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

- WebView loads **local** `Assets/Continuuuum/Editor/ScriptEditor/WebView/editor-host.html` only.
- No login forms, cookies, or external navigation.
- All HTTP uses `ContinuuuumEditorApiClient` with `X-User-ID` and `X-Tenant-ID` from `ContinuuuumEditorSession`.

## JS entry points

- `ContinuuuumScriptEditor.mountWithBridge(el, bridge, options)` — shared module with bridge delegation.
- `window.continuuuumHost.mount(options)` — host page helper used by Unity spike.
