# Perf Trace View

Hierarchical **performance histogram** with breadcrumb drill-down for scoped timings recorded via `PerfTrace`.

**Menu:** Window → System Drawer → Diagnostics → Perf Trace View

## Service grades

| Grade | API | When active |
|-------|-----|-------------|
| **Fine** (default) | `PerfTrace.Mark(note)`, `PerfTrace.Scope(note)` | Editor, Development Builds, or when `ENABLE_PERF_TRACE` is defined |
| **Rough** | `PerfTrace.MarkRough(note)`, `PerfTrace.ScopeRough(note)` | Production-safe aggregates when enabled in settings |

Fine marks are stripped from release player builds so they do not pollute production telemetry.

## Quick start

```csharp
using (PerfTrace.Scope("rebuild planet"))
{
    PerfTrace.Mark("heightmap sample");
    // ...
}
```

Explicit benchmark bracket:

```csharp
PerfTrace.BeginBenchmark("Planet rebuild");
using (PerfTrace.Scope("RebuildAll"))
{
    // ...
}
PerfTrace.EndBenchmark();
```

## View modes

| Mode | Description |
|------|-------------|
| **Live** | Most recent in-memory fine session |
| **Rough Summary** | Flat histogram of production rough aggregates |
| **Saved run** | Loaded from run history dropdown |

## Run history

- Toggle **Auto-collect benchmark** while this window is open to persist completed fine sessions under `Library/PerfTrace/runs/`.
- Dropdown format: `yyyy-MM-dd HH:mm:ss.fff — {label}` (local editor time).
- Play Mode exit bundles scopes from that session into one run when auto-collect is on.

## Memory Swizzle correlation

- **Capture Correlated Memory** triggers Memory Swizzle snapshot capture and stamps the perf session.
- **Open Memory Swizzle** opens the linked memory profiler window.
- Memory Swizzle toolbar shows last correlated perf capture time when available.

## Related

- [MemorySwizzleView.md](MemorySwizzleView.md) — heap / memory treemap profiler
- Optional define: Window → System Drawer → Diagnostics → Perf Trace → Toggle `ENABLE_PERF_TRACE` Define
