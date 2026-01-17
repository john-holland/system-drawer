using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the current state of a ragdoll for card matching and feasibility checking.
/// Contains joint states, muscle activations, root transform, and physics contacts.
/// </summary>
[System.Serializable]
public class RagdollState
{
    [Header("Joint States")]
    [Tooltip("Joint states (joint name -> state)")]
    public Dictionary<string, JointState> jointStates = new Dictionary<string, JointState>();

    [Header("Muscle Activations")]
    [Tooltip("Current muscle activations (muscle name -> activation 0-1)")]
    public Dictionary<string, float> muscleActivations = new Dictionary<string, float>();

    [Header("Root Transform")]
    [Tooltip("Root position")]
    public Vector3 rootPosition;

    [Tooltip("Root rotation")]
    public Quaternion rootRotation;

    [Tooltip("Root linear velocity")]
    public Vector3 rootVelocity;

    [Tooltip("Root angular velocity")]
    public Vector3 rootAngularVelocity;

    [Header("Physics Contacts")]
    [Tooltip("Physics contacts for this state")]
    public List<ContactPoint> contacts = new List<ContactPoint>();

    /// <summary>
    /// Get joint state by name.
    /// </summary>
    public JointState GetJointState(string jointName)
    {
        jointStates.TryGetValue(jointName, out JointState state);
        return state;
    }

    /// <summary>
    /// Get muscle activation by name.
    /// </summary>
    public float GetMuscleActivation(string muscleName)
    {
        muscleActivations.TryGetValue(muscleName, out float activation);
        return activation;
    }

    /// <summary>
    /// Copy this state to a new RagdollState instance.
    /// </summary>
    public RagdollState CopyState()
    {
        RagdollState copy = new RagdollState
        {
            rootPosition = this.rootPosition,
            rootRotation = this.rootRotation,
            rootVelocity = this.rootVelocity,
            rootAngularVelocity = this.rootAngularVelocity
        };

        // Deep copy dictionaries
        copy.jointStates = new Dictionary<string, JointState>();
        foreach (var kvp in this.jointStates)
        {
            copy.jointStates[kvp.Key] = kvp.Value; // JointState is a struct, so this is a copy
        }

        copy.muscleActivations = new Dictionary<string, float>();
        foreach (var kvp in this.muscleActivations)
        {
            copy.muscleActivations[kvp.Key] = kvp.Value;
        }

        copy.contacts = new List<ContactPoint>(this.contacts);

        return copy;
    }

    /// <summary>
    /// Calculate distance between two ragdoll states (for feasibility scoring).
    /// </summary>
    public float CalculateDistance(RagdollState other)
    {
        float distance = 0f;

        // Position distance
        distance += Vector3.Distance(this.rootPosition, other.rootPosition);

        // Rotation distance (angle between rotations)
        distance += Quaternion.Angle(this.rootRotation, other.rootRotation) / 180f; // Normalize to 0-1 range

        // Velocity distance
        distance += Vector3.Distance(this.rootVelocity, other.rootVelocity) * 0.1f; // Weighted

        // Joint state distances
        int jointCount = 0;
        foreach (var kvp in this.jointStates)
        {
            if (other.jointStates.TryGetValue(kvp.Key, out JointState otherState))
            {
                distance += Vector3.Distance(kvp.Value.position, otherState.position);
                distance += Quaternion.Angle(kvp.Value.rotation, otherState.rotation) / 180f;
                jointCount++;
            }
        }

        // Normalize by joint count
        if (jointCount > 0)
        {
            distance /= jointCount;
        }

        return distance;
    }

    /// <summary>
    /// Check if this state is similar to another state (within tolerance).
    /// </summary>
    public bool IsSimilarTo(RagdollState other, float tolerance = 0.1f)
    {
        float distance = CalculateDistance(other);
        return distance < tolerance;
    }

    /// <summary>
    /// Get summary string of this state.
    /// </summary>
    public override string ToString()
    {
        return $"RagdollState: Pos={rootPosition}, Rot={rootRotation.eulerAngles}, Joints={jointStates.Count}, Muscles={muscleActivations.Count}, Contacts={contacts.Count}";
    }
}
