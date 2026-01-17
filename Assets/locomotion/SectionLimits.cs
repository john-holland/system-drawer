using UnityEngine;

/// <summary>
/// Physical limits for good section feasibility checking.
/// Defines constraints on degrees difference, torque, force, and velocity change.
/// </summary>
[System.Serializable]
public class SectionLimits
{
    [Header("Degrees Difference")]
    [Tooltip("Maximum degrees difference required from current state")]
    public float maxDegreesDifference = 180f;

    [Header("Torque Limits")]
    [Tooltip("Maximum torque required (Newton-meters)")]
    public float maxTorque = 1000f;

    [Header("Force Limits")]
    [Tooltip("Maximum force required (Newtons)")]
    public float maxForce = 2000f;

    [Header("Velocity Change")]
    [Tooltip("Maximum velocity change required (m/s)")]
    public float maxVelocityChange = 10f;

    [Header("Angular Velocity Change")]
    [Tooltip("Maximum angular velocity change required (rad/s)")]
    public float maxAngularVelocityChange = 10f;

    /// <summary>
    /// Check if limits are feasible given a current state and required state.
    /// Returns true if all limits are within bounds.
    /// </summary>
    public bool CheckFeasibility(RagdollState currentState, RagdollState requiredState)
    {
        // Check degrees difference
        float degreesDiff = CalculateDegreesDifference(currentState, requiredState);
        if (degreesDiff > maxDegreesDifference)
        {
            return false;
        }

        // Check torque feasibility (simplified check)
        float torqueReq = EstimateTorqueRequirement(currentState, requiredState);
        if (torqueReq > maxTorque)
        {
            return false;
        }

        // Check force feasibility
        float forceReq = EstimateForceRequirement(currentState, requiredState);
        if (forceReq > maxForce)
        {
            return false;
        }

        // Check velocity change
        float velChange = EstimateVelocityChange(currentState, requiredState);
        if (velChange > maxVelocityChange)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get limit score (0-1) for feasibility scoring.
    /// 1 = fully within limits, 0 = exceeded all limits.
    /// </summary>
    public float GetLimitScore(RagdollState currentState, RagdollState requiredState)
    {
        float score = 1f;
        float weightSum = 4f; // Number of factors

        // Degrees difference (30% weight)
        float degreesDiff = CalculateDegreesDifference(currentState, requiredState);
        float degreesScore = 1f - Mathf.Clamp01(degreesDiff / maxDegreesDifference);
        score += degreesScore * 0.3f;

        // Torque feasibility (30% weight)
        float torqueReq = EstimateTorqueRequirement(currentState, requiredState);
        float torqueScore = 1f - Mathf.Clamp01(torqueReq / maxTorque);
        score += torqueScore * 0.3f;

        // Force feasibility (20% weight)
        float forceReq = EstimateForceRequirement(currentState, requiredState);
        float forceScore = 1f - Mathf.Clamp01(forceReq / maxForce);
        score += forceScore * 0.2f;

        // Velocity change (20% weight)
        float velChange = EstimateVelocityChange(currentState, requiredState);
        float velScore = 1f - Mathf.Clamp01(velChange / maxVelocityChange);
        score += velScore * 0.2f;

        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Calculate degrees difference between two states.
    /// </summary>
    private float CalculateDegreesDifference(RagdollState currentState, RagdollState requiredState)
    {
        // Calculate rotation difference
        float rootRotDiff = Quaternion.Angle(currentState.rootRotation, requiredState.rootRotation);

        // Calculate joint angle differences
        float totalJointDiff = 0f;
        int jointCount = 0;

        foreach (var kvp in requiredState.jointStates)
        {
            if (currentState.jointStates.TryGetValue(kvp.Key, out JointState currentJoint))
            {
                float jointDiff = Quaternion.Angle(currentJoint.rotation, kvp.Value.rotation);
                totalJointDiff += jointDiff;
                jointCount++;
            }
        }

        float avgJointDiff = jointCount > 0 ? totalJointDiff / jointCount : 0f;

        // Combine root and joint differences
        return rootRotDiff + avgJointDiff;
    }

    /// <summary>
    /// Estimate torque requirement for transition (simplified).
    /// </summary>
    private float EstimateTorqueRequirement(RagdollState currentState, RagdollState requiredState)
    {
        // Simplified: torque proportional to angular velocity change
        float angularVelChange = (requiredState.rootAngularVelocity - currentState.rootAngularVelocity).magnitude;
        
        // Estimate based on angular acceleration needed
        return angularVelChange * 100f; // Rough estimate
    }

    /// <summary>
    /// Estimate force requirement for transition (simplified).
    /// </summary>
    private float EstimateForceRequirement(RagdollState currentState, RagdollState requiredState)
    {
        // Simplified: force proportional to velocity change
        float velChange = (requiredState.rootVelocity - currentState.rootVelocity).magnitude;
        
        // Estimate based on acceleration needed
        return velChange * 200f; // Rough estimate
    }

    /// <summary>
    /// Estimate velocity change required for transition.
    /// </summary>
    private float EstimateVelocityChange(RagdollState currentState, RagdollState requiredState)
    {
        return (requiredState.rootVelocity - currentState.rootVelocity).magnitude;
    }
}
