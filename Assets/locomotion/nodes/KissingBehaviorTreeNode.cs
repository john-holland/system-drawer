using UnityEngine;

/// <summary>
/// BT leaf for LoveMakingMoveKind.Kiss: lip-midpoint IkTow via HeavyPettingIKActorRegistry.
/// </summary>
public class KissingBehaviorTreeNode : BehaviorTreeNode
{
    public LoveCard loveCard;
    public HeavyPettingIKActorRegistry registry;
    public LoveMakingSession session;
    public float maxExecuteSeconds = 4f;
    public float successLipDistanceMeters = 0.08f;

    bool _started;
    GoodSection _active;
    float _elapsed;
    LoveCard _card;
    GameObject _partner;

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _active = null;
        _elapsed = 0f;
        _card = null;
        _partner = null;
        status = BehaviorTreeStatus.Running;
    }

    public override void OnExit(BehaviorTree tree)
    {
        var ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (_active != null)
        {
            _active.Stop();
            _active = null;
        }
        if (ragdoll != null)
            KissingExecution.End(ragdoll.gameObject);
        _started = false;
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
                    if (cards[i] is LoveCard lc && lc.loveMoveKind == LoveMakingMoveKind.Kiss)
                    {
                        card = lc;
                        break;
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
            if (!KissingExecution.Begin(ragdoll.gameObject, partner, card, registry))
                return BehaviorTreeStatus.Failure;

            card.Execute(state);
            _active = card;
            _card = card;
            _partner = partner;
            _started = true;
            _elapsed = 0f;
        }

        _elapsed += Time.deltaTime;
        KissingExecution.Tick(ragdoll.gameObject, Time.deltaTime);
        session?.Tick(Time.deltaTime);
        RagdollState current = ragdoll.GetCurrentState();
        bool still = _active != null && _active.Update(current, Time.deltaTime);
        bool closeEnough = KissingExecution.LipDistance(ragdoll.gameObject) <= successLipDistanceMeters;

        if (!still || closeEnough || _elapsed >= maxExecuteSeconds ||
            (session != null && session.AllRequiredGoalsMet()))
        {
            if (session != null && !session.psychApplied)
            {
                LoveMakingPsychEffectService.Apply(session, ragdoll.gameObject, _partner, _card);
                RomanceSocietalImpactService.ApplyLoveEvent(ragdoll.gameObject, _partner, _card, session);
                session.psychApplied = true;
            }
            KissingExecution.End(ragdoll.gameObject);
            _active = null;
            _started = false;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }
}
