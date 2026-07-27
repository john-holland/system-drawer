using UnityEngine;

/// <summary>
/// Executes a LoveCard: session/solver pick → topology BeginEmbrace (or KissingExecution for Kiss)
/// → impulse stack → psych + societal.
/// </summary>
public class LoveMakeObjectNode : BehaviorTreeNode
{
    public LoveCard loveCard;
    public LoveMakingTopologyRuntime topology;
    public LoveMakingSession session;
    public LoveMakingPlannerService planner;
    public HeavyPettingIKActorRegistry heavyPettingRegistry;
    public float maxExecuteSeconds = 4f;

    bool _started;
    bool _kissPath;
    GoodSection _active;
    float _elapsed;

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _kissPath = false;
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
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (_kissPath && ragdoll != null)
            KissingExecution.End(ragdoll.gameObject);
        else
            topology?.EndExchange();
        _started = false;
        _kissPath = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GameObject partner = tree?.currentGoal?.target;
        LoveCard card = loveCard;

        if (card == null && tree != null)
        {
            var sel = tree.GetComponent<LoveMakingCardSelectionSession>();
            if (sel != null && sel.selectedCard != null)
                card = sel.selectedCard;
        }

        if (card == null && tree?.currentGoal != null && tree.currentGoal.type == GoalType.LoveMaking)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] is LoveCard lc)
                    {
                        card = lc;
                        break;
                    }
                    if (cards[i] != null && cards[i].isLoveMakingGoal)
                    {
                        card = cards[i] as LoveCard;
                        if (card != null) break;
                    }
                }
            }
        }

        if (card == null)
            return BehaviorTreeStatus.Failure;
        if (partner == null)
            partner = card.opponent;
        if (partner == null)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state) || !card.MeetsLoveRequirements(ragdoll.gameObject, partner, ragdoll))
                return BehaviorTreeStatus.Failure;

            if (session == null)
                session = ragdoll.GetComponent<LoveMakingSession>()
                           ?? ragdoll.gameObject.AddComponent<LoveMakingSession>();
            if (session.participants.Count == 0)
                session.Begin(new[] { ragdoll.gameObject, partner }, session.timeBudgetSeconds, session.goals);

            session.activeCard = card;
            _kissPath = card.loveMoveKind == LoveMakingMoveKind.Kiss;
            if (_kissPath)
            {
                if (!KissingExecution.Begin(ragdoll.gameObject, partner, card, heavyPettingRegistry))
                    return BehaviorTreeStatus.Failure;
            }
            else
            {
                if (topology == null)
                    topology = ragdoll.GetComponent<LoveMakingTopologyRuntime>()
                                ?? ragdoll.gameObject.AddComponent<LoveMakingTopologyRuntime>();
                topology.BeginEmbrace(ragdoll.gameObject, partner, card);
            }

            card.Execute(state);
            _active = card;
            _started = true;
            _elapsed = 0f;
        }

        _elapsed += Time.deltaTime;
        if (_kissPath)
            KissingExecution.Tick(ragdoll.gameObject, Time.deltaTime);
        session?.Tick(Time.deltaTime);
        RagdollState current = ragdoll.GetCurrentState();
        bool still = _active != null && _active.Update(current, Time.deltaTime);

        if (!still || _elapsed >= maxExecuteSeconds || (session != null && session.AllRequiredGoalsMet()))
        {
            if (session != null && !session.psychApplied)
            {
                LoveMakingPsychEffectService.Apply(session, ragdoll.gameObject, partner, card);
                RomanceSocietalImpactService.ApplyLoveEvent(ragdoll.gameObject, partner, card, session);
                session.psychApplied = true;
            }
            if (_kissPath)
                KissingExecution.End(ragdoll.gameObject);
            else
                topology?.EndExchange();
            _active = null;
            _started = false;
            _kissPath = false;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }
}
