# Memory Swizzle View

WinDirStat-style **squarified treemap** for Unity memory: tile area is proportional to bytes, click to drill down, breadcrumbs to navigate back.

**Menu:** Window → System Drawer → Diagnostics → Memory Swizzle View

## View modes

| Mode | What it shows | Snapshot required |
|------|----------------|-------------------|
| **Unity Systems** | Mono, Gfx, textures, meshes, audio, physics, GC, profiler, untracked | No (live counters) |
| **Component Totals** | Sum by component / Unity type; top instances per type | Yes (then live scan) |
| **Entity Totals** | Memory rolled up to root GameObjects; optional **Registered only** (`SceneObjectRegistry`) | Yes |
| **Type Tree** | Assembly → namespace → type | Yes |
| **Scene Hierarchy** | Scene → GameObject → components | Yes |

## Workflow

1. Enter **Play Mode** for representative runtime memory.
2. Open **Memory Swizzle View**.
3. For object modes: click **Capture Snapshot** (uses `com.unity.memoryprofiler`; stored under `Library/MemorySwizzle/last.snap`).
4. Pick a mode, hover tiles for size, **click** to drill into children, **Root** / **Back** / breadcrumb to pop.
5. Side panel: child list, **Ping** (instance ID), **Copy TSV**.

**Unity Systems** mode supports **Auto-refresh** every ~2s without a snapshot.

## Notes

- Cross-mode totals may differ (native vs managed attribution paths).
- Snapshot capture triggers a full heap capture; object rows are filled via live scene scan with `Profiler.GetRuntimeMemorySizeLong` when package parse is unavailable.
- Edit Mode systems counters work; object attribution is most accurate during Play Mode.

## Related

- [README.md](../../README.md#documentation-index) — project documentation index
