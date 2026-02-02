using UnityEngine;

/// <summary>
/// Interface for spatial generator layers (3D or 4D). Used by the orchestrator to manage a unified list of generators.
/// </summary>
public interface ISpatialGeneratorLayer
{
    /// <summary>World-space bounds of the generator (or local if appropriate for the implementation).</summary>
    Bounds GetSpatialBounds();

    /// <summary>Whether the generator is enabled (active).</summary>
    bool Enabled { get; set; }

    /// <summary>Display name for editor UI (e.g. gameObject name or type name).</summary>
    string DisplayName { get; }
}
