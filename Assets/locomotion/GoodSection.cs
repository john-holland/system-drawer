using System;
using System.Collections.Generic;
using System.Text;
using Locomotion.Narrative;
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
    Place,
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

    [Header("Physical pathing (medium / drive stubs)")]
    [Tooltip("Physical environment tag for planners and filters.")]
    public PhysicalPathingMedium physicalPathingMedium = PhysicalPathingMedium.Unspecified;

    [Tooltip("Optional custom tag for medium-specific matching.")]
    public string physicalPathingTag;

    [Tooltip("Driving animation phase when this section applies to vehicle/instrument control.")]
    public DriveAnimationPhase driveAnimationPhase = DriveAnimationPhase.None;

    [Tooltip("Vehicle instrument slot id when this section targets driving.")]
    public string driveInstrumentId;

    [Tooltip("Terminal travel leg when this section represents parking/landing.")]
    public TravelLegMode terminalLegMode = TravelLegMode.Walk;

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

    [Tooltip("Multiple tools required for this section. When non-empty, used for multi-tool; when empty, requiredTool is used.")]
    public List<GameObject> requiredTools = new List<GameObject>();

    [Tooltip("Max number of tools this card uses (0 = no limit). When > 0, only first maxToolCount from effective tool list are used.")]
    public int maxToolCount = 0;

    [Tooltip("Max number of targets allowed with this section (default 1).")]
    public int maxTargetCount = 1;

    [Tooltip("Target and any additional targets for this task (redundant with target on goal when single).")]
    public List<GameObject> targets = new List<GameObject>();

    [Tooltip("When true, held tools must make contact with the list of targets per tool (planning checks all targets).")]
    public bool requireAllTargetsToMakeContact;

    [Tooltip("When true, planning only accepts this section if all required tools (up to maxToolCount) can make contact with the goal.")]
    public bool requireAllHeldToolsToMakeContact;

    [Tooltip("Optional max distance for tool-to-goal contact (meters). When > 0, overrides default in ToolContactFeasibility.")]
    public float toolReachDistance;

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

    [Header("Carry")]
    [Tooltip("When true, this section is a carry card (hold object, update transform each frame).")]
    public bool isCarry;
    [Tooltip("Object or transform carried. Used with CarriedObjectAttachment.")]
    public UnityEngine.Object carriedObject;
    [Tooltip("When true, re-grasp if object is put down; do not wait for user prompt.")]
    public bool pleaseHold;
    [Tooltip("When true, omit release card after tool use.")]
    public bool skipRelease;
    [Tooltip("When true, omit return-to-rest cards after hold/use.")]
    public bool skipReturn;
    [Tooltip("Repeat this section N times (e.g. sip-count).")]
    public int repeatCount = 1;
    [Tooltip("Bone/attach point name for carry (e.g. RightHand). Empty = solver default.")]
    public string carryAttachBoneName = "";

    [Header("Isometric")]
    [Tooltip("When true, this section is an isometric hold (plank, wall sit); fitness = least movement.")]
    public bool isIsometric;

    [Header("Place")]
    [Tooltip("When true, this section is a place/lift card (lift object into place).")]
    public bool isPlaceGoal;
    [Tooltip("Offset from ragdoll root for final place position. Optional.")]
    public Vector3 placeTargetOffset = Vector3.zero;

    [Header("Hit")]
    [Tooltip("When true, this section is a hit card (limb or tool meets target).")]
    public bool isHitGoal;
    [Tooltip("Limb/bone name for hit (e.g. RightHand). Can use hitLimbBoneNames for multiple.")]
    public string hitLimbBoneName = "";
    [Tooltip("Multiple limb names for hit. Used when more than one limb can strike.")]
    public List<string> hitLimbBoneNames = new List<string>();
    [Tooltip("Use a tool for this hit.")]
    public bool useToolForHit;
    [Tooltip("Tool GameObject when useToolForHit is true.")]
    public GameObject hitTool;

    [Header("Weightlift")]
    [Tooltip("When true, this section is a weightlift card (pick tool + activate muscle group).")]
    public bool isWeightlift;
    [Tooltip("Weight/tool to pick up for this weightlift.")]
    public GameObject weightliftTool;
    [Tooltip("Muscle group name to activate (e.g. Biceps, Back).")]
    public string weightliftMuscleGroupName = "";

    [Header("Catch")]
    [Tooltip("When true, this section is a catch card (intercept object with hand(s)).")]
    public bool isCatchGoal;
    [Tooltip("Primary hand/bone for catch (e.g. RightHand).")]
    public string catchLimbBoneName = "";
    [Tooltip("Multiple limb names for catch (e.g. RightHand, LeftHand).")]
    public List<string> catchLimbBoneNames = new List<string>();

    [Header("Shoot")]
    [Tooltip("When true, this section is a shoot card (launch toward target).")]
    public bool isShootGoal;
    [Tooltip("Object or transform that is shot (GameObject, Transform, or bone).")]
    public UnityEngine.Object shootLaunchedObject;
    [Tooltip("Which hand(s) perform the shoot.")]
    public ThrowHandMode shootHandMode = ThrowHandMode.Right;
    [Tooltip("Optional min horizontal distance for this shoot card (0 = no minimum).")]
    public float shootMinRange;
    [Tooltip("Optional max horizontal distance for this shoot card (0 = no maximum).")]
    public float shootMaxRange;

    [Header("Sit / Stand-On / Chair")]
    [Tooltip("Sit on surface with hierarchical CoG tow (GoalType.Sit).")]
    public bool isSitGoal;
    [Tooltip("Stand on surface with feet plant tow (GoalType.StandOnSurface).")]
    public bool isStandOnSurfaceGoal;
    [Tooltip("Rotating-chair swivel tool-use card.")]
    public bool isChairRotateGoal;
    [Tooltip("Non-rotating chair schooch / scoot tool-use card.")]
    public bool isChairSchoochGoal;

    [Header("Wrestling")]
    [Tooltip("Wrestling exchange card (GoalType.Wrestling).")]
    public bool isWrestlingGoal;

    [Header("Eat / Toilet / Hygiene")]
    [Tooltip("Eat food card (GoalType.Eat → EatFoodNode).")]
    public bool isEatGoal;
    [Tooltip("Toilet visit card (GoalType.Toilet → ToiletVisitNode).")]
    public bool isToiletGoal;
    [Tooltip("Hygiene card (GoalType.Hygiene → HygieneGoalNode).")]
    public bool isHygieneGoal;
    [Tooltip("When isHygieneGoal: brush_teeth, brush_tongue, floss, wash_hands, shower.")]
    public string hygieneKind;

    [Header("Love Making")]
    [Tooltip("Love making exchange card (GoalType.LoveMaking).")]
    public bool isLoveMakingGoal;

    [Header("Combat")]
    [Tooltip("Combat exchange card (GoalType.Combat).")]
    public bool isCombatGoal;

    [Header("Kitchen / Threat")]
    [Tooltip("Chef / cooking duty card (GoalType.Cooking).")]
    public bool isChefGoal;
    [Tooltip("Scribe / document card (Copy / Illuminate / Bind / Deliver).")]
    public bool isScribeGoal;
    [Tooltip("Threat escalation card (GoalType.Threat).")]
    public bool isThreatGoal;
    [Tooltip("Corrective justice card (GoalType.Justice).")]
    public bool isJusticeGoal;

    [Header("Civil Life")]
    [Tooltip("Building / civic repair card (GoalType.Civic).")]
    public bool isCivicGoal;
    [Tooltip("Civilian personal-duty card (GoalType.Civil).")]
    public bool isCivilGoal;
    [Tooltip("Travel agent / patrol card (GoalType.TravelAgent).")]
    public bool isTravelAgentGoal;
    [Tooltip("Plumbing card (GoalType.Plumbing).")]
    public bool isPlumbingGoal;
    [Tooltip("Polling-place vote card (GoalType.Vote).")]
    public bool isVoteGoal;

    // Execution state
    private int currentActionIndex = 0;
    private bool isExecuting = false;
    private RagdollState executionStartState;

    /// <summary>
    /// Returns the list of tools to use for this section: from requiredTools (up to maxToolCount) or [requiredTool] when requiredTools is empty.
    /// </summary>
    public List<GameObject> GetRequiredToolsList()
    {
        if (requiredTools != null && requiredTools.Count > 0)
        {
            int take = maxToolCount > 0 ? Mathf.Min(maxToolCount, requiredTools.Count) : requiredTools.Count;
            var list = new List<GameObject>(take);
            for (int i = 0; i < take; i++)
            {
                if (requiredTools[i] != null)
                    list.Add(requiredTools[i]);
            }
            return list;
        }
        if (requiredTool != null)
            return new List<GameObject> { requiredTool };
        return new List<GameObject>();
    }

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


// ---- orphan card / parkour / vehicle compile hosts (pending full AssetDB import) ----

public enum ThreatAlertLevel
{
    AllClear = 0, Advisory = 1, OnEdge = 2, Elevated = 3, UnderAttack = 4
}

public enum ThreatLevel
{
    None = 0, PotentialIntruders = 1, LocalizedHazard = 2, ActiveThreat = 3, Critical = 4
}

public enum ThreatKind
{
    Generic = 0, Fire = 1, Intruder = 2, Flood = 3, Chemical = 4, Torture = 5,
    SmokeDetectorBattery = 6, SmokeDetectorAlarm = 7, EquipmentFault = 8
}

public static class ThreatAgencyId
{
    public const string Kitchen = "kitchen";
    public const string BuildingMaintenance = "building_maintenance";
    public const string FireDepartment = "fire_department";
    public const string Security = "security";
    public const string Owner = "owner";
}

[System.Serializable]
public struct ThreatAgencyState
{
    public string agencyId;
    public ThreatAlertLevel alertLevel;
    public ThreatLevel threatLevel;
    public float alertScore01;
    public float threatScore01;
    public string lemmaTag;
}

public enum JusticeAction
{
    ShutOffHeat = 0, SecureArea = 1, Evict = 2, Arrest = 3, Deescalate = 4, CallAuthorities = 5
}

[System.Serializable]
public class ThreatCard : GoodSection
{
    [Header("Threat")]
    public ThreatKind threatKind = ThreatKind.Generic;
    public ThreatAlertLevel alertLevel = ThreatAlertLevel.OnEdge;
    public ThreatLevel threatLevel = ThreatLevel.LocalizedHazard;
    public string alertLemma = "on-edge";
    public GameObject contextOwner;
    public GameObject reportedSource;
    public List<string> telecomContacts = new List<string>();
    public List<string> resolutionHints = new List<string>();
    public float requiredWaterPourLitersPerSec;
    public float requiredBucketVolumeLiters;
    public bool preferExtinguisher;
    public bool preferGrainSmother;

    public ThreatCard()
    {
        isThreatGoal = true;
        physicalPathingTag = "threat";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "threat";
    }

    public bool MeetsThreatRequirements(GameObject actor, GameObject context = null)
    {
        if (actor == null) return false;
        // Anyone in hierarchy can hold the card; tool gates checked by solvers
        return true;
    }

    public static ThreatCard Generate(
        ThreatKind kind,
        GameObject contextOwner,
        GameObject source,
        ThreatAlertLevel alert = ThreatAlertLevel.OnEdge)
    {
        var card = new ThreatCard
        {
            threatKind = kind,
            contextOwner = contextOwner,
            reportedSource = source,
            alertLevel = alert,
            sectionName = $"threat_{kind}",
            description = kind.ToString(),
            isThreatGoal = true,
            physicalPathingTag = $"threat_{kind.ToString().ToLowerInvariant()}",
            alertLemma = LemmaFor(alert, kind),
            telecomContacts = DefaultContacts(kind),
            resolutionHints = DefaultHints(kind)
        };
        if (kind == ThreatKind.Fire || kind == ThreatKind.SmokeDetectorAlarm)
        {
            card.requiredWaterPourLitersPerSec = 0.8f;
            card.requiredBucketVolumeLiters = 8f;
            card.preferExtinguisher = true;
            card.threatLevel = ThreatLevel.ActiveThreat;
        }
        else if (kind == ThreatKind.SmokeDetectorBattery)
        {
            card.threatLevel = ThreatLevel.LocalizedHazard;
            card.telecomContacts.Add(ThreatAgencyId.BuildingMaintenance);
        }
        return card;
    }

    public static string LemmaFor(ThreatAlertLevel alert, ThreatKind kind)
    {
        if (alert == ThreatAlertLevel.AllClear) return "all-clear";
        if (alert == ThreatAlertLevel.UnderAttack) return "under-attack";
        if (kind == ThreatKind.Torture) return LegalLemmaPropertyKeys.Torture;
        if (kind == ThreatKind.Intruder) return "potential-intruders";
        if (alert >= ThreatAlertLevel.OnEdge) return "on-edge";
        return "advisory";
    }

    public static List<string> DefaultContacts(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Fire:
            case ThreatKind.SmokeDetectorAlarm:
                return new List<string> { ThreatAgencyId.FireDepartment, ThreatAgencyId.BuildingMaintenance, ThreatAgencyId.Kitchen };
            case ThreatKind.SmokeDetectorBattery:
            case ThreatKind.EquipmentFault:
                return new List<string> { ThreatAgencyId.BuildingMaintenance };
            case ThreatKind.Intruder:
            case ThreatKind.Torture:
                return new List<string> { ThreatAgencyId.Security, ThreatAgencyId.Owner };
            default:
                return new List<string> { ThreatAgencyId.Owner };
        }
    }

    public static List<string> DefaultHints(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Fire:
                return new List<string> { "extinguisher", "water_pour", "grain_smother", "shut_off_heat" };
            case ThreatKind.SmokeDetectorBattery:
                return new List<string> { "replace_battery", "telecom_maintenance" };
            default:
                return new List<string> { "investigate", "telecom" };
        }
    }
}

[System.Serializable]
public class JusticeCard : GoodSection
{
    [Header("Justice")]
    public JusticeAction justiceAction = JusticeAction.ShutOffHeat;
    public GameObject hazardTarget;
    [Range(0f, 1f)] public float maxBurnInjury01 = 0.55f;
    public bool requireNotBurnedTooBadly = true;

    [Header("Violence threshold")]
    [Tooltip("Threat intensity required before physical response; below this civilians flee / de-escalate.")]
    [Range(0f, 1f)] public float violenceThreshold01 = 0.65f;
    [Tooltip("Developer in-painting / persona override added to threshold (negative = easier to snap).")]
    [Range(-0.5f, 0.5f)] public float statusAttributeBias01;
    [Tooltip("Temporary snap depression (Travis-style); lowers effective threshold.")]
    [Range(0f, 1f)] public float snapDepression01;
    [Tooltip("Default civilian graft: flee unless ShouldRespondPhysically.")]
    public bool defaultFleeUnlessTriggered = true;

    public JusticeCard()
    {
        isJusticeGoal = true;
        physicalPathingTag = "justice";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "justice";
    }

    public float EffectiveViolenceThreshold01()
    {
        return Mathf.Clamp01(violenceThreshold01 + statusAttributeBias01 - snapDepression01);
    }

    /// <summary>True when threat intensity meets/exceeds the (biased) violence threshold.</summary>
    public bool ShouldRespondPhysically(GameObject actor, float threatIntensity01)
    {
        float thr = EffectiveViolenceThreshold01();
        if (actor != null)
        {
            var sheet = actor.GetComponent<LifeSystemsSheet>();
            if (sheet != null)
            {
                // Adrenaline lowers threshold slightly (easier to respond).
                float adr = sheet.Get01(LifeSystemsChannelCatalog.Adrenaline);
                thr = Mathf.Clamp01(thr - adr * 0.15f);
            }
        }
        return threatIntensity01 >= thr;
    }

    /// <summary>Apply a temporary snap (lowers threshold); decays externally.</summary>
    public void ApplySnap(float amount01)
    {
        snapDepression01 = Mathf.Clamp01(snapDepression01 + Mathf.Max(0f, amount01));
    }

    public bool MeetsJusticeRequirements(GameObject actor, GameObject target = null)
    {
        GameObject t = target != null ? target : hazardTarget;
        if (actor == null) return false;
        if (justiceAction == JusticeAction.ShutOffHeat && t == null)
            return false;
        if (requireNotBurnedTooBadly && actor != null)
        {
            var limbs = actor.GetComponent<LimbIntegrityState>();
            if (limbs != null)
            {
                // If limb integrity exposes overall damage, gate; otherwise allow
                // Soft gate via LifeSystems fatigue/adrenaline as burn proxy
                var sheet = actor.GetComponent<LifeSystemsSheet>();
                if (sheet != null)
                {
                    float painProxy = sheet.Get01(LifeSystemsChannelCatalog.Fatigue);
                    if (painProxy > maxBurnInjury01)
                        return false;
                }
            }
        }
        return true;
    }

