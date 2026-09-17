using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClothSplineKind
{
    Cut = 0,
    Fold = 1,
    Stitch = 2,
    Hem = 3
}

[Serializable]
public sealed class ClothGrabberPin
{
    public Vector3 local;
    public Vector2 uv;
    [Range(0f, 1f)] public float weight01 = 1f;
}

[Serializable]
public sealed class ClothSplinePath
{
    public ClothSplineKind kind = ClothSplineKind.Cut;
    public string pathId;
    public List<Vector3> controlPoints = new List<Vector3>();
    public List<ClothGrabberPin> grabbers = new List<ClothGrabberPin>();
    public string joinLoopIdA;
    public string joinLoopIdB;
    public SewingStitchProgram program;
    [Range(0f, 1f)] public float gauge01 = 0.4f;
    public HemSeamApplyMode applyMode = HemSeamApplyMode.Decal;
}

[CreateAssetMenu(fileName = "ClothBolt", menuName = "Locomotion/Civil/Cloth Bolt")]
public sealed class ClothBoltSpec : ScriptableObject
{
    public float widthM = 1.2f;
    public float lengthM = 2.4f;
    public float thicknessM = 0.002f;
    public Vector2 weaveUv = Vector2.one;
    public string commodityKey = ClothingCommodities.Bolt;
    public List<ClothSplinePath> paths = new List<ClothSplinePath>();
    public string joinLoopIdA;
    public string joinLoopIdB;
    public SkinnedMeshLoopSectionAsset loopSection;
    public PaintCanvas dyeCanvas;
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    public Rect BoltRect => new Rect(0f, 0f, Mathf.Max(0.01f, widthM), Mathf.Max(0.01f, lengthM));

    public ClothSplinePath AddPath(ClothSplineKind kind)
    {
        var p = new ClothSplinePath
        {
            kind = kind,
            pathId = kind.ToString().ToLowerInvariant() + "_" + paths.Count
        };
        paths.Add(p);
        return p;
    }
}
