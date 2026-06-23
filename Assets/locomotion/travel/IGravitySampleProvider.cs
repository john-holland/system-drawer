using Planetary;
using UnityEngine;

/// <summary>Gravity sample at a world position for terminal alignment.</summary>
public struct GravitySample
{
    public Vector3 up;
    public float strength;
}

/// <summary>Provides up-vector and gravity strength at terminal slots.</summary>
public interface IGravitySampleProvider
{
    GravitySample Sample(Vector3 worldPos);
}

public sealed class UnityGravityProvider : IGravitySampleProvider
{
    public GravitySample Sample(Vector3 worldPos)
    {
        Vector3 g = Physics.gravity;
        float mag = g.magnitude;
        Vector3 up = mag > 1e-4f ? (-g / mag) : Vector3.up;
        return new GravitySample { up = up, strength = mag };
    }
}

public sealed class TerrainNormalGravityProvider : IGravitySampleProvider
{
    readonly Terrain[] _terrains;

    public TerrainNormalGravityProvider(Terrain[] terrains)
    {
        _terrains = terrains;
    }

    public GravitySample Sample(Vector3 worldPos)
    {
        var unity = new UnityGravityProvider();
        GravitySample baseSample = unity.Sample(worldPos);
        if (_terrains == null || _terrains.Length == 0)
            return baseSample;

        for (int i = 0; i < _terrains.Length; i++)
        {
            Terrain t = _terrains[i];
            if (t == null || t.terrainData == null)
                continue;
            Vector3 tPos = t.transform.position;
            Vector3 tSize = t.terrainData.size;
            if (worldPos.x < tPos.x || worldPos.z < tPos.z
                || worldPos.x > tPos.x + tSize.x || worldPos.z > tPos.z + tSize.z)
                continue;
            float nx = (worldPos.x - tPos.x) / tSize.x;
            float nz = (worldPos.z - tPos.z) / tSize.z;
            Vector3 normal = t.terrainData.GetInterpolatedNormal(nx, nz);
            return new GravitySample { up = normal.normalized, strength = baseSample.strength };
        }
        return baseSample;
    }
}

public sealed class WaterSurfaceGravityProvider : IGravitySampleProvider
{
    readonly IGravitySampleProvider _fallback;
    readonly float _waterY;

    public WaterSurfaceGravityProvider(float waterY, IGravitySampleProvider fallback = null)
    {
        _waterY = waterY;
        _fallback = fallback ?? new UnityGravityProvider();
    }

    public GravitySample Sample(Vector3 worldPos)
    {
        GravitySample s = _fallback.Sample(worldPos);
        s.up = Vector3.up;
        return s;
    }
}

public sealed class PlanetManifoldGravityProvider : IGravitySampleProvider
{
    readonly PhysicalManifold _manifold;
    readonly IGravitySampleProvider _fallback;

    public PlanetManifoldGravityProvider(PhysicalManifold manifold, IGravitySampleProvider fallback = null)
    {
        _manifold = manifold;
        _fallback = fallback ?? new UnityGravityProvider();
    }

    public GravitySample Sample(Vector3 worldPos)
    {
        GravitySample s = _fallback.Sample(worldPos);
        if (_manifold != null)
            s.strength *= Mathf.Max(0.01f, _manifold.gravityWellStrength);
        return s;
    }
}

/// <summary>Chain: planet manifold when present, else terrain, else Unity gravity.</summary>
public sealed class ChainedGravitySampleProvider : IGravitySampleProvider
{
    readonly IGravitySampleProvider[] _chain;

    public ChainedGravitySampleProvider(params IGravitySampleProvider[] chain)
    {
        _chain = chain;
    }

    public static IGravitySampleProvider CreateDefault(HierarchicalPathingSolver pathingSolver = null)
    {
        Terrain[] terrains = null;
        if (pathingSolver != null && pathingSolver.fitToTerrains != null && pathingSolver.fitToTerrains.Count > 0)
            terrains = pathingSolver.fitToTerrains.ToArray();

        var unity = new UnityGravityProvider();
        var terrain = terrains != null && terrains.Length > 0
            ? new TerrainNormalGravityProvider(terrains)
            : unity;

        PhysicalManifold manifold = Object.FindAnyObjectByType<PhysicalManifold>();
        if (manifold != null)
            return new ChainedGravitySampleProvider(new PlanetManifoldGravityProvider(manifold, terrain));
        return terrain;
    }

    public GravitySample Sample(Vector3 worldPos)
    {
        if (_chain == null || _chain.Length == 0)
            return new UnityGravityProvider().Sample(worldPos);
        return _chain[0].Sample(worldPos);
    }
}
