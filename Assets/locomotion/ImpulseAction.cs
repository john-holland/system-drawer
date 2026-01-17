using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual impulse action in a good section's impulse stack.
/// Defines muscle activation with curves and conditions.
/// </summary>
[System.Serializable]
public class ImpulseAction
{
    [Header("Muscle Activation")]
    [Tooltip("Muscle group to activate")]
    public string muscleGroup;

    [Tooltip("Activation strength (0-1)")]
    [Range(0f, 1f)]
    public float activation = 0.5f;

    [Tooltip("Duration to maintain activation (0 = until next action)")]
    public float duration = 0f;

    [Header("Activation Curve")]
    [Tooltip("Activation curve over duration")]
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Conditions")]
    [Tooltip("Conditions that must be met for this action to execute")]
    public List<ImpulseCondition> conditions = new List<ImpulseCondition>();

    [Header("Force/Torque Direction")]
    [Tooltip("Direction to apply force (if needed)")]
    public Vector3 forceDirection = Vector3.zero;

    [Tooltip("Direction to apply torque (if needed)")]
    public Vector3 torqueDirection = Vector3.zero;

    // Execution state
    private float elapsedTime = 0f;
    private bool isExecuting = false;

    /// <summary>
    /// Execute this impulse action.
    /// </summary>
    public void Execute(RagdollState currentState)
    {
        // Check conditions
        if (!CheckConditions(currentState))
        {
            return;
        }

        isExecuting = true;
        elapsedTime = 0f;
    }

    /// <summary>
    /// Update this action (call every frame while executing).
    /// Returns true if action is still executing.
    /// </summary>
    public bool Update(RagdollState currentState, float deltaTime)
    {
        if (!isExecuting)
            return false;

        elapsedTime += deltaTime;

        // Check if duration exceeded (if duration > 0)
        if (duration > 0f && elapsedTime >= duration)
        {
            isExecuting = false;
            return false;
        }

        // Check conditions again
        if (!CheckConditions(currentState))
        {
            isExecuting = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if action is complete.
    /// </summary>
    public bool IsComplete()
    {
        if (duration <= 0f)
        {
            return !isExecuting; // No duration means execute once and complete
        }

        return elapsedTime >= duration;
    }

    /// <summary>
    /// Check all conditions for this action.
    /// </summary>
    public bool CheckConditions(RagdollState currentState)
    {
        if (conditions == null || conditions.Count == 0)
        {
            return true; // No conditions = always pass
        }

        foreach (var condition in conditions)
        {
            if (condition != null && !condition.Evaluate(currentState))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get current activation value (accounting for curve and elapsed time).
    /// </summary>
    public float GetCurrentActivation()
    {
        if (!isExecuting)
            return 0f;

        if (duration <= 0f)
        {
            return activation; // No duration = constant activation
        }

        // Evaluate curve based on elapsed time
        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
        float curveValue = curve != null ? curve.Evaluate(normalizedTime) : 1f;

        return activation * curveValue;
    }

    /// <summary>
    /// Stop executing this action.
    /// </summary>
    public void Stop()
    {
        isExecuting = false;
        elapsedTime = 0f;
    }

    /// <summary>
    /// Reset action state.
    /// </summary>
    public void Reset()
    {
        isExecuting = false;
        elapsedTime = 0f;
    }
}

/// <summary>
/// Condition that must be met for an impulse action to execute.
/// </summary>
[System.Serializable]
public class ImpulseCondition
{
    [Tooltip("Type of condition")]
    public ConditionType conditionType;

    [Tooltip("Target value or threshold")]
    public float targetValue;

    [Tooltip("Comparison operator")]
    public ComparisonOperator comparison;

    [Tooltip("Joint or muscle name (for joint/muscle conditions)")]
    public string targetName;

    /// <summary>
    /// Evaluate this condition against a ragdoll state.
    /// </summary>
    public bool Evaluate(RagdollState state)
    {
        float currentValue = GetCurrentValue(state);

        switch (comparison)
        {
            case ComparisonOperator.LessThan:
                return currentValue < targetValue;
            case ComparisonOperator.LessThanOrEqual:
                return currentValue <= targetValue;
            case ComparisonOperator.GreaterThan:
                return currentValue > targetValue;
            case ComparisonOperator.GreaterThanOrEqual:
                return currentValue >= targetValue;
            case ComparisonOperator.Equal:
                return Mathf.Approximately(currentValue, targetValue);
            case ComparisonOperator.NotEqual:
                return !Mathf.Approximately(currentValue, targetValue);
            default:
                return false;
        }
    }

    /// <summary>
    /// Get current value based on condition type.
    /// </summary>
    private float GetCurrentValue(RagdollState state)
    {
        switch (conditionType)
        {
            case ConditionType.JointAngle:
                JointState jointState = state.GetJointState(targetName);
                if (jointState != null)
                {
                    // Simplified: return angle from forward
                    return Vector3.Angle(jointState.rotation * Vector3.forward, Vector3.forward);
                }
                return 0f;

            case ConditionType.MuscleActivation:
                return state.GetMuscleActivation(targetName);

            case ConditionType.RootVelocity:
                return state.rootVelocity.magnitude;

            case ConditionType.RootAngularVelocity:
                return state.rootAngularVelocity.magnitude;

            case ConditionType.ContactCount:
                return state.contacts.Count;

            default:
                return 0f;
        }
    }
}

/// <summary>
/// Types of conditions for impulse actions.
/// </summary>
public enum ConditionType
{
    JointAngle,
    MuscleActivation,
    RootVelocity,
    RootAngularVelocity,
    ContactCount
}

/// <summary>
/// Comparison operators for conditions.
/// </summary>
public enum ComparisonOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equal,
    NotEqual
}
