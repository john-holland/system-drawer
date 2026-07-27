using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>Enter slow-time love-making card selection.</summary>
[Serializable]
public sealed class NarrativeEnterSlowTimeLoveMakingAction : NarrativeActionSpec
{
    public string sessionKey = "lovemaking.session";
    public string considerKey = "lovemaking.consider";
    public string partnerKey = "lovemaking.partner";
    [Range(0f, 1f)] public float timeScaleCoefficient = 0.32f;
    public LoveMakingMode mode = LoveMakingMode.Tender;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        session.mode = mode;
        if (ctx.TryResolveGameObject(partnerKey, out var partner))
            session.partner = partner;

        var pool = new List<LoveCard>();
        if (ctx.TryResolveGameObject(considerKey, out var considerGo))
        {
            var consider = considerGo.GetComponent<ConsiderLoveMakingCards>();
            if (consider != null)
            {
                consider.mode = mode;
                var cards = consider.GenerateCards(session.partner);
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i] is LoveCard lc)
                        pool.Add(lc);
            }
        }

        session.Begin(pool, timeScaleCoefficient);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    internal static bool TryGetSession(NarrativeExecutionContext ctx, string key, out LoveMakingCardSelectionSession session)
    {
        session = null;
        if (ctx == null || !ctx.TryResolveGameObject(key, out var go) || go == null)
            return false;
        session = go.GetComponent<LoveMakingCardSelectionSession>()
                  ?? go.GetComponentInChildren<LoveMakingCardSelectionSession>();
        return session != null;
    }
}

[Serializable]
public sealed class NarrativeChooseLoveMakingCardAction : NarrativeActionSpec
{
    public string sessionKey = "lovemaking.session";
    public float timeoutUnscaledSeconds;
    public bool requirePlayerConfirm = true;

    [NonSerialized] bool _started;
    [NonSerialized] float _elapsed;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeLoveMakingAction.TryGetSession(ctx, sessionKey, out var session))
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
            if (session.hoveredCard != null)
                session.TryConfirmHovered();
            _started = false;
            return session.selectedCard != null
                ? Locomotion.Narrative.BehaviorTreeStatus.Success
                : Locomotion.Narrative.BehaviorTreeStatus.Failure;
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

[Serializable]
public sealed class NarrativeCommitLoveMakingCardAction : NarrativeActionSpec
{
    public string sessionKey = "lovemaking.session";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeLoveMakingAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        session.Commit();
        return session.selectedCard != null
            ? Locomotion.Narrative.BehaviorTreeStatus.Success
            : Locomotion.Narrative.BehaviorTreeStatus.Failure;
    }
}

[Serializable]
public sealed class NarrativeLoveMakingBioRhythmAction : NarrativeActionSpec
{
    public string actorKey = "actor";
    public string partnerKey = "partner";
    public bool queueLoveMakingGoal = true;
    public LoveMakingMode mode = LoveMakingMode.Tender;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(actorKey, out var actor) || actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        var sheet = LifeSystemsServices.Instance != null
            ? LifeSystemsServices.Instance.GetOrCreate(actor)
            : actor.GetComponent<LifeSystemsSheet>();
        sheet?.bioRhythm?.ApplyAmplitudeDelta(0.05f);
        sheet?.Adjust01(LifeSystemsChannelCatalog.Affection, 0.04f);

        if (queueLoveMakingGoal)
        {
            var bt = actor.GetComponent<BehaviorTree>();
            if (bt != null)
            {
                GameObject partner = null;
                ctx.TryResolveGameObject(partnerKey, out partner);
                bt.SetGoal(new BehaviorTreeGoal
                {
                    goalName = "lovemaking",
                    type = GoalType.LoveMaking,
                    target = partner,
                    priority = 6
                });
            }
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }
}
