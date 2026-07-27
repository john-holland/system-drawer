using System;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>Enter slow-time wrestling card selection.</summary>
[Serializable]
public sealed class NarrativeEnterSlowTimeWrestlingAction : NarrativeActionSpec
{
    public string sessionKey = "wrestling.session";
    public string considerKey = "wrestling.consider";
    public string opponentKey = "wrestling.opponent";
    [Range(0f, 1f)] public float timeScaleCoefficient = 0.28f;
    public WrestlingMode mode = WrestlingMode.Play;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        session.mode = mode;
        if (ctx.TryResolveGameObject(opponentKey, out var opp))
            session.opponent = opp;

        var pool = new System.Collections.Generic.List<WrestlingCard>();
        if (ctx.TryResolveGameObject(considerKey, out var considerGo))
        {
            var consider = considerGo.GetComponent<ConsiderWrestlingCards>();
            if (consider != null)
            {
                consider.mode = mode;
                var cards = consider.GenerateCards(session.opponent);
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i] is WrestlingCard wc)
                        pool.Add(wc);
            }
        }

        session.Begin(pool, timeScaleCoefficient);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    internal static bool TryGetSession(NarrativeExecutionContext ctx, string key, out WrestlingCardSelectionSession session)
    {
        session = null;
        if (ctx == null || !ctx.TryResolveGameObject(key, out var go) || go == null)
            return false;
        session = go.GetComponent<WrestlingCardSelectionSession>()
                  ?? go.GetComponentInChildren<WrestlingCardSelectionSession>();
        return session != null;
    }
}

/// <summary>Running until confirm selects a WrestlingCard (or cancel/timeout).</summary>
[Serializable]
public sealed class NarrativeChooseWrestlingCardAction : NarrativeActionSpec
{
    public string sessionKey = "wrestling.session";
    public float timeoutUnscaledSeconds;
    public bool requirePlayerConfirm = true;

    [NonSerialized] bool _started;
    [NonSerialized] float _elapsed;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeWrestlingAction.TryGetSession(ctx, sessionKey, out var session))
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
                _started = false;
                return Locomotion.Narrative.BehaviorTreeStatus.Success;
            }
        }

        _elapsed += Time.unscaledDeltaTime;
        if (timeoutUnscaledSeconds > 0f && _elapsed >= timeoutUnscaledSeconds)
        {
            _started = false;
            session.Cancel();
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        }

        var buf = session.inputBuffer;
        if (buf != null && buf.TryConsume(out var kind, out _))
        {
            if (kind == GambitInputTriggerKind.MouseClickConfirm)
            {
                if (session.selectedCard != null || session.TryConfirmHovered())
                {
                    _started = false;
                    return Locomotion.Narrative.BehaviorTreeStatus.Success;
                }
            }
            else if (kind == GambitInputTriggerKind.MouseClickCancel)
            {
                session.Cancel();
                _started = false;
                return Locomotion.Narrative.BehaviorTreeStatus.Failure;
            }
        }

        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

/// <summary>Commit chosen WrestlingCard into WrestleObjectNode / planner binding.</summary>
[Serializable]
public sealed class NarrativeCommitWrestlingCardAction : NarrativeActionSpec
{
    public string sessionKey = "wrestling.session";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeWrestlingAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        return session.Commit()
            ? Locomotion.Narrative.BehaviorTreeStatus.Success
            : Locomotion.Narrative.BehaviorTreeStatus.Failure;
    }
}

/// <summary>Spike bio-rhythm / LifeSystems on match beats; optionally queue Wrestling BT goal.</summary>
[Serializable]
public sealed class NarrativeWrestlingBioRhythmAction : NarrativeActionSpec
{
    public WrestlingMode mode = WrestlingMode.Play;
    public float bioRhythmAmplitudeDelta = 0.15f;
    public float adrenalineChannelDelta = 0.2f;
    public float durationSeconds = 4f;
    public string actorKey = "agent";
    public string opponentKey = "wrestling.opponent";
    public bool queueWrestlingGoal = true;
    public string behaviorTreeActorKey = "agent";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;

        GameObject actor = null;
        if (ctx != null)
            ctx.TryResolveGameObject(actorKey, out actor);
        if (actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        var life = LifeSystemsServices.Instance;
        var sheet = life != null ? life.GetOrCreate(actor) : actor.GetComponent<LifeSystemsSheet>();
        if (sheet == null)
            sheet = actor.AddComponent<LifeSystemsSheet>();

        var spec = new LifeSystemsEffectSpec
        {
            source = LifeSystemsEffectSource.Dev,
            promptLabel = $"wrestling_{mode}",
            durationSeconds = durationSeconds,
            bioRhythmAmplitudeDelta = bioRhythmAmplitudeDelta,
            channelDeltas = new System.Collections.Generic.List<LifeSystemsChannelDelta>
            {
                new LifeSystemsChannelDelta
                {
                    channelId = LifeSystemsChannelCatalog.Adrenaline,
                    delta01 = adrenalineChannelDelta
                }
            }
        };

        if (life != null)
            life.ApplyEffect(sheet, spec);
        else
        {
            sheet.EnsureDefaults();
            sheet.bioRhythm.ApplyAmplitudeDelta(bioRhythmAmplitudeDelta);
            sheet.Adjust01(LifeSystemsChannelCatalog.Adrenaline, adrenalineChannelDelta);
        }

        if (queueWrestlingGoal && ctx != null &&
            ctx.TryResolveGameObject(behaviorTreeActorKey, out var btGo) && btGo != null)
        {
            var bt = btGo.GetComponent<BehaviorTree>();
            if (bt != null)
            {
                GameObject opp = null;
                ctx.TryResolveGameObject(opponentKey, out opp);
                var goal = new BehaviorTreeGoal
                {
                    goalName = $"wrestling_{mode}",
                    type = GoalType.Wrestling,
                    target = opp,
                    priority = 8
                };
                bt.SetGoal(goal);
            }
        }

        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }
}
