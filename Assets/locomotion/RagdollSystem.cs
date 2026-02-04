using System.Collections.Generic;
using UnityEngine;
using Locomotion.Musculature;
using Locomotion.Rig;

/// <summary>
/// Main ragdoll physics coordinator. Manages ragdoll structure, muscle activations,
/// and animation blending (procedural + keyframe).
/// </summary>
public enum BodySide
{
    Left,
    Right
}

public enum HandType
{
    Left,
    Right
}

public class RagdollSystem : MonoBehaviour
{
    [Header("Ragdoll Structure")]
    [Tooltip("Root transform of the ragdoll hierarchy")]
    public Transform ragdollRoot;

    [Header("Animation Blending")]
    [Tooltip("How to blend procedural and keyframe animations")]
    public AnimationBlendMode animationBlendMode = AnimationBlendMode.FullRagdoll;

    [Header("Animation Tree")]
    [Tooltip("Animation tree (multiple roots). Ragdoll stores animations as children of animationContainer or by reference.")]
    public AnimationBehaviorTree animationTree;
    [Tooltip("Optional container under ragdoll for reparenting animation roots. If set, animation tree roots can be reparented here.")]
    public Transform animationContainer;
    [Tooltip("Max depth when querying animation nodes (0 = roots only).")]
    public int animationNodeQueryMaxDepth = 2;

    [Header("Muscle Groups")]
    [Tooltip("Organized muscle groups for coordinated activation")]
    public List<MuscleGroup> muscleGroups = new List<MuscleGroup>();

    [Header("Breakable Sections")]
    [Tooltip("Sections that can temporarily break out of animation")]
    public List<RagdollSection> breakableSections = new List<RagdollSection>();

    // Internal state
    private Dictionary<string, MuscleGroup> muscleGroupDict = new Dictionary<string, MuscleGroup>();
    private Dictionary<string, RagdollSection> sectionDict = new Dictionary<string, RagdollSection>();
    private RagdollState currentState;

    [Header("Auto-wired Anatomy (cached)")]
    public RagdollHand leftHandComponent;
    public RagdollHand rightHandComponent;
    public RagdollCollarbone leftCollarboneComponent;
    public RagdollCollarbone rightCollarboneComponent;
    public RagdollShoulder leftShoulderComponent;
    public RagdollShoulder rightShoulderComponent;
    public RagdollUpperarm leftUpperarmComponent;
    public RagdollUpperarm rightUpperarmComponent;
    public RagdollElbow leftElbowComponent;
    public RagdollElbow rightElbowComponent;
    public RagdollForearm leftForearmComponent;
    public RagdollForearm rightForearmComponent;
    public RagdollHead headComponent;
    public RagdollJaw jawComponent;
    public RagdollNeck neckComponent;
    public RagdollPelvis pelvisComponent;
    public RagdollTorso torsoComponent;
    public RagdollLeg leftLegComponent;
    public RagdollLeg rightLegComponent;
    public RagdollKnee leftKneeComponent;
    public RagdollKnee rightKneeComponent;
    public RagdollShin leftShinComponent;
    public RagdollShin rightShinComponent;
    public RagdollFoot leftFootComponent;
    public RagdollFoot rightFootComponent;

    // Legacy compatibility (was referenced but never defined in file).
    // Prefer using FindOrAddHand/Hands() in new code.
    [SerializeField] private Hand leftHand;
    [SerializeField] private Hand rightHand;

    private void Awake()
    {
        // Auto-find ragdoll root if not set
        if (ragdollRoot == null)
        {
            ragdollRoot = transform;
        }

        // Build muscle group dictionary
        foreach (var group in muscleGroups)
        {
            if (group != null && !string.IsNullOrEmpty(group.groupName))
            {
                muscleGroupDict[group.groupName] = group;
            }
        }

        // Build section dictionary
        foreach (var section in breakableSections)
        {
            if (section != null && !string.IsNullOrEmpty(section.sectionName))
            {
                sectionDict[section.sectionName] = section;
            }
        }

        // Initialize current state
        currentState = GetCurrentState();
    }

    private void Update()
    {
        // Update current state
        currentState = GetCurrentState();
    }

    /// <summary>
    /// Activate a muscle group by name with specified strength (0-1).
    /// </summary>
    public void ActivateMuscleGroup(string groupName, float activation)
    {
        if (muscleGroupDict.TryGetValue(groupName, out MuscleGroup group))
        {
            group.ActivateGroup(Mathf.Clamp01(activation));
        }
        else
        {
            Debug.LogWarning($"Muscle group '{groupName}' not found in RagdollSystem");
        }
    }

    /// <summary>
    /// Temporarily break a section out of animation for procedural control.
    /// </summary>
    public void BreakSection(RagdollSection section)
    {
        if (section != null)
        {
            section.BreakFromAnimation();
        }
    }