    public static JusticeCard Generate(JusticeAction action, GameObject hazard, RagdollState state = null)
    {
        return new JusticeCard
        {
            justiceAction = action,
            hazardTarget = hazard,
            sectionName = $"justice_{action}",
            description = action.ToString(),
            isJusticeGoal = true,
            physicalPathingTag = $"justice_{action.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 80f, maxTorque = 20f, maxVelocityChange = 1.5f },
            violenceThreshold01 = 0.65f,
            defaultFleeUnlessTriggered = true
        };
    }
}

[System.Serializable]
public class ChefCard : GoodSection
{
    [Header("Chef")]
    public ChefDutyMode dutyMode = ChefDutyMode.Line;
    public ChefActivity activity = ChefActivity.Place;
    public GameObject stationOrTarget;
    public GameObject ingredientOrTool;
    public List<string> dutyChecklist = new List<string>();
    public List<ChefMaterialEvolutionCard> evolutionCards = new List<ChefMaterialEvolutionCard>();
    public string orderTicketId;
    public float pourRateLitersPerSec = 0.5f;
    public float accuracy01 = 0.85f;
    public bool requireCleanHands;

    public ChefCard()
    {
        isChefGoal = true;
        physicalPathingTag = "chef";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "kitchen";
    }

    public bool MeetsChefRequirements(GameObject actor, GameObject target = null, RagdollSystem actorRagdoll = null)
    {
        GameObject t = target != null ? target : stationOrTarget;
        if (activity == ChefActivity.Idle)
            return actor != null;
        if (t == null && activity != ChefActivity.WashHands && activity != ChefActivity.Plating)
            return false;
        if (requireCleanHands && actor != null)
        {
            var life = actor.GetComponent<LifeSystemsSheet>();
            if (life != null && life.Get01(LifeSystemsChannelCatalog.Ablution) < 0.35f)
                return false;
        }
        return true;
    }

    public static ChefCard Generate(
        ChefDutyMode mode,
        ChefActivity activity,
        GameObject stationOrTarget,
        RagdollState state = null)
    {
        var card = new ChefCard
        {
            dutyMode = mode,
            activity = activity,
            stationOrTarget = stationOrTarget,
            sectionName = $"chef_{mode}_{activity}",
            description = $"{mode} {activity}",
            isChefGoal = true,
            physicalPathingTag = $"chef_{activity.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 120f, maxTorque = 40f, maxVelocityChange = 2f },
            dutyChecklist = DefaultChecklist(activity)
        };
        return card;
    }

    public static List<string> DefaultChecklist(ChefActivity activity)
    {
        switch (activity)
        {
            case ChefActivity.Sear:
                return new List<string> { "heat_on", "oil_present", "flip_once", "rest" };
            case ChefActivity.Filet:
                return new List<string> { "stabilize", "cut_along_bone", "portion" };
            case ChefActivity.Pour:
            case ChefActivity.Sprinkle:
                return new List<string> { "aim", "meter_flow", "stop" };
            case ChefActivity.WashHands:
                return new List<string> { "wet", "soap", "lather", "rinse", "dry" };
            case ChefActivity.SeasonPan:
                return new List<string> { "scrape", "clean", "season", "park" };
            case ChefActivity.WashDish:
                return new List<string> { "pick", "scrub_or_rinse", "place", "tool_return" };
            default:
                return new List<string> { activity.ToString().ToLowerInvariant() };
        }
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return activity.ToString();
        return $"{activity}: {string.Join(",", dutyChecklist)}";
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Kitchen/Consider Chef Cards")]
public sealed class ConsiderChefCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public ChefDutyMode dutyMode = ChefDutyMode.Line;
    public float scanRangeM = 6f;
    public LayerMask stationMask = ~0;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null) actorRagdoll = GetComponent<RagdollSystem>();
    }

    public List<GoodSection> GenerateCards(GameObject forcedStation = null)
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;
        if (forcedStation != null)
            EmitForStation(forcedStation, state);
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, stationMask, QueryTriggerInteraction.Collide);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var root = c.transform.root != null ? c.transform.root.gameObject : c.gameObject;
                if (!seen.Add(root)) continue;
                if (root == gameObject) continue;
                EmitForStation(root, state);
            }
            if (_generated.Count == 0)
                _generated.Add(MakeDefaultCard());
        }
        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    void EmitForStation(GameObject station, RagdollState state)
    {
        var activities = PreferredForMode(dutyMode);
        for (int i = 0; i < activities.Length; i++)
        {
            var card = ChefCard.Generate(dutyMode, activities[i], station, state);
            if (!card.MeetsChefRequirements(gameObject, station, actorRagdoll))
                continue;
            _generated.Add(card);
        }
    }

    public static ChefActivity[] PreferredForMode(ChefDutyMode mode)
    {
        switch (mode)
        {
            case ChefDutyMode.Prep:
                return new[] { ChefActivity.Cut, ChefActivity.Filet, ChefActivity.Pour, ChefActivity.Place };
            case ChefDutyMode.Pass:
            case ChefDutyMode.Expo:
                return new[] { ChefActivity.Plating, ChefActivity.Place };
            case ChefDutyMode.Hygiene:
                return new[] { ChefActivity.WashHands, ChefActivity.CleanStation, ChefActivity.SeasonPan };
            case ChefDutyMode.Dish:
                return new[] { ChefActivity.WashDish, ChefActivity.WashHands, ChefActivity.CleanStation };
            default:
                return new[] { ChefActivity.Sear, ChefActivity.Stir, ChefActivity.Place, ChefActivity.Pour };
        }
    }

    public static ChefCard MakeDefaultCard() =>
        ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Place, null);
}

[System.Serializable]
public class VoterCard : GoodSection
{
    public GameObject ballotUiHost;
    public BallotSpec ballot;
    public string demographicSliceId;
    public VotingPlaceCard place;
    public string chosenOptionId;
    public bool hasChosen;

    public VoterCard()
    {
        isVoteGoal = true;
        isCivilGoal = true;
        sectionName = "voter";
        physicalPathingTag = "vote";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "vote";
    }

    public bool BlockedByDeveloperInpaint() =>
        place != null && place.developerInpaint;

    public static VoterCard GenerateDefault(GameObject target = null)
    {
        return new VoterCard { ballotUiHost = target };
    }
}

[System.Serializable]
public class CivicCard : GoodSection
{
    [Header("Civic")]
    public CivicDutyKind duty = CivicDutyKind.Repair;
    public GameObject buildingOrTarget;
    public GameObject damagedObject;
    public string buildingStableId;
    public string damagedObjectId;
    public string waypointGroup;
    public List<string> dutyChecklist = new List<string>();
    public bool requireRepairZoneOpen = true;

    public CivicCard()
    {
        isCivicGoal = true;
        physicalPathingTag = "civic";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "civic";
    }

    public bool MeetsCivicRequirements(GameObject actor, GameObject target = null)
    {
        if (actor == null) return false;
        if (duty == CivicDutyKind.Inspect)
            return buildingOrTarget != null || target != null || !string.IsNullOrEmpty(buildingStableId);
        return damagedObject != null || target != null || !string.IsNullOrEmpty(damagedObjectId)
               || buildingOrTarget != null;
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return duty.ToString();
        return $"{duty}: {string.Join(",", dutyChecklist)}";
    }

    public static CivicCard Generate(
        CivicDutyKind duty,
        GameObject buildingOrTarget,
        GameObject damagedObject = null,
        RagdollState state = null)
    {
        return new CivicCard
        {
            duty = duty,
            buildingOrTarget = buildingOrTarget,
            damagedObject = damagedObject,
            sectionName = $"civic_{duty}",
            description = duty.ToString(),
            isCivicGoal = true,
            physicalPathingTag = $"civic_{duty.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 100f, maxTorque = 30f, maxVelocityChange = 1.8f },
            dutyChecklist = DefaultChecklist(duty)
        };
    }

    public static List<string> DefaultChecklist(CivicDutyKind duty)
    {
        switch (duty)
        {
            case CivicDutyKind.Inspect:
                return new List<string> { "arrive", "survey", "report" };
            case CivicDutyKind.Repair:
                return new List<string> { "stage", "open_zone", "repair", "verify", "close_zone" };
            case CivicDutyKind.Replace:
                return new List<string> { "remove", "install", "verify" };
            case CivicDutyKind.Secure:
                return new List<string> { "cordon", "hold", "handoff" };
            case CivicDutyKind.Clean:
                return new List<string> { "clear_debris", "wipe", "dispose" };
            default:
                return new List<string> { duty.ToString().ToLowerInvariant() };
        }
    }
}

public static class ConsiderCivicCards
{
    public static CivicCard MakeDefaultCard() => new CivicCard { sectionName = "civic_default", isCivicGoal = true };
}

[System.Serializable]
public class CivilCard : GoodSection
{
    [Header("Civil")]
    public CivilianDutyKind civicDuty = CivilianDutyKind.WorkShift;
    public string personaKey;
    public string venueStableId;
    public GameObject venueOrTarget;
    public string scheduleSlotId;
    public List<string> dutyChecklist = new List<string>();
    [Tooltip("When true, content filters may hide this duty from default catalogs.")]
    public bool crassOrOptional;

    public CivilCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "civil";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "civil";
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return civicDuty.ToString();
        return $"{civicDuty}: {string.Join(",", dutyChecklist)}";
    }

    public static CivilCard Generate(
        CivilianDutyKind duty,
        string personaKey = null,
        GameObject venue = null,
        RagdollState state = null)
    {
        bool optional = duty == CivilianDutyKind.PrivateLeisure || duty == CivilianDutyKind.FakeLibraryCard;
        return new CivilCard
        {
            civicDuty = duty,
            personaKey = personaKey ?? "",
            venueOrTarget = venue,
            sectionName = $"civil_{duty}",
            description = duty.ToString(),
            isCivilGoal = true,
            crassOrOptional = optional,
            physicalPathingTag = $"civil_{duty.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 60f, maxTorque = 20f, maxVelocityChange = 1.5f },
            dutyChecklist = DefaultChecklist(duty)
        };
    }

    public static List<string> DefaultChecklist(CivilianDutyKind duty)
    {
        switch (duty)
        {
            case CivilianDutyKind.Commute:
                return new List<string> { "leave", "travel", "arrive" };
            case CivilianDutyKind.FleeThreat:
                return new List<string> { "assess", "flee", "shelter" };
            case CivilianDutyKind.WorkShift:
                return new List<string> { "clock_in", "duty", "clock_out" };
            case CivilianDutyKind.JobSearch:
                return new List<string> { "intake", "browse_board", "apply" };
            case CivilianDutyKind.BenefitsClaim:
                return new List<string> { "window", "claim", "receipt" };
            case CivilianDutyKind.CareerInterview:
                return new List<string> { "arrive", "interview", "depart" };
            case CivilianDutyKind.JobTraining:
                return new List<string> { "station", "practice", "credential" };
            default:
                return new List<string> { duty.ToString().ToLowerInvariant() };
        }
    }
}

public static class ConsiderCivilCards
{
    public static CivilCard MakeDefaultCard() => new CivilCard { sectionName = "civil_default", isCivilGoal = true };
}

[System.Serializable]
public class ClogToiletCard : GoodSection { }
[System.Serializable]
public class PlungeToiletCard : GoodSection
{
    public ToiletFixture toilet;
    [Range(0f, 1f)] public float clear01 = 0.55f;
    [Range(0f, 1f)] public float mixPerturbation01 = 0.35f;

    public void Apply() => toilet?.plumbing?.clog?.Plunge(clear01, mixPerturbation01);

    public static PlungeToiletCard Generate(ToiletFixture fixture) =>
        new PlungeToiletCard { toilet = fixture, sectionName = "plunge_toilet", isPlumbingGoal = true };
}
[System.Serializable]
public class SnakeToiletCard : GoodSection
{
    public ToiletFixture toilet;
    [Range(0f, 1f)] public float clear01 = 0.85f;

    public void Apply() => toilet?.plumbing?.clog?.SnakeClear(clear01);

    public static SnakeToiletCard Generate(ToiletFixture fixture, GameObject tool = null) =>
        new SnakeToiletCard { toilet = fixture, sectionName = "snake_toilet", isPlumbingGoal = true };
}

public static class ConsiderPlumbingCards
{
    public static GoodSection MakeDefaultCard() => new PlungeToiletCard { sectionName = "plumbing_default" };
}

[System.Serializable]
public class ConstructionPhaseCard : GoodSection
{
    [Range(0f, 1f)] public float progress01;
    public bool IsComplete => progress01 >= 1f - 1e-4f;
    public void AddProgress(float delta) => progress01 = Mathf.Clamp01(progress01 + Mathf.Max(0f, delta));
    public static ConstructionPhaseCard GenerateDefault(Vector3 pos) =>
        new ConstructionPhaseCard { sectionName = "construction_phase" };
}

public sealed class CardHistoryManager : MonoBehaviour
{
    public static CardHistoryManager Instance { get; private set; }
    public int historyBufferSize = 5000;
    readonly List<CardHistorySnapshot> _history = new List<CardHistorySnapshot>();
    public int HistoryCount => _history.Count;
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void SetBufferSize(int size)
    {
        historyBufferSize = Mathf.Max(16, size);
        while (_history.Count > historyBufferSize)
            _history.RemoveAt(0);
    }

    public void RecordCard(GoodSection card, object solver, string tag)
    {
        string sid = solver is Component c && c != null ? c.gameObject.name : "";
        Push(CardHistorySnapshot.FromCard(card, sid, tag));
    }

    public void RecordPool(PhysicsCardSolver solver, string tag)
    {
        if (solver?.availableCards == null) return;
        for (int i = 0; i < solver.availableCards.Count; i++)
            RecordCard(solver.availableCards[i], solver, tag);
    }

    void Push(CardHistorySnapshot snap)
    {
        if (snap == null) return;
        _history.Add(snap);
        while (_history.Count > historyBufferSize)
            _history.RemoveAt(0);
    }

    public IReadOnlyList<CardHistorySnapshot> GetHistoryNewestFirst(int max = 200)
    {
        int n = Mathf.Min(max, _history.Count);
        var list = new List<CardHistorySnapshot>(n);
        for (int i = _history.Count - 1; i >= 0 && list.Count < n; i--)
            list.Add(_history[i]);
        return list;
    }

    public void ClearHistory() => _history.Clear();

