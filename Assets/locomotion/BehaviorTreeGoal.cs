using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Behavior tree goal structure with cleanup tracking.
/// Represents a goal for the behavior tree to achieve.
/// </summary>
[System.Serializable]
public class BehaviorTreeGoal
{
    [Header("Goal Properties")]
    [Tooltip("Name of the goal (e.g., 'turn_screw', 'pick_up_object')")]
    public string goalName;

    [Tooltip("Type of goal")]
    public GoalType type = GoalType.Movement;

    [Header("Target")]
    [Tooltip("Target object/location for this goal")]
    public GameObject target;

    [Tooltip("Target position for this goal")]
    public Vector3 targetPosition;

    [Tooltip("Target rotation for placement goals")]
    public Quaternion targetRotation = Quaternion.identity;

    [Header("Carry / Hold")]
    [Tooltip("When true (Carry goal), agent re-grasps if object is put down; do not wait for user prompt.")]
    public bool pleaseHold;

    [Header("Hit")]
    [Tooltip("Limb/bone names to use for hit (e.g. RightHand, LeftHand). Empty = solver default.")]
    public List<string> hitLimbNames = new List<string>();
    [Tooltip("Use a tool for hit (weapon/implement).")]
    public bool useToolForHit;
    [Tooltip("Tool GameObject when useToolForHit is true.")]
    public GameObject hitTool;

    [Header("Parameters")]
    [Tooltip("Goal-specific parameters")]
    public Dictionary<string, object> parameters = new Dictionary<string, object>();

    [Header("Cleanup Tracking")]
    [Tooltip("Should we put things back after this goal?")]
    public bool requiresCleanup = false;

    [Tooltip("Tools that need returning after use")]
    public List<GameObject> toolsUsed = new List<GameObject>();

    [Tooltip("Original positions of tools for cleanup")]
    public Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    [Header("Priority and Timing")]
    [Tooltip("Goal priority (higher = more important)")]
    public int priority = 5;

    [Tooltip("Cleanup urgency level")]
    public CleanupUrgency cleanupUrgency = CleanupUrgency.AfterTask;

    [Header("Narrative time / causality (optional)")]
    [Tooltip("When set, goal is only valid when narrative time (seconds) is >= this value. Use float.NegativeInfinity to disable.")]
    public float validAfterNarrativeTime = float.NegativeInfinity;
    [Tooltip("When set, goal is only valid when narrative time (seconds) is <= this value. Use float.PositiveInfinity to disable.")]
    public float validBeforeNarrativeTime = float.PositiveInfinity;
    [Tooltip("When > 0, goal requires NarrativeVolumeQuery.Sample4D(agentPosition, t).causalDepth >= this at query time. 0 = no requirement.")]
    public float requireMinCausalDepth;
}

/// <summary>
/// Types of behavior tree goals.
/// </summary>
public enum GoalType
{
    ToolUsage,
    Movement,
    Interaction,
    Cleanup,
    Composite, // Multiple sub-goals
    /// <summary>Throw at target: goal.target or goal.targetPosition is the throw target; no pathfinding after the throw.</summary>
    Throw,
    /// <summary>Carry an object; optional pleaseHold to re-grasp if put down.</summary>
    Carry,
    /// <summary>Hold an isometric pose (e.g. plank, wall sit); fitness = least movement.</summary>
    Isometric,
    /// <summary>Lift object into place at targetPosition/targetRotation.</summary>
    Place,
    /// <summary>Hit target with limb(s) or tool; track target trajectory, propel limb to meet it.</summary>
    Hit,
    /// <summary>Pick up weight/tool, activate muscle group; fitness = least radial movement (not fall over).</summary>
    Weightlift,
    /// <summary>Fly toward target (wing/jet cards); use procedural flying cards when available.</summary>
    Flying,
    /// <summary>Intercept object with hand(s); goal.target = object to catch.</summary>
    Catch,
    /// <summary>Launch toward target; goal.target or goal.targetPosition = shoot target.</summary>
    Shoot
}

/// <summary>
/// Cleanup urgency levels for tool return goals.
/// </summary>
public enum CleanupUrgency
{
    Immediate,    // Return immediately after use
    AfterTask,    // Return after current task complete
    LowPriority   // Return when convenient
}
