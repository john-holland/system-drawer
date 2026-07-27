using UnityEngine;

/// <summary>
/// Executes a WrestlingCard: SolveForGoal → topology BeginLock → impulse stack → EndExchange.
/// </summary>
public class WrestleObjectNode : BehaviorTreeNode
{
    [Header("Wrestling")]
    [Tooltip("Card to execute. If null, solver picks GoalType.Wrestling / selectedWrestlingCard binding.")]
    public WrestlingCard wrestlingCard;

    public WrestlingTopologyRuntime topology;
    public float maxExecuteSeconds = 2.5f;

    bool _started;
    GoodSection _active;
    float _elapsed;

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _active = null;
        _elapsed = 0f;
        status = BehaviorTreeStatus.Running;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_active != null)
        {
            _active.Stop();
            _active = null;
        }
        if (topology != null)
            topology.EndExchange();
        _started = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GameObject targetObj = tree?.currentGoal?.target;
        WrestlingCard card = wrestlingCard;

        if (card == null && tree != null)
        {
            var session = tree.GetComponent<WrestlingCardSelectionSession>();
            if (session != null && session.selectedCard != null)
                card = session.selectedCard;
        }

        if (card == null && tree?.currentGoal != null && tree.currentGoal.type == GoalType.Wrestling)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c is WrestlingCard wc)
                    {
                        card = wc;
                        break;
                    }
                    if (c != null && c.isWrestlingGoal)
                    {
                        card = c as WrestlingCard;
                        if (card != null) break;
                    }
                }
            }
        }

        if (card == null)
            return BehaviorTreeStatus.Failure;

        if (targetObj == null)
            targetObj = card.opponent;
        if (targetObj == null)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state) || !card.MeetsWrestlingRequirements(ragdoll.gameObject, targetObj, ragdoll))
                return BehaviorTreeStatus.Failure;

            if (topology == null)
                topology = ragdoll.GetComponent<WrestlingTopologyRuntime>();
            if (topology == null)
                topology = ragdoll.gameObject.AddComponent<WrestlingTopologyRuntime>();

            topology.BeginLock(ragdoll.gameObject, targetObj, card);
            if (card.mode == WrestlingMode.Pin)
                topology.UpdatePin(Vector3.up, null);

            card.Execute(state);
            _active = card;
            _started = true;
            _elapsed = 0f;
        }

        _elapsed += Time.deltaTime;
        RagdollState current = ragdoll.GetCurrentState();
        bool still = _active != null && _active.Update(current, Time.deltaTime);

        if (!still || _elapsed >= maxExecuteSeconds)
        {
            if (topology != null)
                topology.EndExchange();
            _active = null;
            _started = false;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }
}
