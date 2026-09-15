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
}

[CreateAssetMenu(fileName = "Serger", menuName = "Locomotion/Civil/Serger")]
public sealed class SergerSpec : ScriptableObject
{
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public TayloringIkTrainingCatalog ikCatalog;
    [Range(0f, 1f)] public float needlePhase01;
    [Range(0f, 1f)] public float looperPhase01;
    [Range(0f, 1f)] public float differentialFeed01 = 0.5f;
    public float threadTension01 = 0.45f;
    public float tensionLimit01 = 0.9f;

    public bool IsJammed => threadTension01 > tensionLimit01 + 1e-4f;

    public float DifferentialFeed()
        => Mathf.Repeat(needlePhase01 - looperPhase01 + 1f, 1f);
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
