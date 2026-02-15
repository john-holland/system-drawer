using UnityEngine;

/// <summary>
/// Utilities to apply a pose to a ragdoll (e.g. sample an animation clip at a given time)
/// and zero velocities so the ragdoll starts from rest. Used by the IK trainer for optional
/// initial pose (first frame, idle, T-pose, etc.).
/// </summary>
public static class RagdollPoseUtility
{
    /// <summary>
    /// Sample an animation clip at the given time onto the ragdoll hierarchy.
    /// Uses the same convention as AnimationBehaviorTree: clip is applied to ragdollRoot.gameObject.
    /// Call ZeroRagdollVelocities after this so the ragdoll does not carry momentum.
    /// </summary>
    public static void SampleClipOntoRagdoll(RagdollSystem ragdollSystem, AnimationClip clip, float time)
    {
        if (ragdollSystem == null || ragdollSystem.ragdollRoot == null || clip == null)
            return;
        if (time < 0f || time > clip.length)
            time = Mathf.Clamp(time, 0f, clip.length);
        try
        {
            clip.SampleAnimation(ragdollSystem.ragdollRoot.gameObject, time);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[RagdollPoseUtility] SampleAnimation failed: {e.Message}");
        }
    }

    /// <summary>
    /// Zero linear and angular velocity on all rigidbodies under the ragdoll root
    /// so the ragdoll starts from rest after a pose is applied.
    /// </summary>
    public static void ZeroRagdollVelocities(RagdollSystem ragdollSystem)
    {
        if (ragdollSystem == null || ragdollSystem.ragdollRoot == null)
            return;
        Rigidbody[] rbs = ragdollSystem.ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        if (rbs == null) return;
        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] != null)
            {
                rbs[i].linearVelocity = Vector3.zero;
                rbs[i].angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Sample the clip at time 0 onto the ragdoll and zero all rigidbody velocities.
    /// </summary>
    public static void ApplyPoseFromClipAndZeroVelocities(RagdollSystem ragdollSystem, AnimationClip clip)
    {
        SampleClipOntoRagdoll(ragdollSystem, clip, 0f);
        ZeroRagdollVelocities(ragdollSystem);
    }
}
