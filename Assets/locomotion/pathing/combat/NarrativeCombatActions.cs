using System;
using UnityEngine;
using Locomotion.Narrative;

[Serializable]
public sealed class NarrativeEnterSlowTimeCombatAction : NarrativeActionSpec
{
    public string sessionKey = "combat.session";
    public string considerKey = "combat.consider";
    public string targetKey = "combat.target";
    [Range(0f, 1f)] public float timeScaleCoefficient = 0.28f;
    public CombatMode mode = CombatMode.Melee;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        session.mode = mode;
        if (ctx.TryResolveGameObject(targetKey, out var target))
            session.target = target;
        var pool = new System.Collections.Generic.List<CombatCard>();
        if (ctx.TryResolveGameObject(considerKey, out var considerGo))
        {
            var consider = considerGo.GetComponent<ConsiderCombatCards>();
            if (consider != null)
            {
                consider.mode = mode;
                var cards = consider.GenerateCards(session.target);
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i] is CombatCard cc) pool.Add(cc);
            }
        }
        session.Begin(pool, timeScaleCoefficient);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    internal static bool TryGetSession(NarrativeExecutionContext ctx, string key, out CombatCardSelectionSession session)
    {
        session = null;
        if (ctx == null || !ctx.TryResolveGameObject(key, out var go) || go == null) return false;
        session = go.GetComponent<CombatCardSelectionSession>()
                  ?? go.GetComponentInChildren<CombatCardSelectionSession>();
        return session != null;
    }
}

[Serializable]
public sealed class NarrativeChooseCombatCardAction : NarrativeActionSpec
{
    public string sessionKey = "combat.session";
    public float timeoutUnscaledSeconds;
    public bool requirePlayerConfirm = true;
    [NonSerialized] bool _started;
    [NonSerialized] float _elapsed;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeCombatAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        if (!_started)
        {
            _started = true;
            _elapsed = 0f;
            session.requirePlayerConfirm = requirePlayerConfirm;
            if (!requirePlayerConfirm && session.candidates.Count > 0)
            {
                session.SetHovered(session.candidates[0]);
                session.TryConfirmHovered();
            }
        }
        _elapsed += Time.unscaledDeltaTime;
        if (session.selectedCard != null)
        {
            _started = false;
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        }
        if (timeoutUnscaledSeconds > 0f && _elapsed >= timeoutUnscaledSeconds)
        {
            if (session.hoveredCard != null) session.TryConfirmHovered();
            _started = false;
            return session.selectedCard != null
                ? Locomotion.Narrative.BehaviorTreeStatus.Success
                : Locomotion.Narrative.BehaviorTreeStatus.Failure;
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

[Serializable]
public sealed class NarrativeCommitCombatCardAction : NarrativeActionSpec
{
    public string sessionKey = "combat.session";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeCombatAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        session.Commit();
        return session.selectedCard != null
            ? Locomotion.Narrative.BehaviorTreeStatus.Success
            : Locomotion.Narrative.BehaviorTreeStatus.Failure;
    }
}
