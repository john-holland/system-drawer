using UnityEngine;

/// <summary>
/// Brush shape definition: end/conical curves + ferrule hairline for bristle emergence.
/// </summary>
[CreateAssetMenu(fileName = "PaintBrushDefinition", menuName = "Locomotion/Painting/Brush Definition")]
public sealed class PaintBrushDefinition : ScriptableObject
{
    public enum BrushKind
    {
        Fan,
        Pointed,
        Square,
        Round,
        Angle,
        FlatLiner
    }

    public BrushKind kind = BrushKind.Round;
    public string displayName = "Round";
    public AnimationCurve endCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
    public AnimationCurve conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.1f);
    public HairLineCurve ferruleHairLine = HairLineCurve.Constant(1f);
    public HairLineAngleCurve ferruleAngle = HairLineAngleCurve.Zero();
    public Vector3 ferrulePateLocal = new Vector3(0f, 0f, 0.02f);
    [Range(0f, 1f)] public float pateAngleBlend = 0.4f;
    [Min(0.01f)] public float bristleLengthM = 0.05f;
    [Min(0.005f)] public float ferruleRadiusM = 0.012f;
    [Range(0f, 1f)] public float plumeTipHold = 0.85f;
    [Min(0.01f)] public float saturationSpeed = 0.35f;
    public Color defaultPaintColor = new Color(0.15f, 0.2f, 0.55f, 1f);

    public HairPlumeConfig BuildBristleConfig()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.name = displayName + "_BristleHair";
        cfg.azimuthBins = 32;
        cfg.lengthBins = 16;
        cfg.maxStrandLengthM = bristleLengthM;
        cfg.scalpRadiusM = ferruleRadiusM;
        cfg.peakHeightM = bristleLengthM;
        cfg.plumeTipHold = plumeTipHold;
        cfg.hairLineCurve = ferruleHairLine ?? HairLineCurve.Constant(1f);
        cfg.hairLineAngleCurve = ferruleAngle ?? HairLineAngleCurve.Zero();
        cfg.conicalEmergenceCurve = conicalCurve != null ? conicalCurve : AnimationCurve.Linear(0f, 1f, 1f, 1.1f);
        cfg.centerPateLocal = ferrulePateLocal;
        cfg.pateAngleBlend = pateAngleBlend;
        cfg.usePhysicsMaterials = false;
        return cfg;
    }

    public float TipSilhouette(float length01) =>
        endCurve != null ? Mathf.Max(0f, endCurve.Evaluate(Mathf.Clamp01(length01))) : 1f;

    public float ConicalFlare(float length01) =>
        conicalCurve != null ? Mathf.Max(0f, conicalCurve.Evaluate(Mathf.Clamp01(length01))) : 1f;
}
