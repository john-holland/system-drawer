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

        // Extract impulse actions from bone transforms using ragdoll system's bone resolution
        List<ImpulseAction> impulseActions = ExtractImpulseActions(frame, ragdoll);

        card.impulseStack = impulseActions;

        // Calculate required and target states
        card.requiredState = CalculateRequiredState(frame, ragdoll);
        card.targetState = CalculateTargetState(frame, ragdoll);

        return card;
    }

    /// <summary>
    /// Extract impulse actions from frame bone transforms.
    /// Uses RagdollSystem's bone map to resolve body parts and determine correct channels.
    /// </summary>
    public static List<ImpulseAction> ExtractImpulseActions(AnimationFrame frame, RagdollSystem ragdoll)
    {
        List<ImpulseAction> actions = new List<ImpulseAction>();

        if (frame == null || frame.boneTransforms == null || ragdoll == null)
        {
            Debug.LogWarning("[AnimationPhysicsCardGenerator] Cannot extract impulse actions: frame, boneTransforms, or ragdoll is null");
            return actions;
        }

        // Map Unity HumanBodyBones names to possible role names (can have multiple roles per bone)
        Dictionary<string, List<string>> humanBoneToRoles = new Dictionary<string, List<string>>
        {
            // Arms
            { "LeftHand", new List<string> { "Hand" } },
            { "RightHand", new List<string> { "Hand" } },
            { "LeftLowerArm", new List<string> { "Forearm" } },
            { "RightLowerArm", new List<string> { "Forearm" } },
            { "LeftUpperArm", new List<string> { "Upperarm" } },
            { "RightUpperArm", new List<string> { "Upperarm" } },
            { "LeftShoulder", new List<string> { "Shoulder", "Collarbone" } }, // Can be both
            { "RightShoulder", new List<string> { "Shoulder", "Collarbone" } }, // Can be both
            // Legs - LowerLeg can map to both Shin and Knee if they're on the same GameObject
            { "LeftFoot", new List<string> { "Foot" } },
            { "RightFoot", new List<string> { "Foot" } },
            { "LeftLowerLeg", new List<string> { "Shin", "Knee" } }, // Can be both
            { "RightLowerLeg", new List<string> { "Shin", "Knee" } }, // Can be both
            { "LeftUpperLeg", new List<string> { "Leg" } },
            { "RightUpperLeg", new List<string> { "Leg" } },
            // Torso
            { "Head", new List<string> { "Head" } },
            { "Neck", new List<string> { "Neck" } },
            { "Spine", new List<string> { "Torso" } },
            { "Chest", new List<string> { "Torso" } },
            { "UpperChest", new List<string> { "Torso" } },
            { "Hips", new List<string> { "Pelvis" } }
        };

        // Process each bone transform
        foreach (var kvp in frame.boneTransforms)
        {
            string boneName = kvp.Key;
            TransformData transform = kvp.Value;

            // Determine side from bone name
            BodySide? side = null;
            if (boneName.Contains("Left"))
            {
                side = BodySide.Left;
            }
            else if (boneName.Contains("Right"))
            {
                side = BodySide.Right;
            }

            // Get possible roles for this bone
            List<string> possibleRoles = new List<string>();
            if (humanBoneToRoles.TryGetValue(boneName, out List<string> roles))
            {
                possibleRoles.AddRange(roles);
            }
            else
            {
                // Try to infer role from bone name patterns
                if (boneName.Contains("Hand")) possibleRoles.Add("Hand");
                else if (boneName.Contains("Foot")) possibleRoles.Add("Foot");
                else if (boneName.Contains("LowerLeg") || boneName.Contains("Shin"))
                {
                    possibleRoles.Add("Shin");
                    possibleRoles.Add("Knee"); // Could be either or both
                }
                else if (boneName.Contains("UpperLeg") || boneName.Contains("Thigh")) possibleRoles.Add("Leg");
                else if (boneName.Contains("LowerArm") || boneName.Contains("Forearm")) possibleRoles.Add("Forearm");
                else if (boneName.Contains("UpperArm")) possibleRoles.Add("Upperarm");
                else if (boneName.Contains("Shoulder"))
                {
                    possibleRoles.Add("Shoulder");
                    possibleRoles.Add("Collarbone"); // Could be either or both
                }
                else if (boneName.Contains("Head")) possibleRoles.Add("Head");
                else if (boneName.Contains("Neck")) possibleRoles.Add("Neck");
                else if (boneName.Contains("Spine") || boneName.Contains("Chest")) possibleRoles.Add("Torso");
                else if (boneName.Contains("Hips") || boneName.Contains("Pelvis")) possibleRoles.Add("Pelvis");
            }

            // Try to resolve each possible role and create impulse actions for components that exist
            HashSet<string> processedChannels = new HashSet<string>(); // Avoid duplicate channels
            bool foundAnyComponent = false;

            foreach (string role in possibleRoles)
            {
                UnityEngine.Component bodyPartComponent = null;

                if (side.HasValue)
                {
                    bodyPartComponent = ResolveBodyPartComponent(ragdoll, role, side.Value);
                }
                else
                {
                    bodyPartComponent = ResolveBodyPartComponent(ragdoll, role, null);
                }

                if (bodyPartComponent != null)
                {
                    // Determine muscle group/channel name from body part component
                    string componentTypeName = bodyPartComponent.GetType().Name;
                    string muscleGroup = GetMuscleGroupFromComponent(componentTypeName, bodyPartComponent.gameObject.name);

                    // Only create one impulse action per unique channel
                    if (!processedChannels.Contains(muscleGroup))
                    {
                        processedChannels.Add(muscleGroup);
                        foundAnyComponent = true;

                        // Create impulse action for this bone/component
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
                        Debug.Log($"[AnimationPhysicsCardGenerator] Resolved bone '{boneName}' -> role '{role}' -> {componentTypeName} '{bodyPartComponent.gameObject.name}' -> channel '{muscleGroup}'");
                    }
                }
            }

            // If no components were found, use fallback
            if (!foundAnyComponent)
            {
                string muscleGroup = GetMuscleGroupFromBoneName(boneName);
                if (!processedChannels.Contains(muscleGroup))
                {
                    processedChannels.Add(muscleGroup);
                    ImpulseAction action = new ImpulseAction
                    {
                        muscleGroup = muscleGroup,
                        activation = 0.5f,
                        duration = 0.1f,
                        forceDirection = transform.position.normalized,
                        torqueDirection = (transform.rotation * Vector3.forward - Vector3.forward).normalized
                    };
                    action.curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    actions.Add(action);
                    Debug.LogWarning($"[AnimationPhysicsCardGenerator] Could not resolve body part for bone '{boneName}', using fallback channel '{muscleGroup}'");
                }
            }
        }

        Debug.Log($"[AnimationPhysicsCardGenerator] Generated {actions.Count} impulse actions from {frame.boneTransforms.Count} bone transforms");
        return actions;
    }

    /// <summary>
    /// Resolve body part component from ragdoll system using reflection to access private ResolveBone method.
    /// </summary>
    private static UnityEngine.Component ResolveBodyPartComponent(RagdollSystem ragdoll, string role, BodySide? side)
    {
        if (ragdoll == null || string.IsNullOrEmpty(role))
            return null;

        // Use reflection to call the private ResolveBone method
        var resolveBoneMethod = typeof(RagdollSystem).GetMethod("ResolveBone",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (resolveBoneMethod == null)
        {
            Debug.LogWarning("[AnimationPhysicsCardGenerator] Could not find ResolveBone method in RagdollSystem");
            return null;
        }

        try
        {
            Transform boneTransform = null;
            if (side.HasValue)
            {
                boneTransform = (Transform)resolveBoneMethod.Invoke(ragdoll, new object[] { role, side.Value });
            }
            else
            {
                boneTransform = (Transform)resolveBoneMethod.Invoke(ragdoll, new object[] { role, null });
            }

            if (boneTransform != null)
            {
                // Find the RagdollBodyPart component on this transform (or any MonoBehaviour)
                var bodyPart = boneTransform.GetComponent<Locomotion.Musculature.RagdollBodyPart>();
                if (bodyPart != null)
                    return bodyPart;
                
                // Fallback to any MonoBehaviour component
                return boneTransform.GetComponent<MonoBehaviour>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AnimationPhysicsCardGenerator] Error resolving body part '{role}' (side: {side}): {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// Get muscle group/channel name from body part component type and name.
    /// </summary>
    private static string GetMuscleGroupFromComponent(string componentTypeName, string gameObjectName)
    {
        // Extract meaningful channel name from component type
        // e.g., "RagdollLeg" -> "Leg", "RagdollKnee" -> "Knee", etc.
        if (componentTypeName.StartsWith("Ragdoll"))
        {
            string baseName = componentTypeName.Substring(7); // Remove "Ragdoll" prefix
            
            // Combine with GameObject name for specificity (e.g., "LeftLeg", "RightKnee")
            if (gameObjectName.Contains("Left") || gameObjectName.Contains("L_") || gameObjectName.Contains("l_"))
            {
                return "Left" + baseName;
            }
            else if (gameObjectName.Contains("Right") || gameObjectName.Contains("R_") || gameObjectName.Contains("r_"))
            {
                return "Right" + baseName;
            }
            
            return baseName;
        }

        return gameObjectName;
    }

    /// <summary>
    /// Fallback: Get muscle group from bone name when body part resolution fails.
    /// Creates specific channel names based on bone names to avoid overcrowding "Spinal".
    /// Channels will be auto-created by NervousSystem when used.
    /// </summary>
    private static string GetMuscleGroupFromBoneName(string boneName)
    {
        // Extract side prefix if present
        string sidePrefix = "";
        string baseBoneName = boneName;
        
        if (boneName.StartsWith("Left") || boneName.Contains("Left"))
        {
            sidePrefix = "Left";
            baseBoneName = boneName.Replace("Left", "").Trim();
        }
        else if (boneName.StartsWith("Right") || boneName.Contains("Right"))
        {
            sidePrefix = "Right";
            baseBoneName = boneName.Replace("Right", "").Trim();
        }

        // Map bone names to specific muscle group names
        string groupName = null;
        
        if (baseBoneName.Contains("Hand") || boneName.Contains("Hand"))
        {
            groupName = "Hand";
        }
        else if (baseBoneName.Contains("Foot") || boneName.Contains("Foot"))
        {
            groupName = "Foot";
        }
        else if (baseBoneName.Contains("LowerLeg") || baseBoneName.Contains("Shin") || boneName.Contains("LowerLeg") || boneName.Contains("Shin"))
        {
            groupName = "Shin";
        }
        else if (baseBoneName.Contains("UpperLeg") || baseBoneName.Contains("Thigh") || boneName.Contains("UpperLeg") || boneName.Contains("Thigh"))
        {
            groupName = "Leg";
        }
        else if (baseBoneName.Contains("LowerArm") || baseBoneName.Contains("Forearm") || boneName.Contains("LowerArm") || boneName.Contains("Forearm"))
        {
            groupName = "Forearm";
        }
        else if (baseBoneName.Contains("UpperArm") || boneName.Contains("UpperArm"))
        {
            groupName = "Upperarm";
        }
        else if (baseBoneName.Contains("Shoulder") || boneName.Contains("Shoulder"))
        {
            groupName = "Shoulder";
        }
        else if (baseBoneName.Contains("Head") || boneName.Contains("Head"))
        {
            groupName = "Head";
        }
        else if (baseBoneName.Contains("Neck") || boneName.Contains("Neck"))
        {
            groupName = "Neck";
        }
        else if (baseBoneName.Contains("Spine") || baseBoneName.Contains("Chest") || boneName.Contains("Spine") || boneName.Contains("Chest"))
        {
            groupName = "Torso";
        }
        else if (baseBoneName.Contains("Hips") || baseBoneName.Contains("Pelvis") || boneName.Contains("Hips") || boneName.Contains("Pelvis"))
        {
            groupName = "Pelvis";
        }
        else
        {
            // Use the bone name itself as the channel name (cleaned up)
            // This ensures every bone gets its own channel instead of defaulting to "Spinal"
            groupName = boneName.Replace(" ", "").Replace("_", "");
        }

        // Combine side prefix with group name for specificity
        if (!string.IsNullOrEmpty(sidePrefix) && !groupName.StartsWith(sidePrefix))
        {
            return sidePrefix + groupName;
        }

        return groupName;
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
