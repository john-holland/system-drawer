using UnityEngine;

/// <summary>BT stub: resolve ChefCard for GoalType.Cooking and execute / update.</summary>
[AddComponentMenu("Locomotion/Kitchen/Cook Duty Node")]
public sealed class CookDutyNode : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem ragdoll;
    public ChefCard boundCard;
    public float maxSeconds = 12f;
    float _elapsed;
    bool _running;

    public bool Begin(BehaviorTreeGoal goal)
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (ragdoll == null) ragdoll = GetComponent<RagdollSystem>();
        var state = ragdoll != null ? ragdoll.GetCurrentState() : new RagdollState();
        if (boundCard == null && cardSolver != null && goal != null)
        {
            var solved = cardSolver.SolveForGoal(goal, state);
            if (solved != null && solved.Count > 0 && solved[0] is ChefCard cc)
                boundCard = cc;
        }
        if (boundCard == null)
            boundCard = ConsiderChefCards.MakeDefaultCard();
        if (!boundCard.MeetsChefRequirements(gameObject, goal != null ? goal.target : null, ragdoll))
            return false;
        boundCard.Execute(state);
        _elapsed = 0f;
        _running = true;
        return true;
    }

    public bool Tick(float dt)
    {
        if (!_running || boundCard == null) return false;
        _elapsed += dt;
        var state = ragdoll != null ? ragdoll.GetCurrentState() : new RagdollState();
        ChefActivitySolvers.TrySolve(boundCard, gameObject, dt, out _);
        bool cont = boundCard.Update(state, dt);
        if (!cont || _elapsed >= maxSeconds)
        {
            boundCard.Stop();
            _running = false;
            KitchenBioRhythmService.Instance?.NotifyOrderTicket();
            return false;
        }
        return true;
    }
}