    /// <summary>
    /// Break a section by name out of animation.
    /// </summary>
    public void BreakSection(string sectionName)
    {
        if (sectionDict.TryGetValue(sectionName, out RagdollSection section))
        {
            BreakSection(section);
        }
        else
        {
            Debug.LogWarning($"Section '{sectionName}' not found in RagdollSystem");
        }
    }

    /// <summary>
    /// Restore a section back to animation control.
    /// </summary>
    public void RestoreSection(RagdollSection section)
    {
        if (section != null)
        {
            section.RestoreToAnimation();
        }
    }

    /// <summary>
    /// Restore a section by name back to animation control.
    /// </summary>
    public void RestoreSection(string sectionName)
    {
        if (sectionDict.TryGetValue(sectionName, out RagdollSection section))
        {
            RestoreSection(section);
        }
    }

    /// <summary>
    /// Merge keyframe animation with procedural animation using blend factor (0-1).
    /// 0 = full procedural, 1 = full keyframe.
    /// </summary>
    public void MergeAnimation(AnimationClip clip, float blend)
    {
        // TODO: Implement animation blending with Unity Animation system
        // This would involve blending Animation component with procedural muscle control
        Debug.Log($"Merging animation '{clip?.name}' with blend factor {blend}");
    }

    /// <summary>
    /// Get current ragdoll state for card matching and feasibility checking.
    /// </summary>
    public RagdollState GetCurrentState()
    {
        RagdollState state = new RagdollState();

        // Get root transform state
        if (ragdollRoot != null)
        {
            Rigidbody rootRb = ragdollRoot.GetComponent<Rigidbody>();
            if (rootRb != null)
            {
                state.rootPosition = ragdollRoot.position;
                state.rootRotation = ragdollRoot.rotation;
                state.rootVelocity = rootRb.linearVelocity;
                state.rootAngularVelocity = rootRb.angularVelocity;
            }
            else
            {
                state.rootPosition = ragdollRoot.position;
                state.rootRotation = ragdollRoot.rotation;
                state.rootVelocity = Vector3.zero;
                state.rootAngularVelocity = Vector3.zero;
            }
        }

        // Get joint states and muscle activations
        state.jointStates = new Dictionary<string, JointState>();
        state.muscleActivations = new Dictionary<string, float>();

        foreach (var group in muscleGroups)
        {
            if (group != null)
            {
                var activations = group.GetMuscleActivations();
                foreach (var kvp in activations)
                {
                    state.muscleActivations[kvp.Key] = kvp.Value;
                }

                // Get joint states from muscles
                var jointStates = group.GetJointStates();
                foreach (var kvp in jointStates)
                {
                    state.jointStates[kvp.Key] = kvp.Value;
                }
            }
        }

        // Get physics contacts
        state.contacts = GetPhysicsContacts();

        return state;
    }

    /// <summary>
    /// Get all current physics contacts for the ragdoll.
    /// </summary>
    private List<ContactPoint> GetPhysicsContacts()
    {
        List<ContactPoint> contacts = new List<ContactPoint>();

        // Collect contacts from all colliders
        Collider[] colliders = ragdollRoot.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            // Note: Unity's ContactPoint requires OnCollisionStay to populate
            // For now, return empty list - this would be populated by collision callbacks
        }

