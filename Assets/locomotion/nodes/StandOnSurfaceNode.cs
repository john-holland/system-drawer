using UnityEngine;

/// <summary>BT node: stand on a sit surface (e.g. stand on a chair).</summary>
public class StandOnSurfaceNode : BehaviorTreeNode
{
    [Header("Stand On")]
    public StandOnSurfaceCard standCard;
    public SitSurfaceContact surface;
    public Transform surfaceHost;
    public Vector3 surfaceWorldPoint;
    public Vector3 surfaceWorldNormal = Vector3.up;
    public float halfExtentX = 0.25f;
    public float halfExtentZ = 0.25f;

    bool _started;
    GoodSection _active;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        SitSurfaceContact contact = ResolveContact(tree);
        RagdollState state = ragdoll.GetCurrentState();
        StandOnSurfaceCard card = standCard ?? StandOnSurfaceCard.Generate(contact, state);
        card.BindSurface(contact);
        card.occupancyMode = SurfaceOccupancyMode.StandOn;

        if (!_started)
        {
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
            card.ApplyOccupancy(ragdoll.gameObject);
            var runtime = ragdoll.GetComponent<SeatedOccupancyRuntime>();
            if (runtime != null && runtime.cog != null)
            {
                runtime.cog.Evaluate(ragdoll.gameObject);
                if (runtime.cog.LastTipRisk01 > runtime.cog.tipRiskThreshold)
                    card.AppendCogRestore(runtime.cog, true);
            }
            card.Execute(state);
            _active = card;
            _started = true;
        }

        if (_active != null && _active.Update(ragdoll.GetCurrentState(), Time.deltaTime))
            return BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Success;
    }

    SitSurfaceContact ResolveContact(BehaviorTree tree)
    {
        if (surface != null)
            return surface;
        Transform host = surfaceHost;
        Vector3 point = surfaceWorldPoint;
        Vector3 normal = surfaceWorldNormal.sqrMagnitude > 1e-6f ? surfaceWorldNormal.normalized : Vector3.up;
        if (host == null && tree != null && tree.currentGoal != null && tree.currentGoal.target != null)
        {
            host = tree.currentGoal.target.transform;
            point = host.position;
        }
        return SitSurfaceContact.FromWorldPlane(host, point, normal, halfExtentX, halfExtentZ);
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _active = null;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_active != null)
        {
            _active.Stop();
            _active = null;
        }
        _started = false;
    }
}
