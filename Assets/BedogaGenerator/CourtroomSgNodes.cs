using UnityEngine;

/// <summary>SG node: gallery cell with an <see cref="AngleBase3D"/> sitting angle.</summary>
[AddComponentMenu("Bedoga/Courtroom Gallery Node")]
public sealed class CourtroomGalleryNode : SGBehaviorTreeNode
{
    public string cellId = "gallery";
    public AngleBase3D angleBase;
    public float yawDeg;
    public float pitchDeg;
    public float rollDeg;

    public AngleBase3D BindAngle(GameObject host)
    {
        if (host == null) return null;
        var angle = angleBase != null ? angleBase : host.GetComponent<AngleBase3D>() ?? host.AddComponent<AngleBase3D>();
        angle.galleryCellId = cellId;
        angle.yawDeg = yawDeg;
        angle.pitchDeg = pitchDeg;
        angle.rollDeg = rollDeg;
        angleBase = angle;
        return angle;
    }
}

/// <summary>SG node: bench / well / jury / bar zone with optional sg4d prompt.</summary>
[AddComponentMenu("Bedoga/Courtroom Zone Node")]
public sealed class CourtroomZoneNode : SGBehaviorTreeNode
{
    public CityPixelLayerKind layer = CityPixelLayerKind.CourtBench;
    public string sg4dPrompt;
    public string inpaintPrompt;
}

/// <summary>Insert painted courtroom clusters as Bounds4 payloads.</summary>
public static class CourtroomBounds4Export
{
    public const string CourtroomVolume = "CourtroomVolume";

    public static int Insert(Narrative4DPlacer placer, CityPixelGrid grid, int frameIndex)
    {
        if (placer == null || grid == null) return 0;
        placer.ResolveReferences();
        var vols = grid.ExportCourtroomClustersToBounds4(frameIndex);
        return vols != null ? vols.Count : 0;
    }
}
