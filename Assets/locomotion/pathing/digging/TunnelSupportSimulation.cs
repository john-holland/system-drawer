using SpatialVolumes;
using UnityEngine;
using Weather;

/// <summary>Tunnel support SPH fill + collapse; heightmap portal when overburden depletes.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Digging/Tunnel Support Simulation")]
public sealed class TunnelSupportSimulation : MonoBehaviour
{
    public PrisonWallVolume wall;
    public HeightMapInteriorShaderBuffer heightMap;
    public float availableVolume = 8f;
    public float sphUsed;
    public bool preventSurfaceDip;
    public MeshTerrainPortal portal;
    public Texture2D stashedHeightSection;
    public bool collapsed;

    public float Dip01 => availableVolume > 1e-4f ? Mathf.Clamp01(sphUsed / availableVolume) : 0f;

    public void FillFromTop(float amount)
    {
        if (preventSurfaceDip) return;
        sphUsed = Mathf.Min(availableVolume, sphUsed + Mathf.Max(0f, amount));
        if (sphUsed >= availableVolume - 1e-4f)
            OpenHeightmapPortal();
    }

    public void OpenHeightmapPortal()
    {
        if (heightMap != null && heightMap.heightMap != null && stashedHeightSection == null)
            stashedHeightSection = heightMap.heightMap;
        if (portal == null)
            portal = gameObject.GetComponent<MeshTerrainPortal>() ?? gameObject.AddComponent<MeshTerrainPortal>();
        if (heightMap != null)
            heightMap.dirty = true;
    }

    public void ResetPortal()
    {
        if (heightMap != null && stashedHeightSection != null)
            heightMap.heightMap = stashedHeightSection;
        stashedHeightSection = null;
        sphUsed = 0f;
        collapsed = false;
    }

    public void Collapse()
    {
        collapsed = true;
        var region = gameObject.GetComponent<DoNotPathRegion>() ?? gameObject.AddComponent<DoNotPathRegion>();
        region.enabled = true;
        SpatialVolumeCacheRegistry.InvalidateAll();
    }

    public float SurfaceLerp(float supportBeamPolar01)
    {
        float dip = Dip01;
        return Mathf.Lerp(dip, 1f - dip, Mathf.Clamp01(supportBeamPolar01));
    }
}
