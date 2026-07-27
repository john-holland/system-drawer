using UnityEngine;

/// <summary>Executes CombatCard: topology, proxy fire, damage apply, wards.</summary>
public class CombatObjectNode : BehaviorTreeNode
{
    public CombatCard combatCard;
    public CombatTopologyRuntime topology;
    public CombatSession session;
    public CombatPlannerService planner;
    public SafetyLockWardenPlannerService safetyLock;
    public float maxExecuteSeconds = 3f;

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
        _active?.Stop();
        _active = null;
        topology?.EndExchange();
        _started = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null) return BehaviorTreeStatus.Failure;

        GameObject target = tree?.currentGoal?.target;
        CombatCard card = combatCard;
        if (card == null && tree != null)
        {
            var sel = tree.GetComponent<CombatCardSelectionSession>();
            if (sel != null) card = sel.selectedCard;
        }
        if (card == null && tree?.currentGoal != null && tree.currentGoal.type == GoalType.Combat)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var cards = solver.SolveForGoal(tree.currentGoal, ragdoll.GetCurrentState());
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i] is CombatCard cc) { card = cc; break; }
            }
        }
        if (card == null) return BehaviorTreeStatus.Failure;
        if (target == null) target = card.primaryTarget;
        if (target == null && card.combatMoveKind != CombatMoveKind.Reload && card.combatMoveKind != CombatMoveKind.Aim)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            if (!card.MeetsCombatRequirements(ragdoll.gameObject, target, ragdoll))
                return BehaviorTreeStatus.Failure;
            if (safetyLock == null)
                safetyLock = ragdoll.GetComponent<SafetyLockWardenPlannerService>();
            if (safetyLock != null && !safetyLock.GateFire(card))
                return BehaviorTreeStatus.Failure;

            if (topology == null)
                topology = ragdoll.GetComponent<CombatTopologyRuntime>()
                           ?? ragdoll.gameObject.AddComponent<CombatTopologyRuntime>();
            if (session == null)
                session = ragdoll.GetComponent<CombatSession>()
                          ?? ragdoll.gameObject.AddComponent<CombatSession>();
            if (session.participants.Count == 0)
                session.Begin(new[] { ragdoll.gameObject, target }, session.timeBudgetSeconds, session.goals);

            session.activeCard = card;
            var wards = ragdoll.GetComponent<DefendWardRuntime>()
                        ?? ragdoll.gameObject.AddComponent<DefendWardRuntime>();
            wards.SetWardsFromCard(card);

            if (card.combatMode == CombatMode.Cqc || card.combatMoveKind == CombatMoveKind.GrappleBreak)
                topology.BeginClinch(ragdoll.gameObject, target, card);

            card.instrumentProxy?.TryRoute(card, ragdoll.gameObject, Time.deltaTime);

            if (target != null &&
                (card.combatMoveKind == CombatMoveKind.Strike || card.combatMoveKind == CombatMoveKind.Slash ||
                 card.combatMoveKind == CombatMoveKind.Stab || card.combatMoveKind == CombatMoveKind.Fire ||
                 card.combatMoveKind == CombatMoveKind.Throw || card.combatMoveKind == CombatMoveKind.Suppress))
            {
                Vector3 hit = target.transform.position;
                Vector3 dir = (target.transform.position - ragdoll.transform.position).normalized;
                var evt = card.BuildDamageEvent(ragdoll.gameObject, target, hit, dir);
                var result = CombatDamageFamilyRouter.ApplyForCard(card, evt);
                if (result.ok)
                    session.damageDealt01 += result.appliedToActor01;
            }

            card.Execute(ragdoll.GetCurrentState());
            _active = card;
            _started = true;
            _elapsed = 0f;
        }

        _elapsed += Time.deltaTime;
        session?.Tick(Time.deltaTime);
        bool still = _active != null && _active.Update(ragdoll.GetCurrentState(), Time.deltaTime);
        if (!still || _elapsed >= maxExecuteSeconds || (session != null && session.AllRequiredGoalsMet()))
        {
            topology?.EndExchange();
            _active = null;
            _started = false;
            return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}