    public IReadOnlyList<CardHistorySnapshot> CopyActiveFrom(PhysicsCardSolver solver)
    {
        var list = new List<CardHistorySnapshot>();
        if (solver?.availableCards == null) return list;
        string sid = solver.gameObject != null ? solver.gameObject.name : "";
        for (int i = 0; i < solver.availableCards.Count; i++)
            list.Add(CardHistorySnapshot.FromCard(solver.availableCards[i], sid, "active"));
        return list;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Helicopter Vehicle Ragdoll")]
public sealed class HelicopterVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string craftName = "Helicopter";
    public string callsign = "CUU-H1";
    public string prefabId;
    public MagnetoHelicopterConfigurationAsset configurationAsset;

    [Header("Magnetos")]
    public List<MagnetoLiftParams> magnetos = new List<MagnetoLiftParams>();
    public MagnetoLiftParams tailRotor = new MagnetoLiftParams { magnetoId = "tail", spanLength = 2.5f };
    public MagnetoLiftRequirements requirements = new MagnetoLiftRequirements();
    public List<Transform> magnetoAnchors = new List<Transform>();

    [Header("Cabin")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> grabBars = new List<Transform>();
    public List<Transform> standupSupportBarAnchors = new List<Transform>();
    public bool standupSupportBars = true;
    public GrabBarShape grabBarShape = GrabBarShape.Rail;
    public bool hasBathroom;
    public Transform bathroomAnchor;
    public bool bathroomOccupied;
    public string bathroomDoorOpenCloseTopologyId = "bathroom_door";
    public bool hasKitchen;
    public RestaurantVenueRuntime galleyKitchen;
    public CompanyRegistration galleyCompany;
    public string parentKitchenCompanyId;

    [Header("Doors / gear")]
    public string doorOpenCloseTopologyId = "heli_door";
    public BehaviorTree doorOpenCloseBt;
    public string landingGearOpenCloseTopologyId = "heli_landing_gear";
    public BehaviorTree landingGearOverrideBt;
    public bool landingGearDown = true;

    [Header("Instruments")]
    public List<Component> instrumentProxies = new List<Component>();
    public string magnetoCollectiveSurfaceId = HelicopterLemmaPropertyKeys.MagnetoCollective;
    public string magnetoCyclicSurfaceId = HelicopterLemmaPropertyKeys.MagnetoCyclic;
    public string tailRudderSurfaceId = HelicopterLemmaPropertyKeys.TailRudder;
    public string accelerationSurfaceId = HelicopterLemmaPropertyKeys.Acceleration;
    public string airBrakeSurfaceId = HelicopterLemmaPropertyKeys.AirBrake;

    [Header("Telecom / GPS")]
    public Component telecomBridge;
    public Transform gpsWebtopMount;
    public PilotGpsHudWebtop gpsHud;
    public UnityRenderPortal renderPortal;
    public string gpsPortalId = "gps";
    public PilotGpsHudMode defaultHudMode = PilotGpsHudMode.BakedRoute;
    public string webtopUrl = "http://127.0.0.1:5175";

    [Header("Lights / slots")]
    public List<PixelLightGridMountGameObject> lightMounts = new List<PixelLightGridMountGameObject>();
    public List<HelicoptorGridSlotGameObject> gridSlots = new List<HelicoptorGridSlotGameObject>();
    [Tooltip("Per view×scope PixelLight settings + multi grid-slot catalog.")]
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    [Header("Route")]
    public bool insertLandingQueue = true;
    public string activeFlightId;

    protected override void Awake()
    {
        base.Awake();
        EnsureSystems();
        if (magnetos.Count == 0)
            magnetos.Add(new MagnetoLiftParams { magnetoId = "main" });
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (telecomBridge == null)
            telecomBridge = GetComponent("TelecomUnityBridge");
        if (configurationAsset != null)
            configurationAsset.ApplyTo(this);
        for (int i = 0; i < magnetos.Count; i++)
            magnetos[i]?.RecomputeTipEndCache(transform);
    }

    public void EnsureSystems()
    {
        if (gpsHud == null)
            gpsHud = GetComponent<PilotGpsHudWebtop>() ?? gameObject.AddComponent<PilotGpsHudWebtop>();
        gpsHud.helicopter = this;
        gpsHud.mode = defaultHudMode;

        if (renderPortal == null)
            renderPortal = GetComponent<UnityRenderPortal>() ?? gameObject.AddComponent<UnityRenderPortal>();
        renderPortal.portalId = gpsPortalId;
        renderPortal.sourceTexture = gpsHud.displayTexture;
        renderPortal.BindTelecom(telecomBridge);

        if (hasKitchen && galleyKitchen == null)
            galleyKitchen = GetComponentInChildren<RestaurantVenueRuntime>();
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>();
        if (!string.IsNullOrEmpty(parentKitchenCompanyId) && galleyCompany != null)
            galleyCompany.parentCompanyId = parentKitchenCompanyId;
    }

    public MagnetoLiftParams MainMagneto =>
        magnetos != null && magnetos.Count > 0 ? magnetos[0] : null;

    public void ApplyRequirementsToSelected(int magnetoIndex)
    {
        if (magnetos == null || magnetoIndex < 0 || magnetoIndex >= magnetos.Count) return;
        requirements?.ApplyMinimumsTo(magnetos[magnetoIndex]);
    }

    public void SetLandingGearDown(bool down)
    {
        landingGearDown = down;
        SendMessage("OnNarrativeSchedulerAction",
            down ? HelicopterNarrativeActionIds.GearDown : HelicopterNarrativeActionIds.GearUp,
            SendMessageOptions.DontRequireReceiver);
    }

    public Transform ResolveGrabOrStandup(Transform seat)
    {
        if (seat == null) return null;
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 origin = seat.position + seat.forward * 0.4f;
        void Consider(List<Transform> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                float d = (list[i].position - origin).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = list[i]; }
            }
        }
        Consider(grabBars);
        if (standupSupportBars)
            Consider(standupSupportBarAnchors);
        return best;
    }
}
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Vehicle Ragdoll")]
public sealed class AirplaneVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string planeName = "Airliner";
    public string callsign = "CUU-A1";
    public string prefabId;
    public string[] pilotPersonaKeys = { "pilot" };
    public AirplaneConfigurationAsset configurationAsset;

    [Header("Cabin")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> seatTrays = new List<Transform>();
    public string seatTrayOpenCloseTopologyId = "seat_tray";
    public bool allowPassengerTalk = true;
    public bool hasBathroom = true;
    public Transform bathroomAnchor;
    public bool bathroomOccupied;
    public string bathroomDoorOpenCloseTopologyId = "bathroom_door";

    [Header("Galley")]
    public RestaurantVenueRuntime galleyKitchen;
    public CompanyRegistration galleyCompany;
    public string parentKitchenCompanyId;

    [Header("Flight ops")]
    public Transform cockpit;
    public Component telecomBridge;
    public bool webtopEnabled = true;
    public string activeFlightId;
    public string cabinMusicTrackId;
    public bool cabinMusicPlaying;
    public float fuelTankCapacity = 100f;
    [Range(0f, 1f)] public float fuel01 = 1f;
    public string engineGooseContents;
    public AirportExtensionGate dockedGate;

    [Header("Aero")]
    public AirplaneWingSurfaceParams leftWing = new AirplaneWingSurfaceParams { surfaceId = "left_wing" };
    public AirplaneWingSurfaceParams rightWing = new AirplaneWingSurfaceParams
    {
        surfaceId = "right_wing",
        centerlineAngleDeg = 180f
    };
    public AirplaneWingSurfaceParams horizontalTail = new AirplaneWingSurfaceParams { surfaceId = "h_tail", spanLength = 12f };
    public AirplaneWingSurfaceParams verticalTail = new AirplaneWingSurfaceParams
    {
        surfaceId = "v_tail",
        spanLength = 8f,
        centerlineAngleDeg = 90f
    };
    public AirplaneEllipsoidAeroParams fuselageEllipsoid = new AirplaneEllipsoidAeroParams();
    public List<AirplaneJetEngineParams> jets = new List<AirplaneJetEngineParams>();
    public AirplaneWeatherAeroBridge weatherAeroBridge;

    [Header("Power")]
    public List<AirplaneBatteryPack> batteries = new List<AirplaneBatteryPack>();
    public List<AirplanePowerSystemDraw> powerSystems = new List<AirplanePowerSystemDraw>();
    public float chargeKwWhenEnginesOn = 25f;
    public AirplaneBioRhythm airplaneBio;
    public AirplaneCabinMusicSystem cabinMusicSystem;
    public AirplaneCabinMusicSource defaultMusicSource = AirplaneCabinMusicSource.Chorus;
    public bool paDucksMusic = true;
    public int seatbackWebtopCount = 120;
    public int seatPowerOutletCount = 120;
    public float seatOutletDrawKwEach = 0.05f;
    public float seatbackWebtopDrawKwEach = 0.02f;
    public bool seatbackWebtopsEnabled = true;

    [Header("Topology / systems")]
    public string noseOpenCloseTopologyId = "concorde_nose";
    public string landingGearOpenCloseTopologyId = "landing_gear";
    public BehaviorTree landingGearOverrideBt;
    public bool landingGearDown = true;
    public string ejectorSeatOpenCloseTopologyId;
    public string weaponBayOpenCloseTopologyId;
    public string webtopOpenCloseTopologyId = "cabin_webtop";
    public string seatbackWebtopOpenCloseTopologyId = "seatback_webtop";
    public WebtopUscVideoPlayer webtopPlayer;
    public bool windshieldWipersOn;
    public bool radiationPathingEnabled;

    [Header("Route / ATC")]
    public bool insertLandingQueue = true;
    public bool insertRefuelBeforePark = true;
    [Range(0f, 1f)] public float refuelFuelThreshold01 = 0.35f;
    public string defaultDestinationAtcServiceId;
    public AtcDispatcherDialogueCatalog dialogueCatalog = new AtcDispatcherDialogueCatalog();
    public TSAChecklistCard checklistTemplate;

    [Header("Biplane magnetos / GPS")]
    public List<MagnetoLiftParams> magnetos = new List<MagnetoLiftParams>();
    public PilotGpsHudWebtop gpsHud;
    public UnityRenderPortal renderPortal;

    [Header("PixelLight multi-slot")]
    public List<PixelLightGridMountGameObject> lightMounts = new List<PixelLightGridMountGameObject>();
    public List<HelicoptorGridSlotGameObject> gridSlots = new List<HelicoptorGridSlotGameObject>();
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "baggage") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "baggage", capacity = 200f });
        if (interiors.Find(s => s != null && s.sectionName == "galley") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "galley", capacity = 40f });
        if (galleyKitchen == null)
            galleyKitchen = GetComponentInChildren<RestaurantVenueRuntime>();
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>();
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (telecomBridge == null)
            telecomBridge = GetComponent("TelecomUnityBridge");
        if (configurationAsset != null)
            configurationAsset.ApplyTo(this);
        else
            EnsureSystems();
        LinkGalleyToAirportKitchen();
        RecomputeWingTipCaches();
    }

    public void EnsureSystems()
    {
        EnsureDefaultPowerSystems();
        if (airplaneBio == null)
            airplaneBio = GetComponent<AirplaneBioRhythm>() ?? gameObject.AddComponent<AirplaneBioRhythm>();
        airplaneBio.airplane = this;

        if (cabinMusicSystem == null)
            cabinMusicSystem = GetComponent<AirplaneCabinMusicSystem>() ?? gameObject.AddComponent<AirplaneCabinMusicSystem>();
        cabinMusicSystem.airplane = this;
        cabinMusicSystem.source = defaultMusicSource;
        cabinMusicSystem.paDucksMusic = paDucksMusic;

        if (weatherAeroBridge == null)
            weatherAeroBridge = GetComponent<AirplaneWeatherAeroBridge>() ?? gameObject.AddComponent<AirplaneWeatherAeroBridge>();
        weatherAeroBridge.airplane = this;

        if (webtopEnabled && webtopPlayer == null)
            webtopPlayer = GetComponent<WebtopUscVideoPlayer>() ?? gameObject.AddComponent<WebtopUscVideoPlayer>();
        if (webtopPlayer != null)
        {
            webtopPlayer.airplane = this;
            webtopPlayer.openCloseTopologyId = webtopOpenCloseTopologyId;
        }

        if (gpsHud == null)
            gpsHud = GetComponent<PilotGpsHudWebtop>();
        if (renderPortal == null)
            renderPortal = GetComponent<UnityRenderPortal>();
        if (dialogueCatalog == null)
            dialogueCatalog = new AtcDispatcherDialogueCatalog();
        dialogueCatalog.EnsureDefaults();
        if (checklistTemplate == null)
            checklistTemplate = TSAChecklistCard.Generate(null);
    }

    public void EnsureDefaultPowerSystems()
    {
        if (batteries == null) batteries = new List<AirplaneBatteryPack>();
        if (batteries.Count == 0)
            batteries.Add(new AirplaneBatteryPack());
        if (powerSystems == null) powerSystems = new List<AirplanePowerSystemDraw>();
        if (powerSystems.Count == 0)
            AirplanePowerBus.FillDefaultPowerSystems(powerSystems);
    }

    public void RecomputeWingTipCaches()
    {
        leftWing?.RecomputeTipEndCache(transform);
        rightWing?.RecomputeTipEndCache(transform);
        horizontalTail?.RecomputeTipEndCache(transform);
        verticalTail?.RecomputeTipEndCache(transform);
        if (magnetos != null)
            for (int i = 0; i < magnetos.Count; i++)
                magnetos[i]?.RecomputeTipEndCache(transform);
    }

    public void LinkGalleyToAirportKitchen(string airportKitchenCompanyId = null)
    {
        string parent = !string.IsNullOrEmpty(airportKitchenCompanyId)
            ? airportKitchenCompanyId
            : parentKitchenCompanyId;
        if (string.IsNullOrEmpty(parent)) return;
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>()
                            ?? galleyKitchen.gameObject.AddComponent<CompanyRegistration>();
        if (galleyCompany != null)
        {
            galleyCompany.parentCompanyId = parent;
            parentKitchenCompanyId = parent;
        }
    }

    public void SetCabinMusic(string trackId, bool play)
    {
        cabinMusicTrackId = trackId;
        cabinMusicPlaying = play;
        SendMessage(play ? "OnAirplaneCabinMusicPlay" : "OnAirplaneCabinMusicStop",
            trackId ?? "", SendMessageOptions.DontRequireReceiver);
    }

    public void SetCabinLocked(bool locked)
    {
        SendMessage(locked ? "OnAirplaneCabinLocked" : "OnAirplaneCabinUnlocked",
            this, SendMessageOptions.DontRequireReceiver);
    }

    public void SetLandingGearDown(bool down)
    {
        landingGearDown = down;
        NotifyNarrative(down ? AirplaneNarrativeActionIds.GearDown : AirplaneNarrativeActionIds.GearUp);
    }

    public void NotifyNarrative(string actionId)
    {
        if (string.IsNullOrEmpty(actionId)) return;
        SendMessage("OnNarrativeSchedulerAction", actionId, SendMessageOptions.DontRequireReceiver);
    }

    public void ApplyPowerSystemEnabled(string systemId, bool enabled)
    {
        if (string.IsNullOrEmpty(systemId)) return;
        if (systemId == "seatback_webtops" && !enabled)
            seatbackWebtopsEnabled = false;
        else if (systemId == "seatback_webtops" && enabled)
            seatbackWebtopsEnabled = true;
        if (systemId == "webtops" && !enabled && webtopPlayer != null && webtopPlayer.playing)
            webtopPlayer.Close();
        if ((systemId == "music_system" || systemId == "pa_speakers") && !enabled)
            SetCabinMusic(cabinMusicTrackId, false);
        if (systemId == "seat_aux" && !enabled && cabinMusicSystem != null
            && cabinMusicSystem.source == AirplaneCabinMusicSource.SeatAux)
            cabinMusicSystem.SetMusicSource(AirplaneCabinMusicSource.Silent);
    }
}

public static class HelicopterTravelRouteMerger
{
    public static void MergeIntoLeg(TravelLegSequenceNode legNode, HelicopterVehicleRagdoll heli, MultiModalSegment seg)
    {
        if (legNode == null || heli == null || seg == null) return;
        if (legNode.children == null) legNode.children = new List<BehaviorTreeNode>();
        if (seg.mode == TravelLegMode.Fly)
        {
            bool hasTakeoff = false;
            for (int i = 0; i < legNode.children.Count; i++)
                if (legNode.children[i] is HelicopterTakeoffPlanNode) { hasTakeoff = true; break; }
            if (!hasTakeoff)
            {
                var go = new GameObject("HelicopterTakeoff");
                go.transform.SetParent(legNode.transform, false);
                var node = go.AddComponent<HelicopterTakeoffPlanNode>();
                node.helicopter = heli;
                legNode.children.Insert(0, node);
            }
        }
        if (seg.mode == TravelLegMode.Land || seg.mode == TravelLegMode.Park || seg.mode == TravelLegMode.Fly)
        {
            bool hasLanding = false;
            for (int i = 0; i < legNode.children.Count; i++)
                if (legNode.children[i] is HelicopterLandingPlanNode) { hasLanding = true; break; }
            if (!hasLanding)
            {
                var go = new GameObject("HelicopterLanding");
                go.transform.SetParent(legNode.transform, false);
                var node = go.AddComponent<HelicopterLandingPlanNode>();
                node.helicopter = heli;
                if (!string.IsNullOrEmpty(seg.roadLotId))
                    node.targetRoadLot = RoadLot.FindById(seg.roadLotId);
                legNode.children.Add(node);
            }
        }
    }
}

public static class AircraftTravelRouteMerger
{
    public static void MergeIntoLeg(TravelLegSequenceNode legNode, AirplaneVehicleRagdoll plane, MultiModalSegment seg)
    {
        if (legNode == null || plane == null || seg == null) return;
        if (legNode.children == null) legNode.children = new List<BehaviorTreeNode>();
        bool landish = seg.mode == TravelLegMode.Land || seg.mode == TravelLegMode.Park || seg.mode == TravelLegMode.Fly;
        if (!landish) return;
        if (plane.insertLandingQueue)
        {
            bool has = false;
            for (int i = 0; i < legNode.children.Count; i++)
                if (legNode.children[i] is AircraftLandingQueueNode existing) { existing.airplane = plane; has = true; break; }
            if (!has)
            {
                var go = new GameObject("AircraftLandingQueue");
                go.transform.SetParent(legNode.transform, false);
                var node = go.AddComponent<AircraftLandingQueueNode>();
                node.airplane = plane;
                legNode.children.Insert(Mathf.Max(0, legNode.children.Count - 1), node);
            }
        }
        if (plane.insertRefuelBeforePark || plane.fuel01 <= plane.refuelFuelThreshold01)
        {
            bool has = false;
            for (int i = 0; i < legNode.children.Count; i++)
                if (legNode.children[i] is AircraftRefuelNode existing) { existing.airplane = plane; has = true; break; }
            if (!has)
            {
                var go = new GameObject("AircraftRefuel");
                go.transform.SetParent(legNode.transform, false);
                var node = go.AddComponent<AircraftRefuelNode>();
                node.airplane = plane;
                legNode.children.Add(node);
            }
        }
    }
}

