using UnityEngine;

/// <summary>
/// Authoring defaults for the prebaked procedural hair plume (radial lattice + gaussian manifold).
/// </summary>
[CreateAssetMenu(fileName = "HairPlumeConfig", menuName = "Locomotion/Hair/Plume Config")]
public sealed class HairPlumeConfig : ScriptableObject
{
    public const int CapsuleSlotCount = 10;
    public const int BodyCapsuleSlots = 6;
    public const int DynamicCapsuleSlots = 4;
    public const float GoldenRatio = 1.6180339887f;

    [Header("Radial cache")]
    [Min(8)] public int azimuthBins = 64;
    [Min(4)] public int lengthBins = 32;
    [Min(0.05f)] public float maxStrandLengthM = 0.35f;
    [Min(0.01f)] public float scalpRadiusM = 0.11f;

    [Header("Gaussian plume")]
    [Range(0.01f, 2f)] public float gaussianSigma = 0.45f;
    [Min(0.01f)] public float peakHeightM = 0.28f;
    [Tooltip("0 = flat water (flux-driven tip break); 1 = high hold (integral/mass preserve).")]
    [Range(0f, 1f)] public float plumeTipHold = 0.55f;
    [Tooltip("Scales radial gaussian flux used for tip break / break-spread thinning.")]
    [Range(0f, 2f)] public float gaussianFluxGain = 1f;
    [Tooltip("When true, part bisect also applies lateral flux away from the part line.")]
    public bool usePartLateralFlux = true;
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [Range(0f, 2f)] public float gravityTipGain = 0.35f;

    [Header("Soft tension (toroidal)")]
    [Range(0f, 1f)] public float tensionFalloff = 0.65f;
    [Range(0f, 2f)] public float tensionPartGain = 1f;
    [Range(0f, 1f)] public float resealRate = 0.4f;

    [Header("Capsule defaults")]
    public float headCapsuleRadius = 0.12f;
    public float chestCapsuleRadius = 0.18f;
    public float armCapsuleRadius = 0.05f;
    public float kneeCapsuleRadius = 0.07f;
    public float dynamicScanRadiusM = 0.55f;
    public LayerMask dynamicScanMask = ~0;

    [Header("Physics")]
    [Tooltip("When false, skip PhysX materials / spring integrate; use shader capsule bounce only.")]
    public bool usePhysicsMaterials = false;
    [Range(0f, 1f)] public float shaderBounceGain = 0.35f;

    [Header("Helmet tuck")]
    [Min(2)] public int tuckFrameCount = 8;
    [Min(0.01f)] public float tuckStartRadiusM = 0.22f;
    [Range(0f, 1f)] public float helmetRimUvEdge = 0.92f;

    [Header("Hairline (conical emergence ring)")]
    public HairLineCurve hairLineCurve = HairLineCurve.Constant(1f);
    public HairLineAngleCurve hairLineAngleCurve = HairLineAngleCurve.Zero();
    public AnimationCurve conicalEmergenceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.15f);
    [Tooltip("Local-space pate / crown aim point for emergence averaging.")]
    public Vector3 centerPateLocal = new Vector3(0f, 0.05f, 0f);
    [Range(0f, 1f)] public float pateAngleBlend = 0.35f;
    [Range(0f, 1f)] public float authoredRadialBias = 0.35f;
    [Range(0.1f, 2f)] public float hairLineDefaultRadius = 1f;

    [Header("Hair part (bisects gaussian)")]
    public HairPartSpline hairPartSpline = new HairPartSpline();

    [Header("Curls")]
    [Tooltip("Overall curl strength (0 = straight).")]
    [Range(0f, 1f)] public float curlAmount = 0f;
    [Tooltip("Turns along strand length.")]
    [Range(0.5f, 8f)] public float curlFrequency = 3f;
    [Tooltip("0 = loose waves, 1 = tight ringlets.")]
    [Range(0f, 1f)] public float curlTightness = 0.5f;

    /// <summary>
    /// Apply lattice-bake defaults: hairline, angle→pate, conical flare, and part spline all enabled.
    /// </summary>
    public void ApplyLatticeBakeDefaults()
    {
        if (azimuthBins < 8) azimuthBins = 64;
        if (lengthBins < 4) lengthBins = 32;
        if (scalpRadiusM < 0.01f) scalpRadiusM = 0.11f;
        if (peakHeightM < 0.01f) peakHeightM = 0.28f;
        if (gaussianSigma < 0.01f) gaussianSigma = 0.45f;
        plumeTipHold = Mathf.Clamp01(plumeTipHold <= 0f ? 0.55f : plumeTipHold);
        if (gaussianFluxGain <= 0f) gaussianFluxGain = 1f;
        usePartLateralFlux = true;
        curlAmount = Mathf.Clamp01(curlAmount);
        if (curlFrequency < 0.5f) curlFrequency = 3f;
        curlTightness = Mathf.Clamp01(curlTightness);

        hairLineCurve ??= HairLineCurve.Constant(1f);
        if (hairLineCurve.radiusByAzimuth01 == null || hairLineCurve.radiusByAzimuth01.length == 0)
            hairLineCurve.radiusByAzimuth01 = AnimationCurve.Constant(0f, 1f, 1f);
        if (hairLineCurve.emergenceHeightByAzimuth01 == null || hairLineCurve.emergenceHeightByAzimuth01.length == 0)
            hairLineCurve.emergenceHeightByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f);

        hairLineAngleCurve ??= HairLineAngleCurve.Zero();
        if (hairLineAngleCurve.emergenceAngleDegByAzimuth01 == null ||
            hairLineAngleCurve.emergenceAngleDegByAzimuth01.length == 0)
            hairLineAngleCurve.emergenceAngleDegByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f);

        if (conicalEmergenceCurve == null || conicalEmergenceCurve.length == 0)
            conicalEmergenceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.15f);

        if (centerPateLocal.sqrMagnitude < 1e-8f)
            centerPateLocal = new Vector3(0f, 0.05f, 0f);
        if (pateAngleBlend <= 0f)
            pateAngleBlend = 0.35f;
        if (authoredRadialBias <= 0f)
            authoredRadialBias = 0.35f;
        if (hairLineDefaultRadius < 0.1f)
            hairLineDefaultRadius = 1f;

        hairPartSpline ??= new HairPartSpline();
        hairPartSpline.enabled = true;
        hairPartSpline.EnsureDefaults();
        if (hairPartSpline.partWidthM < 0.001f)
            hairPartSpline.partWidthM = 0.01f;
        if (hairPartSpline.bisectStrength <= 0f)
            hairPartSpline.bisectStrength = 1f;
        if (hairPartSpline.gizmoRibbonHalfWidthM < 0.001f)
            hairPartSpline.gizmoRibbonHalfWidthM = 0.008f;
    }
}
