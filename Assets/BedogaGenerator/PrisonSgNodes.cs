using UnityEngine;

/// <summary>SG node that compiles to open/close topology joint ids for cell doors.</summary>
[AddComponentMenu("Bedoga/Prison Cell Door Node")]
public sealed class PrisonCellDoorNode : SGBehaviorTreeNode
{
    public string jointId = "cell_door";
    public bool defaultLocked = true;
    public string keycardZoneId = "cells";

    public string CompileOpenCloseJointId() => string.IsNullOrEmpty(jointId) ? name : jointId;
}

/// <summary>Prison wall with destructibility + diggable flag.</summary>
[AddComponentMenu("Bedoga/Prison Wall Node")]
public sealed class PrisonWallNode : SGBehaviorTreeNode
{
    [Range(0f, 1f)] public float destructibility01 = 0.5f;
    public bool diggable = true;

    public PrisonWallVolume BindVolume(GameObject host)
    {
        if (host == null) return null;
        var vol = host.GetComponent<PrisonWallVolume>() ?? host.AddComponent<PrisonWallVolume>();
        vol.diggable = diggable;
        vol.destructibility01 = destructibility01;
        vol.volumeKind = DiggableVolumeKind.Wall;
        return vol;
    }
}

/// <summary>Tunnel collapse thresholds (skeleton; SPH later).</summary>
[AddComponentMenu("Bedoga/Tunnel Collapse Node")]
public sealed class TunnelCollapseNode : SGBehaviorTreeNode
{
    [Range(0f, 1f)] public float stressThreshold01 = 0.8f;
    public bool emitDoNotPath = true;
}

/// <summary>Tunnel support thresholds (skeleton; SPH later).</summary>
[AddComponentMenu("Bedoga/Tunnel Support Node")]
public sealed class TunnelSupportNode : SGBehaviorTreeNode
{
    [Range(0f, 1f)] public float supportFill01 = 0.5f;
    public bool blockSurfaceDip;
}

/// <summary>Insert painted prison clusters as Bounds4 payloads into SpatialGenerator4D via Narrative4DPlacer.</summary>
public static class PrisonBounds4Export
{
    public const string PrisonCellVolume = "PrisonCellVolume";
    public const string DigContactCentroid = "DigContactCentroid";

    public static int Insert(Narrative4DPlacer placer, CityPixelGrid grid, int frameIndex)
    {
        if (placer == null || grid == null) return 0;
        placer.ResolveReferences();
        var vols = grid.ExportPrisonClustersToBounds4(frameIndex);
        return vols != null ? vols.Count : 0;
    }
}