public class RailTrackStructure : MonoBehaviour
{
    public string segmentId;
    public string railSegmentId { get => segmentId; set => segmentId = value; }
    public List<Vector3> controlPoints = new List<Vector3>();
    static readonly List<RailTrackStructure> Registry = new List<RailTrackStructure>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() { Registry.Remove(this); }
    public static RailTrackStructure FindBySegmentId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].segmentId == id) return Registry[i];
        return null;
    }

    public void EnsureSplinePoints()
    {
        if (controlPoints == null) controlPoints = new List<Vector3>();
        if (controlPoints.Count >= 2) return;
        controlPoints.Clear();
        controlPoints.Add(Vector3.zero);
        controlPoints.Add(new Vector3(0f, 0f, 40f));
    }

    public Vector3 SamplePosition(float t01)
    {
        EnsureSplinePoints();
        float t = Mathf.Clamp01(t01);
        if (controlPoints.Count == 1) return transform.TransformPoint(controlPoints[0]);
        float span = (controlPoints.Count - 1) * t;
        int i = Mathf.Min(controlPoints.Count - 2, Mathf.FloorToInt(span));
        float u = span - i;
        return transform.TransformPoint(Vector3.Lerp(controlPoints[i], controlPoints[i + 1], u));
    }
}

public sealed class RailTrackFollowPlanNode : BehaviorTreeNode
{
    public RailTrackStructure track;
    public string railSegmentId;
    public TrainVehicleRagdoll train;
    public List<Vector3> sampledPath = new List<Vector3>();
    public void Sample() { sampledPath = sampledPath ?? new List<Vector3>(); }
    public override BehaviorTreeStatus Execute(BehaviorTree tree) => BehaviorTreeStatus.Success;
}

public static class RagdollGetUpBootstrap
{
    public static bool TryMerge(RagdollActor actor)
    {
        if (actor == null || !actor.enableGetUp || actor.GetUpMerged) return false;
        Brain brain = actor.GetComponentInChildren<Brain>(true);
        if (brain == null) return false;
        if (brain.behaviorTree != null && brain.behaviorTree.rootNode is RagdollGetUpSelectorNode)
        {
            actor.MarkGetUpMerged();
            return false;
        }
        BehaviorTreeNode previousRoot = brain.behaviorTree != null ? brain.behaviorTree.rootNode : null;
        BehaviorTree newTree = RagdollGetUpTreeFactory.Build(brain.transform);
        if (newTree == null) return false;
        var selector = newTree.rootNode as RagdollGetUpSelectorNode;
        if (selector != null && previousRoot != null && previousRoot != selector)
            selector.SetPassthrough(previousRoot);
        brain.behaviorTree = newTree;
        actor.MarkGetUpMerged();
        return true;
    }
}

public sealed class PaintCanvasCurvedDecal : MonoBehaviour
{
    public float radiusM = 0.25f;
    public float arcDeg = 90f;
    public float heightM = 0.2f;

    public void RebuildMesh()
    {
        var canvas = GetComponent<PaintCanvas>();
        if (canvas != null) canvas.surfaceKind = PaintCanvas.SurfaceKind.CurvedDecal;
    }

    public bool WorldToUv(Vector3 world, out Vector2 uv)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        float r = new Vector2(local.x, local.z).magnitude;
        float shell = Mathf.Max(0.01f, radiusM);
        if (Mathf.Abs(r - shell) > shell * 0.35f)
        {
            uv = default;
            return false;
        }
        float ang = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float half = Mathf.Max(1f, arcDeg) * 0.5f;
        if (Mathf.Abs(ang) > half + 1f)
        {
            uv = default;
            return false;
        }
        float halfH = Mathf.Max(0.01f, heightM) * 0.5f;
        if (Mathf.Abs(local.y) > halfH + 0.02f)
        {
            uv = default;
            return false;
        }
        uv = new Vector2(Mathf.InverseLerp(-half, half, ang), Mathf.InverseLerp(-halfH, halfH, local.y));
        return true;
    }
}

public sealed class PlantCutTakeRuntime : MonoBehaviour
{
    public LotGrassGrowthController grass;
    public void ApplyCutTake(Vector3 worldHit, Vector3 planeNormal, float severity01, float timeT = -1f) { }
}

public enum PulleySurfaceKind
{
    Slats = 0,
    Cloth = 1,
    Reeds = 2
}

public sealed class PulleySurfaceRagdoll : MonoBehaviour
{
    public const string PullStringId = "shade.pull_string";

    public PulleySurfaceKind kind = PulleySurfaceKind.Slats;
    [Range(0f, 1f)] public float pull01;
    public AnimationCurve raiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public int slatCount = 8;
    public float dropMeters = 1.2f;
    public Transform headrail;
    public readonly List<Transform> slats = new List<Transform>();
    public LineRenderer cord;
    public ClothUvStretchDriver cloth;
    public MonoBehaviour ropeSystem;

    ShadePullStringSurface _surface;

    public IVehicleControlSurface PullSurface => _surface ??= new ShadePullStringSurface(this);

    public bool MatchesPullString(string localSurfaceId) =>
        string.Equals(localSurfaceId, PullStringId, System.StringComparison.OrdinalIgnoreCase);

    public void EnsureSlats()
    {
        if (headrail == null)
        {
            var go = new GameObject("headrail");
            go.transform.SetParent(transform, false);
            headrail = go.transform;
        }
        while (slats.Count < Mathf.Max(2, slatCount))
        {
            var go = new GameObject("slat_" + slats.Count);
            go.transform.SetParent(transform, false);
            slats.Add(go.transform);
        }
        ApplyPull();
    }

    public void SetPull01(float value)
    {
        pull01 = Mathf.Clamp01(value);
        ApplyPull();
    }

    public void ApplyPullImpulse(float normalized, float dt)
    {
        float step = Mathf.Clamp(normalized, -1f, 1f) * Mathf.Max(dt, 1f / 60f) * 1.5f;
        SetPull01(pull01 + step);
    }

    public float EvaluateRaise(float t) =>
        raiseCurve != null ? raiseCurve.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

    public float SampleSlatT(int index, float pull)
    {
        int n = Mathf.Max(1, slats.Count > 0 ? slats.Count : slatCount);
        float along = n <= 1 ? 0f : index / (float)(n - 1);
        float raised = EvaluateRaise(Mathf.Clamp01(pull));
        return Mathf.Lerp(along, 0f, raised);
    }

    public void ApplyPull()
    {
        if (slats.Count == 0 && slatCount > 0)
            EnsureSlats();
        Vector3 origin = headrail != null ? headrail.position : transform.position;
        Vector3 down = -transform.up * dropMeters;
        for (int i = 0; i < slats.Count; i++)
        {
            if (slats[i] == null) continue;
            float t = SampleSlatT(i, pull01);
            slats[i].position = origin + down * t;
        }
        if (cord != null)
        {
            cord.positionCount = 2;
            cord.SetPosition(0, origin);
            Vector3 end = slats.Count > 0 && slats[slats.Count - 1] != null
                ? slats[slats.Count - 1].position
                : origin + down * (1f - EvaluateRaise(pull01));
            cord.SetPosition(1, end);
        }
        if (cloth != null)
            cloth.NotifyContact(gameObject, EvaluateRaise(pull01));
    }

    public float LerpPull(float from, float to, float t) =>
        Mathf.Lerp(from, to, EvaluateRaise(Mathf.Clamp01(t)));
}

public sealed class ShadePullStringSurface : IVehicleControlSurface
{
    readonly PulleySurfaceRagdoll _pulley;

    public ShadePullStringSurface(PulleySurfaceRagdoll pulley)
    {
        _pulley = pulley;
        Id = PulleySurfaceRagdoll.PullStringId;
        ImpulseChannelKey = PulleySurfaceRagdoll.PullStringId;
    }

    public string Id { get; }
    public string ImpulseChannelKey { get; }
    public VehicleActor Owner => null;

    public void ApplyImpulse(float normalized, float dt)
    {
        _pulley?.ApplyPullImpulse(normalized, dt);
    }
}

public sealed class PulleyPullNode : BehaviorTreeNode
{
    public PulleySurfaceRagdoll pulley;
    public AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float duration = 1f;
    public bool raise = true;
    float _elapsed;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (pulley == null) return BehaviorTreeStatus.Failure;
        _elapsed += Time.deltaTime;
        float t = duration > 1e-4f ? Mathf.Clamp01(_elapsed / duration) : 1f;
        float u = pullCurve != null ? pullCurve.Evaluate(t) : t;
        pulley.SetPull01(raise ? u : 1f - u);
        return t >= 1f ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}

public sealed class ConsentWarden : MonoBehaviour
{
    public ThreatWarden threatWarden;
    public TheocraticWarden theocraticWarden;
    public JusticeWarden justiceWarden;
    public RightsWarden rightsWarden;
    public LoveWarden loveWarden;
    [Range(0f, 1f)] public float wThreat = 1f / 3f;
    [Range(0f, 1f)] public float wTheo = 1f / 3f;
    [Range(0f, 1f)] public float wJust = 1f / 3f;
    [Range(0f, 1f)] public float wRights;
    [Range(0f, 1f)] public float maxPhysicality01 = 0.95f;
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public float Allow01() => Evaluate();
    public float Evaluate()
    {
        lastScore01 = Blend01(
            wThreat, threatWarden != null ? threatWarden.MaxThreat01() : 0f, threatWarden != null,
            wTheo, theocraticWarden != null ? theocraticWarden.Allow01() : 0f, theocraticWarden != null,
            wJust, justiceWarden != null ? justiceWarden.Allow01() : 0f, justiceWarden != null,
            wRights, rightsWarden != null ? rightsWarden.Allow01() : 0f, rightsWarden != null);
        maxPhysicality01 = lastScore01;
        return lastScore01;
    }

    public static float Blend01(
        float wThreat, float threat01, bool hasThreat,
        float wTheo, float theoAllow, bool hasTheo,
        float wJust, float justAllow, bool hasJust,
        float wRights, float rightsAllow, bool hasRights)
    {
        float tw = hasThreat ? wThreat : 0f;
        float thw = hasTheo ? wTheo : 0f;
        float jw = hasJust ? wJust : 0f;
        float rw = hasRights ? wRights : 0f;
        if (hasRights && wRights <= 1e-6f)
        {
            int n = (hasThreat ? 1 : 0) + (hasTheo ? 1 : 0) + (hasJust ? 1 : 0) + 1;
            float eq = 1f / n;
            tw = hasThreat ? eq : 0f;
            thw = hasTheo ? eq : 0f;
            jw = hasJust ? eq : 0f;
            rw = eq;
        }
        float sum = tw + thw + jw + rw;
        if (sum < 1e-6f) return 1f;
        return Mathf.Clamp01(
            (tw / sum) * (1f - Mathf.Clamp01(threat01)) +
            (thw / sum) * Mathf.Clamp01(theoAllow) +
            (jw / sum) * Mathf.Clamp01(justAllow) +
            (rw / sum) * Mathf.Clamp01(rightsAllow));
    }
}


// --- AssetDB orphan host stubs (pending originals under _PendingAssetDbImport) ---


public enum CivicDutyKind
{
    Inspect = 0,
    Repair = 1,
    Replace = 2,
    Secure = 3,
    Clean = 4
}

public enum CivilianDutyKind
{
    WorkShift = 0,
    Commute = 1,
    Leisure = 2,
    SchoolAttend = 3,
    Shop = 4,
    Worship = 5,
    Exercise = 6,
    RestAtHome = 7,
    FleeThreat = 8,
    Socialize = 9,
    PrivateLeisure = 10,
    GatherHomeless = 11,
    GatherKids = 12,
    FakeLibraryCard = 13,
    PrisonCustody = 14,
    PrisonYard = 15,
    PrisonCafeteria = 16,
    PrisonClinic = 17,
    PrisonParole = 18,
    PrisonRehabOuting = 19,
    PrisonLibrary = 20,
    PrisonFarm = 21,
    PrisonWeights = 22,
    PrisonNursery = 23,
    JobSearch = 24,
    BenefitsClaim = 25,
    CareerInterview = 26,
    JobTraining = 27
}

public enum BuildingMaterialClass
{
    Generic = 0,
    Wood = 1,
    Metal = 2,
    Masonry = 3,
    Glass = 4
}

public class BuildingRagdoll : MonoBehaviour
{
    public string buildingStableId;
    public BuildingBioRhythmService bio;
    public BuildingHealthState Health = new BuildingHealthState();
    public DamagedObjectQueue damageQueue;
    [Range(0f, 1f)] public float enqueueDamageThreshold01 = 0.2f;

    protected virtual void Awake()
    {
        if (bio == null)
            bio = GetComponent<BuildingBioRhythmService>();
        if (damageQueue == null)
            damageQueue = GetComponent<DamagedObjectQueue>();
        if (Health == null)
            Health = new BuildingHealthState();
    }

    public virtual void Tick(float dt)
    {
        bio?.Tick(dt);
        Health?.TickDecay(dt);
    }

    public virtual void ReportPieceMemory(ImpulseMaterialMemory piece, float impulseNorm01)
    {
        if (Health == null)
            Health = new BuildingHealthState();
        Health.ApplyImpulseDamage(impulseNorm01);
        if (damageQueue == null || impulseNorm01 < enqueueDamageThreshold01)
            return;
        damageQueue.Enqueue(new DamagedObjectRecord
        {
            objectId = piece != null ? piece.gameObject.name : "piece",
            buildingId = buildingStableId,
            damage01 = impulseNorm01,
            materialClass = piece != null ? piece.materialClass : BuildingMaterialClass.Generic,
            worldPos = piece != null ? piece.transform.position : transform.position,
            source = piece != null ? piece.gameObject : gameObject
        });
    }

    public virtual void ReportAnonymousImpulse(float impulseN, Vector3 worldPoint, GameObject source) { }

    public void ApplyRepair(float amount01)
    {
        if (bio?.health == null) return;
        bio.health.integrity01 = Mathf.Clamp01(bio.health.integrity01 + amount01);
    }
}

[DisallowMultipleComponent]
public sealed class ImpulseMaterialMemory : MonoBehaviour
{
    public BuildingMaterialClass materialClass = BuildingMaterialClass.Generic;
    public BuildingRagdoll buildingRagdoll;
    public float memoryTau = 8f;
    public float bendGain = 0.08f;
    public float dentGain = 0.04f;
    [Range(0f, 1f)] public float memory01;
    [Range(0f, 1f)] public float bend01;
    [Range(0f, 1f)] public float dent01;

    public void ApplyImpulse(float impulseN, Vector3 worldPoint, bool likelyDent)
    {
        float norm = Mathf.Clamp01(impulseN / 2000f);
        float rate = 1f / Mathf.Max(0.1f, memoryTau);
        bend01 = Mathf.Clamp01(bend01 + norm * bendGain * rate * 10f);
        if (likelyDent || materialClass == BuildingMaterialClass.Metal)
            dent01 = Mathf.Clamp01(dent01 + norm * dentGain * rate * 10f);
        memory01 = Mathf.Clamp01(Mathf.Max(bend01, dent01 * 0.85f));
        buildingRagdoll?.ReportPieceMemory(this, norm);
    }

    public static void NotifyImpulse(GameObject source, float impulseN, Vector3 worldPoint)
    {
        if (source == null || impulseN <= 0f) return;
        var mem = source.GetComponent<ImpulseMaterialMemory>()
                  ?? source.GetComponentInParent<ImpulseMaterialMemory>();
        if (mem == null)
        {
            var ragdoll = source.GetComponentInParent<BuildingRagdoll>();
            ragdoll?.ReportAnonymousImpulse(impulseN, worldPoint, source);
            return;
        }
        mem.ApplyImpulse(impulseN, worldPoint, impulseN > 400f);
    }
}

public static class RigidbodyPhysicsWalk
{
    public const string DoorwayPortalTag = "doorway_portal";

    public sealed class BodyMeshGroup
    {
        public Rigidbody body;
        public readonly List<MeshFilter> meshFilters = new List<MeshFilter>();
        public readonly List<MeshCollider> meshColliders = new List<MeshCollider>();
        public readonly List<Renderer> renderers = new List<Renderer>();
    }

