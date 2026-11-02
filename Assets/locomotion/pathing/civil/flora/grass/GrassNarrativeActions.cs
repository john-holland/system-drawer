using Locomotion.Narrative;
using UnityEngine;

[System.Serializable]
public class GrassCutAction : NarrativeActionSpec
{
    public GroundCompositionSet composition;
    public string bladeId = "blade";
    public float angleDeg;
    public float radius = 1f;
    public float ribbonWidth;
    public bool useSpline;
    public float shedSize = 0.1f;
    public NarrativeDateTime at;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply(ctx);
    }

    public override Locomotion.Narrative.BehaviorTreeStatus ApplyFastForward(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply(ctx);
    }

    Locomotion.Narrative.BehaviorTreeStatus Apply(NarrativeExecutionContext ctx)
    {
        if (composition == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        composition.RecordCut(bladeId, angleDeg, radius, ribbonWidth, useSpline, shedSize, Seconds(ctx));
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    float Seconds(NarrativeExecutionContext ctx)
    {
        if (at.year > 0)
            return NarrativeCalendarMath.DateTimeToSeconds(at);
        return ctx != null && ctx.clock != null ? NarrativeCalendarMath.DateTimeToSeconds(ctx.clock.Now) : 0f;
    }
}

[System.Serializable]
public class GrassShedAction : NarrativeActionSpec
{
    public GroundCompositionSet composition;
    public GrassTypeDef grassType;
    public string bladeId = "blade";
    public float shedSize = -1f;
    public NarrativeDateTime at;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply(ctx);
    }

    public override Locomotion.Narrative.BehaviorTreeStatus ApplyFastForward(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply(ctx);
    }

    Locomotion.Narrative.BehaviorTreeStatus Apply(NarrativeExecutionContext ctx)
    {
        if (composition == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        float size = shedSize >= 0f ? shedSize : grassType != null ? grassType.shedSize : 0.08f;
        float seconds = at.year > 0
            ? NarrativeCalendarMath.DateTimeToSeconds(at)
            : ctx != null && ctx.clock != null ? NarrativeCalendarMath.DateTimeToSeconds(ctx.clock.Now) : 0f;
        composition.RecordShed(bladeId, size, seconds);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }
}

[System.Serializable]
public class RampantRegrowthAction : NarrativeActionSpec
{
    public GroundCompositionSet composition;
    public GrassTravelAgent agent;
    public Vector3 center;
    public Vector3 size = Vector3.one * 4f;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply();
    }

    public override Locomotion.Narrative.BehaviorTreeStatus ApplyFastForward(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply();
    }

    Locomotion.Narrative.BehaviorTreeStatus Apply()
    {
        if (composition != null)
            composition.MarkRampant(new Bounds(center, size));
        if (agent != null)
        {
            agent.AdvanceThrough(GrassDevStepKind.RhizomeNode);
            agent.AdvanceThrough(GrassDevStepKind.Tiller);
        }
        return composition != null || agent != null ? Locomotion.Narrative.BehaviorTreeStatus.Success : Locomotion.Narrative.BehaviorTreeStatus.Failure;
    }
}

[System.Serializable]
public class AreaResetAction : NarrativeActionSpec
{
    public GroundCompositionSet composition;
    public float narrativeSeconds;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply();
    }

    public override Locomotion.Narrative.BehaviorTreeStatus ApplyFastForward(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        return Apply();
    }

    Locomotion.Narrative.BehaviorTreeStatus Apply()
    {
        if (composition == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        composition.ClearRampant();
        composition.RestoreSnapshot(narrativeSeconds);
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }
}
