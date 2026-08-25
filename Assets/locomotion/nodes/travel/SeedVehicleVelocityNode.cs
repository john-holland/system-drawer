using UnityEngine;

/// <summary>Applies first-segment chassis velocity, preferring DimensionalLemmaVelocityBridge.</summary>
public sealed class SeedVehicleVelocityNode : BehaviorTreeNode
{
    public Vector3 linearVelocity;
    public Vector3 angularVelocity;
    public Rigidbody body;
    public DimensionalLemmaVelocityBridge velocityBridge;

    void Awake() => nodeType = NodeType.Action;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        var slot = new DimensionalPositionalSlot
        {
            hasVelocity = true,
            linearVelocity = linearVelocity,
            angularVelocity = angularVelocity
        };

        DimensionalLemmaVelocityBridge bridge = velocityBridge;
        if (bridge == null && tree != null)
            bridge = tree.GetComponentInParent<DimensionalLemmaVelocityBridge>();
        if (bridge == null)
            bridge = GetComponentInParent<DimensionalLemmaVelocityBridge>();
        if (bridge != null)
        {
            bridge.ApplyFrom(slot);
            return BehaviorTreeStatus.Success;
        }

        Rigidbody rb = body;
        if (rb == null && tree != null)
            rb = tree.GetComponentInParent<Rigidbody>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
        return BehaviorTreeStatus.Success;
    }
}
