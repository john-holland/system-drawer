using UnityEngine;

/// <summary>
/// Abstract base for spatial generators (3D and 4D). Implements ISpatialGeneratorLayer so both can live in the orchestrator list.
/// </summary>
public abstract class SpatialGeneratorBase : MonoBehaviour, ISpatialGeneratorLayer
{
    /// <inheritdoc />
    public abstract Bounds GetSpatialBounds();

    /// <inheritdoc />
    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    /// <inheritdoc />
    public virtual string DisplayName => gameObject != null ? gameObject.name : GetType().Name;
}
