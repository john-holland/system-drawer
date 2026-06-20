using System;
using UnityEngine;

/// <summary>
/// Per-agent multibody travel policy: convoy spacing, static vs soft goal approach, and dynamic actor layers.
/// </summary>
[Serializable]
public class TravelAgentMultibodySettings
{
    [Tooltip("When enabled, TravelMultibodyPathAdjuster post-processes the planner plan against peers and cached actors.")]
    public bool enableMultibody = true;

    [Range(0f, 1f)]
    [Tooltip("Higher = stay closer to the authored polyline (more aggressive progress along path, less lateral push).")]
    public float aggressiveness01 = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Higher = tighter convoy (smaller exclusion radius). Lower = assume others need more berth.")]
    public float confidence01 = 0.7f;

    [Tooltip("How this agent paces relative to cohort peers along the shared route.")]
    public TravelPaceMode paceMode = TravelPaceMode.Keep;

    [Tooltip("When false, near the final target we inflate clearance from static grid obstacles (e.g. park short of a pole instead of grazing it).")]
    public bool shouldCollideWithPathObstacles = true;

    [Tooltip("When set, used as the approach / terminal reference for clearance tuning.")]
    public Transform finalTarget;

    [Tooltip("Used when finalTarget is null.")]
    public Vector3 finalTargetWorld;

    [Tooltip("If true and finalTarget is set, use its position; otherwise use finalTargetWorld.")]
    public bool useFinalTargetTransform = true;

    [Tooltip("Layers for dynamic actors (pedestrians, wildlife, etc.) queried near the route union bounds.")]
    public LayerMask dynamicActorAvoidanceMask = ~0;

    [Min(0.05f)]
    [Tooltip("Base XZ exclusion radius before confidence scaling.")]
    public float clearanceRadius = 0.45f;

    [Min(0.5f)]
    [Tooltip("World radius around resolved final target where static soft-clearance applies when shouldCollide is false.")]
    public float approachRadius = 6f;

    [Range(1, 12)]
    [Tooltip("Iterations of lateral relaxation when resolving convoy overlaps along the path.")]
    public int relaxationIterations = 4;

    [Min(1f)]
    [Tooltip("Multiplier on solver agent radius for static obstacle checks near goal when not colliding.")]
    public float staticClearanceInflate = 1.35f;

    [Min(0.05f)]
    [Tooltip("Extra margin on near-path actor cache bounds.")]
    public float nearPathBoundsMargin = 2.5f;

    [Header("Formation (optional)")]
    [Tooltip("When set with a non-empty formation group id on TravelAgent, waypoints are offset before multibody relaxation.")]
    public TravelFormationAsset formation;

    public TravelFormationWrapDirection formationWrapDirection = TravelFormationWrapDirection.Back;

    [Min(0f)]
    [Tooltip("When > 0, used as spacing between wrap rows; otherwise formation asset defaultWrapRowSpacing is used.")]
    public float formationWrapRowSpacing;

    [Tooltip("When true, wrap row spacing uses clearanceRadius * 2 instead of explicit spacing.")]
    public bool formationRowSpacingUsesClearance;

    [Tooltip("When true, multibody relaxation only considers peers in the same multibodyFormationGroupId (unsafe if group is incomplete).")]
    public bool limitMultibodyPeersToSameFormationGroup;

    public Vector3 ResolveFinalTargetWorld()
    {
        if (useFinalTargetTransform && finalTarget != null)
            return finalTarget.position;
        return finalTargetWorld;
    }

    /// <summary>Row spacing for formation wrap rows (meters).</summary>
    public float ResolveFormationWrapRowSpacing(TravelFormationAsset formationAsset)
    {
        if (formationRowSpacingUsesClearance)
            return Mathf.Max(0.05f, clearanceRadius * 2f);
        if (formationWrapRowSpacing > 0.001f)
            return formationWrapRowSpacing;
        if (formationAsset != null && formationAsset.defaultWrapRowSpacing > 0.001f)
            return formationAsset.defaultWrapRowSpacing;
        return 1.2f;
    }
}
