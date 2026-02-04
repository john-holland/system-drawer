using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One set of solver coefficients and outcome metrics from a training run.
/// Used to persist and later apply tuned weights to PhysicsCardSolver.
/// </summary>
[Serializable]
public struct PhysicsIKTrainedSet
{
    [Header("Solver Weights")]
    [Range(0f, 1f)] public float degreesWeight;
    [Range(0f, 1f)] public float torqueWeight;
    [Range(0f, 1f)] public float forceWeight;
    [Range(0f, 1f)] public float velocityWeight;
    [Range(0f, 1f)] public float comfortWeight;
    [Range(0f, 1f)] public float feasibilityWeight;

    [Header("Power")]
    [Tooltip("Global scale for force/activation (e.g. 1 = default, 2 = MORE POWER)")]
    public float powerScale;

    [Header("Outcome Metrics")]
    public float completionTime;
    public float accuracyScore;
    public float powerUsed;

    [Header("Reproducibility")]
    public int seed;
    public string tag;

    [Header("Ragdoll Constraints (tool / animal form)")]
    [Tooltip("RigidbodyConstraints applied to ragdoll capsule during run (e.g. FreezeRotationZ). Stored as int for serialization.")]
    public int rigidbodyConstraints;

    public static PhysicsIKTrainedSet FromSolver(PhysicsCardSolver solver, float power = 1f)
    {
        if (solver == null)
            return Default();
        return new PhysicsIKTrainedSet
        {
            degreesWeight = solver.degreesWeight,
            torqueWeight = solver.torqueWeight,
            forceWeight = solver.forceWeight,
            velocityWeight = solver.velocityWeight,
            comfortWeight = solver.comfortWeight,
            feasibilityWeight = solver.feasibilityWeight,
            powerScale = power,
            completionTime = 0f,
            accuracyScore = 0f,
            powerUsed = 0f,
            seed = 0,
            tag = "",
            rigidbodyConstraints = 0
        };
    }

    public static PhysicsIKTrainedSet Default()
    {
        return new PhysicsIKTrainedSet
        {
            degreesWeight = 0.3f,
            torqueWeight = 0.3f,
            forceWeight = 0.2f,
            velocityWeight = 0.2f,
            comfortWeight = 0.3f,
            feasibilityWeight = 0.7f,
            powerScale = 1f,
            completionTime = 0f,
            accuracyScore = 0f,
            powerUsed = 0f,
            seed = 0,
            tag = "",
            rigidbodyConstraints = 0
        };
    }

    public void ApplyTo(PhysicsCardSolver solver)
    {
        if (solver == null) return;
        solver.degreesWeight = degreesWeight;
        solver.torqueWeight = torqueWeight;
        solver.forceWeight = forceWeight;
        solver.velocityWeight = velocityWeight;
        solver.comfortWeight = comfortWeight;
        solver.feasibilityWeight = feasibilityWeight;
    }

    /// <summary>Apply stored rigidbody constraints to ragdoll capsule (for tool/animal-form runs).</summary>
    public void ApplyConstraintsTo(Rigidbody rb)
    {
        if (rb == null) return;
        rb.constraints = (RigidbodyConstraints)rigidbodyConstraints;
    }
}

/// <summary>
/// Test category for IK training runs.
/// </summary>
public enum PhysicsIKTrainingCategory
{
    Locomotion,
    ToolUse,
    /// <summary>Goal: not falling over — most stable rigid body given physical sim / animation baked tree.</summary>
    Idle,
    Climb,
    Swing,
    Pick,
    Roll,
    /// <summary>Train for throw target then perform implied throw (thrown object + hand mode).</summary>
    Throw,
    /// <summary>Carry an object; optional pleaseHold to re-grasp if put down.</summary>
    Carry,
    /// <summary>Hold isometric pose (plank, wall sit); fitness = least movement.</summary>
    Isometric,
    /// <summary>Lift object into place at target position/rotation.</summary>
    Place,
    /// <summary>Hit target with limb(s) or tool; limb meets target using baked IK.</summary>
    Hit,
    /// <summary>Pick up weight/tool, activate muscle group; fitness = least radial movement.</summary>
    Weightlift,
    /// <summary>Intercept object with hand(s); fitness = catch success.</summary>
    Catch,
    /// <summary>Launch toward target (e.g. basketball shot); fitness = accuracy toward target.</summary>
    Shoot
}

/// <summary>
/// Hand(s) used for throw: left, right, or two hands.
/// </summary>
public enum ThrowHandMode
{
    Left,
    Right,
    TwoHands
}

/// <summary>
/// Asset that holds trained coefficient sets from IK animation training runs.
/// References AnimationBehaviorTree (and optionally PhysicsCardSolver); supports overwrite and append.
/// </summary>
[CreateAssetMenu(fileName = "PhysicsIKTrainingRun", menuName = "Locomotion/Physics IK Training Run Asset", order = 1)]
public class PhysicsIKTrainingRunAsset : ScriptableObject
{
    [Header("Metadata")]
    public string displayName = "IK Training Run";
    public string creationTime;

    [Header("References")]
    [Tooltip("Animation tree used for this training run")]
    public AnimationBehaviorTree animationTree;
    [Tooltip("Optional solver reference (scene context)")]
    public PhysicsCardSolver solver;
    public PhysicsIKTrainingCategory testCategory = PhysicsIKTrainingCategory.Locomotion;

    [Header("Card and Tool (Climb / Swing / Pick / Roll)")]
    [Tooltip("Card to use for this run (e.g. climb card, swing card). Used when category is Climb/Swing/Pick/Roll.")]
    public GoodSection cardSlot;
    [Tooltip("Tool to carry/use (e.g. ladder, batterang). Used when category is Climb/Swing/Pick/Roll.")]
    public GameObject toolSlot;

