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
    public RagdollForearm leftForearmComponent;
    public RagdollForearm rightForearmComponent;
    public RagdollUpperarm leftUpperarmComponent;
    public RagdollUpperarm rightUpperarmComponent;
    public RagdollHead headComponent;
    public RagdollNeck neckComponent;
    public RagdollPelvis pelvisComponent;
    public RagdollTorso torsoComponent;
    public RagdollLeg leftLegComponent;
    public RagdollLeg rightLegComponent;
    public RagdollKnee leftKneeComponent;
    public RagdollKnee rightKneeComponent;
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
                state.rootVelocity = rootRb.velocity;
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

        FindOrAddUpperarm(BodySide.Left);
        FindOrAddUpperarm(BodySide.Right);
        FindOrAddForearm(BodySide.Left);
        FindOrAddForearm(BodySide.Right);
        FindOrAddHands();

        FindOrAddLeg(BodySide.Left);
        FindOrAddLeg(BodySide.Right);
        FindOrAddKnee(BodySide.Left);
        FindOrAddKnee(BodySide.Right);
        FindOrAddFeet();

        Debug.Log(
            "[RagdollSystem] Validation complete:\n" +
            $"Pelvis: {pelvisComponent?.name ?? "(null)"}\n" +
            $"Torso: {torsoComponent?.name ?? "(null)"}\n" +
            $"Neck: {neckComponent?.name ?? "(null)"}\n" +
            $"Head: {headComponent?.name ?? "(null)"}\n" +
            $"Upperarm L/R: {leftUpperarmComponent?.name ?? "(null)"} / {rightUpperarmComponent?.name ?? "(null)"}\n" +
            $"Forearm L/R: {leftForearmComponent?.name ?? "(null)"} / {rightForearmComponent?.name ?? "(null)"}\n" +
            $"Hand L/R: {leftHandComponent?.name ?? "(null)"} / {rightHandComponent?.name ?? "(null)"}\n" +
            $"Leg L/R: {leftLegComponent?.name ?? "(null)"} / {rightLegComponent?.name ?? "(null)"}\n" +
            $"Knee L/R: {leftKneeComponent?.name ?? "(null)"} / {rightKneeComponent?.name ?? "(null)"}\n" +
            $"Foot L/R: {leftFootComponent?.name ?? "(null)"} / {rightFootComponent?.name ?? "(null)"}",
            this);
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
                roleTokens = new[] { "calf", "lowerleg", "lower_leg", "leg" };
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
        if (role == "Forearm" && side.HasValue)
        {
            var t = ResolveViaBoneMap(boneMap, $"Human:{SideToken(side.Value)}LowerArm", $"Generic:Forearm{SideToken(side.Value)}");
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
