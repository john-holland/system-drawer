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
    Generic = 0, Fire = 1, Intruder = 2, Flood = 3, Chemical = 4
}

public enum JusticeAction
{
    ShutOffHeat = 0, SecureArea = 1, Evict = 2, Arrest = 3, Deescalate = 4
}

[System.Serializable]
public class ThreatCard : GoodSection
{
    public ThreatKind threatKind = ThreatKind.Generic;
    public ThreatAlertLevel alertLevel;
    public ThreatLevel threatLevel;
    public GameObject contextOwner;
    public GameObject reportedSource;
    public ThreatCard() { isThreatGoal = true; physicalPathingTag = "threat"; }
    public static ThreatCard Generate(ThreatKind kind, GameObject contextOwner, GameObject source, ThreatAlertLevel alert = default)
    {
        return new ThreatCard
        {
            threatKind = kind, contextOwner = contextOwner, reportedSource = source,
            alertLevel = alert, sectionName = "threat_" + kind, isThreatGoal = true
        };
    }
}

[System.Serializable]
public class JusticeCard : GoodSection
{
    public JusticeAction justiceAction = JusticeAction.ShutOffHeat;
    public GameObject hazardTarget;
    public JusticeCard() { isJusticeGoal = true; physicalPathingTag = "justice"; }
    public static JusticeCard Generate(JusticeAction action, GameObject target)
    {
        return new JusticeCard
        {
            justiceAction = action, hazardTarget = target,
            sectionName = "justice_" + action, isJusticeGoal = true
        };
    }
}

[System.Serializable]
public class ChefCard : GoodSection
{
    public ChefCard() { isChefGoal = true; physicalPathingTag = "chef"; }
}

public static class ConsiderChefCards
{
    public static ChefCard MakeDefaultCard() => new ChefCard { sectionName = "chef_default", isChefGoal = true };
}

[System.Serializable]
public class VoterCard : GoodSection
{
    public VoterCard() { isVoteGoal = true; physicalPathingTag = "vote"; }
    public bool BlockedByDeveloperInpaint() => false;
    public static VoterCard GenerateDefault(GameObject target) =>
        new VoterCard { sectionName = "vote_default", isVoteGoal = true };
}

[System.Serializable]
public class CivicCard : GoodSection
{
    public CivicCard() { isCivicGoal = true; physicalPathingTag = "civic"; }
}

public static class ConsiderCivicCards
{
    public static CivicCard MakeDefaultCard() => new CivicCard { sectionName = "civic_default", isCivicGoal = true };
}

[System.Serializable]
public class CivilCard : GoodSection
{
    public CivilCard() { isCivilGoal = true; physicalPathingTag = "civil"; }
}

public static class ConsiderCivilCards
{
    public static CivilCard MakeDefaultCard() => new CivilCard { sectionName = "civil_default", isCivilGoal = true };
}

[System.Serializable]
public class ClogToiletCard : GoodSection { }
[System.Serializable]
public class PlungeToiletCard : GoodSection { }
[System.Serializable]
public class SnakeToiletCard : GoodSection { }

public static class ConsiderPlumbingCards
{
    public static GoodSection MakeDefaultCard() => new PlungeToiletCard { sectionName = "plumbing_default" };
}

[System.Serializable]
public class ConstructionPhaseCard : GoodSection
{
    public static ConstructionPhaseCard GenerateDefault(Vector3 pos) =>
        new ConstructionPhaseCard { sectionName = "construction_default" };
}

public sealed class CardHistoryManager : MonoBehaviour
{
    public static CardHistoryManager Instance { get; private set; }
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void RecordCard(GoodSection card, object solver, string tag) { }
    public void RecordPool(object solver, string tag) { }
}

public sealed class ParkourLandAnimationDriver : MonoBehaviour
{
    public string activeAnimationGroupTag;
    public PhysicsIKTrainingCategory activeCategory;
    public LandAnimationPrep activePrep;
    public Vector3 landingGoalWorld;
    public bool hasLandingGoal;
    public RagdollIKAnimationManager ikAnimationManager;
    public bool showGizmo = true;

    public static bool IsLandingCategory(PhysicsIKTrainingCategory cat) =>
        cat.ToString().IndexOf("Land", System.StringComparison.OrdinalIgnoreCase) >= 0;

    public static bool IsLandingTag(string tag) =>
        !string.IsNullOrEmpty(tag) && tag.IndexOf("land", System.StringComparison.OrdinalIgnoreCase) >= 0;

    public static ParkourLandAnimationDriver FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        var d = host.GetComponent<ParkourLandAnimationDriver>() ?? host.AddComponent<ParkourLandAnimationDriver>();
        return d;
    }

    public void PlayLanding(string animationGroupTag, Vector3 goalWorld, LandAnimationPrep prep, float durationSeconds = 1f)
    {
        activeAnimationGroupTag = animationGroupTag ?? "";
        landingGoalWorld = goalWorld;
        hasLandingGoal = true;
        activePrep = prep ?? new LandAnimationPrep();
        activePrep.EnsureReady();
    }
}

public sealed class PrepareLandAnimationNode : TravelContextBehaviorTreeNode
{
    public MultiModalSegment segment;
    public Vector3 landingGoalOverride;
    public bool useLandingGoalOverride;
    public float landDurationSeconds = 1.2f;

