# USC Build Modes (Drawer 2)

This document describes the first-pass USC build mode integration in Unity.

## Added Components

- `Assets/SystemDrawer/UscBuildManifest.cs`
- `Assets/SystemDrawer/UscBuildServiceWizard.cs`
- `Assets/Misc/Editor/ServiceWizards/UscBuildServiceWizardEditor.cs`
- `Assets/Editor/ContinuumBuildManagerWindow.cs`

## Build Modes

### `packed`

- USC service is available at runtime.
- Asset resolution prefers manifest-local assets first.
- If unresolved and fallback is enabled, the service returns a USC runtime endpoint path for reconstitution/generation.

### `unpacked`

- Runtime resolves strictly from manifest-local asset references.
- No USC server fallback.
- Intended for minimal offline/read-only packaged content where included assets are already prepared.

### `packed publish`

- Export-oriented mode.
- Build Manager creates a reduced manifest containing:
  - scene references from enabled build scenes
  - prompt language assets (CSV input in Build Manager)
- Stub command is produced for a future USC packed-publish pipeline:
  - `python -m unified_semantic_archiver packed-publish ...`

## Build Manager Workflow (v1)

1. Open `Window -> Continuum -> Build Manager`.
2. Select build mode and fill tenant/DB/language version.
3. Enter prompt language assets as CSV.
4. Refresh scene references.
5. Generate draft manifest.
6. Save manifest JSON (`Assets/Generated/USC/usc_build_manifest.json` by default).
7. Preview/run packed-publish stub command.

## Service Locator Integration

`UscBuildServiceWizard` registers itself with `SystemDrawerService` under key:

- `USCBuildService`

This keeps USC mode behavior discoverable using the same System Drawer patterns as other wizard services.

## Notes

- This is intentionally a manifest + orchestration stub pass.
- Full Unity BuildPipeline integration and real USC publish execution are deferred.
- Manifest schema is simple JSON via `JsonUtility` and can be expanded as build automation matures.
