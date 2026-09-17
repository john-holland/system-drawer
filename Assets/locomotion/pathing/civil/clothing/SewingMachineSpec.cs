using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SewingMachine", menuName = "Locomotion/Civil/Sewing Machine")]
public sealed class SewingMachineSpec : ScriptableObject
{
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public GearboxSpec needleBarGearbox;
    public CutToolKind needleKind = CutToolKind.Needle;
    public List<string> threadGuides = new List<string> { "spool", "tension", "takeup", "needle", "hook", "bobbin" };
    public PlanarSplinePathLocomotion threadingPath;
    public Bounds4SdfInclusionPixelLightMount frameMount;
    public Bounds4SdfInclusionPixelLightMount shellMount;
    public SewingStitchProgram stitchProgram = SewingStitchProgram.DefaultLockstitch();

    public RopeConfig ToRopeConfig(float gauge01)
    {
        float g = Mathf.Clamp01(gauge01);
        var program = stitchProgram ?? SewingStitchProgram.DefaultLockstitch();
        return new RopeConfig
        {
            totalLengthM = Mathf.Max(0.05f, program.ConnectingSpanM()),
            segmentLengthM = 0.02f,
            ropeRadiusM = 0.0004f + g * 0.002f,
            mode = RopeMode.Spool,
            arcBinSizeM = 0.02f,
            ringBufferSize = 8,
            yieldTensionN = 8f + g * 24f,
            breakTensionN = 16f + g * 40f,
            totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink
        };
    }
}

[CreateAssetMenu(fileName = "Serger", menuName = "Locomotion/Civil/Serger")]
public sealed class SergerSpec : ScriptableObject
{
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public TayloringIkTrainingCatalog ikCatalog;
    public Bounds4SdfInclusionPixelLightMount frameMount;
    public Bounds4SdfInclusionPixelLightMount shellMount;
    public SewingStitchProgram stitchProgram = SewingStitchProgram.DefaultOverlock();
    [Range(0f, 1f)] public float needlePhase01;
    [Range(0f, 1f)] public float looperPhase01;
    [Range(0f, 1f)] public float differentialFeed01 = 0.5f;
    public float threadTension01 = 0.45f;
    public float tensionLimit01 = 0.9f;

    public bool IsJammed => threadTension01 > tensionLimit01 + 1e-4f;

    public float DifferentialFeed()
        => Mathf.Repeat(needlePhase01 - looperPhase01 + 1f, 1f);

    public RopeConfig ToRopeConfig(float gauge01)
    {
        float g = Mathf.Clamp01(gauge01);
        var program = stitchProgram ?? SewingStitchProgram.DefaultOverlock();
        return new RopeConfig
        {
            totalLengthM = Mathf.Max(0.08f, program.ConnectingSpanM()),
            segmentLengthM = 0.02f,
            ropeRadiusM = 0.0005f + g * 0.0024f,
            mode = RopeMode.Spool,
            arcBinSizeM = 0.02f,
            ringBufferSize = 8,
            yieldTensionN = 10f + g * 28f,
            breakTensionN = 18f + g * 48f,
            totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink
        };
    }
}

[CreateAssetMenu(fileName = "TayloringIkTrainingCatalog", menuName = "Locomotion/Civil/Tayloring IK Training Catalog")]
public sealed class TayloringIkTrainingCatalog : ScriptableObject
{
    public string[] modeIds =
    {
        "needle_down", "looper", "differential_feed", "presser_foot", "serge_overlock"
    };
    public string defaultSewModeId = "needle_down";
    public string defaultSergeModeId = "serge_overlock";
}