    [Header("Throw (needs to be thrown)")]
    [Tooltip("When true (or category is Throw), trainer runs throw mode: spatial goal = throw target, then implied throw action.")]
    public bool needsToBeThrown;
    [Tooltip("Thrown object: GameObject, Transform, or bone. What gets thrown; trainer uses this for release/origin.")]
    public UnityEngine.Object thrownObject;
    [Tooltip("Hand(s) used for throw: left, right, or two hands.")]
    public ThrowHandMode throwHandMode = ThrowHandMode.Right;
    [Tooltip("World position used as the throw target when category is Throw.")]
    public Vector3 throwTargetPosition;
    [Tooltip("If set, throw target is this object's position when category is Throw (overrides throwTargetPosition at runtime).")]
    public GameObject throwGoalTarget;
    [Tooltip("Animation tree nodes to try for baking impulse values (same optimization strategy).")]
    public List<AnimationBehaviorTreeNode> throwAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Tooltip("Per-slot effective range min (meters). Same count as throwAnimationTrees; used by SelectThrowAnimationByDistance.")]
    public List<float> throwAnimationRangeMin = new List<float>();

    [Tooltip("Per-slot effective range max (meters). Same count as throwAnimationTrees; used by SelectThrowAnimationByDistance.")]
    public List<float> throwAnimationRangeMax = new List<float>();

    [Header("Carry")]
    [Tooltip("Object to carry when category is Carry.")]
    public GameObject carriedObject;
    [Tooltip("When true, re-grasp if object is put down (do not wait for user prompt).")]
    public bool pleaseHold;
    [Tooltip("Optional carry animation trees.")]
    public List<AnimationBehaviorTreeNode> carryAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Isometric")]
    [Tooltip("Pose/card that defines the hold (e.g. plank, wall sit). Used when category is Isometric.")]
    public GoodSection isometricCard;
    [Tooltip("Target hold duration in seconds. 0 = use default.")]
    public float isometricHoldDuration = 5f;

    [Header("Place")]
    [Tooltip("Object to place when category is Place.")]
    public GameObject placeObject;
    [Tooltip("Target position for placement.")]
    public Vector3 placeTargetPosition;
    [Tooltip("Target rotation for placement.")]
    public Quaternion placeTargetRotation = Quaternion.identity;
    [Tooltip("Optional place/lift animation trees.")]
    public List<AnimationBehaviorTreeNode> placeAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Hit")]
    [Tooltip("Target to hit when category is Hit.")]
    public GameObject hitTarget;
    [Tooltip("Limb bone names (e.g. RightHand). Empty = solver default.")]
    public List<string> hitLimbNames = new List<string>();
    [Tooltip("Use a tool for hit.")]
    public bool hitUseTool;
    [Tooltip("Tool to use when hitUseTool is true.")]
    public GameObject hitTool;
    [Tooltip("Optional hit animation trees.")]
    public List<AnimationBehaviorTreeNode> hitAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Weightlift")]
    [Tooltip("Weight/tool to pick up when category is Weightlift.")]
    public GameObject weightliftTool;
    [Tooltip("Muscle group name to activate (e.g. Biceps, Back).")]
    public string weightliftMuscleGroup = "";
    [Tooltip("Optional weightlift animation trees.")]
    public List<AnimationBehaviorTreeNode> weightliftAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Catch")]
    [Tooltip("Object to catch when category is Catch.")]
    public GameObject catchObject;
    [Tooltip("Limb bone names for catch (e.g. RightHand, LeftHand). Empty = solver default.")]
    public List<string> catchLimbNames = new List<string>();
    [Tooltip("Optional catch animation trees.")]
    public List<AnimationBehaviorTreeNode> catchAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Shoot")]
    [Tooltip("Shoot target when category is Shoot (overrides shootTargetPosition at runtime when set).")]
    public GameObject shootTarget;
    [Tooltip("World position used as shoot target when category is Shoot and shootTarget is null.")]
    public Vector3 shootTargetPosition;
    [Tooltip("Object that is shot (GameObject, Transform, or bone).")]
    public UnityEngine.Object shootLaunchedObject;
    [Tooltip("Hand(s) used for shoot.")]
    public ThrowHandMode shootHandMode = ThrowHandMode.Right;
    [Tooltip("Optional shoot animation trees.")]
    public List<AnimationBehaviorTreeNode> shootAnimationTrees = new List<AnimationBehaviorTreeNode>();

    [Header("Trained Sets")]
    [Tooltip("List of coefficient sets with metrics (overwrite replaces this; append adds to it)")]
    public PhysicsIKTrainedSet[] trainedSets = Array.Empty<PhysicsIKTrainedSet>();

    [Header("Range Diamond (optional)")]
    [Tooltip("Min/max per coefficient from successful runs for naturalized sampling")]
    public PhysicsIKTrainedSet rangeDiamondMin;
    public PhysicsIKTrainedSet rangeDiamondMax;

    public void OverwriteWith(PhysicsIKTrainedSet[] sets)
    {
        trainedSets = sets != null ? (PhysicsIKTrainedSet[])sets.Clone() : Array.Empty<PhysicsIKTrainedSet>();
    }

    public void Append(PhysicsIKTrainedSet[] sets)
    {
        if (sets == null || sets.Length == 0) return;
        int oldLen = trainedSets?.Length ?? 0;
        var combined = new PhysicsIKTrainedSet[oldLen + sets.Length];
        if (oldLen > 0)
            Array.Copy(trainedSets, combined, oldLen);
        Array.Copy(sets, 0, combined, oldLen, sets.Length);
        trainedSets = combined;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(creationTime))
            creationTime = DateTime.UtcNow.ToString("o");
    }
}
