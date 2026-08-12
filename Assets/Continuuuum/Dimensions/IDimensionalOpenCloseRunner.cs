using UnityEngine;

/// <summary>
/// Thin bridge so Continuuuum.Runtime can request open/close topology BT without
/// referencing Locomotion.Runtime (Locomotion registers the implementation).
/// </summary>
public interface IDimensionalOpenCloseRunner
{
    /// <param name="host">DimensionalShaderComponent host / aesthetic root.</param>
    /// <param name="topologyAsset">Typically OpenCloseTopologyAsset.</param>
    /// <param name="entering">True = open/enter dim; false = close/exit.</param>
    /// <param name="runtimeMilliseconds">-1 = default; &gt;=0 override.</param>
    void Begin(GameObject host, ScriptableObject topologyAsset, bool entering, int runtimeMilliseconds);
}

/// <summary>Static registration point for <see cref="IDimensionalOpenCloseRunner"/>.</summary>
public static class DimensionalOpenCloseRunnerHost
{
    public static IDimensionalOpenCloseRunner Instance { get; set; }

    public static bool TryBegin(
        GameObject host,
        ScriptableObject topologyAsset,
        bool entering,
        int runtimeMilliseconds)
    {
        if (Instance == null || host == null || topologyAsset == null)
            return false;
        Instance.Begin(host, topologyAsset, entering, runtimeMilliseconds);
        return true;
    }
}