    public static List<BodyMeshGroup> Collect(Transform root, bool includeInactive = true)
    {
        var groups = new List<BodyMeshGroup>();
        if (root == null)
            return groups;

        var bodies = root.GetComponentsInChildren<Rigidbody>(includeInactive);
        if (bodies == null || bodies.Length == 0)
        {
            var group = new BodyMeshGroup();
            var filters = root.GetComponentsInChildren<MeshFilter>(includeInactive);
            for (int i = 0; i < filters.Length; i++)
            {
                if (SkipPortal(filters[i] != null ? filters[i].gameObject : null))
                    continue;
                if (filters[i] != null && filters[i].sharedMesh != null)
                    group.meshFilters.Add(filters[i]);
            }
            if (group.meshFilters.Count > 0)
                groups.Add(group);
            return groups;
        }

        var claimed = new HashSet<int>();
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (rb == null) continue;
            var group = new BodyMeshGroup { body = rb };
            CollectOwned(rb.transform, rb, includeInactive, group, claimed);
            if (group.meshFilters.Count > 0 || group.meshColliders.Count > 0)
                groups.Add(group);
        }
        return groups;
    }

    static void CollectOwned(Transform t, Rigidbody owner, bool includeInactive, BodyMeshGroup group, HashSet<int> claimed)
    {
        if (t == null || (!includeInactive && !t.gameObject.activeInHierarchy))
            return;
        if (SkipPortal(t.gameObject))
            return;

        var other = t.GetComponent<Rigidbody>();
        if (other != null && other != owner)
            return;

        int id = t.GetInstanceID();
        if (!claimed.Add(id))
            return;

        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            group.meshFilters.Add(mf);
        var mc = t.GetComponent<MeshCollider>();
        if (mc != null)
            group.meshColliders.Add(mc);
        var rend = t.GetComponent<Renderer>();
        if (rend != null)
            group.renderers.Add(rend);

        for (int i = 0; i < t.childCount; i++)
            CollectOwned(t.GetChild(i), owner, includeInactive, group, claimed);
    }

    public static bool SkipPortal(GameObject go)
    {
        if (go == null) return false;
        if (string.Equals(go.tag, DoorwayPortalTag, System.StringComparison.Ordinal)
            || go.name.IndexOf(DoorwayPortalTag, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }
}

public static class FloodDrainageAmounts
{
    public const float SpawnRatePerLiterPerSecond = 400f;

    public static float ApplyDrain(ref float standingLiters, float requestLiters)
    {
        float taken = Mathf.Min(Mathf.Max(0f, standingLiters), Mathf.Max(0f, requestLiters));
        standingLiters -= taken;
        return taken;
    }

    public static float SpawnRateFromLitersPerSecond(float litersPerSecond) =>
        Mathf.Max(0f, litersPerSecond) * SpawnRatePerLiterPerSecond;

    public static float HeightFromLiters(float standingLiters, float pitAreaM2)
    {
        float area = Mathf.Max(0.01f, pitAreaM2);
        return Mathf.Max(0f, standingLiters) * 0.001f / area;
    }

    public static float SumpFlowLitersPerSecond(
        float standingLiters,
        float minActivationLiters,
        float maxFlowLitersPerSecond,
        bool powerOn)
    {
        if (!powerOn || standingLiters < minActivationLiters)
            return 0f;
        return Mathf.Min(standingLiters, Mathf.Max(0f, maxFlowLitersPerSecond));
    }
}

public enum CareerWardenAction
{
    Retain = 0,
    Fire = 1
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Career Warden")]
public sealed class CareerWarden : MonoBehaviour
{
    public CivilianDemographics demographics = new CivilianDemographics();
    public List<CivilianPaperDoll> unemployedPool = new List<CivilianPaperDoll>();
    public CareerAdvancementTree tree;
    public CompanyRegistration company;
    public ThreatWarden threatWarden;
    public PrisonWarden prisonWarden;
    public TrafficWarden trafficWarden;
    public SafetyWardenPlannerService safetyWarden;
    public AuthWarden authWarden;
    public string threatAgencyId = "career";
    public CareerWardenAction lastRecommendation = CareerWardenAction.Retain;
    [Range(0f, 1f)] public float lastGrade01;

    void Awake()
    {
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>();
        if (prisonWarden == null)
            prisonWarden = GetComponent<PrisonWarden>();
        if (authWarden == null)
            authWarden = GetComponent<AuthWarden>();
    }

    public void ApplySocietyFeatures(IReadOnlyDictionary<string, float> societyFeatures, int population = 100)
    {
        var demo = CivilianDemographics.FromSocietyFeatures(societyFeatures, population);
        var dao = StatisticalRetinueDao.Resolve(this);
        if (dao != null)
        {
            var seed = StatSeed.ForCity(dao.cityId, societyFeatures, population);
            if (dao.EnsureModel("civ.demographics", seed) is CivDemographicsModel model)
                model.Replace(demo);
        }
        demographics = demo;
    }

    public CivilianPaperDoll RequestCivilianPaperDoll(string personaKey = "civilian", int seed = 0)
        => RequestCivilianPaperDoll(personaKey, seed, null);

    public CivilianPaperDoll RequestCivilianPaperDoll(string personaKey, int seed, Vector3? spawnPos)
    {
        var doll = demographics.SampleUnemployed(personaKey, unemployedPool, seed, spawnPos);
        if (doll == null)
            return null;
        unemployedPool.Add(doll);
        return doll;
    }

    public bool AssignJob(CivilianPaperDoll doll, CareerRoleSpec role, CompanyRegistration employer, EducationalTravelAgent agent = null)
    {
        if (doll == null || role == null) return false;
        if (role.requireNoPretraining)
            return Hire(doll, role, employer);
        if (agent == null)
            agent = GetComponent<EducationalTravelAgent>() ?? gameObject.AddComponent<EducationalTravelAgent>();
        agent.warden = this;
        agent.ResolvePath(doll, role);
        doll.educationalPlan = agent;
        doll.employment = CivilianEmploymentStatus.Training;
        doll.CopyLimitsFrom(role);
        if (employer != null)
            doll.employerCompanyId = employer.companyId;
        return true;
    }

    public bool Hire(CivilianPaperDoll doll, CareerRoleSpec role, CompanyRegistration employer)
    {
        if (doll == null || role == null) return false;
        employer = employer != null ? employer : company;
        if (employer == null) return false;
        unemployedPool.Remove(doll);
        doll.CopyLimitsFrom(role);
        doll.employment = CivilianEmploymentStatus.Employed;
        doll.employerCompanyId = employer.companyId;
        doll.isGovernmentJob = role.isGovernment;
        if (role.isGovernment && string.IsNullOrEmpty(employer.parentCompanyId))
            employer.parentCompanyId = "government";
        employer.TryHire(doll.personaKey, role.roleId, role.peckingOrder);
        BindWorkBio(doll, employer);
        lastRecommendation = CareerWardenAction.Retain;
        return true;
    }

    public bool Fire(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null) return false;
        employer = employer != null ? employer : company;
        employer?.TryFire(doll.personaKey);
        doll.employment = CivilianEmploymentStatus.Unemployed;
        doll.employerCompanyId = "";
        doll.currentRoleId = "";
        if (!unemployedPool.Contains(doll) && demographics.TryAcceptUnemployed(doll, unemployedPool))
            unemployedPool.Add(doll);
        lastRecommendation = CareerWardenAction.Fire;
        return true;
    }

    public bool Promote(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null || tree == null) return false;
        var next = !string.IsNullOrEmpty(doll.currentRoleId)
            ? tree.NextPromotion(doll.currentRoleId)
            : null;
        if (next == null) return false;
        return Hire(doll, next, employer);
    }

    public bool Demote(CivilianPaperDoll doll, CompanyRegistration employer = null)
    {
        if (doll == null || tree == null) return false;
        var prev = tree.PreviousDemotion(doll.currentRoleId);
        if (prev == null)
            return Fire(doll, employer);
        return Hire(doll, prev, employer);
    }

    public void ApplyPlanEffect(CivilianPaperDoll doll, CareerPlanEffect effect, string targetRoleId)
    {
        if (doll == null || effect == CareerPlanEffect.None) return;
        var role = tree != null ? tree.FindRole(targetRoleId) : null;
        switch (effect)
        {
            case CareerPlanEffect.Hire:
                if (role != null) Hire(doll, role, company);
                break;
            case CareerPlanEffect.Promote:
                Promote(doll, company);
                break;
            case CareerPlanEffect.Demote:
                Demote(doll, company);
                break;
            case CareerPlanEffect.Fire:
                Fire(doll, company);
                break;
        }
    }

    public float[] GradeEmployee(CivilianPaperDoll doll)
    {
        float skill = Skill01(doll);
        float conduct = Conduct01();
        float reliability = Reliability01(doll);
        float authority = Authority01(doll);
        lastGrade01 = (skill + conduct + reliability + authority) * 0.25f;
        var grade = new[] { skill, conduct, reliability, authority };
        lastRecommendation = OverFireLimit(doll, grade) ? CareerWardenAction.Fire : CareerWardenAction.Retain;
        return grade;
    }

    public bool OverFireLimit(CivilianPaperDoll doll, float[] grade = null)
    {
        if (doll == null) return false;
        grade = grade ?? GradeEmployee(doll);
        var red = doll.FireLimit01();
        for (int i = 0; i < 4; i++)
            if (grade[i] > red[i] + 1e-4f)
                return true;
        return false;
    }

    float Skill01(CivilianPaperDoll doll)
    {
        if (doll == null) return 0.4f;
        float s = 0.35f;
        if (doll.education == CivilianEducationAttainment.Certification) s += 0.2f;
        if (doll.education == CivilianEducationAttainment.Degree) s += 0.35f;
        if (doll.certificationIds != null) s += 0.05f * doll.certificationIds.Length;
        if (doll.degreeIds != null) s += 0.08f * doll.degreeIds.Length;
        if (doll.employment == CivilianEmploymentStatus.Training) s += 0.1f;
        if (doll.educationalPlan != null && doll.educationalPlan.steps != null && doll.educationalPlan.steps.Count > 0)
            s += 0.15f * ((doll.selectedStepIndex + 1f) / doll.educationalPlan.steps.Count);
        return Mathf.Clamp01(s);
    }

    float Conduct01()
    {
        float threat = 0f;
        if (threatWarden != null && !string.IsNullOrEmpty(threatAgencyId))
        {
            var agency = threatWarden.GetAgency(threatAgencyId);
            threat = agency.threatScore01;
        }
        float prison = prisonWarden != null ? prisonWarden.lastScore01 : 0f;
        float safety = 0f;
        if (safetyWarden != null)
            safety = 0.2f;
        return Mathf.Clamp01(1f - (threat * 0.5f + prison * 0.3f + safety * 0.2f));
    }

    float Reliability01(CivilianPaperDoll doll)
    {
        float attend = 0.7f;
        if (doll != null && doll.employment == CivilianEmploymentStatus.Employed)
            attend = 0.8f;
        if (doll != null && doll.employment == CivilianEmploymentStatus.Unemployed)
            attend = 0.4f;
        float traffic = 0f;
        if (trafficWarden != null)
            traffic = Mathf.Clamp01(trafficWarden.MaxEdgeDemand / 16f);
        return Mathf.Clamp01(attend * (1f - traffic * 0.25f));
    }

    float Authority01(CivilianPaperDoll doll)
    {
        float peck = 0.4f;
        if (company != null && doll != null)
        {
            var entry = company.FindStaff(doll.personaKey);
            if (entry != null)
                peck = Mathf.Clamp01(1f - entry.peckingOrder / 40f);
        }
        float auth = authWarden != null && doll != null && authWarden.HasGrant(gameObject.name, doll.personaKey)
            ? 0.2f
            : 0f;
        var role = tree != null && doll != null ? tree.FindRole(doll.currentRoleId) : null;
        float mgmt = 0f;
        if (role != null)
        {
            if (role.requiresManagement) mgmt += 0.15f;
            if (role.requiresHiringManager) mgmt += 0.15f;
        }
        return Mathf.Clamp01(peck + auth + mgmt);
    }

    static void BindWorkBio(CivilianPaperDoll doll, CompanyRegistration employer)
    {
        if (doll == null || employer == null) return;
        var runtime = UnityEngine.Object.FindObjectsByType<CivilianPaperDollRuntime>(FindObjectsSortMode.None);
        for (int i = 0; i < runtime.Length; i++)
        {
            if (runtime[i] == null || runtime[i].doll != doll || runtime[i].schedule == null)
                continue;
            runtime[i].schedule.workBio = employer.GetComponent<BuildingBioRhythmService>();
        }
    }
}

public sealed class InteractedObjectCheckpoint
{
    public struct Entry
    {
        public GameObject go;
        public bool activeSelf;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool hasRigidbody;
    }

    readonly Dictionary<int, Entry> _firstSeen = new Dictionary<int, Entry>();
    readonly List<GoodSection> _enabledSections = new List<GoodSection>();
    PhysicsCardSolver _solver;
    bool _dirty;

    public bool CanReset => _dirty && _firstSeen.Count > 0;
    public int SnapshotCount => _firstSeen.Count;

    public void RememberFirstSeen(GameObject go)
    {
        if (go == null) return;
        int id = go.GetInstanceID();
        if (_firstSeen.ContainsKey(id)) return;
        _firstSeen[id] = Capture(go);
    }

    public void MarkDirtyFromGoodSection(GoodSection section, PhysicsCardSolver solver)
    {
        if (section == null) return;
        _solver = solver;
        if (!_enabledSections.Contains(section))
            _enabledSections.Add(section);
        _dirty = true;
    }

    public void MarkDirtyFromPhysicsTranslation(GameObject go)
    {
        if (go == null) return;
        RememberFirstSeen(go);
        int id = go.GetInstanceID();
        if (!_firstSeen.TryGetValue(id, out var e)) return;
        var t = go.transform;
        if ((t.localPosition - e.localPosition).sqrMagnitude > 1e-8f ||
            Quaternion.Angle(t.localRotation, e.localRotation) > 0.01f)
            _dirty = true;
    }

    public void Reset()
    {
        foreach (var kv in _firstSeen)
        {
            var e = kv.Value;
            if (e.go == null) continue;
            e.go.SetActive(e.activeSelf);
            e.go.transform.localPosition = e.localPosition;
            e.go.transform.localRotation = e.localRotation;
            e.go.transform.localScale = e.localScale;
            var rb = e.go.GetComponent<Rigidbody>();
            if (rb != null && e.hasRigidbody)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        if (_solver != null && _enabledSections.Count > 0)
            _solver.RemoveCards(_enabledSections);
        _enabledSections.Clear();
        _firstSeen.Clear();
        _dirty = false;
        _solver = null;
    }

    public static Entry Capture(GameObject go)
    {
        var e = new Entry
        {
            go = go,
            activeSelf = go.activeSelf,
            localPosition = go.transform.localPosition,
            localRotation = go.transform.localRotation,
            localScale = go.transform.localScale
        };
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            e.hasRigidbody = true;
            e.linearVelocity = rb.linearVelocity;
            e.angularVelocity = rb.angularVelocity;
        }
        return e;
    }
}

public static class GoodSectionContactActivation
{
    public struct TickResult
    {
        public int contactCount;
        public int sectionsEnabled;
        public readonly List<GameObject> contacts;
        public readonly List<GoodSection> sections;

        public TickResult(int dummy)
        {
            contactCount = 0;
            sectionsEnabled = 0;
            contacts = new List<GameObject>();
            sections = new List<GoodSection>();
        }
    }

    public static TickResult Tick(
        RagdollSystem ragdoll,
        IList<GameObject> objects,
        InteractedObjectCheckpoint checkpoint = null)
    {
        var result = new TickResult(0);
        if (ragdoll == null || objects == null || objects.Count == 0)
            return result;

        Physics.SyncTransforms();
        var ragdollColliders = ragdoll.GetComponentsInChildren<Collider>(true);
        if (ragdollColliders == null || ragdollColliders.Length == 0)
            return result;

        var nervous = ragdoll.GetComponent<NervousSystem>() ?? ragdoll.GetComponentInParent<NervousSystem>()
                      ?? ragdoll.GetComponentInChildren<NervousSystem>();
        var solver = ragdoll.GetComponent<PhysicsCardSolver>() ?? ragdoll.GetComponentInParent<PhysicsCardSolver>()
                     ?? ragdoll.GetComponentInChildren<PhysicsCardSolver>();
        var seen = new HashSet<int>();

        for (int oi = 0; oi < objects.Count; oi++)
        {
            var go = objects[oi];
            if (go == null || !go.activeInHierarchy)
                continue;
            var cols = go.GetComponentsInChildren<Collider>(true);
            bool hit = false;
            if (cols == null || cols.Length == 0)
            {
                hit = OverlapsBounds(ragdollColliders, go);
            }
            else
            {
                for (int ri = 0; ri < ragdollColliders.Length && !hit; ri++)
                {
                    var rc = ragdollColliders[ri];
                    if (rc == null || !rc.enabled) continue;
                    for (int ci = 0; ci < cols.Length; ci++)
                    {
                        var oc = cols[ci];
                        if (oc == null || !oc.enabled) continue;
                        if (rc.bounds.Intersects(oc.bounds))
                        {
                            hit = true;
                            break;
                        }
                    }
                }
            }
            if (hit)
                ActivateContact(go, ragdoll, nervous, solver, checkpoint, ref result, seen);
        }

        if (nervous != null)
            nervous.PumpImpulsesForEditor();
        return result;
    }

