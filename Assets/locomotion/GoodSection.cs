using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Traversability mode for tool-enabled pathfinding (ladder, swing, pick, roll, throw).
/// </summary>
public enum TraversabilityMode
{
    None,
    Climb,
    Swing,
    Pick,
    Roll,
    Throw,
    Custom
}

/// <summary>
/// Physics card ("good section") structure that defines a transition between physical states.
/// Contains impulse action stacks, required/target states, limits, and connections to other sections.
/// </summary>
[System.Serializable]
public class GoodSection
{
    [Header("Section Properties")]
    [Tooltip("Name/identifier of this good section")]
    public string sectionName;

    [Tooltip("Description of what this section does")]
    public string description;

    [Header("Impulse Stack")]
    [Tooltip("Stack of impulse actions to execute for this section")]
    public List<ImpulseAction> impulseStack = new List<ImpulseAction>();

    [Header("State Requirements")]
    [Tooltip("Required starting state for this section to be feasible")]
    public RagdollState requiredState;

    [Tooltip("Target ending state after executing this section")]
    public RagdollState targetState;

    [Header("Limits")]
    [Tooltip("Physical limits for feasibility checking")]
    public SectionLimits limits = new SectionLimits();

    [Header("Connections")]
    [Tooltip("Other good sections reachable from this one (use section names to avoid circular references)")]
    [System.NonSerialized]
    public List<GoodSection> connectedSections = new List<GoodSection>();

    [Tooltip("Names of connected sections (serialized to avoid circular references)")]
    public List<string> connectedSectionNames = new List<string>();

    [Header("Behavior Tree")]
    [Tooltip("Associated behavior tree (optional)")]
    public BehaviorTree behaviorTree;

    [Header("Traversability (tool-use pathfinding)")]
    [Tooltip("When true, this section can be used to traverse gaps (e.g. climb ladder, swing, pick, roll, throw).")]
    public bool enablesTraversability;

    [Tooltip("Mode of traversability. Used by ToolTraversabilityPlanner to match sections to gaps.")]
    public TraversabilityMode traversabilityMode = TraversabilityMode.None;

    [Tooltip("When TraversabilityMode is Custom, optional tag string for matching.")]
    public string traversabilityTag;

    [Tooltip("When set, section is only considered for traversability when (position, t) is inside this 4D volume.")]
    public bool useValidInVolume;

    [Tooltip("4D validity: center of spatial bounds (when useValidInVolume).")]
    public Vector3 validInVolumeCenter;

    [Tooltip("4D validity: size of spatial bounds (when useValidInVolume).")]
    public Vector3 validInVolumeSize = Vector3.one;

    [Tooltip("4D validity: time window start (when useValidInVolume).")]
    public float validInVolumeTMin;

    [Tooltip("4D validity: time window end (when useValidInVolume).")]
    public float validInVolumeTMax = 1f;

    [Tooltip("Optional tool key (e.g. inventory key) required to use this section for traversability.")]
    public string requiredToolKey;

    [Tooltip("Optional reference to the tool GameObject required (ladder, batterang, etc.).")]
    public GameObject requiredTool;

    [Header("Throw (when traversability is Throw or needsToBeThrown)")]
    [Tooltip("When true, this section is a throw-at-target card only; do not use for traversability bridging. Use when goal is GoalType.Throw.")]
    public bool isThrowGoalOnly;

    [Tooltip("When true, this section represents a throw; pathfinding/planner uses thrown object and hand mode.")]
    public bool needsToBeThrown;

    [Tooltip("Object or transform that is thrown (GameObject, Transform, or bone reference).")]
    public UnityEngine.Object thrownObject;

    [Tooltip("Which hand(s) perform the throw.")]
    public ThrowHandMode throwHandMode = ThrowHandMode.Right;

    [Tooltip("Optional min horizontal distance for this throw card (0 = no minimum). Used with ThrowTrajectoryUtility.")]
    public float throwMinRange;

    [Tooltip("Optional max horizontal distance for this throw card (0 = no maximum). Used with ThrowTrajectoryUtility.")]
    public float throwMaxRange;

    // Execution state
    private int currentActionIndex = 0;
    private bool isExecuting = false;
    private RagdollState executionStartState;