    void Awake() { nodeType = NodeType.Action; }

    public override BehaviorTreeStatus Execute(BehaviorTree tree) => BehaviorTreeStatus.Success;
}

public class HelicopterVehicleRagdoll : VehicleRagdoll
{
    public PilotGpsHudWebtop gpsHud;
    public PilotGpsHudMode defaultHudMode = PilotGpsHudMode.BakedRoute;

    public void EnsureSystems()
    {
        if (gpsHud == null)
            gpsHud = GetComponent<PilotGpsHudWebtop>() ?? gameObject.AddComponent<PilotGpsHudWebtop>();
        gpsHud.helicopter = this;
        gpsHud.mode = defaultHudMode;
    }
}
public class AirplaneVehicleRagdoll : VehicleRagdoll { }

public static class HelicopterTravelRouteMerger
{
    public static void MergeIntoLeg(TravelLegSequenceNode legNode, HelicopterVehicleRagdoll heli, MultiModalSegment seg) { }
}

public static class AircraftTravelRouteMerger
{
    public static void MergeIntoLeg(TravelLegSequenceNode legNode, AirplaneVehicleRagdoll plane, MultiModalSegment seg) { }
}

public class RailTrackStructure : MonoBehaviour
{
    public string segmentId;
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
    public static bool TryMerge(RagdollActor actor) => false;
}

public sealed class PaintCanvasCurvedDecal : MonoBehaviour
{
    public bool WorldToUv(Vector3 world, out Vector2 uv)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        uv = new Vector2(local.x + 0.5f, local.y + 0.5f);
        return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
    }
}

public sealed class PlantCutTakeRuntime : MonoBehaviour
{
    public void ApplyCutTake(Vector3 worldHit, Vector3 planeNormal, float severity01, float timeT = -1f) { }
}

public sealed class PulleySurfaceRagdoll : MonoBehaviour
{
    public const string PullStringId = "shade.pull_string";
    public IVehicleControlSurface PullSurface => null;
    public bool MatchesPullString(string localSurfaceId) =>
        string.Equals(localSurfaceId, PullStringId, System.StringComparison.OrdinalIgnoreCase);
}

public sealed class ConsentWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float maxPhysicality01 = 0.95f;
    public float Evaluate() => maxPhysicality01;
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
    public virtual void ReportPieceMemory(ImpulseMaterialMemory piece, float impulseNorm01) { }
    public virtual void ReportAnonymousImpulse(float impulseN, Vector3 worldPoint, GameObject source) { }
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
public sealed class CareerWarden : MonoBehaviour
{
    public CivilianDemographics demographics = new CivilianDemographics();
    public CareerWardenAction lastRecommendation = CareerWardenAction.Retain;

    public void ApplySocietyFeatures(System.Collections.Generic.IReadOnlyDictionary<string, float> societyFeatures, int population = 100)
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
public sealed class StationConfig
{
    [TextArea(2, 6)] public string notes;
    public string vehicleId;
    public string vehicleRouteId;
    public string buildingStableId;
    public float staffingWeight = 1f;
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
}

[AddComponentMenu("Locomotion/Build/Radial Build Host")]
[ExecuteAlways]
public sealed class RadialBuildHost : MonoBehaviour
{
    public const string StartPostAnchorName = "startPostAnchor";
    public const string StartPostBoundsName = "startPostBounds";

    public GameObject centerPost;
    public RadialBuildSpec spec = new RadialBuildSpec();
    public Vector3 pieceSize = Vector3.one;
    public List<RadialSolvedConfig> solvedConfigs = new List<RadialSolvedConfig>();

    public Transform StartPostAnchor =>
        FindChild(centerPost != null ? centerPost.transform : transform, StartPostAnchorName);

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
        }
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

public sealed class WebcamAnimRecordingAsset : ScriptableObject
{
    public string recordingId;
    public string displayName = "New Recording";
    public string poseTrackPath = "";
    public PoseTrack lastTrack;
}

public sealed class EducationalTravelAgent : TravelAgent
{
    public Vector3 PredictedPlacement() => previewGoalWorld;
    public Vector3 InpaintPlacement() => previewGoalWorld;
}

public enum JusticeRehabStepKind
{
    Arrest = 0, Holding = 1, Trial = 2, Bail = 3, Sentencing = 4,
    Intake = 5, Custody = 6, Parole = 7, Rehab = 8, Outing = 9
}

public sealed class JusticeRehabilitationTravelAgent : TravelAgent
{
    public Vector3 PredictedPlacement() => previewGoalWorld;
    public Vector3 InpaintPlacement() => previewGoalWorld;
    public bool SelectedOverLimit() => false;
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
        _ = agent;
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

    public bool TryValidateWallSections(out string error)
    {
        error = null;
        EnsureClosedLoopDefault();
        return true;
    }
}

public sealed class ConversationBusTravelAgent : TravelAgent
{
    public string ComposeDialoguePrompt() => "";
}

public static class RagdollGetUpTreeFactory
{
    public const string PrefabAssetPath = "Assets/locomotion/Prefabs/ActorRagdolls/RagdollGetUpBehaviorTree.prefab";
    public static BehaviorTree Build(Transform parent = null) => null;
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
}
