/// <summary>Global lock registry so SpatialGenerator.SetSeed can enforce orchestrator-derived seeds.</summary>
public static class SeedDependencyLockRegistry
{
    static SpatialGenerator4DOrchestrator _active;
    static bool _applying;

    public static SpatialGenerator4DOrchestrator ActiveOrchestrator =>
        _active != null && _active.lockSeedDependencyTree ? _active : null;

    public static bool IsApplying => _applying;

    public static void Register(SpatialGenerator4DOrchestrator orchestrator) => _active = orchestrator;

    public static void BeginApply() => _applying = true;

    public static void EndApply() => _applying = false;
}
