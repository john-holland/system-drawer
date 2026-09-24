using System;
using System.Collections.Generic;
using UnityEngine;

public enum RailDesignType
{
    Classic = 0,
    Modern = 1
}

public enum RailPartRenderMode
{
    Mesh = 0,
    Sdf = 1,
    Both = 2
}

[Serializable]
public sealed class RailSideCurveParams
{
    [Tooltip("Rail head (running surface) cross-section curve.")]
    public AnimationCurve head = AnimationCurve.Linear(0f, 0.04f, 1f, 0.04f);
    [Tooltip("Rail web (vertical stem) thickness curve.")]
    public AnimationCurve web = AnimationCurve.Linear(0f, 0.015f, 1f, 0.015f);
    [Tooltip("Rail foot / base flange curve.")]
    public AnimationCurve foot = AnimationCurve.Linear(0f, 0.07f, 1f, 0.07f);
}

[Serializable]
public sealed class RailPartSpec
{
    public string partId;
    [TextArea] public string tooltip;
    public RailPartRenderMode renderMode = RailPartRenderMode.Mesh;
    public Mesh mesh;
    public ScriptableObject sdfComposition;
    public bool enabled = true;
}

[Serializable]
public sealed class RailSwitchSpec
{
    [Tooltip("Switch point angle (degrees) or use mesh.")]
    public float pointAngleDeg = 12f;
    public float pointRadiusM = 80f;
    public RailPartSpec point = new RailPartSpec { partId = "rail_point", tooltip = "Switch point — angle/radius or mesh." };
    public RailPartSpec frog = new RailPartSpec { partId = "frog", tooltip = "Frog — crossing intersection of rails." };
    public RailPartSpec heel = new RailPartSpec { partId = "heel", tooltip = "Heel — heel of switch point." };
    public RailPartSpec wingRails = new RailPartSpec { partId = "wing_rails", tooltip = "Wing rails beside frog." };
    public RailPartSpec switchRod = new RailPartSpec { partId = "switch_rod", tooltip = "Switch rod linking points." };
    public RailPartSpec headblocks = new RailPartSpec { partId = "headblocks", tooltip = "Headblocks under switch." };
    public string switchOpenCloseTopologyId = "rail_switch";
    public BehaviorTree switchBt;
    [Range(0f, 1f)] public float throw01;
}

[Serializable]
public sealed class RailGroundBakeParams
{
    [Tooltip("Ground quality for heightmap / planetary composition under ballast.")]
    [Range(0f, 1f)] public float groundQuality01 = 0.7f;
    public PhysicsMaterial physicsMaterial;
    public Texture2D groundTexture;
    public Texture2D compositionMask;
    public bool prebakeHeightmap = true;
    public bool prebakeBallastMesh = true;
}

/// <summary>Spline-backed rail track with mesh/SDF part catalog for bake and Rail travel.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Rail Track Structure")]
public sealed class RailTrackStructure : MonoBehaviour
{
    public string railSegmentId = "rail_seg_1";
    public PlanarSplinePathLocomotion alignmentSpline;
    public List<Vector3> controlPoints = new List<Vector3>();

    [Header("Rails")]
    public float railWidthM = 1.435f;
    public RailDesignType designType = RailDesignType.Classic;
    public RailSideCurveParams sideCurves = new RailSideCurveParams();
    public RailPartRenderMode railRenderMode = RailPartRenderMode.Sdf;

    [Header("Sleepers / ballast")]
    public RailPartSpec sleepers = new RailPartSpec
    {
        partId = "sleepers",
        tooltip = "Sleepers / ties — mesh or SDF under rails."
    };
    public RailPartSpec ballast = new RailPartSpec
    {
        partId = "ballast",
        tooltip = "Ballast — noise + SDF/mesh rubble collection for prebake."
    };
    public float ballastNoiseScale = 1.2f;
    public List<Mesh> ballastRubbleMeshes = new List<Mesh>();
    public bool useSubBallast = true;
    [Tooltip("Finer gravel under ballast to prevent mud mixing upward.")]
    public RailPartSpec subBallast = new RailPartSpec { partId = "sub_ballast", tooltip = "Sub-ballast — finer grain gravel." };
    [Tooltip("Fabric layer between stone layers.")]
    public bool useGeotextile;
    public RailPartSpec geotextile = new RailPartSpec { partId = "geotextile", tooltip = "Geotextile fabric between stone layers." };

    [Header("Fasteners")]
    public RailPartSpec spikesClips = new RailPartSpec { partId = "spikes_clips", tooltip = "Rail spikes & clips." };
    public RailPartSpec tiePlate = new RailPartSpec { partId = "tie_plate", tooltip = "Tie plate under rail foot." };
    public RailPartSpec fishplate = new RailPartSpec { partId = "fishplate", tooltip = "Fishplate joining rail ends." };
    public RailPartSpec railAnchor = new RailPartSpec { partId = "rail_anchor", tooltip = "Rail anchor against creep." };

    [Header("Switch / crossing / junction")]
    public RailSwitchSpec railSwitch = new RailSwitchSpec();
    public bool hasCrossing;
    public bool hasJunction;
    public List<RailPartSpec> lightsAndSigns = new List<RailPartSpec>();

    [Header("Ground / bake")]
    public RailGroundBakeParams ground = new RailGroundBakeParams();
    public bool baked;

    static readonly Dictionary<string, RailTrackStructure> ById = new Dictionary<string, RailTrackStructure>();

    void OnEnable()
    {
        if (!string.IsNullOrEmpty(railSegmentId))
            ById[railSegmentId] = this;
        if (alignmentSpline == null)
            alignmentSpline = GetComponent<PlanarSplinePathLocomotion>();
        EnsureSplinePoints();
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(railSegmentId) && ById.TryGetValue(railSegmentId, out var cur) && cur == this)
            ById.Remove(railSegmentId);
    }

    public static RailTrackStructure FindBySegmentId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (ById.TryGetValue(id, out var s) && s != null) return s;
        foreach (var t in FindObjectsByType<RailTrackStructure>(FindObjectsSortMode.None))
            if (t != null && t.railSegmentId == id)
                return t;
        return null;
    }

    public void EnsureSplinePoints()
    {
        if (alignmentSpline == null)
            alignmentSpline = GetComponent<PlanarSplinePathLocomotion>();
        if (alignmentSpline == null)
            alignmentSpline = gameObject.AddComponent<PlanarSplinePathLocomotion>();
        if (controlPoints != null && controlPoints.Count >= 2)
            alignmentSpline.controlPoints = new List<Vector3>(controlPoints);
        else if (alignmentSpline.controlPoints == null || alignmentSpline.controlPoints.Count < 2)
        {
            alignmentSpline.controlPoints = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(0f, 0f, 50f)
            };
        }
        alignmentSpline.defaultWidth = railWidthM;
        alignmentSpline.Rebuild();
    }

    public Vector3 SamplePosition(float t01)
    {
        EnsureSplinePoints();
        return alignmentSpline != null ? alignmentSpline.Evaluate(t01) : transform.position;
    }

    public Vector3 SampleTangent(float t01)
    {
        EnsureSplinePoints();
        return alignmentSpline != null ? alignmentSpline.EvaluateTangent(t01) : transform.forward;
    }

    public void SetSwitchThrow(float throw01)
    {
        railSwitch.throw01 = Mathf.Clamp01(throw01);
        SendMessage("OnNarrativeSchedulerAction", "rail_switch_throw", SendMessageOptions.DontRequireReceiver);
    }

    public void MarkBaked() => baked = true;
}