    /// <summary>
    /// True if this section is valid for traversability at the given position and time (causality / 4D).
    /// When useValidInVolume is false, always returns true. Otherwise checks position/time against validInVolume* fields.
    /// </summary>
    public bool IsTraversabilityValidAt(Vector3 position, float t)
    {
        if (!useValidInVolume)
            return true;
        Vector3 mn = validInVolumeCenter - validInVolumeSize * 0.5f;
        Vector3 mx = validInVolumeCenter + validInVolumeSize * 0.5f;
        return position.x >= mn.x && position.x <= mx.x && position.y >= mn.y && position.y <= mx.y
            && position.z >= mn.z && position.z <= mx.z && t >= validInVolumeTMin && t <= validInVolumeTMax;
    }

    /// <summary>
    /// True if this section enables traversability and is valid at (position, t).
    /// </summary>
    public bool EnablesTraversabilityAt(Vector3 position, float t)
    {
        return enablesTraversability && traversabilityMode != TraversabilityMode.None && IsTraversabilityValidAt(position, t);
    }

    /// <summary>
    /// True if this section is throw-only (throw-at-target card, not used for traversability bridging).
    /// </summary>
    public bool IsThrowGoalOnly()
    {
        return isThrowGoalOnly || (needsToBeThrown && !enablesTraversability);
    }

    /// <summary>
    /// Check if this section is feasible given the current ragdoll state.
    /// </summary>
    public bool IsFeasible(RagdollState currentState)
    {
        // Check limits
        if (limits != null && !limits.CheckFeasibility(currentState, requiredState))
        {
            return false;
        }

        // Check if current state matches required state (within tolerance)
        if (requiredState != null)
        {
            return requiredState.IsSimilarTo(currentState, tolerance: 0.2f);
        }

        return true; // No required state = always feasible
    }