        return contacts;
    }

    /// <summary>
    /// Get a muscle group by name.
    /// </summary>
    public MuscleGroup GetMuscleGroup(string groupName)
    {
        muscleGroupDict.TryGetValue(groupName, out MuscleGroup group);
        return group;
    }

    /// <summary>
    /// Get a section by name.
    /// </summary>
    public RagdollSection GetSection(string sectionName)
    {
        sectionDict.TryGetValue(sectionName, out RagdollSection section);
        return section;
    }

    /// <summary>
    /// Get animation root nodes from the animation tree (for IK trainer and playback).
    /// Returns empty list if no animation tree or no roots.
    /// </summary>
    public List<AnimationBehaviorTreeNode> GetAnimationRoots()
    {
        var list = new List<AnimationBehaviorTreeNode>();
        if (animationTree == null) return list;
        var roots = animationTree.GetRootNodes();
        if (roots == null) return list;
        foreach (var r in roots)
        {
            if (r != null) list.Add(r);
        }
        return list;
    }

    /// <summary>
    /// Get animation nodes at a given depth (0 = roots only, 1 = roots + immediate children, etc.).
    /// Uses animationNodeQueryMaxDepth if depth is negative.
    /// </summary>
    public List<AnimationBehaviorTreeNode> GetAnimationNodesAtLevel(int depth)
    {
        var list = new List<AnimationBehaviorTreeNode>();
        if (animationTree == null) return list;
        int maxDepth = depth >= 0 ? depth : animationNodeQueryMaxDepth;
        var roots = animationTree.GetRootNodes();
        if (roots == null) return list;
        foreach (var root in roots)
        {
            if (root == null) continue;
            CollectAnimationNodesAtDepth(root, 0, maxDepth, list);
        }
        return list;
    }

    /// <summary>
    /// Get all animation nodes up to maxDepth (uses animationNodeQueryMaxDepth if maxDepth &lt; 0).
    /// </summary>
    public List<AnimationBehaviorTreeNode> GetAllAnimationNodes(int maxDepth = -1)
    {
        return GetAnimationNodesAtLevel(maxDepth);
    }

    private static void CollectAnimationNodesAtDepth(AnimationBehaviorTreeNode node, int currentDepth, int maxDepth, List<AnimationBehaviorTreeNode> list)
    {
        if (node == null || currentDepth > maxDepth) return;
        list.Add(node);
        if (currentDepth >= maxDepth) return;
        if (node.children == null) return;
        foreach (var child in node.children)
        {
            var animChild = child as AnimationBehaviorTreeNode;
            if (animChild != null)
                CollectAnimationNodesAtDepth(animChild, currentDepth + 1, maxDepth, list);
        }
    }

    public Hand GetHand(HandType handType)
    {
        switch (handType) {
            case HandType.Right:
                return rightHand;
            case HandType.Left:
                return leftHand;
            default:
                return null;
        }
    }

    /// <summary>
    /// Resolve a bone transform by name (e.g. RightHand, LeftHand, Head). Used by CarriedObjectAttachment and hit/place logic.
    /// Tries BoneMap trait id (Generic:HandRight etc.), then role+side, then name heuristics.
    /// </summary>
    public Transform GetBoneTransform(string boneName)
    {
        if (string.IsNullOrWhiteSpace(boneName)) return null;
        var boneMap = FindBoneMap();
        if (boneMap != null && boneMap.TryGet("Generic:" + boneName, out var t))
            return t;
        if (boneMap != null && boneMap.TryGet(boneName, out t))
            return t;
        var n = boneName.Trim();
        if (n.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase))
            return ResolveBone("Hand", BodySide.Right);
        if (n.Equals("LeftHand", System.StringComparison.OrdinalIgnoreCase))
            return ResolveBone("Hand", BodySide.Left);
        if (n.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
            return ResolveBone("Head", null);
        if (n.Equals("Pelvis", System.StringComparison.OrdinalIgnoreCase))
            return ResolveBone("Pelvis", null);
        return ResolveViaNameHeuristics(n, null);
    }

    public RagdollHand FindOrAddHand(BodySide side)
    {
        var t = ResolveBone("Hand", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollHand>();
        if (c == null) c = t.gameObject.AddComponent<RagdollHand>();
        c.side = side;

        if (side == BodySide.Left) leftHandComponent = c;
        else rightHandComponent = c;

        return c;
    }

    public List<RagdollHand> FindOrAddHands()
    {
        var list = new List<RagdollHand>(2);
        var l = FindOrAddHand(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddHand(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    public List<RagdollHand> Hands()
    {
        var list = new List<RagdollHand>(2);
        if (leftHandComponent != null) list.Add(leftHandComponent);
        if (rightHandComponent != null) list.Add(rightHandComponent);
        return list;
    }

    public RagdollElbow FindOrAddElbow(BodySide side)
    {
        var t = ResolveBone("Elbow", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollElbow>();
        if (c == null) c = t.gameObject.AddComponent<RagdollElbow>();
        c.side = side;

        if (side == BodySide.Left) leftElbowComponent = c;
        else rightElbowComponent = c;

        return c;
    }

    public List<RagdollElbow> FindOrAddElbows()
    {
        var list = new List<RagdollElbow>(2);
        var l = FindOrAddElbow(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddElbow(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    public RagdollForearm FindOrAddForearm(BodySide side)
    {
        var t = ResolveBone("Forearm", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollForearm>();
        if (c == null) c = t.gameObject.AddComponent<RagdollForearm>();
        c.side = side;

        if (side == BodySide.Left) leftForearmComponent = c;
        else rightForearmComponent = c;

        return c;
    }

    public RagdollCollarbone FindOrAddCollarbone(BodySide side)
    {
        var t = ResolveBone("Collarbone", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollCollarbone>();
        if (c == null) c = t.gameObject.AddComponent<RagdollCollarbone>();
        c.side = side;

        if (side == BodySide.Left) leftCollarboneComponent = c;
        else rightCollarboneComponent = c;

        return c;
    }

    public List<RagdollCollarbone> FindOrAddCollarbones()
    {
        var list = new List<RagdollCollarbone>(2);
        var l = FindOrAddCollarbone(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddCollarbone(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    public RagdollShoulder FindOrAddShoulder(BodySide side)
    {
        var t = ResolveBone("Shoulder", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollShoulder>();
        if (c == null) c = t.gameObject.AddComponent<RagdollShoulder>();
        c.side = side;

        if (side == BodySide.Left) leftShoulderComponent = c;
        else rightShoulderComponent = c;

        return c;
    }

    public List<RagdollShoulder> FindOrAddShoulders()
    {
        var list = new List<RagdollShoulder>(2);
        var l = FindOrAddShoulder(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddShoulder(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    public RagdollUpperarm FindOrAddUpperarm(BodySide side)
    {
        var t = ResolveBone("Upperarm", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollUpperarm>();
        if (c == null) c = t.gameObject.AddComponent<RagdollUpperarm>();
        c.side = side;

        if (side == BodySide.Left) leftUpperarmComponent = c;
        else rightUpperarmComponent = c;

        return c;
    }

    public RagdollHead FindOrAddHead()
    {
        var t = ResolveBone("Head", null);
        if (t == null) return null;

        var c = t.GetComponent<RagdollHead>();
        if (c == null) c = t.gameObject.AddComponent<RagdollHead>();
        headComponent = c;
        return c;
    }

    public RagdollJaw FindOrAddJaw()
    {
        // Jaw is typically a child of the head
        if (headComponent != null)
        {
            var c = headComponent.GetComponentInChildren<RagdollJaw>();
            if (c != null)
            {
                jawComponent = c;
                return c;
            }
        }

        // Try to resolve bone directly
        var t = ResolveBone("Jaw", null);
        if (t == null)
        {
            // Fallback: try to find in head's children
            if (headComponent != null)
            {
                t = headComponent.transform.Find("Jaw");
            }
        }

        if (t == null) return null;

        var jaw = t.GetComponent<RagdollJaw>();
        if (jaw == null) jaw = t.gameObject.AddComponent<RagdollJaw>();
        jawComponent = jaw;
        return jaw;
    }

    public RagdollNeck FindOrAddNeck()
    {
        var t = ResolveBone("Neck", null);
        if (t == null) return null;

        var c = t.GetComponent<RagdollNeck>();
        if (c == null) c = t.gameObject.AddComponent<RagdollNeck>();
        neckComponent = c;
        return c;
    }

    public RagdollPelvis FindOrAddPelvis()
    {
        var t = ResolveBone("Pelvis", null);
        if (t == null) return null;

        var c = t.GetComponent<RagdollPelvis>();
        if (c == null) c = t.gameObject.AddComponent<RagdollPelvis>();
        pelvisComponent = c;
        return c;
    }

    public RagdollTorso FindOrAddTorso()
    {
        var t = ResolveBone("Torso", null);
        if (t == null) return null;

        var c = t.GetComponent<RagdollTorso>();
        if (c == null) c = t.gameObject.AddComponent<RagdollTorso>();
        torsoComponent = c;
        return c;
    }

    public RagdollLeg FindOrAddLeg(BodySide side)
    {
        var t = ResolveBone("Leg", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollLeg>();
        if (c == null) c = t.gameObject.AddComponent<RagdollLeg>();
        c.side = side;

        if (side == BodySide.Left) leftLegComponent = c;
        else rightLegComponent = c;

        return c;
    }

    public RagdollKnee FindOrAddKnee(BodySide side)
    {
        var t = ResolveBone("Knee", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollKnee>();
        if (c == null) c = t.gameObject.AddComponent<RagdollKnee>();
        c.side = side;

        if (side == BodySide.Left) leftKneeComponent = c;
        else rightKneeComponent = c;

        return c;
    }

    public RagdollShin FindOrAddShin(BodySide side)
    {
        var t = ResolveBone("Shin", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollShin>();
        if (c == null) c = t.gameObject.AddComponent<RagdollShin>();
        c.side = side;

        if (side == BodySide.Left) leftShinComponent = c;
        else rightShinComponent = c;

        return c;
    }

    public List<RagdollShin> FindOrAddShins()
    {
        var list = new List<RagdollShin>(2);
        var l = FindOrAddShin(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddShin(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    /// <summary>
    /// Create shin GameObject if missing, completing the tree map between knee and foot.
    /// </summary>
    public RagdollShin CreateShinIfMissing(BodySide side)
    {
        // Check if shin already exists
        var existing = side == BodySide.Left ? leftShinComponent : rightShinComponent;
        if (existing != null) return existing;

        // Get knee and foot references
        var knee = side == BodySide.Left ? leftKneeComponent : rightKneeComponent;
        var foot = side == BodySide.Left ? leftFootComponent : rightFootComponent;

        if (knee == null || foot == null)
        {
            Debug.LogWarning($"[RagdollSystem] Cannot create shin for {side} leg: missing knee or foot component");
            return null;
        }

        // Create shin GameObject halfway between knee and foot
        Vector3 kneePos = knee.PrimaryBoneTransform.position;
        Vector3 footPos = foot.PrimaryBoneTransform.position;
        Vector3 shinPos = (kneePos + footPos) * 0.5f;

        // Determine parent - prefer knee's parent
        Transform parentTransform = knee.transform.parent ?? foot.transform.parent;
        if (parentTransform == null && ragdollRoot != null)
            parentTransform = ragdollRoot;

        GameObject shinObj = new GameObject($"{side}_Shin");
        shinObj.transform.position = shinPos;
        shinObj.transform.SetParent(parentTransform, worldPositionStays: true);

        // Add RagdollShin component
        RagdollShin shinComponent = shinObj.AddComponent<RagdollShin>();
        shinComponent.side = side;
        shinComponent.knee = knee;
        shinComponent.foot = foot;

        if (side == BodySide.Left) leftShinComponent = shinComponent;
        else rightShinComponent = shinComponent;

        Debug.Log($"[RagdollSystem] Created shin GameObject at position {shinPos} for {side} leg");
        return shinComponent;
    }

    public RagdollFoot FindOrAddFoot(BodySide side)
    {
        var t = ResolveBone("Foot", side);
        if (t == null) return null;

        var c = t.GetComponent<RagdollFoot>();
        if (c == null) c = t.gameObject.AddComponent<RagdollFoot>();
        c.side = side;

        if (side == BodySide.Left) leftFootComponent = c;
        else rightFootComponent = c;

        return c;
    }

    public List<RagdollFoot> FindOrAddFeet()
    {
        var list = new List<RagdollFoot>(2);
        var l = FindOrAddFoot(BodySide.Left);
        if (l != null) list.Add(l);
        var r = FindOrAddFoot(BodySide.Right);
        if (r != null) list.Add(r);
        return list;
    }

    public List<RagdollFoot> Feet()
    {
        var list = new List<RagdollFoot>(2);
        if (leftFootComponent != null) list.Add(leftFootComponent);
        if (rightFootComponent != null) list.Add(rightFootComponent);
        return list;
    }

    [ContextMenu("RagdollSystem/Validate bone components")]
    private void ValidateBoneComponents()
    {
        if (ragdollRoot == null) ragdollRoot = transform;

        FindOrAddPelvis();
        FindOrAddTorso();
        FindOrAddNeck();
        FindOrAddHead();
        FindOrAddJaw();

        FindOrAddCollarbone(BodySide.Left);
        FindOrAddCollarbone(BodySide.Right);
        FindOrAddShoulder(BodySide.Left);
        FindOrAddShoulder(BodySide.Right);
        FindOrAddUpperarm(BodySide.Left);
        FindOrAddUpperarm(BodySide.Right);
        FindOrAddElbow(BodySide.Left);
        FindOrAddElbow(BodySide.Right);
        FindOrAddForearm(BodySide.Left);
        FindOrAddForearm(BodySide.Right);
        FindOrAddHands();

        // Auto-link arm components
        AutoLinkCollarbones();
        AutoLinkShoulders();
        AutoLinkForearms();

        FindOrAddLeg(BodySide.Left);
        FindOrAddLeg(BodySide.Right);
        FindOrAddKnee(BodySide.Left);
        FindOrAddKnee(BodySide.Right);
        FindOrAddShins();
        FindOrAddFeet();

        // Auto-link shin components
        AutoLinkShins();

        Debug.Log(
            "[RagdollSystem] Validation complete:\n" +
            $"Pelvis: {pelvisComponent?.name ?? "(null)"}\n" +
            $"Torso: {torsoComponent?.name ?? "(null)"}\n" +
            $"Neck: {neckComponent?.name ?? "(null)"}\n" +
            $"Head: {headComponent?.name ?? "(null)"}\n" +
            $"Collarbone L/R: {leftCollarboneComponent?.name ?? "(null)"} / {rightCollarboneComponent?.name ?? "(null)"}\n" +
            $"Shoulder L/R: {leftShoulderComponent?.name ?? "(null)"} / {rightShoulderComponent?.name ?? "(null)"}\n" +
            $"Upperarm L/R: {leftUpperarmComponent?.name ?? "(null)"} / {rightUpperarmComponent?.name ?? "(null)"}\n" +
            $"Elbow L/R: {leftElbowComponent?.name ?? "(null)"} / {rightElbowComponent?.name ?? "(null)"}\n" +
            $"Forearm L/R: {leftForearmComponent?.name ?? "(null)"} / {rightForearmComponent?.name ?? "(null)"}\n" +
            $"Hand L/R: {leftHandComponent?.name ?? "(null)"} / {rightHandComponent?.name ?? "(null)"}\n" +
            $"Leg L/R: {leftLegComponent?.name ?? "(null)"} / {rightLegComponent?.name ?? "(null)"}\n" +
            $"Knee L/R: {leftKneeComponent?.name ?? "(null)"} / {rightKneeComponent?.name ?? "(null)"}\n" +
            $"Shin L/R: {leftShinComponent?.name ?? "(null)"} / {rightShinComponent?.name ?? "(null)"}\n" +
            $"Foot L/R: {leftFootComponent?.name ?? "(null)"} / {rightFootComponent?.name ?? "(null)"}",
            this);
    }

    /// <summary>
    /// Auto-link shin components to their knee and foot components.
    /// </summary>
    private void AutoLinkShins()
    {
        if (leftShinComponent != null)
        {
            if (leftShinComponent.knee == null && leftKneeComponent != null)
                leftShinComponent.knee = leftKneeComponent;
            if (leftShinComponent.foot == null && leftFootComponent != null)
                leftShinComponent.foot = leftFootComponent;
        }

        if (rightShinComponent != null)
        {
            if (rightShinComponent.knee == null && rightKneeComponent != null)
                rightShinComponent.knee = rightKneeComponent;
            if (rightShinComponent.foot == null && rightFootComponent != null)
                rightShinComponent.foot = rightFootComponent;
        }

        // Also link knee components to their shin components
        if (leftKneeComponent != null)
        {
            if (leftKneeComponent.shin == null && leftShinComponent != null)
                leftKneeComponent.shin = leftShinComponent;
            if (leftKneeComponent.foot == null && leftFootComponent != null)
                leftKneeComponent.foot = leftFootComponent;
        }

        if (rightKneeComponent != null)
        {
            if (rightKneeComponent.shin == null && rightShinComponent != null)
                rightKneeComponent.shin = rightShinComponent;
            if (rightKneeComponent.foot == null && rightFootComponent != null)
                rightKneeComponent.foot = rightFootComponent;
        }
    }

    /// <summary>
    /// Auto-link collarbone components to their neck and shoulder components.
    /// </summary>
    private void AutoLinkCollarbones()
    {
        if (leftCollarboneComponent != null)
        {
            if (leftCollarboneComponent.neck == null && neckComponent != null)
                leftCollarboneComponent.neck = neckComponent;
            if (leftCollarboneComponent.shoulder == null && leftShoulderComponent != null)
                leftCollarboneComponent.shoulder = leftShoulderComponent;
        }

        if (rightCollarboneComponent != null)
        {
            if (rightCollarboneComponent.neck == null && neckComponent != null)
                rightCollarboneComponent.neck = neckComponent;
            if (rightCollarboneComponent.shoulder == null && rightShoulderComponent != null)
                rightCollarboneComponent.shoulder = rightShoulderComponent;
        }

        // Link shoulders to collarbones
        if (leftShoulderComponent != null)
        {
            if (leftShoulderComponent.collarbone == null && leftCollarboneComponent != null)
                leftShoulderComponent.collarbone = leftCollarboneComponent;
        }

        if (rightShoulderComponent != null)
        {
            if (rightShoulderComponent.collarbone == null && rightCollarboneComponent != null)
                rightShoulderComponent.collarbone = rightCollarboneComponent;
        }

        // Link neck to collarbones
        if (neckComponent != null)
        {
            if (neckComponent.leftCollarbone == null && leftCollarboneComponent != null)
                neckComponent.leftCollarbone = leftCollarboneComponent;
            if (neckComponent.rightCollarbone == null && rightCollarboneComponent != null)
                neckComponent.rightCollarbone = rightCollarboneComponent;
        }
    }

    /// <summary>
    /// Auto-link shoulder components to their upper arm components.
    /// </summary>
    private void AutoLinkShoulders()
    {
        if (leftShoulderComponent != null)
        {
            if (leftShoulderComponent.upperarm == null && leftUpperarmComponent != null)
                leftShoulderComponent.upperarm = leftUpperarmComponent;
            if (leftShoulderComponent.elbow == null && leftElbowComponent != null)
                leftShoulderComponent.elbow = leftElbowComponent;
        }

        if (rightShoulderComponent != null)
        {
            if (rightShoulderComponent.upperarm == null && rightUpperarmComponent != null)
                rightShoulderComponent.upperarm = rightUpperarmComponent;
            if (rightShoulderComponent.elbow == null && rightElbowComponent != null)
                rightShoulderComponent.elbow = rightElbowComponent;
        }

        // Also link upper arm components to their shoulder components
        if (leftUpperarmComponent != null)
        {
            if (leftUpperarmComponent.shoulder == null && leftShoulderComponent != null)
                leftUpperarmComponent.shoulder = leftShoulderComponent;
            if (leftUpperarmComponent.elbow == null && leftElbowComponent != null)
                leftUpperarmComponent.elbow = leftElbowComponent;
        }

        if (rightUpperarmComponent != null)
        {
            if (rightUpperarmComponent.shoulder == null && rightShoulderComponent != null)
                rightUpperarmComponent.shoulder = rightShoulderComponent;
            if (rightUpperarmComponent.elbow == null && rightElbowComponent != null)
                rightUpperarmComponent.elbow = rightElbowComponent;
        }
    }

    /// <summary>
    /// Auto-link forearm components to their elbow and hand components.
    /// </summary>
    private void AutoLinkForearms()
    {
        if (leftForearmComponent != null)
        {
            if (leftForearmComponent.elbow == null && leftElbowComponent != null)
                leftForearmComponent.elbow = leftElbowComponent;
            if (leftForearmComponent.hand == null && leftHandComponent != null)
                leftForearmComponent.hand = leftHandComponent;
        }

        if (rightForearmComponent != null)
        {
            if (rightForearmComponent.elbow == null && rightElbowComponent != null)
                rightForearmComponent.elbow = rightElbowComponent;
            if (rightForearmComponent.hand == null && rightHandComponent != null)
                rightForearmComponent.hand = rightHandComponent;
        }

        // Also link elbow components to their forearm components
        if (leftElbowComponent != null)
        {
            if (leftElbowComponent.forearm == null && leftForearmComponent != null)
                leftElbowComponent.forearm = leftForearmComponent;
            if (leftElbowComponent.hand == null && leftHandComponent != null)
                leftElbowComponent.hand = leftHandComponent;
        }

        if (rightElbowComponent != null)
        {
            if (rightElbowComponent.forearm == null && rightForearmComponent != null)
                rightElbowComponent.forearm = rightForearmComponent;
            if (rightElbowComponent.hand == null && rightHandComponent != null)
                rightElbowComponent.hand = rightHandComponent;
        }
    }

    private BoneMap FindBoneMap()
    {
        if (ragdollRoot == null) return null;
        return ragdollRoot.GetComponentInChildren<BoneMap>();
    }

    private Animator FindAnimator()
    {
        if (ragdollRoot == null) return null;
        return ragdollRoot.GetComponentInChildren<Animator>();
    }

    private static string SideToken(BodySide side) => side == BodySide.Left ? "Left" : "Right";

    private Transform ResolveViaBoneMap(BoneMap boneMap, string humanBoneId, string genericId)
    {
        if (boneMap == null) return null;

        if (!string.IsNullOrEmpty(humanBoneId) && boneMap.TryGet(humanBoneId, out var tHuman))
            return tHuman;

        if (!string.IsNullOrEmpty(genericId) && boneMap.TryGet(genericId, out var tGeneric))
            return tGeneric;

        return null;
    }

    private Transform ResolveViaAnimator(Animator animator, HumanBodyBones bone)
    {
        if (animator == null) return null;
        if (!animator.isHuman) return null;
        return animator.GetBoneTransform(bone);
    }

    private static bool HasAny(string hay, params string[] tokens)
    {
        if (string.IsNullOrEmpty(hay)) return false;
        hay = hay.ToLowerInvariant();
        for (int i = 0; i < tokens.Length; i++)
        {
            if (hay.Contains(tokens[i])) return true;
        }
        return false;
    }

    private Transform ResolveViaNameHeuristics(string role, BodySide? side)
    {
        if (ragdollRoot == null) return null;

        string sideName = side.HasValue ? side.Value.ToString().ToLowerInvariant() : null;
        string sideShort = null;
        if (side.HasValue)
            sideShort = side.Value == BodySide.Left ? "l" : "r";

        string[] roleTokens;
        switch (role)
        {
            case "Hand":
                roleTokens = new[] { "hand", "wrist" };
                break;
            case "Elbow":
                roleTokens = new[] { "elbow" };
                break;
            case "Collarbone":
                roleTokens = new[] { "collarbone", "clavicle" };
                break;
            case "Shoulder":
                roleTokens = new[] { "shoulder", "clavicle", "scapula" };
                break;
            case "Forearm":
                roleTokens = new[] { "forearm", "lowerarm", "lower_arm", "radius", "ulna" };
                break;
            case "Upperarm":
                roleTokens = new[] { "upperarm", "upper_arm", "arm" };
                break;
            case "Leg":
                roleTokens = new[] { "thigh", "upperleg", "upper_leg", "upleg" };
                break;
            case "Knee":
                roleTokens = new[] { "knee", "patella" };
                break;
            case "Shin":
            case "Foreleg":
                roleTokens = new[] { "shin", "foreleg", "calf", "lowerleg", "lower_leg", "tibia", "fibula" };
                break;
            case "Foot":
                roleTokens = new[] { "foot", "ankle" };
                break;
            case "Pelvis":
                roleTokens = new[] { "hips", "pelvis" };
                break;
            case "Torso":
                roleTokens = new[] { "spine", "chest", "torso" };
                break;
            case "Neck":
                roleTokens = new[] { "neck" };
                break;
            case "Head":
                roleTokens = new[] { "head" };
                break;
            case "Jaw":
                roleTokens = new[] { "jaw", "mandible" };
                break;
            default:
                roleTokens = new[] { role.ToLowerInvariant() };
                break;
        }

        Transform best = null;
        int bestScore = int.MinValue;

        var all = ragdollRoot.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null) continue;

            string n = t.name ?? "";
            string nl = n.ToLowerInvariant();

            int score = 0;

            // role tokens
            bool roleMatch = false;
            for (int rt = 0; rt < roleTokens.Length; rt++)
            {
                if (nl.Contains(roleTokens[rt]))
                {
                    score += 25;
                    roleMatch = true;
                }
            }
            if (!roleMatch) continue;

            // side tokens
            if (side.HasValue)
            {
                bool sideMatch = false;
                if (!string.IsNullOrEmpty(sideName) && nl.Contains(sideName)) sideMatch = true;
                if (!string.IsNullOrEmpty(sideShort) && (nl.StartsWith(sideShort + "_") || nl.EndsWith("_" + sideShort))) sideMatch = true;
                if (HasAny(nl, side.Value == BodySide.Left ? "l_" : "r_", side.Value == BodySide.Left ? "_l" : "_r")) sideMatch = true;

                score += sideMatch ? 20 : -50;
            }

            // depth preference: extremities deeper
            int depth = 0;
            for (var p = t; p != null; p = p.parent) depth++;
            score += Mathf.Clamp(depth, 0, 30);

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private Transform ResolveBone(string role, BodySide? side)
    {
        var boneMap = FindBoneMap();
        var animator = FindAnimator();

        // 1) BoneMap (Human:<HumanBodyBones> then Generic:*)
        if (role == "Hand" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}Hand", $"Generic:Hand{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Elbow" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}LowerArm", $"Generic:Elbow{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Forearm" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}LowerArm", $"Generic:Forearm{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Collarbone" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}Shoulder", $"Generic:Collarbone{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Shoulder" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}Shoulder", $"Generic:Shoulder{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Upperarm" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}UpperArm", $"Generic:Upperarm{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Leg" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}UpperLeg", $"Generic:Leg{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Knee" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}LowerLeg", $"Generic:Knee{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if ((role == "Shin" || role == "Foreleg") && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}LowerLeg", $"Generic:Shin{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Foot" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}Foot", $"Generic:Foot{SideToken(side.Value)}");
            if (t != null) return t;
        }
        if (role == "Pelvis")
        {
            var t = ResolveViaBoneMap(boneMap, "Human:Hips", "Generic:Pelvis");
            if (t != null) return t;
        }
        if (role == "Torso")
        {
            var t = ResolveViaBoneMap(boneMap, "Human:Spine", "Generic:Torso");
            if (t != null) return t;
            t = ResolveViaBoneMap(boneMap, "Human:Chest", "Generic:Chest");
            if (t != null) return t;
        }
        if (role == "Neck")
        {
            var t = ResolveViaBoneMap(boneMap, "Human:Neck", "Generic:Neck");
            if (t != null) return t;
        }
        if (role == "Head")
        {
            var t = ResolveViaBoneMap(boneMap, "Human:Head", "Generic:Head");
            if (t != null) return t;
        }
        if (role == "Jaw")
        {
            var t = ResolveViaBoneMap(boneMap, "Human:Jaw", "Generic:Jaw");
            if (t != null) return t;
        }

        // 2) Animator humanoid fallback
        if (role == "Hand" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand)
                   ?? ResolveViaNameHeuristics(role, side);
        if (role == "Forearm" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm)
                   ?? ResolveViaNameHeuristics(role, side);
        if (role == "Upperarm" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm)
                   ?? ResolveViaNameHeuristics(role, side);
        if (role == "Leg" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg)
                   ?? ResolveViaNameHeuristics(role, side);
        if (role == "Knee" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg)
                   ?? ResolveViaNameHeuristics(role, side);
        if (role == "Foot" && side.HasValue)
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot)
                   ?? ResolveViaNameHeuristics(role, side);

        if (role == "Pelvis")
            return ResolveViaAnimator(animator, HumanBodyBones.Hips) ?? ResolveViaNameHeuristics(role, null);
        if (role == "Torso")
            return ResolveViaAnimator(animator, HumanBodyBones.Spine) ?? ResolveViaAnimator(animator, HumanBodyBones.Chest) ?? ResolveViaNameHeuristics(role, null);
        if (role == "Neck")
            return ResolveViaAnimator(animator, HumanBodyBones.Neck) ?? ResolveViaNameHeuristics(role, null);
        if (role == "Head")
            return ResolveViaAnimator(animator, HumanBodyBones.Head) ?? ResolveViaNameHeuristics(role, null);
        if (role == "Jaw")
            return ResolveViaNameHeuristics(role, null); // No HumanBodyBones.Jaw, use name heuristics
        if (role == "Collarbone" && side.HasValue)
        {
            return ResolveViaAnimator(animator, side.Value == BodySide.Left ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder)
                ?? ResolveViaNameHeuristics(role, side);
        }

        // 3) Name heuristics fallback
        return ResolveViaNameHeuristics(role, side);
    }
}

/// <summary>
/// Animation blending mode for procedural vs keyframe animation.
/// </summary>
public enum AnimationBlendMode
{
    FullRagdoll,      // Fully procedural, no keyframe animation
    PartialRagdoll,   // Mix of procedural and keyframe
    KeyframeOnly      // Fully keyframe, minimal procedural
}

/// <summary>
/// Represents a ragdoll section that can break out of animation.
/// </summary>
[System.Serializable]
public class RagdollSection
{
    public string sectionName;
    public Transform sectionRoot;
    public bool isBroken = false;
    public bool wasAnimated = true;

    public void BreakFromAnimation()
    {
        isBroken = true;
        // TODO: Disable animation control for this section
        // Would involve disabling Animator components or marking joints as manually controlled
    }

    public void RestoreToAnimation()
    {
        isBroken = false;
        // TODO: Re-enable animation control for this section
    }
}