    static bool OverlapsBounds(Collider[] ragdollColliders, GameObject go)
    {
        var b = new Bounds(go.transform.position, Vector3.one * 0.25f);
        var r = go.GetComponentInChildren<Renderer>();
        if (r != null) b = r.bounds;
        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            if (ragdollColliders[i] != null && ragdollColliders[i].bounds.Intersects(b))
                return true;
        }
        return false;
    }

    static void ActivateContact(
        GameObject go,
        RagdollSystem ragdoll,
        NervousSystem nervous,
        PhysicsCardSolver solver,
        InteractedObjectCheckpoint checkpoint,
        ref TickResult result,
        HashSet<int> seen)
    {
        int id = go.GetInstanceID();
        if (!seen.Add(id)) return;
        checkpoint?.RememberFirstSeen(go);
        result.contacts.Add(go);
        result.contactCount++;

        var sensory = new SensoryData(go.transform.position, Vector3.up, 1f, go, "Contact");
        if (nervous != null)
        {
            var impulse = new ImpulseData(ImpulseType.Sensory, "EditModeContact", "NervousSystem", sensory);
            nervous.SendImpulseUp("Spinal", impulse);
            var sections = nervous.GetAvailableGoodSections(go);
            if (sections != null)
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    if (sections[i] == null) continue;
                    result.sections.Add(sections[i]);
                    checkpoint?.MarkDirtyFromGoodSection(sections[i], solver);
                }
                if (solver != null && sections.Count > 0)
                    solver.AddCards(sections);
                result.sectionsEnabled += sections.Count;
            }
        }
        else
        {
            var consider = ragdoll.GetComponentInChildren<Consider>();
            if (consider != null)
            {
                var cards = consider.GenerateCardsForTarget(go);
                if (cards != null)
                {
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (cards[i] == null) continue;
                        result.sections.Add(cards[i]);
                        checkpoint?.MarkDirtyFromGoodSection(cards[i], solver);
                    }
                    if (solver != null && cards.Count > 0)
                        solver.AddCards(cards);
                    result.sectionsEnabled += cards.Count;
                }
            }
        }

        checkpoint?.MarkDirtyFromPhysicsTranslation(go);
        _ = ragdoll;
    }

    public static void CollectCascadeFromMoved(
        GameObject moved,
        IList<GameObject> candidates,
        InteractedObjectCheckpoint checkpoint)
    {
        if (moved == null || candidates == null || checkpoint == null)
            return;
        var mc = moved.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < candidates.Count; i++)
        {
            var other = candidates[i];
            if (other == null || other == moved) continue;
            var oc = other.GetComponentsInChildren<Collider>(true);
            bool hit = false;
            if (mc != null && oc != null)
            {
                for (int a = 0; a < mc.Length && !hit; a++)
                {
                    if (mc[a] == null) continue;
                    for (int b = 0; b < oc.Length; b++)
                    {
                        if (oc[b] != null && mc[a].bounds.Intersects(oc[b].bounds))
                        {
                            hit = true;
                            break;
                        }
                    }
                }
            }
            if (hit)
                checkpoint.RememberFirstSeen(other);
        }
    }
}

public enum StationKind
{
    Generic = 0,
    Cooking = 1,
    Train = 2,
    Bus = 3,
    Computer = 4,
    Silo = 5,
    RailMaintenance = 6,
    Desk = 7,
    Class = 8,
    Library = 9,
    Phone = 10,
    Conversation = 11,
    VotingBooth = 12
}

[System.Serializable]
public sealed class StationCommodityEntry
{
    public string commodityKey;
    public float quantity = 1f;
}

[System.Serializable]
public sealed class StationAssignmentEntry
{
    public string assignType;
    public string refId;
    public string role;
    public int peckingOrder;
}

[System.Serializable]
public sealed class StationConfig
{
    [TextArea(2, 6)] public string notes;
    public string vehicleId;
    public string vehicleRouteId;
    public string buildingStableId;
    public float staffingWeight = 1f;
    public VotingBoothStation votingBooth;
    public List<StationCommodityEntry> commodities = new List<StationCommodityEntry>();
    public List<StationAssignmentEntry> assignments = new List<StationAssignmentEntry>();
}

[AddComponentMenu("Locomotion/Stations/Station Hierarchy Node")]
public sealed class StationHierarchyNode : MonoBehaviour
{
    public string stableId;
    public string displayName;
    public StationKind kind = StationKind.Generic;
    public string parentStableId;
    public string causalityLeafId;
    public string levelId = "default";
    public string cityId = "demo-city";
    public StationConfig config = new StationConfig();

    void Awake()
    {
        if (string.IsNullOrEmpty(stableId)) stableId = gameObject.name;
        if (string.IsNullOrEmpty(displayName)) displayName = gameObject.name;
        if (config == null) config = new StationConfig();
        StationRegistry.Instance?.Register(this);
    }

    void OnDestroy() => StationRegistry.Instance?.Unregister(this);

    public static string KindToApi(StationKind kind) => kind.ToString().ToLowerInvariant();

    public void TryBridge()
    {
        var pdm = FindFirstObjectByType<PersonaDayManager>();
        if (pdm == null || pdm.lattice == null || string.IsNullOrEmpty(stableId)) return;
        if (pdm.lattice.Get(stableId) != null) return;
        var civil = kind == StationKind.Cooking ? CivilSystemKind.Kitchen : CivilSystemKind.Generic;
        pdm.RegisterVenue(new CivilVenueNode { stableId = stableId, kind = civil, contextOwner = gameObject });
    }

    public Dictionary<string, object> ToPlacardDto()
    {
        var assignments = new List<object>();
        if (config?.assignments != null)
            for (int i = 0; i < config.assignments.Count; i++)
            {
                var a = config.assignments[i];
                if (a == null) continue;
                assignments.Add(new Dictionary<string, object>
                {
                    ["assignType"] = a.assignType ?? "",
                    ["refId"] = a.refId ?? "",
                    ["role"] = a.role ?? ""
                });
            }
        return new Dictionary<string, object>
        {
            ["kind"] = KindToApi(kind),
            ["vehicleId"] = config != null ? config.vehicleId ?? "" : "",
            ["assignments"] = assignments
        };
    }
}

[AddComponentMenu("Locomotion/Stations/Station Registry")]
public sealed class StationRegistry : MonoBehaviour
{
    static StationRegistry _instance;
    public static StationRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<StationRegistry>();
            return _instance;
        }
    }

    readonly List<StationHierarchyNode> _nodes = new List<StationHierarchyNode>();
    public string defaultCityId = "demo-city";
    public string defaultLevelId = "default";

    void Awake()
    {
        _instance = this;
        RefreshFromScene();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void Register(StationHierarchyNode node)
    {
        if (node == null || _nodes.Contains(node)) return;
        _nodes.Add(node);
    }

    public void Unregister(StationHierarchyNode node) => _nodes.Remove(node);

    public void RefreshFromScene()
    {
        _nodes.Clear();
        var found = FindObjectsByType<StationHierarchyNode>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            Register(found[i]);
    }

    public List<StationHierarchyNode> OrderedHierarchy()
    {
        RefreshFromScene();
        var byId = new Dictionary<string, StationHierarchyNode>();
        for (int i = 0; i < _nodes.Count; i++)
        {
            var n = _nodes[i];
            if (n != null && !string.IsNullOrEmpty(n.stableId))
                byId[n.stableId] = n;
        }
        var ordered = new List<StationHierarchyNode>();
        var visiting = new HashSet<string>();
        void Visit(StationHierarchyNode n)
        {
            if (n == null || string.IsNullOrEmpty(n.stableId) || !visiting.Add(n.stableId))
                return;
            if (!string.IsNullOrEmpty(n.parentStableId) && byId.TryGetValue(n.parentStableId, out var parent))
                Visit(parent);
            if (!ordered.Contains(n))
                ordered.Add(n);
        }
        foreach (var kv in byId)
            Visit(kv.Value);
        return ordered;
    }

    public Dictionary<string, object> BuildLevelStatsPayload()
    {
        RefreshFromScene();
        var nodes = OrderedHierarchy();
        var counts = new Dictionary<string, int>();
        float commodityTotal = 0f;
        int assignmentCount = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            string key = StationHierarchyNode.KindToApi(n.kind);
            counts.TryGetValue(key, out int c);
            counts[key] = c + 1;
            if (n.config?.commodities != null)
                for (int k = 0; k < n.config.commodities.Count; k++)
                    if (n.config.commodities[k] != null) commodityTotal += n.config.commodities[k].quantity;
            if (n.config?.assignments != null)
                assignmentCount += n.config.assignments.Count;
        }
        return new Dictionary<string, object>
        {
            ["stationCount"] = nodes.Count,
            ["countsByKind"] = counts,
            ["commodityQuantityTotal"] = commodityTotal,
            ["assignmentCount"] = assignmentCount
        };
    }

    public Dictionary<string, object> BuildUploadBody()
    {
        var stats = BuildLevelStatsPayload();
        stats["cityId"] = defaultCityId ?? "";
        stats["levelId"] = defaultLevelId ?? "";
        return stats;
    }
}

[AddComponentMenu("Locomotion/Build/Radial Build Host")]
[ExecuteAlways]
public sealed class RadialBuildHost : MonoBehaviour
{
    public const string StartPostAnchorName = "startPostAnchor";
    public const string StartPostBoundsName = "startPostBounds";

    public GameObject centerPost;
    public CustomRadialSideAsset customSide;
    public float customAngle;
    public GameObject customAngleObject;
    public int previewConfigIndex = -1;
    public RadialBuildSpec spec = new RadialBuildSpec();
    public Vector3 pieceSize = Vector3.one;
    public List<RadialSolvedConfig> solvedConfigs = new List<RadialSolvedConfig>();

    public Transform StartPostAnchor =>
        FindChild(centerPost != null ? centerPost.transform : transform, StartPostAnchorName);

    public RadialStartPostBounds StartPostBounds =>
        FindChild(centerPost != null ? centerPost.transform : transform, StartPostBoundsName)
            ?.GetComponent<RadialStartPostBounds>();

    public GameObject EnsureCenterPost()
    {
        if (centerPost != null)
            return centerPost;
        var go = new GameObject("CenterPost");
        go.transform.SetParent(transform, false);
        centerPost = go;
        return go;
    }

    public void CreateAnchorObjects()
    {
        var post = EnsureCenterPost();
        var t = post.transform;
        if (FindChild(t, StartPostAnchorName) == null)
        {
            var a = new GameObject(StartPostAnchorName);
            a.transform.SetParent(t, false);
            a.transform.localPosition = Vector3.forward;
        }
        if (FindChild(t, StartPostBoundsName) == null)
        {
            var b = new GameObject(StartPostBoundsName);
            b.transform.SetParent(t, false);
            b.transform.localPosition = Vector3.forward;
            b.transform.localScale = Vector3.one * 0.25f;
            var col = b.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = Vector3.zero;
            col.size = Vector3.one;
            b.AddComponent<RadialStartPostBounds>();
        }
        else if (FindChild(t, StartPostBoundsName).GetComponent<RadialStartPostBounds>() == null)
            FindChild(t, StartPostBoundsName).gameObject.AddComponent<RadialStartPostBounds>();
    }

    public void SnapStartPostFromBounds()
    {
        var bounds = StartPostBounds;
        var anchor = StartPostAnchor;
        if (bounds == null || anchor == null) return;
        Vector3 facing = bounds.FacingVector();
        anchor.position = bounds.SelectedCentroid();
        if (facing.sqrMagnitude > 1e-8f)
            anchor.rotation = Quaternion.LookRotation(facing, spec != null && spec.axis.sqrMagnitude > 1e-8f ? spec.axis : Vector3.up);
    }

    public void RefreshSolved()
    {
        if (solvedConfigs == null) solvedConfigs = new List<RadialSolvedConfig>();
        solvedConfigs.Clear();
        var pose = customSide != null ? customSide.ToPose() : (spec != null ? spec.customSide : default);
        if (customAngle > 0f) pose.customAngle = customAngle;
        if (customAngleObject != null)
        {
            pose.hasCustomAngleObject = true;
            pose.customAngleObjectWorld = customAngleObject.transform.position;
        }
        Vector3 center = centerPost != null ? centerPost.transform.position : transform.position;
        Vector3 axis = spec != null && spec.axis.sqrMagnitude > 1e-8f ? spec.axis : Vector3.up;
        var anchor = StartPostAnchor;
        bool hasStart = anchor != null;
        Vector3 startPos = hasStart ? anchor.position : Vector3.zero;
        Vector3 startFace = hasStart ? anchor.forward : Vector3.zero;
        var bounds = StartPostBounds;
        if (bounds != null) startFace = bounds.FacingVector();
        var join = spec != null ? spec.joinKind : RadialJoinKind.Natural;
        float off = spec != null ? spec.joinOffset : 0f;
        solvedConfigs.AddRange(RadialSlotMath.SolveWorkingJoints(
            pose, pieceSize, join, off, center, axis, hasStart, startPos, startFace));
        if (previewConfigIndex >= solvedConfigs.Count)
            previewConfigIndex = solvedConfigs.Count > 0 ? 0 : -1;
        if (previewConfigIndex >= 0 && previewConfigIndex < solvedConfigs.Count && spec != null)
            spec.ApplySolved(solvedConfigs[previewConfigIndex]);
    }

    public string[] PreviewLabels()
    {
        if (solvedConfigs == null || solvedConfigs.Count == 0)
            return System.Array.Empty<string>();
        var labels = new string[solvedConfigs.Count];
        for (int i = 0; i < solvedConfigs.Count; i++)
            labels[i] = solvedConfigs[i] != null ? solvedConfigs[i].DisplayLabel() : "(empty)";
        return labels;
    }

    static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c != null && c.name == name)
                return c;
        }
        return null;
    }
}


// --- Webcam / travel orphan hosts ---

[System.Serializable]
public sealed class PoseBoneSample
{
    public string traitId;
    public float timeMs;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
}

[System.Serializable]
public sealed class PoseTrack
{
    public string modelSpec;
    public List<PoseBoneSample> samples = new List<PoseBoneSample>();
    public int Count => samples != null ? samples.Count : 0;

    public float LatestTimeMs()
    {
        if (samples == null || samples.Count == 0) return 0f;
        float latest = samples[0] != null ? samples[0].timeMs : 0f;
        for (int i = 1; i < samples.Count; i++)
            if (samples[i] != null && samples[i].timeMs > latest)
                latest = samples[i].timeMs;
        return latest;
    }

    public void CollectTraitIds(ICollection<string> ids)
    {
        if (ids == null || samples == null) return;
        for (int i = 0; i < samples.Count; i++)
            if (samples[i] != null && !string.IsNullOrEmpty(samples[i].traitId))
                ids.Add(samples[i].traitId);
    }

    public PoseTrack RemapTraitIds(IDictionary<string, string> sourceToTarget)
    {
        var next = new PoseTrack { modelSpec = modelSpec };
        if (samples == null) return next;
        next.samples = new List<PoseBoneSample>(samples.Count);
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            if (s == null) continue;
            string id = s.traitId;
            if (sourceToTarget != null && !string.IsNullOrEmpty(id) && sourceToTarget.TryGetValue(id, out var mapped))
                id = mapped;
            next.samples.Add(new PoseBoneSample
            {
                traitId = id,
                timeMs = s.timeMs,
                localPosition = s.localPosition,
                localRotation = s.localRotation
            });
        }
        return next;
    }

    public void AppendSamples(IList<PoseBoneSample> extra, float windowMs)
    {
        if (extra == null) return;
        if (samples == null) samples = new List<PoseBoneSample>();
        float shift = Count > 0 ? LatestTimeMs() : 0f;
        for (int i = 0; i < extra.Count; i++)
        {
            var s = extra[i];
            if (s == null) continue;
            samples.Add(new PoseBoneSample
            {
                traitId = s.traitId,
                timeMs = s.timeMs + shift,
                localPosition = s.localPosition,
                localRotation = s.localRotation
            });
        }
        if (windowMs > 0f)
        {
            float keepAfter = LatestTimeMs() - windowMs;
            samples.RemoveAll(s => s == null || s.timeMs < keepAfter);
        }
    }

    public bool TrySample(string traitId, float timeMs, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (samples == null || string.IsNullOrEmpty(traitId))
            return false;
        int lo = -1, hi = -1;
        float loT = float.NegativeInfinity, hiT = float.PositiveInfinity;
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            if (s == null || s.traitId != traitId) continue;
            if (s.timeMs <= timeMs && s.timeMs >= loT) { lo = i; loT = s.timeMs; }
            if (s.timeMs >= timeMs && s.timeMs <= hiT) { hi = i; hiT = s.timeMs; }
        }
        if (lo < 0 && hi < 0) return false;
        if (lo < 0) { position = samples[hi].localPosition; rotation = samples[hi].localRotation; return true; }
        if (hi < 0 || lo == hi) { position = samples[lo].localPosition; rotation = samples[lo].localRotation; return true; }
        float span = hiT - loT;
        float u = span > 1e-6f ? Mathf.Clamp01((timeMs - loT) / span) : 0f;
        position = Vector3.Lerp(samples[lo].localPosition, samples[hi].localPosition, u);
        rotation = Quaternion.Slerp(samples[lo].localRotation, samples[hi].localRotation, u);
        return true;
    }

    public static PoseTrack FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new PoseTrack();
        try
        {
            var track = JsonUtility.FromJson<PoseTrack>(json);
            if (track == null) return new PoseTrack();
            if (track.samples == null) track.samples = new List<PoseBoneSample>();
            return track;
        }
        catch { return new PoseTrack(); }
    }

    public string ToJson() => JsonUtility.ToJson(this);
}