    /// <summary>
    /// Calculate feasibility score (0-1) for ordering cards.
    /// Higher score = more feasible.
    /// </summary>
    public float CalculateFeasibilityScore(RagdollState currentState)
    {
        float score = 0f;

        // Degrees difference (30% weight)
        float degreesDiff = CalculateDegreesDifference(currentState);
        float degreesScore = 1f - Mathf.Clamp01(degreesDiff / 180f);
        score += degreesScore * 0.3f;

        // Torque feasibility (30% weight)
        float torqueFeasibility = CheckTorqueFeasibility(currentState);
        score += torqueFeasibility * 0.3f;

        // Force feasibility (20% weight)
        float forceFeasibility = CheckForceFeasibility(currentState);
        score += forceFeasibility * 0.2f;

        // Velocity change likelihood (20% weight)
        float velocityLikelihood = EstimateVelocityChangeLikelihood(currentState);
        score += velocityLikelihood * 0.2f;

        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Execute this good section (start execution).
    /// </summary>
    public void Execute(RagdollState currentState)
    {
        if (isExecuting)
        {
            Debug.LogWarning($"GoodSection '{sectionName}' is already executing");
            return;
        }

        if (!IsFeasible(currentState))
        {
            Debug.LogWarning($"GoodSection '{sectionName}' is not feasible in current state");
            return;
        }

        isExecuting = true;
        currentActionIndex = 0;
        executionStartState = currentState.CopyState();

        // Start first action
        if (impulseStack != null && impulseStack.Count > 0)
        {
            impulseStack[0].Execute(currentState);
        }
    }

    /// <summary>
    /// Update this section (call every frame while executing).
    /// Returns true if section is still executing.
    /// </summary>
    public bool Update(RagdollState currentState, float deltaTime)
    {
        if (!isExecuting)
            return false;

        if (impulseStack == null || impulseStack.Count == 0)
        {
            isExecuting = false;
            return false;
        }

        // Update current action
        ImpulseAction currentAction = impulseStack[currentActionIndex];
        if (currentAction == null || !currentAction.Update(currentState, deltaTime))
        {
            // Move to next action
            currentActionIndex++;

            if (currentActionIndex >= impulseStack.Count)
            {
                // All actions complete
                isExecuting = false;
                return false;
            }

            // Start next action
            currentAction = impulseStack[currentActionIndex];
            if (currentAction != null)
            {
                currentAction.Execute(currentState);
            }
        }

        return true;
    }

    /// <summary>
    /// Stop executing this section.
    /// </summary>
    public void Stop()
    {
        if (!isExecuting)
            return;

        // Stop current action
        if (currentActionIndex < impulseStack.Count && impulseStack[currentActionIndex] != null)
        {
            impulseStack[currentActionIndex].Stop();
        }

        isExecuting = false;
        currentActionIndex = 0;
    }

    /// <summary>
    /// Get required muscle activations for this section.
    /// </summary>
    public Dictionary<string, float> GetRequiredMuscleActivations()
    {
        Dictionary<string, float> activations = new Dictionary<string, float>();

        if (impulseStack != null)
        {
            foreach (var action in impulseStack)
            {
                if (action != null && !string.IsNullOrEmpty(action.muscleGroup))
                {
                    // Use maximum activation for this muscle group across all actions
                    if (!activations.ContainsKey(action.muscleGroup))
                    {
                        activations[action.muscleGroup] = action.activation;
                    }
                    else
                    {
                        activations[action.muscleGroup] = Mathf.Max(activations[action.muscleGroup], action.activation);
                    }
                }
            }
        }

        return activations;
    }

    /// <summary>
    /// Calculate degrees difference from current state.
    /// </summary>
    private float CalculateDegreesDifference(RagdollState currentState)
    {
        if (requiredState == null)
            return 0f;

        return requiredState.CalculateDistance(currentState) * 180f; // Rough conversion
    }

    /// <summary>
    /// Check torque feasibility (0-1).
    /// </summary>
    private float CheckTorqueFeasibility(RagdollState currentState)
    {
        if (limits == null || requiredState == null)
            return 1f;

        float torqueReq = limits.maxTorque; // Simplified
        float maxTorque = limits.maxTorque;

        return 1f - Mathf.Clamp01(torqueReq / maxTorque);
    }

    /// <summary>
    /// Check force feasibility (0-1).
    /// </summary>
    private float CheckForceFeasibility(RagdollState currentState)
    {
        if (limits == null || requiredState == null)
            return 1f;

        float forceReq = limits.maxForce; // Simplified
        float maxForce = limits.maxForce;

        return 1f - Mathf.Clamp01(forceReq / maxForce);
    }

    /// <summary>
    /// Estimate velocity change likelihood (0-1).
    /// </summary>
    private float EstimateVelocityChangeLikelihood(RagdollState currentState)
    {
        if (requiredState == null)
            return 1f;

        float velChange = (requiredState.rootVelocity - currentState.rootVelocity).magnitude;
        float maxVelChange = limits != null ? limits.maxVelocityChange : 10f;

        // Likelihood decreases as velocity change increases
        return 1f - Mathf.Clamp01(velChange / maxVelChange);
    }

    /// <summary>
    /// Check if this section is currently executing.
    /// </summary>
    public bool IsExecuting()
    {
        return isExecuting;
    }

    /// <summary>
    /// Get current action index.
    /// </summary>
    public int GetCurrentActionIndex()
    {
        return currentActionIndex;
    }

    /// <summary>
    /// Add a connected section.
    /// </summary>
    public void AddConnectedSection(GoodSection section)
    {
        if (section != null && !connectedSections.Contains(section))
        {
            connectedSections.Add(section);
            if (!string.IsNullOrEmpty(section.sectionName))
            {
                if (connectedSectionNames == null)
                    connectedSectionNames = new List<string>();
                if (!connectedSectionNames.Contains(section.sectionName))
                {
                    connectedSectionNames.Add(section.sectionName);
                }
            }
        }
    }

    /// <summary>
    /// Remove a connected section.
    /// </summary>
    public void RemoveConnectedSection(GoodSection section)
    {
        if (section != null && connectedSections.Contains(section))
        {
            connectedSections.Remove(section);
            if (!string.IsNullOrEmpty(section.sectionName))
            {
                connectedSectionNames.Remove(section.sectionName);
            }
        }
    }

    /// <summary>
    /// Rebuild connected sections from names (call after deserialization).
    /// </summary>
    public void RebuildConnectionsFromNames(List<GoodSection> allSections)
    {
        if (allSections == null || connectedSectionNames == null)
            return;

        connectedSections.Clear();
        foreach (var name in connectedSectionNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;

            var section = allSections.Find(s => s != null && s.sectionName == name);
            if (section != null && !connectedSections.Contains(section))
            {
                connectedSections.Add(section);
            }
        }
    }

    /// <summary>
    /// Update connected section names from current connections.
    /// </summary>
    public void UpdateConnectedSectionNames()
    {
        if (connectedSectionNames == null)
            connectedSectionNames = new List<string>();

        connectedSectionNames.Clear();
        foreach (var section in connectedSections)
        {
            if (section != null && !string.IsNullOrEmpty(section.sectionName))
            {
                if (!connectedSectionNames.Contains(section.sectionName))
                {
                    connectedSectionNames.Add(section.sectionName);
                }
            }
        }
    }
}
