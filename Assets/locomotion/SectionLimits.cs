using UnityEngine;

/// <summary>
/// Represents upper and lower rotation tolerance bounds for a body part.
/// </summary>
[System.Serializable]
public struct RotationToleranceBounds
{
    public Vector3 lower;
    public Vector3 upper;

    public RotationToleranceBounds(Vector3 lower, Vector3 upper)
    {
        this.lower = lower;
        this.upper = upper;
    }

    /// <summary>
    /// Create symmetric bounds from a single tolerance value.
    /// </summary>
    public static RotationToleranceBounds Symmetric(Vector3 tolerance)
    {
        return new RotationToleranceBounds(-tolerance, tolerance);
    }
}

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

    [Header("Radial Limits")]
    [Tooltip("Enable radial limit checking (position and rotation bounds)")]
    public bool useRadialLimits = false;

    [Tooltip("Maximum radial distance from reference position (meters, 0 = unlimited)")]
    public float maxRadialDistance = 0f;

    [Tooltip("Maximum radial rotation deviation from reference rotation (degrees, 0 = unlimited)")]
    public float maxRadialRotation = 0f;

    [Tooltip("Reference position for radial distance checking (world space)")]
    public Vector3 radialReferencePosition = Vector3.zero;

    [Tooltip("Lower bound for radial rotation checking (Euler angles in degrees)")]
    public Vector3 lowerRadialReferenceRotation = Vector3.zero;

    [Tooltip("Upper bound for radial rotation checking (Euler angles in degrees)")]
    public Vector3 upperRadialReferenceRotation = Vector3.zero;

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

        // Check radial limits if enabled
        if (useRadialLimits && !CheckRadialLimits(currentState))
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

        // Radial limits (if enabled, reduce weight of other factors proportionally)
        if (useRadialLimits)
        {
            float radialScore = GetRadialLimitScore(currentState);
            // Radial limits get 20% weight, reduce other weights proportionally
            score = score * 0.8f + radialScore * 0.2f;
        }

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

    /// <summary>
    /// Check if state is within radial limits (position and rotation).
    /// </summary>
    public bool CheckRadialLimits(RagdollState state)
    {
        if (!useRadialLimits)
            return true;

        // Check radial distance
        if (maxRadialDistance > 0f)
        {
            float distance = Vector3.Distance(state.rootPosition, radialReferencePosition);
            if (distance > maxRadialDistance)
            {
                return false;
            }
        }

        // Check radial rotation (using Euler angle bounds)
        if (maxRadialRotation > 0f || (lowerRadialReferenceRotation != Vector3.zero || upperRadialReferenceRotation != Vector3.zero))
        {
            Vector3 currentEuler = state.rootRotation.eulerAngles;
            
            // Normalize Euler angles to 0-360 range for comparison
            Vector3 lower = NormalizeEuler(lowerRadialReferenceRotation);
            Vector3 upper = NormalizeEuler(upperRadialReferenceRotation);
            Vector3 current = NormalizeEuler(currentEuler);
            
            // Check if current rotation is within bounds for each axis
            bool withinBounds = true;
            
            // Check X axis
            if (lower.x != 0f || upper.x != 0f)
            {
                if (lower.x <= upper.x)
                {
                    // Normal range (e.g., 10 to 30)
                    withinBounds = withinBounds && (current.x >= lower.x && current.x <= upper.x);
                }
                else
                {
                    // Wraparound range (e.g., 350 to 10)
                    withinBounds = withinBounds && (current.x >= lower.x || current.x <= upper.x);
                }
            }
            
            // Check Y axis
            if (lower.y != 0f || upper.y != 0f)
            {
                if (lower.y <= upper.y)
                {
                    withinBounds = withinBounds && (current.y >= lower.y && current.y <= upper.y);
                }
                else
                {
                    withinBounds = withinBounds && (current.y >= lower.y || current.y <= upper.y);
                }
            }
            
            // Check Z axis
            if (lower.z != 0f || upper.z != 0f)
            {
                if (lower.z <= upper.z)
                {
                    withinBounds = withinBounds && (current.z >= lower.z && current.z <= upper.z);
                }
                else
                {
                    withinBounds = withinBounds && (current.z >= lower.z || current.z <= upper.z);
                }
            }
            
            // Also check maxRadialRotation if specified (legacy support)
            if (maxRadialRotation > 0f && withinBounds)
            {
                // Calculate average deviation from center of range
                Vector3 center = (lower + upper) * 0.5f;
                float avgDeviation = Vector3.Distance(current, center);
                if (avgDeviation > maxRadialRotation)
                {
                    return false;
                }
            }
            else if (!withinBounds)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get radial limit score (0-1) for feasibility scoring.
    /// 1 = fully within limits, 0 = exceeded all limits.
    /// </summary>
    public float GetRadialLimitScore(RagdollState state)
    {
        if (!useRadialLimits)
            return 1f;

        float score = 1f;

        // Position radial score
        if (maxRadialDistance > 0f)
        {
            float distance = Vector3.Distance(state.rootPosition, radialReferencePosition);
            float distanceScore = 1f - Mathf.Clamp01(distance / maxRadialDistance);
            score = Mathf.Min(score, distanceScore);
        }

        // Rotation radial score (using Euler angle bounds)
        if (maxRadialRotation > 0f || (lowerRadialReferenceRotation != Vector3.zero || upperRadialReferenceRotation != Vector3.zero))
        {
            Vector3 currentEuler = state.rootRotation.eulerAngles;
            Vector3 lower = NormalizeEuler(lowerRadialReferenceRotation);
            Vector3 upper = NormalizeEuler(upperRadialReferenceRotation);
            Vector3 current = NormalizeEuler(currentEuler);
            
            float rotationScore = 1f;
            
            // Calculate score for each axis
            if (lower != Vector3.zero || upper != Vector3.zero)
            {
                Vector3 center = (lower + upper) * 0.5f;
                Vector3 range = upper - lower;
                
                // Handle wraparound for each axis
                float xScore = CalculateAxisScore(current.x, lower.x, upper.x);
                float yScore = CalculateAxisScore(current.y, lower.y, upper.y);
                float zScore = CalculateAxisScore(current.z, lower.z, upper.z);
                
                // Use minimum score (most restrictive axis)
                rotationScore = Mathf.Min(xScore, yScore, zScore);
            }
            
            // Also apply maxRadialRotation if specified (legacy support)
            if (maxRadialRotation > 0f)
            {
                Vector3 center = (lower + upper) * 0.5f;
                float avgDeviation = Vector3.Distance(current, center);
                float maxRadialScore = 1f - Mathf.Clamp01(avgDeviation / maxRadialRotation);
                rotationScore = Mathf.Min(rotationScore, maxRadialScore);
            }
            
            score = Mathf.Min(score, rotationScore);
        }

        return score;
    }

    /// <summary>
    /// Normalize Euler angles to 0-360 range.
    /// </summary>
    private Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z)
        );
    }

    /// <summary>
    /// Normalize a single angle to 0-360 range.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle < 0f)
            angle += 360f;
        return angle;
    }

    /// <summary>
    /// Calculate score for a single axis within bounds.
    /// </summary>
    private float CalculateAxisScore(float current, float lower, float upper)
    {
        if (lower == 0f && upper == 0f)
            return 1f; // No bounds = always valid

        float center = (lower + upper) * 0.5f;
        float range = Mathf.Abs(upper - lower);
        
        if (range == 0f)
            return 1f; // No range = always valid

        // Handle wraparound
        if (lower > upper)
        {
            // Wraparound case (e.g., 350 to 10)
            float dist1 = Mathf.Abs(current - lower);
            float dist2 = Mathf.Abs(current - upper);
            float dist = Mathf.Min(dist1, dist2);
            
            // Normalize to 0-1 range (0 = at center, 1 = at edge)
            float normalizedDist = dist / (360f - range);
            return 1f - Mathf.Clamp01(normalizedDist);
        }
        else
        {
            // Normal range case
            float dist = Mathf.Abs(current - center);
            float normalizedDist = dist / (range * 0.5f);
            return 1f - Mathf.Clamp01(normalizedDist);
        }
    }

    /// <summary>
    /// Set radial reference from a RagdollState.
    /// Sets both lower and upper bounds to the same rotation.
    /// </summary>
    public void SetRadialReference(RagdollState state)
    {
        if (state != null)
        {
            radialReferencePosition = state.rootPosition;
            Vector3 euler = state.rootRotation.eulerAngles;
            lowerRadialReferenceRotation = euler;
            upperRadialReferenceRotation = euler;
        }
    }

    /// <summary>
    /// Set radial reference from a transform.
    /// Sets both lower and upper bounds to the same rotation.
    /// </summary>
    public void SetRadialReference(Transform transform)
    {
        if (transform != null)
        {
            radialReferencePosition = transform.position;
            Vector3 euler = transform.rotation.eulerAngles;
            lowerRadialReferenceRotation = euler;
            upperRadialReferenceRotation = euler;
        }
    }

    /// <summary>
    /// Set radial reference from position and rotation.
    /// Sets both lower and upper bounds to the same rotation.
    /// </summary>
    public void SetRadialReference(Vector3 position, Quaternion rotation)
    {
        radialReferencePosition = position;
        Vector3 euler = rotation.eulerAngles;
        lowerRadialReferenceRotation = euler;
        upperRadialReferenceRotation = euler;
    }

    /// <summary>
    /// Set radial rotation bounds from lower and upper Euler angles.
    /// </summary>
    public void SetRadialRotationBounds(Vector3 lowerEuler, Vector3 upperEuler)
    {
        lowerRadialReferenceRotation = lowerEuler;
        upperRadialReferenceRotation = upperEuler;
    }

    /// <summary>
    /// Set radial rotation bounds from a center rotation and tolerance.
    /// </summary>
    public void SetRadialRotationBounds(Quaternion centerRotation, Vector3 toleranceDegrees)
    {
        Vector3 centerEuler = centerRotation.eulerAngles;
        lowerRadialReferenceRotation = centerEuler - toleranceDegrees;
        upperRadialReferenceRotation = centerEuler + toleranceDegrees;
    }

    /// <summary>
    /// Automatically establish radial limits from a GameObject's body part component,
    /// physics joints, and associated cards.
    /// </summary>
    public void EstablishLimitsFromGameObject(GameObject gameObject)
    {
        if (gameObject == null)
            return;

        Vector3 currentRotation = gameObject.transform.rotation.eulerAngles;
        Vector3 lowerBounds = currentRotation;
        Vector3 upperBounds = currentRotation;
        bool hasLimits = false;

        // Check for RagdollBodyPart component
        Locomotion.Musculature.RagdollBodyPart bodyPart = gameObject.GetComponent<Locomotion.Musculature.RagdollBodyPart>();
        if (bodyPart != null)
        {
            // Get the primary bone transform
            Transform boneTransform = bodyPart.PrimaryBoneTransform;
            if (boneTransform != null)
            {
                // Check for ConfigurableJoint on this GameObject or its children
                ConfigurableJoint[] joints = boneTransform.GetComponentsInChildren<ConfigurableJoint>();
                foreach (var joint in joints)
                {
                    if (joint == null) continue;

                    // Extract limits from ConfigurableJoint
                    Vector3 jointLower = ExtractJointLowerLimits(joint, currentRotation);
                    Vector3 jointUpper = ExtractJointUpperLimits(joint, currentRotation);

                    // Merge with existing bounds
                    if (!hasLimits)
                    {
                        lowerBounds = jointLower;
                        upperBounds = jointUpper;
                        hasLimits = true;
                    }
                    else
                    {
                        // Take the most restrictive bounds (intersection)
                        lowerBounds.x = Mathf.Max(lowerBounds.x, jointLower.x);
                        lowerBounds.y = Mathf.Max(lowerBounds.y, jointLower.y);
                        lowerBounds.z = Mathf.Max(lowerBounds.z, jointLower.z);
                        upperBounds.x = Mathf.Min(upperBounds.x, jointUpper.x);
                        upperBounds.y = Mathf.Min(upperBounds.y, jointUpper.y);
                        upperBounds.z = Mathf.Min(upperBounds.z, jointUpper.z);
                    }
                }

                // Check for HingeJoint (simpler, single axis)
                HingeJoint[] hingeJoints = boneTransform.GetComponentsInChildren<HingeJoint>();
                foreach (var hingeJoint in hingeJoints)
                {
                    if (hingeJoint == null || !hingeJoint.useLimits) continue;

                    Vector3 hingeLower = ExtractHingeJointLimits(hingeJoint, currentRotation);
                    Vector3 hingeUpper = ExtractHingeJointLimits(hingeJoint, currentRotation);
                    hingeUpper.x = hingeJoint.limits.max; // Hinge uses X axis

                    if (!hasLimits)
                    {
                        lowerBounds = hingeLower;
                        upperBounds = hingeUpper;
                        hasLimits = true;
                    }
                    else
                    {
                        lowerBounds.x = Mathf.Max(lowerBounds.x, hingeLower.x);
                        upperBounds.x = Mathf.Min(upperBounds.x, hingeUpper.x);
                    }
                }
            }
        }

        // Check for associated GoodSection cards
        PhysicsCardSolver cardSolver = gameObject.GetComponentInParent<PhysicsCardSolver>();
        if (cardSolver != null && cardSolver.availableCards != null)
        {
            foreach (var card in cardSolver.availableCards)
            {
                if (card == null || card.requiredState == null) continue;

                // Check if card is associated with this GameObject
                if (IsCardAssociatedWithGameObject(card, gameObject))
                {
                    Vector3 cardRotation = card.requiredState.rootRotation.eulerAngles;
                    Vector3 cardTolerance = GetCardRotationTolerance(card);

                    Vector3 cardLower = cardRotation - cardTolerance;
                    Vector3 cardUpper = cardRotation + cardTolerance;

                    if (!hasLimits)
                    {
                        lowerBounds = cardLower;
                        upperBounds = cardUpper;
                        hasLimits = true;
                    }
                    else
                    {
                        // Expand bounds to include card requirements (union)
                        lowerBounds.x = Mathf.Min(lowerBounds.x, cardLower.x);
                        lowerBounds.y = Mathf.Min(lowerBounds.y, cardLower.y);
                        lowerBounds.z = Mathf.Min(lowerBounds.z, cardLower.z);
                        upperBounds.x = Mathf.Max(upperBounds.x, cardUpper.x);
                        upperBounds.y = Mathf.Max(upperBounds.y, cardUpper.y);
                        upperBounds.z = Mathf.Max(upperBounds.z, cardUpper.z);
                    }
                }
            }
        }

        // Check BehaviorTree for associated cards
        BehaviorTree behaviorTree = gameObject.GetComponentInParent<BehaviorTree>();
        if (behaviorTree != null && behaviorTree.availableCards != null)
        {
            foreach (var card in behaviorTree.availableCards)
            {
                if (card == null || card.requiredState == null) continue;

                if (IsCardAssociatedWithGameObject(card, gameObject))
                {
                    Vector3 cardRotation = card.requiredState.rootRotation.eulerAngles;
                    Vector3 cardTolerance = GetCardRotationTolerance(card);

                    Vector3 cardLower = cardRotation - cardTolerance;
                    Vector3 cardUpper = cardRotation + cardTolerance;

                    if (!hasLimits)
                    {
                        lowerBounds = cardLower;
                        upperBounds = cardUpper;
                        hasLimits = true;
                    }
                    else
                    {
                        lowerBounds.x = Mathf.Min(lowerBounds.x, cardLower.x);
                        lowerBounds.y = Mathf.Min(lowerBounds.y, cardLower.y);
                        lowerBounds.z = Mathf.Min(lowerBounds.z, cardLower.z);
                        upperBounds.x = Mathf.Max(upperBounds.x, cardUpper.x);
                        upperBounds.y = Mathf.Max(upperBounds.y, cardUpper.y);
                        upperBounds.z = Mathf.Max(upperBounds.z, cardUpper.z);
                    }
                }
            }
        }

        // Apply default limits if no specific limits found
        if (!hasLimits)
        {
            // Use body part type defaults
            RotationToleranceBounds defaultBounds = GetDefaultToleranceForBodyPart(bodyPart);
            lowerBounds = currentRotation + defaultBounds.lower;
            upperBounds = currentRotation + defaultBounds.upper;
        }

        // Set the limits
        lowerRadialReferenceRotation = NormalizeEuler(lowerBounds);
        upperRadialReferenceRotation = NormalizeEuler(upperBounds);
        useRadialLimits = true;

        // Set position reference
        radialReferencePosition = gameObject.transform.position;
    }

    /// <summary>
    /// Extract lower rotation limits from a ConfigurableJoint.
    /// </summary>
    private Vector3 ExtractJointLowerLimits(ConfigurableJoint joint, Vector3 currentRotation)
    {
        Vector3 lower = currentRotation;

        // X axis (low and high limits)
        if (joint.angularXMotion != ConfigurableJointMotion.Locked)
        {
            lower.x = currentRotation.x + joint.lowAngularXLimit.limit;
        }

        // Y axis (symmetric limit)
        if (joint.angularYMotion != ConfigurableJointMotion.Locked)
        {
            lower.y = currentRotation.y - joint.angularYLimit.limit;
        }

        // Z axis (symmetric limit)
        if (joint.angularZMotion != ConfigurableJointMotion.Locked)
        {
            lower.z = currentRotation.z - joint.angularZLimit.limit;
        }

        return lower;
    }

    /// <summary>
    /// Extract upper rotation limits from a ConfigurableJoint.
    /// </summary>
    private Vector3 ExtractJointUpperLimits(ConfigurableJoint joint, Vector3 currentRotation)
    {
        Vector3 upper = currentRotation;

        // X axis (low and high limits)
        if (joint.angularXMotion != ConfigurableJointMotion.Locked)
        {
            upper.x = currentRotation.x + joint.highAngularXLimit.limit;
        }

        // Y axis (symmetric limit)
        if (joint.angularYMotion != ConfigurableJointMotion.Locked)
        {
            upper.y = currentRotation.y + joint.angularYLimit.limit;
        }

        // Z axis (symmetric limit)
        if (joint.angularZMotion != ConfigurableJointMotion.Locked)
        {
            upper.z = currentRotation.z + joint.angularZLimit.limit;
        }

        return upper;
    }

    /// <summary>
    /// Extract limits from a HingeJoint.
    /// </summary>
    private Vector3 ExtractHingeJointLimits(HingeJoint joint, Vector3 currentRotation)
    {
        Vector3 limits = currentRotation;
        limits.x = currentRotation.x + joint.limits.min; // Hinge rotates around X axis
        return limits;
    }

    /// <summary>
    /// Check if a card is associated with a specific GameObject.
    /// </summary>
    private bool IsCardAssociatedWithGameObject(GoodSection card, GameObject gameObject)
    {
        if (card == null || gameObject == null)
            return false;

        // Check if card's behavior tree is on this GameObject or its children
        if (card.behaviorTree != null)
        {
            if (card.behaviorTree.gameObject == gameObject ||
                card.behaviorTree.transform.IsChildOf(gameObject.transform) ||
                gameObject.transform.IsChildOf(card.behaviorTree.transform))
            {
                return true;
            }
        }

        // Check if card has target objects that match
        if (card is HemisphericalGraspCard graspCard && graspCard.targetObject == gameObject)
        {
            return true;
        }
        if (card is TippingCard tippingCard && tippingCard.targetObject == gameObject)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get rotation tolerance from a card's limits.
    /// </summary>
    private Vector3 GetCardRotationTolerance(GoodSection card)
    {
        // Default tolerance based on maxDegreesDifference
        float tolerance = card.limits != null ? card.limits.maxDegreesDifference : 45f;
        
        // Distribute tolerance across axes (can be customized)
        return new Vector3(tolerance * 0.4f, tolerance * 0.4f, tolerance * 0.2f);
    }

    /// <summary>
    /// Get default tolerance bounds for a body part type.
    /// Returns lower and upper bounds relative to neutral position (0,0,0).
    /// </summary>
    private RotationToleranceBounds GetDefaultToleranceForBodyPart(Locomotion.Musculature.RagdollBodyPart bodyPart)
    {
        if (bodyPart == null)
        {
            // Default: symmetric 45 degrees
            return new RotationToleranceBounds(
                new Vector3(-45f, -45f, -45f),
                new Vector3(45f, 45f, 45f)
            );
        }

        // Body part specific defaults
        string typeName = bodyPart.GetType().Name;

        if (typeName.Contains("Shoulder"))
        {
            // Shoulder: wide range in all directions
            // X: can rotate forward/backward extensively
            // Y: can raise/lower arm
            // Z: can rotate arm around axis
            return new RotationToleranceBounds(
                new Vector3(-90f, -50f, -90f),   // Lower bounds
                new Vector3(90f, 50f, 90f)       // Upper bounds
            );
        }
        else if (typeName.Contains("Hip") || typeName.Contains("Leg"))
        {
            // Hip/Leg: wide forward/back, limited side
            // X: can extend/retract leg
            // Y: can abduct/adduct leg
            // Z: can rotate leg
            return new RotationToleranceBounds(
                new Vector3(-120f, -45f, -45f),  // Lower bounds
                new Vector3(30f, 45f, 45f)        // Upper bounds (less forward extension)
            );
        }
        else if (typeName.Contains("Elbow") || typeName.Contains("Knee"))
        {
            // Elbow/Knee: mostly forward/back (bend/straighten)
            // X: primary bending axis
            // Y: minimal side movement
            // Z: minimal rotation
            return new RotationToleranceBounds(
                new Vector3(-120f, -10f, -10f),  // Lower bounds (bent)
                new Vector3(10f, 10f, 10f)       // Upper bounds (straight, slight hyperextension)
            );
        }
        else if (typeName.Contains("Neck") || typeName.Contains("Head"))
        {
            // Neck/Head: moderate range
            // X: can nod up/down
            // Y: can turn left/right
            // Z: can tilt left/right
            return new RotationToleranceBounds(
                new Vector3(-60f, -90f, -45f),   // Lower bounds
                new Vector3(60f, 90f, 45f)       // Upper bounds
            );
        }
        else if (typeName.Contains("Wrist") || typeName.Contains("Ankle"))
        {
            // Wrist/Ankle: limited range
            // All axes have similar limited range
            return new RotationToleranceBounds(
                new Vector3(-30f, -30f, -30f),   // Lower bounds
                new Vector3(30f, 30f, 30f)       // Upper bounds
            );
        }
        else if (typeName.Contains("Jaw"))
        {
            // Jaw: asymmetric limits for opening/closing
            // Upper bound: (90, 15, 5) - maximum opening
            // Lower bound: (135, -15, -5) - closed position
            // Note: These are relative to current rotation, so we use the difference
            // Upper (open): can rotate down 90 degrees on X, 15 on Y, 5 on Z
            // Lower (closed): can rotate up to 135 degrees on X (from closed), -15 on Y, -5 on Z
            return new RotationToleranceBounds(
                new Vector3(-135f, -15f, -5f),   // Lower bounds (more closed)
                new Vector3(90f, 15f, 5f)        // Upper bounds (more open)
            );
        }

        // Default: symmetric 45 degrees
        return new RotationToleranceBounds(
            new Vector3(-45f, -45f, -45f),
            new Vector3(45f, 45f, 45f)
        );
    }
}
