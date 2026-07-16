using System;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>Enter slow-time gambit selection mode.</summary>
[Serializable]
public sealed class NarrativeEnterSlowTimeGambitAction : NarrativeActionSpec
{
    public string sessionKey = "gambit.session";
    public PathingApertureMode modeFilter = PathingApertureMode.Either;
    public string tagFilter = "";
    [Range(0f, 1f)] public float timeScaleCoefficient = 0.25f;
    [Range(0f, 1f)] public float enforcement01 = 1f;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!TryGetSession(ctx, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        session.Begin(modeFilter, string.IsNullOrEmpty(tagFilter) ? null : tagFilter, timeScaleCoefficient, enforcement01);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    internal static bool TryGetSession(NarrativeExecutionContext ctx, string key, out GambitSelectionSession session)
    {
        session = null;
        if (ctx == null || !ctx.TryResolveGameObject(key, out var go) || go == null)
            return false;
        session = go.GetComponent<GambitSelectionSession>();
        if (session == null)
            session = go.GetComponentInChildren<GambitSelectionSession>();
        return session != null;
    }

    bool TryGetSession(NarrativeExecutionContext ctx, out GambitSelectionSession session)
        => TryGetSession(ctx, sessionKey, out session);
}

/// <summary>Running until mouse confirm selects a gambit aperture (or cancel/timeout).</summary>
[Serializable]
public sealed class NarrativeChooseGambitApertureAction : NarrativeActionSpec
{
    public string sessionKey = "gambit.session";
    public string selectedApertureKey = "gambit.selectedAperture";
    public string agentKey = "agent";
    public string apertureRegistryKey = "gambit.registry";
    [Range(0f, 1f)] public float enforcement01 = 1f;
    public bool requirePlayerConfirm = true;
    public float timeoutUnscaledSeconds = 0f;

    [NonSerialized] bool _started;
    [NonSerialized] float _elapsed;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeGambitAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        if (!_started)
        {
            _started = true;
            _elapsed = 0f;
            session.requirePlayerConfirm = requirePlayerConfirm;
            session.enforcement01 = Mathf.Clamp01(enforcement01);
            if (session.candidates.Count == 0 && ctx.TryResolveGameObject(apertureRegistryKey, out var regGo))
            {
                var reg = regGo.GetComponent<PathingApertureRegistry>();
                if (reg != null)
                {
                    session.registry = reg;
                    session.candidates.AddRange(reg.Query(PathingApertureMode.Either));
                }
            }
            if (!requirePlayerConfirm && session.candidates.Count > 0)
            {
                session.SetHovered(session.candidates[0]);
                session.TryConfirmHovered();
                BindSelected(ctx, session);
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
        if (buf != null && buf.TryConsume(out var kind, out var aperture))
        {
            if (kind == GambitInputTriggerKind.MouseScan && aperture != null)
                session.SetHovered(aperture);
            else if (kind == GambitInputTriggerKind.MouseClickConfirm)
            {
                if (aperture != null)
                    session.SetHovered(aperture);
                if (session.TryConfirmHovered())
                {
                    BindSelected(ctx, session);
                    _started = false;
                    return Locomotion.Narrative.BehaviorTreeStatus.Success;
                }
                return Locomotion.Narrative.BehaviorTreeStatus.Running;
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

    void BindSelected(NarrativeExecutionContext ctx, GambitSelectionSession session)
    {
        if (session.selectedAperture == null || ctx?.bindings == null)
            return;
        // Best-effort: append binding entry for selected aperture GameObject.
        var bindings = ctx.bindings.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null && bindings[i].key == selectedApertureKey)
            {
                bindings[i].value = session.selectedAperture.gameObject;
                ctx.bindings.RebuildIndex();
                return;
            }
        }
        bindings.Add(new NarrativeBindings.BindingEntry
        {
            key = selectedApertureKey,
            value = session.selectedAperture.gameObject
        });
        ctx.bindings.RebuildIndex();
    }
}

/// <summary>Commit chosen aperture to TravelAgent and apply steering enforcement.</summary>
[Serializable]
public sealed class NarrativeCommitGambitPathAction : NarrativeActionSpec
{
    public string sessionKey = "gambit.session";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!NarrativeEnterSlowTimeGambitAction.TryGetSession(ctx, sessionKey, out var session))
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        return session.CommitToTravelAgent()
            ? Locomotion.Narrative.BehaviorTreeStatus.Success
            : Locomotion.Narrative.BehaviorTreeStatus.Failure;
    }
}
