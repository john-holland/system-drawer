using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main ragdoll physics coordinator. Manages ragdoll structure, muscle activations,
/// and animation blending (procedural + keyframe).
/// </summary>
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
