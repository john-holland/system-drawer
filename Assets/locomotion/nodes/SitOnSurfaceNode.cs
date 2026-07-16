using UnityEngine;

/// <summary>BT node: sit on a SitSurfaceContact with hierarchical CoG tow.</summary>
public class SitOnSurfaceNode : BehaviorTreeNode
{
    [Header("Sit")]
    public SitCard sitCard;
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
        SitCard card = sitCard;
        RagdollState state = ragdoll.GetCurrentState();

        if (card == null)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (tree.currentGoal != null && tree.currentGoal.type == GoalType.Sit && solver != null)
            {
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] is SitCard sc && !(cards[i] is StandOnSurfaceCard))
                    {
                        card = sc;
                        break;
                    }
                }
            }
            if (card == null)
                card = SitCard.GenerateSitCard(contact.WorldPlanePoint, contact.WorldPlaneNormal, state, ragdoll.gameObject);
        }

        card.BindSurface(contact);

        if (!_started)
        {
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
            card.ApplyOccupancy(ragdoll.gameObject);
            var runtime = ragdoll.GetComponent<SeatedOccupancyRuntime>();
            if (runtime != null && !runtime.feetReachGround)
            {
                var balance = SitBalanceCard.Generate(contact, state);
                balance.AppendCogRestore(runtime.cog, false);
                balance.ApplyOccupancy(ragdoll.gameObject);
                balance.Execute(state);
                _active = balance;
            }
            else
            {
                card.Execute(state);
                _active = card;
            }
            _started = true;
        }

        RagdollState cur = ragdoll.GetCurrentState();
        if (_active != null && _active.Update(cur, Time.deltaTime))
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
