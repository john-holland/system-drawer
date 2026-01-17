using UnityEngine;

/// <summary>
/// Individual muscle component that applies force/torque to joints.
/// Each muscle is attached to a joint and can be activated with a strength value (0-1).
/// </summary>
[RequireComponent(typeof(ConfigurableJoint))]
public class Muscle : MonoBehaviour
{
    [Header("Muscle Properties")]
    [Tooltip("Maximum force this muscle can apply (Newtons)")]
    public float maxForce = 1000f;

    [Tooltip("Maximum torque this muscle can apply (Newton-meters)")]
    public float maxTorque = 500f;

    [Header("Activation")]
    [Tooltip("Current activation level (0-1)")]
    [Range(0f, 1f)]
    public float activation = 0f;

    [Header("Activation Curve")]
    [Tooltip("Force curve over activation level")]
    public AnimationCurve activationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Joint Configuration")]
    [Tooltip("Joint this muscle affects")]
    public ConfigurableJoint attachedJoint;

    [Tooltip("Force application mode")]
    public ForceMode forceMode = ForceMode.Force;

    // Internal state
    private Rigidbody targetRigidbody;
    private Vector3 currentForce;
    private Vector3 currentTorque;

    private void Awake()
    {
        // Get joint if not set
        if (attachedJoint == null)
        {
            attachedJoint = GetComponent<ConfigurableJoint>();
        }

        // Get target rigidbody from joint
        if (attachedJoint != null)
        {
            targetRigidbody = attachedJoint.GetComponent<Rigidbody>();
        }

        // Create default activation curve if not set
        if (activationCurve == null || activationCurve.length == 0)
        {
            activationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    private void FixedUpdate()
    {
        // Apply force every physics update
        ApplyForce();
    }

    /// <summary>
    /// Activate muscle with specified strength (0-1).
    /// </summary>
    public void Activate(float strength)
    {
        activation = Mathf.Clamp01(strength);
    }

    /// <summary>
    /// Apply force to joint based on current activation.
    /// </summary>
    public void ApplyForce()
    {
        if (targetRigidbody == null || attachedJoint == null)
            return;

        // Calculate force based on activation curve
        float curveValue = activationCurve.Evaluate(activation);
        float forceMagnitude = maxForce * curveValue;
        float torqueMagnitude = maxTorque * curveValue;

        // Calculate force direction (toward joint target rotation/position)
        Vector3 forceDirection = GetForceDirection();
        Vector3 torqueDirection = GetTorqueDirection();

        // Apply force
        currentForce = forceDirection * forceMagnitude;
        if (currentForce.magnitude > 0.001f)
        {
            targetRigidbody.AddForce(currentForce, forceMode);
        }

        // Apply torque
        currentTorque = torqueDirection * torqueMagnitude;
        if (currentTorque.magnitude > 0.001f)
        {
            targetRigidbody.AddTorque(currentTorque, forceMode);
        }
    }

    /// <summary>
    /// Get current force being applied by this muscle.
    /// </summary>
    public Vector3 GetCurrentForce()
    {
        return currentForce;
    }

    /// <summary>
    /// Get current torque being applied by this muscle.
    /// </summary>
    public Vector3 GetCurrentTorque()
    {
        return currentTorque;
    }

    /// <summary>
    /// Calculate force direction based on joint configuration.
    /// This is a simplified version - in practice, this would be more sophisticated.
    /// </summary>
    private Vector3 GetForceDirection()
    {
        if (attachedJoint == null)
            return Vector3.zero;

        // For ConfigurableJoint, we typically want to apply force toward target rotation
        // This is a simplified implementation - real muscle direction would depend on joint type
        // and anatomical attachment points
        
        // Get target rotation if configured
        if (attachedJoint.targetRotation != Quaternion.identity)
        {
            Vector3 targetDirection = (attachedJoint.targetRotation * Vector3.forward);
            return targetDirection.normalized;
        }

        // Fallback: use joint axis
        return attachedJoint.axis.normalized;
    }

    /// <summary>
    /// Calculate torque direction based on joint configuration.
    /// </summary>
    private Vector3 GetTorqueDirection()
    {
        if (attachedJoint == null)
            return Vector3.zero;

        // Similar to force direction, but for rotational torque
        // Typically around the joint's primary axis
        return attachedJoint.axis.normalized;
    }

    /// <summary>
    /// Get joint state for ragdoll state representation.
    /// </summary>
    public JointState GetJointState()
    {
        JointState state = new JointState();

        if (targetRigidbody != null)
        {
            state.position = targetRigidbody.position;
            state.rotation = targetRigidbody.rotation;
            state.angularVelocity = targetRigidbody.angularVelocity;
        }

        if (attachedJoint != null)
        {
            state.jointType = "ConfigurableJoint";
            state.axis = attachedJoint.axis;
            state.targetRotation = attachedJoint.targetRotation;
        }

        return state;
    }

    /// <summary>
    /// Deactivate muscle (set activation to 0).
    /// </summary>
    public void Deactivate()
    {
        activation = 0f;
    }

    /// <summary>
    /// Check if muscle is currently active (activation > threshold).
    /// </summary>
    public bool IsActive(float threshold = 0.01f)
    {
        return activation > threshold;
    }
}

/// <summary>
/// Joint state representation for ragdoll state tracking.
/// </summary>
[System.Serializable]
public class JointState
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 angularVelocity;
    public string jointType;
    public Vector3 axis;
    public Quaternion targetRotation;
}
