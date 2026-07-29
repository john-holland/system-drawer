using UnityEngine;

/// <summary>
/// Writes body capsule slots 0–5 from ragdoll / humanoid bones.
/// </summary>
[AddComponentMenu("Locomotion/Hair/Body Capsule Binder")]
public sealed class HairBodyCapsuleBinder : MonoBehaviour
{
    [HairBodyCapsuleOverrideButtons]
    [SerializeField]
    [Tooltip("Inspector buttons: auto-fill or clear optional bone overrides.")]
    bool inspectorOverrideButtons;

    public RagdollSystem ragdoll;
    public Animator animator;
    public HairPlumeConfig config;
    public Transform scalpRoot;

    [Header("Optional overrides")]
    public Transform head;
    public Transform chest;
    public Transform leftShoulder;
    public Transform rightShoulder;
    public Transform leftUpperArm;
    public Transform leftHand;
    public Transform rightUpperArm;
    public Transform rightHand;
    public Transform leftKnee;
    public Transform rightKnee;

    public void Bind(HairCapsuleBuffer buffer)
    {
        if (buffer == null) return;
        var cfg = config;
        float headR = cfg != null ? cfg.headCapsuleRadius : 0.12f;
        float chestR = cfg != null ? cfg.chestCapsuleRadius : 0.18f;
        float armR = cfg != null ? cfg.armCapsuleRadius : 0.05f;
        float kneeR = cfg != null ? cfg.kneeCapsuleRadius : 0.07f;

        Transform h = Resolve(head, HumanBodyBones.Head, "Head", "Human:Head");
        Transform torso = Resolve(chest, HumanBodyBones.Chest, "Torso", "Human:Chest", "Human:Spine");
        Transform lShoulder = Resolve(leftShoulder, HumanBodyBones.LeftShoulder, "LeftShoulder", "Human:LeftShoulder");
        Transform rShoulder = Resolve(rightShoulder, HumanBodyBones.RightShoulder, "RightShoulder", "Human:RightShoulder");
        Transform lUpper = Resolve(leftUpperArm, HumanBodyBones.LeftUpperArm, "LeftUpperArm", "Human:LeftUpperArm");
        Transform lHand = Resolve(leftHand, HumanBodyBones.LeftHand, "LeftHand", "Human:LeftHand");
        Transform rUpper = Resolve(rightUpperArm, HumanBodyBones.RightUpperArm, "RightUpperArm", "Human:RightUpperArm");
        Transform rHand = Resolve(rightHand, HumanBodyBones.RightHand, "RightHand", "Human:RightHand");
        Transform lKnee = Resolve(leftKnee, HumanBodyBones.LeftLowerLeg, "LeftKnee", "Human:LeftLowerLeg");
        Transform rKnee = Resolve(rightKnee, HumanBodyBones.RightLowerLeg, "RightKnee", "Human:RightLowerLeg");

        if (h != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.Head, h.position, headR);
        else if (scalpRoot != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.Head, scalpRoot.position, headR);

        if (torso != null)
        {
            Vector3 c = torso.position;
            if (lShoulder != null && rShoulder != null)
                c = (torso.position + lShoulder.position + rShoulder.position) / 3f;
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.ChestShoulders, c, chestR);
        }

        if (lUpper != null && lHand != null)
            buffer.SetCapsuleFromSegment((int)HairCapsuleBuffer.BodySlot.LeftArm, lUpper.position, lHand.position, armR);
        else if (lUpper != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.LeftArm, lUpper.position, armR);

        if (rUpper != null && rHand != null)
            buffer.SetCapsuleFromSegment((int)HairCapsuleBuffer.BodySlot.RightArm, rUpper.position, rHand.position, armR);
        else if (rUpper != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.RightArm, rUpper.position, armR);

        if (lKnee != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.LeftKnee, lKnee.position, kneeR);
        if (rKnee != null)
            buffer.SetSlot(HairCapsuleBuffer.BodySlot.RightKnee, rKnee.position, kneeR);
    }

    /// <summary>
    /// Resolve ragdoll / humanoid bones and fill all optional override fields (and ragdoll/animator if missing).
    /// </summary>
    [ContextMenu("Auto Set Optional Overrides")]
    public int AutoSetOptionalOverrides()
    {
        if (ragdoll == null)
            ragdoll = GetComponentInParent<RagdollSystem>() ?? GetComponentInChildren<RagdollSystem>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null && ragdoll != null)
                animator = ragdoll.GetComponentInChildren<Animator>();
        }
        if (scalpRoot == null)
        {
            var driver = GetComponentInParent<HairPlumePhysicsDriver>() ?? GetComponent<HairPlumePhysicsDriver>();
            if (driver != null && driver.scalpRoot != null)
                scalpRoot = driver.scalpRoot;
            else
                scalpRoot = Resolve(null, HumanBodyBones.Head, "Head", "Human:Head");
        }

        int filled = 0;
        filled += Assign(ref head, HumanBodyBones.Head, "Head", "Human:Head");
        filled += Assign(ref chest, HumanBodyBones.Chest, "Torso", "Human:Chest", "Human:Spine");
        filled += Assign(ref leftShoulder, HumanBodyBones.LeftShoulder, "LeftShoulder", "Human:LeftShoulder");
        filled += Assign(ref rightShoulder, HumanBodyBones.RightShoulder, "RightShoulder", "Human:RightShoulder");
        filled += Assign(ref leftUpperArm, HumanBodyBones.LeftUpperArm, "LeftUpperArm", "Human:LeftUpperArm");
        filled += Assign(ref leftHand, HumanBodyBones.LeftHand, "LeftHand", "Human:LeftHand");
        filled += Assign(ref rightUpperArm, HumanBodyBones.RightUpperArm, "RightUpperArm", "Human:RightUpperArm");
        filled += Assign(ref rightHand, HumanBodyBones.RightHand, "RightHand", "Human:RightHand");
        filled += Assign(ref leftKnee, HumanBodyBones.LeftLowerLeg, "LeftKnee", "Human:LeftLowerLeg");
        filled += Assign(ref rightKnee, HumanBodyBones.RightLowerLeg, "RightKnee", "Human:RightLowerLeg");
        return filled;
    }

    [ContextMenu("Clear Optional Overrides")]
    public void ClearOptionalOverrides()
    {
        head = chest = leftShoulder = rightShoulder = null;
        leftUpperArm = leftHand = rightUpperArm = rightHand = null;
        leftKnee = rightKnee = null;
    }

    int Assign(ref Transform field, HumanBodyBones bone, params string[] names)
    {
        Transform found = Resolve(null, bone, names);
        if (found == null) return 0;
        field = found;
        return 1;
    }

    Transform Resolve(Transform overrideT, HumanBodyBones bone, params string[] names)
    {
        if (overrideT != null) return overrideT;
        if (ragdoll != null)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var t = ragdoll.GetBoneTransform(names[i]);
                if (t != null) return t;
            }
        }

        var anim = animator;
        if (anim == null && ragdoll != null)
            anim = ragdoll.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var t = anim.GetBoneTransform(bone);
            if (t != null) return t;
        }

        return null;
    }
}
