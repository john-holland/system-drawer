using UnityEngine;

/// <summary>
/// Provides world-space bounds (e.g. from WeatherPhysicsManifold or PhysicsManifold). Used to align generator bounds with a single source of truth.
/// </summary>
public interface IBoundsProvider
{
    Bounds GetBounds();
}
