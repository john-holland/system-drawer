using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates physics cards from animation frame data.
/// Extracts bone transforms and converts them to GoodSection cards with impulse actions.
/// </summary>
public static class AnimationPhysicsCardGenerator
{
    /// <summary>
    /// Generate a GoodSection card from an animation frame.
    /// </summary>
    public static GoodSection GenerateCardFromFrame(AnimationFrame frame, RagdollSystem ragdoll)
    {
        if (frame == null || ragdoll == null)
            return null;

        GoodSection card = new GoodSection
        {
            sectionName = $"frame_{frame.frameIndex}",
            description = $"Animation frame {frame.frameIndex} at time {frame.time:F2}s"
        };

        // Extract impulse actions from bone transforms
        Dictionary<string, HumanBodyBones> boneMap = GetBoneMap(ragdoll);
        List<ImpulseAction> impulseActions = ExtractImpulseActions(frame, boneMap);

        card.impulseStack = impulseActions;

        // Calculate required and target states
        card.requiredState = CalculateRequiredState(frame, ragdoll);
        card.targetState = CalculateTargetState(frame, ragdoll);

        return card;
    }

    /// <summary>
    /// Extract impulse actions from frame bone transforms.
    /// </summary>
    public static List<ImpulseAction> ExtractImpulseActions(AnimationFrame frame, Dictionary<string, HumanBodyBones> boneMap)
    {
        List<ImpulseAction> actions = new List<ImpulseAction>();

        if (frame == null || frame.boneTransforms == null || boneMap == null)
            return actions;

        // Map bone names to muscle groups
        Dictionary<string, string> boneToMuscleGroup = new Dictionary<string, string>
        {
            { "LeftHand", "LeftArm" },
            { "RightHand", "RightArm" },
            { "LeftLowerArm", "LeftArm" },
            { "RightLowerArm", "RightArm" },
            { "LeftUpperArm", "LeftArm" },
            { "RightUpperArm", "RightArm" },
            { "LeftShoulder", "LeftArm" },
            { "RightShoulder", "RightArm" },
            { "LeftFoot", "LeftLeg" },
            { "RightFoot", "RightLeg" },
            { "LeftLowerLeg", "LeftLeg" },
            { "RightLowerLeg", "RightLeg" },
            { "LeftUpperLeg", "LeftLeg" },
            { "RightUpperLeg", "RightLeg" },
            { "Head", "Neck" },
            { "Neck", "Neck" },
            { "Spine", "Spinal" },
            { "Hips", "Spinal" }
        };

        // Process each bone transform
        foreach (var kvp in frame.boneTransforms)
        {
            string boneName = kvp.Key;
            TransformData transform = kvp.Value;

            // Find muscle group for this bone
            string muscleGroup = "Spinal"; // Default
            if (boneToMuscleGroup.TryGetValue(boneName, out string group))
            {
                muscleGroup = group;
            }

            // Create impulse action for this bone
            ImpulseAction action = new ImpulseAction
            {
                muscleGroup = muscleGroup,
                activation = 0.5f, // Default activation, could be calculated from transform delta
                duration = 0.1f, // Default duration for a frame
                forceDirection = transform.position.normalized,
                torqueDirection = (transform.rotation * Vector3.forward - Vector3.forward).normalized
            };

            // Create activation curve (simple linear for now)
            action.curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            actions.Add(action);
        }

        return actions;
    }

    /// <summary>
    /// Calculate required ragdoll state from frame.
    /// </summary>
    public static RagdollState CalculateRequiredState(AnimationFrame frame, RagdollSystem ragdoll)
    {
        if (frame == null || ragdoll == null)
            return new RagdollState();

        RagdollState state = new RagdollState();

        // Set root transform from frame
        if (ragdoll.ragdollRoot != null)
        {
            state.rootPosition = ragdoll.ragdollRoot.position + frame.rootMotion;
            state.rootRotation = ragdoll.ragdollRoot.rotation * frame.rootRotation;
        }

        // Convert bone transforms to joint states
        state.jointStates = new Dictionary<string, JointState>();
        if (frame.boneTransforms != null)
        {
            foreach (var kvp in frame.boneTransforms)
            {
                string boneName = kvp.Key;
                TransformData transform = kvp.Value;

                JointState jointState = new JointState();
                jointState.position = transform.position;
                jointState.rotation = transform.rotation;

                state.jointStates[boneName] = jointState;
            }
        }

        return state;
    }

    /// <summary>
    /// Calculate target ragdoll state from frame.
    /// </summary>
    public static RagdollState CalculateTargetState(AnimationFrame frame, RagdollSystem ragdoll)
    {
        // For now, target state is same as required state
        // In a more sophisticated implementation, this would predict the end state
        return CalculateRequiredState(frame, ragdoll);
    }

    /// <summary>
    /// Get bone map from ragdoll system.
    /// </summary>
    private static Dictionary<string, HumanBodyBones> GetBoneMap(RagdollSystem ragdoll)
    {
        Dictionary<string, HumanBodyBones> boneMap = new Dictionary<string, HumanBodyBones>();

        if (ragdoll == null)
            return boneMap;

        // Try to get bone map from ragdoll system
        // This is a simplified implementation - actual implementation would use RagdollSystem's bone mapping
        // For now, return empty map and rely on bone name matching

        return boneMap;
    }
}