[CreateAssetMenu(fileName = "WebcamAnimRecording", menuName = "Locomotion/Animation/Webcam Anim Recording")]
public sealed class WebcamAnimRecordingAsset : ScriptableObject
{
    public string recordingId;
    public string displayName = "New Recording";
    public WebcamAnimKind kind = WebcamAnimKind.Ambulatory;
    public GameObject actorPrefab;
    public int animationListIndex;
    public string modelSpec = "";
    public string subsectionId = "";
    public string localClipPath = "";
    public string libraryDocId = "";
    public string mediaJobId = "";
    public double startMs;
    public double endMs = 1000;
    [Tooltip("When > 0, animation length is this many milliseconds. 0 = video length, then loaded animation/pose duration.")]
    public double userDurationLimitMs;
    [Tooltip("True after In/Out/Duration are typed; auto-fill from video or clip will not overwrite.")]
    public bool userSetTimeline;
    [Tooltip("Last prepared video length in milliseconds (0 if unknown).")]
    public double cachedVideoDurationMs;
    public WebcamAnimTimelineGranularity granularity = WebcamAnimTimelineGranularity.Millisecond;
    public string targetHint = "ragdoll";
    public string species = "";
    public string poseTrackPath = "";
    public string vehicleTrackPath = "";
    public string polarVelocityPath = "";
    [Range(0f, 360f)] public float facingYawDegrees;
    public bool cabinCamera;
    public bool inferShoulderShifts;
    public PoseTrack lastTrack;
    public VehicleTrack lastVehicleTrack;
    public CabinPolarVelocity lastPolarVelocity;

    [Header("Preview overlay")]
    [Range(0f, 1f)] public float previewVideoOpacity = WebcamAnimPreviewOverlay.DefaultVideoOpacity;
    public float previewVideoScale = 1f;
    public Vector2 previewVideoOffset01;
    public bool syncVehicleRagdoll = true;
    public WebcamAnimCameraShot[] cameraShots = System.Array.Empty<WebcamAnimCameraShot>();

    void OnValidate()
    {
        if (string.IsNullOrEmpty(recordingId))
            recordingId = name;
        if (string.IsNullOrEmpty(displayName))
            displayName = name;
        if (endMs < startMs)
            endMs = startMs;
        if (userDurationLimitMs < 0.0)
            userDurationLimitMs = 0.0;
        startMs = WebcamAnimTimelineGranularityUtil.SnapMs(startMs, granularity);
        endMs = WebcamAnimTimelineGranularityUtil.SnapMs(endMs, granularity);
        if (userDurationLimitMs > 0.0)
            userDurationLimitMs = WebcamAnimTimelineGranularityUtil.SnapMs(userDurationLimitMs, granularity);
    }

    public double DurationMs => System.Math.Max(1.0, endMs - startMs);

    public bool TimelineLooksDefault =>
        AnimationSpanDuration.LooksLikeDefaultRecordingSpan(startMs, endMs);

    /// <summary>
    /// Fill the take span from user limit, then video, then animation/pose file.
    /// Does not overwrite a user-typed In/Out/Duration.
    /// </summary>
    public void ApplyAutoDurationFromSources(double videoMs, double animationFileMs = 0)
    {
        if (userSetTimeline)
            return;
        bool hasVideo = videoMs > 0.0;
        if (hasVideo)
            cachedVideoDurationMs = videoMs;
        if (!TimelineLooksDefault && userDurationLimitMs <= 0.0 && !hasVideo)
            return;
        double resolved = AnimationSpanDuration.ResolveMs(
            userDurationLimitMs, videoMs, animationFileMs, DurationMs);
        endMs = startMs + System.Math.Max(1.0, resolved);
    }

    public void ApplyLoadedTrackDuration(double videoMs = 0)
    {
        double file = lastTrack != null ? lastTrack.LatestTimeMs() : 0.0;
        if (file <= 0.0 && lastVehicleTrack?.frames != null && lastVehicleTrack.frames.Length > 0)
            file = lastVehicleTrack.frames[lastVehicleTrack.frames.Length - 1].tMs;
        ApplyAutoDurationFromSources(videoMs > 0.0 ? videoMs : cachedVideoDurationMs, file);
    }

    public void SetUserInMs(double ms)
    {
        userSetTimeline = true;
        startMs = ms;
        if (endMs < startMs)
            endMs = startMs + 1.0;
    }

    public void SetUserOutMs(double ms)
    {
        userSetTimeline = true;
        endMs = ms;
        if (endMs < startMs)
            endMs = startMs;
    }

    public void SetUserDurationMs(double durationMs)
    {
        userSetTimeline = true;
        endMs = startMs + System.Math.Max(1.0, durationMs);
    }

    public WebcamAnimTypeMetadata ToTypeMetadata() => WebcamAnimTypeMetadata.FromRecording(this);

    public void ApplyTypeMetadata(WebcamAnimTypeMetadata meta)
    {
        if (meta == null)
            return;
        modelSpec = meta.model_spec ?? "";
        subsectionId = meta.subsection ?? "";
        animationListIndex = meta.animationListIndex;
        startMs = meta.timelineStartMs;
        endMs = meta.timelineEndMs;
        targetHint = meta.targetHint ?? targetHint;
        if (!string.IsNullOrEmpty(meta.species))
            species = meta.species;
        if (!string.IsNullOrEmpty(meta.poseTrackPath))
            poseTrackPath = meta.poseTrackPath;
        if (!string.IsNullOrEmpty(meta.vehicleTrackPath))
            vehicleTrackPath = meta.vehicleTrackPath;
        if (!string.IsNullOrEmpty(meta.polarVelocityPath))
            polarVelocityPath = meta.polarVelocityPath;
        facingYawDegrees = meta.facingYawDegrees;
        cabinCamera = meta.cabinCamera;
        inferShoulderShifts = meta.inferShoulderShifts;
        if (System.Enum.TryParse(meta.webcamAnimKind, true, out WebcamAnimKind parsedKind))
            kind = parsedKind;
        if (!string.IsNullOrEmpty(meta.granularity) &&
            System.Enum.TryParse(meta.granularity, true, out WebcamAnimTimelineGranularity parsedG))
            granularity = parsedG;
    }
}

[AddComponentMenu("Locomotion/Travel/Educational Travel Agent")]
public sealed class EducationalTravelAgent : TravelAgent
{
    public List<EducationalStep> steps = new List<EducationalStep>();
    public int selectedStepIndex;
    public CareerWarden warden;
    public EducationWarden educationWarden;
    public NarrativeCalendarAsset calendar;
    public CivilianPaperDoll doll;
    public CareerRoleSpec targetRole;

    public EducationalStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count ? steps[selectedStepIndex] : null;

    void Awake()
    {
        if (warden == null)
            warden = GetComponent<CareerWarden>();
    }

    public Vector3 PredictedPlacement()
    {
        var step = SelectedStep;
        if (step == null) return previewGoalWorld;
        return step.predictedWorld.sqrMagnitude > 1e-6f ? step.predictedWorld : previewGoalWorld;
    }

    public Vector3 InpaintPlacement()
    {
        var step = SelectedStep;
        if (step == null || !step.hasInpaint) return PredictedPlacement();
        return step.inpaintWorld;
    }

    public float[] BlueExpected01()
    {
        return doll != null ? doll.Expected01() : new[] { 0.55f, 0.55f, 0.55f, 0.4f };
    }

    public float[] RedFire01()
    {
        return doll != null ? doll.FireLimit01() : new[] { 0.9f, 0.9f, 0.9f, 0.85f };
    }

    public float[] WhiteStep01()
    {
        var step = SelectedStep;
        return step != null ? step.Expected01() : BlueExpected01();
    }

    /// <summary>Build steps from the role lane + missing credentials. Empty when RequireNoPretraining.</summary>
    public List<EducationalStep> ResolvePath(CivilianPaperDoll paperDoll, CareerRoleSpec role)
    {
        doll = paperDoll;
        targetRole = role;
        steps = new List<EducationalStep>();
        if (role == null || role.requireNoPretraining)
            return steps;

        if (role.lane != null && role.lane.goals != null)
        {
            for (int i = 0; i < role.lane.goals.Count; i++)
                steps.Add(FromGoal(role.lane.goals[i], CareerPlanEffect.None, role.roleId));
        }

        AppendMissing(role.certificationIds, LearningStationKind.Certification, paperDoll);
        AppendMissing(role.degreeIds, LearningStationKind.UniversityCourse, paperDoll);

        if (role.requiresManagement)
            steps.Add(NewStep(LearningStationKind.Conversation, "management", CareerPlanEffect.None, role.roleId));
        if (role.requiresHiringManager)
            steps.Add(NewStep(LearningStationKind.Phone, "hiring_manager", CareerPlanEffect.None, role.roleId));

        if (steps.Count > 0)
            steps[steps.Count - 1].effect = CareerPlanEffect.Hire;

        if (paperDoll != null)
        {
            paperDoll.educationalPlan = this;
            paperDoll.employment = CivilianEmploymentStatus.Training;
        }
        return steps;
    }

    /// <summary>Build class/dorm steps from a university curriculum course load.</summary>
    public List<EducationalStep> ResolveCourseLoad(
        CivilianPaperDoll paperDoll,
        UniversityCurriculumAsset curriculum,
        UniversityCampusAsset campus,
        UniversityAgeBracket bracket)
    {
        doll = paperDoll;
        steps = new List<EducationalStep>();
        if (curriculum == null || curriculum.courses == null)
            return steps;

        for (int i = 0; i < curriculum.courses.Count; i++)
        {
            var course = curriculum.courses[i];
            if (course == null) continue;
            if (course.ageBracket != bracket) continue;
            var step = NewStep(course.station, course.courseId, CareerPlanEffect.None, course.courseId);
            step.courseId = course.courseId;
            if (campus != null)
            {
                var room = campus.FindRoom(course.campusRoomId);
                if (room != null)
                {
                    step.predictedWorld = campus.RoomWorld(course.campusRoomId);
                    if (!string.IsNullOrEmpty(room.inpaintPrompt) || room.worldPosition.sqrMagnitude > 1e-6f)
                    {
                        step.hasInpaint = true;
                        step.inpaintWorld = step.predictedWorld;
                    }
                }
            }
            var staff = curriculum.StaffForCourse(course.courseId);
            if (staff.Count > 0 && !string.IsNullOrEmpty(staff[0].inpaintPrompt))
            {
                step.hasInpaint = true;
                if (step.inpaintWorld.sqrMagnitude < 1e-6f)
                    step.inpaintWorld = step.predictedWorld;
            }
            steps.Add(step);
        }

        if (campus != null)
        {
            var dorm = campus.FindRoom("dorm");
            if (dorm != null)
            {
                var board = NewStep(LearningStationKind.Desk, "room-and-board", CareerPlanEffect.None, "dorm");
                board.courseId = "room-and-board";
                board.predictedWorld = campus.RoomWorld("dorm");
                steps.Add(board);
            }
        }

        if (paperDoll != null)
        {
            paperDoll.educationalPlan = this;
            paperDoll.employment = CivilianEmploymentStatus.Student;
        }
        return steps;
    }

    public bool CompleteSelected()
    {
        var step = SelectedStep;
        if (step == null) return false;
        if (warden != null)
            warden.ApplyPlanEffect(doll, step.effect, step.targetRoleId);
        return true;
    }

    public int PrebakeCalendar(NarrativeCalendarAsset target)
    {
        calendar = target != null ? target : calendar;
        if (calendar == null) return 0;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        if (calendar.causalLinks == null)
            calendar.causalLinks = new List<NarrativeCausalLink>();
        int n = 0;
        string prevId = null;
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            if (string.IsNullOrEmpty(s.eventId))
                s.eventId = $"education_{s.station}_{i}";
            var wrapped = NarrativeEducationalEvent.FromStep(s, i);
            if (s.timing == EducationalTimingMode.RngRange)
            {
                float span = Mathf.Max(1f, s.maxSeconds - s.minSeconds);
                wrapped.calendarEvent.notes = $"rng {s.minSeconds:0}-{s.maxSeconds:0}s";
                wrapped.calendarEvent.durationSeconds = Mathf.RoundToInt(s.minSeconds + span * 0.5f);
            }
            calendar.events.Add(wrapped.calendarEvent);
            if (s.timing == EducationalTimingMode.Conditional && !string.IsNullOrEmpty(prevId))
            {
                calendar.causalLinks.Add(new NarrativeCausalLink
                {
                    fromEventId = prevId,
                    toEventId = wrapped.calendarEvent.id
                });
            }
            else if (!string.IsNullOrEmpty(s.enablesEventId))
            {
                calendar.causalLinks.Add(new NarrativeCausalLink
                {
                    fromEventId = wrapped.calendarEvent.id,
                    toEventId = s.enablesEventId
                });
            }
            prevId = wrapped.calendarEvent.id;
            n++;
        }
        return n;
    }

    void AppendMissing(string[] ids, LearningStationKind kind, CivilianPaperDoll paperDoll)
    {
        if (ids == null) return;
        for (int i = 0; i < ids.Length; i++)
        {
            if (string.IsNullOrEmpty(ids[i])) continue;
            if (paperDoll != null && paperDoll.HasCredential(ids[i])) continue;
            steps.Add(NewStep(kind, ids[i], CareerPlanEffect.None, targetRole != null ? targetRole.roleId : null));
        }
    }

    static EducationalStep FromGoal(EducationalLaneGoal goal, CareerPlanEffect effect, string roleId)
    {
        if (goal == null) return NewStep(LearningStationKind.Desk, null, effect, roleId);
        var step = NewStep(goal.station, goal.credentialId, effect, roleId);
        step.expected01 = CivilianPaperDoll.Pad4(goal.expected01, 0.5f);
        step.fireLimit01 = CivilianPaperDoll.Pad4(goal.fireLimit01, 0.9f);
        return step;
    }

    static EducationalStep NewStep(LearningStationKind station, string credentialId, CareerPlanEffect effect, string roleId)
    {
        return new EducationalStep
        {
            station = station,
            credentialId = credentialId,
            effect = effect,
            targetRoleId = roleId,
            expected01 = new[] { 0.5f, 0.5f, 0.5f, 0.4f },
            fireLimit01 = new[] { 0.9f, 0.9f, 0.9f, 0.85f }
        };
    }
}

public enum JusticeRehabStepKind
{
    Arrest = 0, Holding = 1, Trial = 2, Bail = 3, Sentencing = 4,
    Intake = 5, Custody = 6, Parole = 7, Rehab = 8, Outing = 9
}

[System.Serializable]
public sealed class JusticeRehabStep
{
    public JusticeRehabStepKind kind;
    public string label;
    public bool hasInpaint;
    [Range(0f, 1f)] public float intensity01 = 0.4f;
    public string axis = "dialog";
}

public sealed class JusticeRehabilitationTravelAgent : TravelAgent
{
    public List<JusticeRehabStep> steps = DefaultPipeline();
    public int selectedStepIndex;
    public PrisonWarden warden;
    public JusticeRehabStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count ? steps[selectedStepIndex] : null;
    public Vector3 PredictedPlacement() => previewGoalWorld;
    public Vector3 InpaintPlacement() => previewGoalWorld;
    public bool SelectedOverLimit()
    {
        var step = SelectedStep;
        return step != null && warden != null && warden.OverUpperLimit(step.axis, step.intensity01);
    }

    public PrisonWardenAction ScoreSelected()
    {
        var step = SelectedStep;
        if (step == null || warden == null) return PrisonWardenAction.Remuneration;
        return warden.ScoreStep(step.axis, step.intensity01, step.hasInpaint);
    }

    public static List<JusticeRehabStep> DefaultPipeline()
    {
        var list = new List<JusticeRehabStep>();
        foreach (JusticeRehabStepKind k in Enum.GetValues(typeof(JusticeRehabStepKind)))
            list.Add(new JusticeRehabStep { kind = k, label = k.ToString() });
        return list;
    }

    public int PrebakeCalendar(NarrativeCalendarAsset cal)
    {
        if (cal == null) return 0;
        if (cal.events == null) cal.events = new List<NarrativeCalendarEvent>();
        if (steps == null || steps.Count == 0) steps = DefaultPipeline();
        for (int i = 0; i < steps.Count; i++)
            cal.events.Add(new NarrativeCalendarEvent { id = "rehab_" + i, title = steps[i] != null ? steps[i].label : "rehab" });
        return steps.Count;
    }
}


