# WebView Go / No-Go (Unity Editor)

## Goal

Embed Ace (via shared `continuum-script-editor.js`) in Unity Editor without exposing credentials.

## Spike

- Window: `Window → Continuum → WebView Spike`
- Host: `Assets/Continuum/Editor/ScriptEditor/WebView/editor-host.html`
- Bridge: `ContinuumEditorBridge` + `ContinuumEditorApiClient`

## Security checklist

| Check | Status |
|-------|--------|
| Local bundle only (no remote HTML) | Pass |
| API only via C# headers | Pass |
| No password / session fields in WebView | Pass |
| External URL navigation blocked | Pass (LoadURL to file URI only) |

## Unity WebView availability

`ContinuumWebViewHost` uses reflection on `UnityEditor.WebView` / `UnityEditor.MacWebView`.

- **Windows/Linux:** Internal WebView type may be absent on some Unity versions → **fallback required**
- **macOS:** MacWebView may be available on older editors

## Decision

| Platform | Recommendation |
|----------|----------------|
| WebView type found | **Go** — enable WebView toggle in Script Editor |
| WebView type missing | **No-go for embed** — use `ContinuumRichScriptEditor` + `SpanOverlayPainter` (textarea/Ace-less parity) |

Minimum Unity version for WebView embed: **document at test time** (spike window logs availability).

## Fallback

`ContinuumScriptEditorWindow` defaults to UIToolkit-style IMGUI rich editor with dotted span overlays when WebView is unavailable or disabled.
