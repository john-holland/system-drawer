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
    Composite // Multiple sub-goals
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