public enum PilotGpsHudMode
{
    BakedRoute = 0,
    RealtimeIsometric = 1
}

public sealed class PilotGpsRouteBakeCache : ScriptableObject
{
    public System.Collections.Generic.List<Vector3> waypoints = new System.Collections.Generic.List<Vector3>();
    public Bounds worldBounds;
    public bool hasBounds;
    public int rtWidth = 512;
    public int rtHeight = 512;
}

public sealed class PilotGpsHudWebtop : MonoBehaviour
{
    public Transform mount;
    public HelicopterVehicleRagdoll helicopter;
    public AirplaneVehicleRagdoll airplane;
    public TravelAgent travelAgent;
    public PilotGpsHudMode mode = PilotGpsHudMode.BakedRoute;
    public RenderTexture displayTexture;
    public PilotGpsRouteBakeCache bakeCache;

    public RenderTexture EnsureRenderTexture(int w = 0, int h = 0)
    {
        if (displayTexture == null)
            displayTexture = new RenderTexture(w > 0 ? w : 512, h > 0 ? h : 512, 16);
        return displayTexture;
    }

    public static PilotGpsRouteBakeCache BakeFromTravelAgent(TravelAgent agent, int width, int height)
    {
        var cache = ScriptableObject.CreateInstance<PilotGpsRouteBakeCache>();
        cache.rtWidth = width;
        cache.rtHeight = height;
        var plan = agent != null ? agent.CachedPlan : null;
        if (plan != null && plan.segments != null)
        {
            for (int s = 0; s < plan.segments.Count; s++)
            {
                var seg = plan.segments[s];
                if (seg?.waypoints == null) continue;
                for (int i = 0; i < seg.waypoints.Count; i++)
                    cache.waypoints.Add(seg.waypoints[i]);
            }
        }
        if (cache.waypoints.Count > 0)
        {
            cache.worldBounds = new Bounds(cache.waypoints[0], Vector3.one);
            for (int i = 1; i < cache.waypoints.Count; i++)
                cache.worldBounds.Encapsulate(cache.waypoints[i]);
            cache.hasBounds = true;
        }
        return cache;
    }
}

[System.Serializable]
public sealed class RoadLotWallSection
{
    [Range(0f, 1f)] public float startT01;
    [Range(0f, 1f)] public float endT01 = 1f;
    public float height = 2f;
    public bool isGap;
    public string gateOpenCloseTopologyId;
    public float Length01 => Mathf.Max(0f, endT01 - startT01);
}

public sealed class RoadLotBoundarySpline : MonoBehaviour
{
    public System.Collections.Generic.List<Vector3> controlPoints = new System.Collections.Generic.List<Vector3>();
    public System.Collections.Generic.List<RoadLotWallSection> wallSections = new System.Collections.Generic.List<RoadLotWallSection>();

    public void EnsureClosedLoopDefault()
    {
        if (controlPoints.Count < 3)
        {
            controlPoints.Clear();
            controlPoints.Add(new Vector3(-20f, 0f, -20f));
            controlPoints.Add(new Vector3(20f, 0f, -20f));
            controlPoints.Add(new Vector3(20f, 0f, 20f));
            controlPoints.Add(new Vector3(-20f, 0f, 20f));
        }
        if (wallSections.Count == 0)
            wallSections.Add(new RoadLotWallSection { startT01 = 0f, endT01 = 1f, height = 2f });
    }

    public Vector3 CentroidLocal()
    {
        if (controlPoints == null || controlPoints.Count == 0) return Vector3.zero;
        Vector3 s = Vector3.zero;
        for (int i = 0; i < controlPoints.Count; i++)
            s += controlPoints[i];
        return s / controlPoints.Count;
    }

    public float wallThickness = 0.15f;
    public MeshFilter wallMeshFilter;
    public MeshCollider wallMeshCollider;
    Mesh bakedWallMesh;

    public bool TryValidateWallSections(out string error)
    {
        try
        {
            ValidateWallSections();
            error = null;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void ValidateWallSections()
    {
        EnsureClosedLoopDefault();
        float sum = 0f;
        if (wallSections != null)
            for (int i = 0; i < wallSections.Count; i++)
                if (wallSections[i] != null) sum += wallSections[i].Length01;
        if (Mathf.Abs(sum - 1f) > 0.02f)
            throw new InvalidOperationException("Wall section lengths must sum to 1.");
    }

    public void SplitAt(float t01)
    {
        if (wallSections == null || wallSections.Count == 0) EnsureClosedLoopDefault();
        float t = Mathf.Clamp01(t01);
        for (int i = 0; i < wallSections.Count; i++)
        {
            var s = wallSections[i];
            if (s == null || t <= s.startT01 + 1e-4f || t >= s.endT01 - 1e-4f) continue;
            float end = s.endT01;
            s.endT01 = t;
            wallSections.Insert(i + 1, new RoadLotWallSection { startT01 = t, endT01 = end, height = s.height });
            return;
        }
    }

    public Mesh BakeWallMesh()
    {
        ValidateWallSections();
        if (bakedWallMesh == null) bakedWallMesh = new Mesh { name = "RoadLotWall" };
        bakedWallMesh.Clear();
        bakedWallMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1f, 1f, 0f) };
        bakedWallMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        bakedWallMesh.RecalculateNormals();
        if (wallMeshCollider == null)
            wallMeshCollider = gameObject.AddComponent<MeshCollider>();
        return bakedWallMesh;
    }
}

[AddComponentMenu("Locomotion/Travel/Conversation Bus Travel Agent")]
public enum ConversationSectionType
{
    Dialog = 0,
    Travel = 1,
    Law = 2,
    Religious = 3,
    Councilor = 4,
    Chancellor = 5,
    Court = 6,
    Government = 7
}

[Serializable]
public sealed class ConversationBusStep
{
    public string displayName = "Step";
    public ConversationSectionType sectionType = ConversationSectionType.Dialog;
    public string dialogNodeId;
    public string dialogTreeSetId;
    public ConversationCard conversationCard;
    public LawConversationCard lawConversationCard;
    public ReligiousLawCard religiousLawCard;
    public LawCard lawCard;
    public CouncilorCard councilorCard;
    public ChancellorCard chancellorCard;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    public bool accordionOpen = true;
    public Vector3 predictedWorld;
    public bool hasInpaint;
    public Vector3 inpaintWorld;
    public string eventId;
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public float durationSeconds = 600f;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);

    public WardenLimitKv AddDefaultLimit()
    {
        if (limits == null) limits = new List<WardenLimitKv>();
        var row = new WardenLimitKv { key = "limit-" + (limits.Count + 1), value01 = 0.5f };
        limits.Add(row);
        return row;
    }
}

public sealed class ConversationBusTravelAgent : TravelAgent
{
    public List<ConversationBusStep> steps = new List<ConversationBusStep>();
    public int selectedStepIndex;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    public CourtWarden courtWarden;
    public CorruptionWarden corruptionWarden;
    public ConstitutionWarden constitutionWarden;
    public RightsWarden rightsWarden;
    public JusticeWarden justiceWarden;
    public TheocraticWarden theocraticWarden;
    public LoveWarden loveWarden;
    public RomanceWarden romanceWarden;
    public ConsentWarden consentWarden;
    public NarrativeCalendarAsset calendar;
    public List<LawCard> observedLaws = new List<LawCard>();
    public List<string> observedScripture = new List<string>();

    public ConversationBusStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    void Awake()
    {
        if (courtWarden == null) courtWarden = GetComponent<CourtWarden>();
        if (corruptionWarden == null) corruptionWarden = GetComponent<CorruptionWarden>();
        if (constitutionWarden == null) constitutionWarden = GetComponent<ConstitutionWarden>();
        if (rightsWarden == null) rightsWarden = GetComponent<RightsWarden>();
        if (justiceWarden == null) justiceWarden = GetComponent<JusticeWarden>();
        if (theocraticWarden == null) theocraticWarden = GetComponent<TheocraticWarden>();
        if (loveWarden == null) loveWarden = GetComponent<LoveWarden>();
        if (romanceWarden == null) romanceWarden = GetComponent<RomanceWarden>();
        if (consentWarden == null) consentWarden = GetComponent<ConsentWarden>();
    }

    public WardenLimitKv AddDefaultLimit()
    {
        if (limits == null) limits = new List<WardenLimitKv>();
        var row = new WardenLimitKv { key = "agent-limit-" + (limits.Count + 1), value01 = 0.5f };
        limits.Add(row);
        return row;
    }

    public ConversationBusStep AddSection(ConversationSectionType type)
    {
        if (steps == null) steps = new List<ConversationBusStep>();
        var step = new ConversationBusStep
        {
            sectionType = type,
            displayName = type.ToString(),
            accordionOpen = true
        };
        switch (type)
        {
            case ConversationSectionType.Law:
                step.lawCard = ScriptableObject.CreateInstance<LawCard>();
                step.lawCard.statuteId = "draft";
                step.lawConversationCard = ScriptableObject.CreateInstance<LawConversationCard>();
                break;
            case ConversationSectionType.Religious:
                step.religiousLawCard = ScriptableObject.CreateInstance<ReligiousLawCard>();
                break;
            case ConversationSectionType.Councilor:
                step.councilorCard = new CouncilorCard();
                break;
            case ConversationSectionType.Chancellor:
                step.chancellorCard = new ChancellorCard();
                break;
            default:
                step.conversationCard = ScriptableObject.CreateInstance<ConversationCard>();
                break;
        }
        steps.Add(step);
        selectedStepIndex = steps.Count - 1;
        return step;
    }

    public static float WardenOrDefault(float? assigned) => assigned ?? 0.5f;

    public float[] DiamondActual01()
    {
        return new[]
        {
            courtWarden != null ? courtWarden.Allow01() : 0.5f,
            constitutionWarden != null ? constitutionWarden.Allow01() : 0.5f,
            rightsWarden != null ? rightsWarden.Allow01() : 0.5f,
            theocraticWarden != null ? theocraticWarden.Allow01() : 0.5f
        };
    }

    public float[] DiamondLimit01()
    {
        return new[]
        {
            0.9f,
            0.9f,
            0.9f,
            0.9f
        };
    }

    public float[] DiamondGreen01()
    {
        return new[]
        {
            loveWarden != null ? loveWarden.Allow01() : 0.5f,
            romanceWarden != null ? romanceWarden.Allow01() : 0.5f,
            consentWarden != null ? consentWarden.Allow01() : 0.5f,
            justiceWarden != null ? justiceWarden.Allow01() : 0.5f
        };
    }

    public string ComposeDialoguePrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Observed laws:");
        if (observedLaws != null)
        {
            for (int i = 0; i < observedLaws.Count; i++)
            {
                var law = observedLaws[i];
                if (law == null) continue;
                sb.Append("- ").Append(law.statuteId).Append(": ").AppendLine(law.billText);
            }
        }
        sb.AppendLine("Scripture:");
        if (observedScripture != null)
        {
            for (int i = 0; i < observedScripture.Count; i++)
                if (!string.IsNullOrEmpty(observedScripture[i]))
                    sb.Append("- ").AppendLine(observedScripture[i]);
        }
        if (theocraticWarden != null && theocraticWarden.activeScriptureRefs != null)
        {
            for (int i = 0; i < theocraticWarden.activeScriptureRefs.Count; i++)
                if (!string.IsNullOrEmpty(theocraticWarden.activeScriptureRefs[i]))
                    sb.Append("- ").AppendLine(theocraticWarden.activeScriptureRefs[i]);
        }
        var step = SelectedStep;
        if (step != null && step.councilorCard != null && step.councilorCard.laws != null)
        {
            for (int i = 0; i < step.councilorCard.laws.Count; i++)
            {
                var law = step.councilorCard.laws[i];
                if (law != null)
                    sb.Append("- councilor ").AppendLine(law.statuteId);
            }
        }
        return sb.ToString();
    }

    public int PrebakeCalendar(NarrativeCalendarAsset target)
    {
        calendar = target != null ? target : calendar;
        if (calendar == null) return 0;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        int n = 0;
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            if (string.IsNullOrEmpty(s.eventId))
                s.eventId = $"conversation_{s.sectionType}_{i}";
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = s.eventId,
                title = s.displayName,
                startDateTime = s.startDateTime,
                durationSeconds = Mathf.RoundToInt(s.durationSeconds),
                notes = ComposeDialoguePrompt(),
                tags = new List<string> { "conversation", s.sectionType.ToString().ToLowerInvariant() }
            });
            n++;
        }
        return n;
    }
}

public static class RagdollGetUpTreeFactory
{
    public const string DefaultTreeName = "RagdollGetUpBehaviorTree";
    public const string PrefabAssetPath = "Assets/locomotion/Prefabs/ActorRagdolls/RagdollGetUpBehaviorTree.prefab";
    public const string ResourcesAssetPath = "Assets/locomotion/Resources/RagdollGetUpBehaviorTree.prefab";
    public const string ResourcesLoadName = "RagdollGetUpBehaviorTree";

    public static BehaviorTree Build(Transform parent = null)
    {
        var root = new GameObject(DefaultTreeName);
        if (parent != null) root.transform.SetParent(parent, false);
        var bt = root.AddComponent<BehaviorTree>();
        var selector = root.AddComponent<RagdollGetUpSelectorNode>();
        bt.rootNode = selector;
        var sequenceGo = new GameObject("GetUpSequence");
        sequenceGo.transform.SetParent(root.transform, false);
        var sequence = sequenceGo.AddComponent<RagdollPlayerSequenceNode>();
        var conditionGo = new GameObject("OnGroundAndFallen");
        conditionGo.transform.SetParent(sequenceGo.transform, false);
        var condition = conditionGo.AddComponent<RagdollOnGroundConditionNode>();
        var actionGo = new GameObject("GetUp");
        actionGo.transform.SetParent(sequenceGo.transform, false);
        var action = actionGo.AddComponent<RagdollGetUpActionNode>();
        condition.getUpAction = action;
        sequence.children = new List<BehaviorTreeNode> { condition, action };
        var idleGo = new GameObject("IdleSuccess");
        idleGo.transform.SetParent(root.transform, false);
        var idle = idleGo.AddComponent<RagdollIdleSuccessNode>();
        selector.children = new List<BehaviorTreeNode> { sequence };
        selector.passthroughChild = idle;
        return bt;
    }
}

[DisallowMultipleComponent]
public sealed class RagdollLimbCapsuleFit : MonoBehaviour
{
    public const string ProxyName = "LimbCapsuleProxy";
    public static bool SuppressValidateApply;
    public Vector3 centerOffsetLocal;
    public Vector3 eulerOffsetDegrees;
    [Range(0, 2)] public int direction = 1;
    public float height = 0.12f;
    public float radius = 0.04f;
    public void Apply() { }
    public void RotateAxisDegrees(int axis, float degrees)
    {
        Vector3 e = eulerOffsetDegrees;
        if (axis == 0) e.x += degrees;
        else if (axis == 1) e.y += degrees;
        else e.z += degrees;
        eulerOffsetDegrees = e;
        Apply();
    }

    public void NudgeLocal(Vector3 delta)
    {
        centerOffsetLocal += delta;
        Apply();
    }
}

public static class AnimationSpanDuration
{
    public const float DefaultPreviewSeconds = 2f;
    public const double DefaultRecordingEndMs = 1000.0;

    public static float ResolveSeconds(float userLimitSeconds, float videoSeconds, float animationFileSeconds, float fallbackSeconds = DefaultPreviewSeconds)
    {
        if (userLimitSeconds > 0f) return userLimitSeconds;
        if (videoSeconds > 0f) return videoSeconds;
        if (animationFileSeconds > 0f) return animationFileSeconds;
        return Mathf.Max(0.1f, fallbackSeconds);
    }

    public static bool TryParseSeconds(string text, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
    }

    public static string FormatSeconds(float seconds) => seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    public static bool TryParseMs(string text, out double ms)
    {
        ms = 0.0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return double.TryParse(text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ms);
    }

    public static string FormatMs(double ms) => ms.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    public static float MsToSeconds(double ms) => (float)(System.Math.Max(0.0, ms) / 1000.0);

    public static bool LooksLikeDefaultRecordingSpan(double startMs, double endMs) =>
        startMs <= 0.0 && System.Math.Abs(endMs - DefaultRecordingEndMs) < 0.51;

    public static double ResolveMs(double userLimitMs, double videoMs, double animationFileMs, double fallbackMs = DefaultRecordingEndMs)
    {
        if (userLimitMs > 0.0) return userLimitMs;
        if (videoMs > 0.0) return videoMs;
        if (animationFileMs > 0.0) return animationFileMs;
        return System.Math.Max(1.0, fallbackMs);
    }
}
