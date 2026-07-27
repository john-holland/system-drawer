using System;
using UnityEngine;
using Locomotion.Narrative;

[Serializable]
public sealed class BiteNarrativeAction : NarrativeActionSpec
{
    public string actorKey = "actor";
    public float duration = 0.25f;
    [NonSerialized] float _t;
    [NonSerialized] bool _started;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(actorKey, out var actor) || actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        var mouth = actor.GetComponent<MouthInteriorRuntime>() ?? actor.GetComponentInChildren<MouthInteriorRuntime>();
        if (mouth == null) mouth = actor.AddComponent<MouthInteriorRuntime>();
        if (!_started) { _started = true; _t = 0f; }
        _t += Time.deltaTime;
        mouth.jawOpen01 = Mathf.PingPong(_t * 4f, 1f);
        if (_t >= duration) { _started = false; return Locomotion.Narrative.BehaviorTreeStatus.Success; }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

[Serializable]
public sealed class ChewNarrativeAction : NarrativeActionSpec
{
    public string actorKey = "actor";
    public float duration = 0.6f;
    [NonSerialized] float _t;
    [NonSerialized] bool _started;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(actorKey, out var actor) || actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        var mouth = actor.GetComponent<MouthInteriorRuntime>() ?? actor.AddComponent<MouthInteriorRuntime>();
        if (!_started) { _started = true; _t = 0f; }
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / duration);
        mouth.jawOpen01 = 0.35f + 0.2f * Mathf.Sin(u * Mathf.PI * 4f);
        mouth.tongue?.SetFoodPocketLocal(ChewStrategy.TongueOffsetForMeat(u, mouth.PreferRightChewSide));
        if (_t >= duration) { _started = false; return Locomotion.Narrative.BehaviorTreeStatus.Success; }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

[Serializable]
public sealed class SwallowNarrativeAction : NarrativeActionSpec
{
    public string actorKey = "actor";
    public string foodKey = "food";
    public float duration = 0.3f;
    [NonSerialized] float _t;
    [NonSerialized] bool _started;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(actorKey, out var actor) || actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        var mouth = actor.GetComponent<MouthInteriorRuntime>() ?? actor.AddComponent<MouthInteriorRuntime>();
        FoodItem food = null;
        if (ctx.TryResolveGameObject(foodKey, out var foodGo))
            food = foodGo.GetComponent<FoodItem>();
        if (!_started) { _started = true; _t = 0f; }
        _t += Time.deltaTime;
        mouth.jawOpen01 = Mathf.Lerp(0.4f, 0.1f, Mathf.Clamp01(_t / duration));
        if (_t >= duration)
        {
            mouth.ClearFoodInMouth();
            if (food != null)
            {
                var proc = FoodProcessorBioRhythmService.Instance
                           ?? actor.GetComponent<FoodProcessorBioRhythmService>()
                           ?? actor.AddComponent<FoodProcessorBioRhythmService>();
                proc.OnSwallow(actor, food);
            }
            _started = false;
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}

[Serializable]
public sealed class AnimationChewNarrativeAction : NarrativeActionSpec
{
    public string actorKey = "actor";
    public string animationGroupTag = "eat.chew";
    public float duration = 1f;
    [NonSerialized] float _t;
    [NonSerialized] bool _started;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(actorKey, out var actor) || actor == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        var mouth = actor.GetComponent<MouthInteriorRuntime>() ?? actor.AddComponent<MouthInteriorRuntime>();
        if (!_started)
        {
            _started = true;
            _t = 0f;
            EatingAnimationDriver.FindOrCreate(actor).PlayTag(animationGroupTag, duration);
        }
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / Mathf.Max(1e-3f, duration));
        var cat = EatingAnimationDriver.CategoryForTag(animationGroupTag);
        if (cat == PhysicsIKTrainingCategory.Bite)
            mouth.DriveFrontBite(Mathf.PingPong(u * 2f, 1f));
        else if (cat == PhysicsIKTrainingCategory.Swallow)
            mouth.DriveFrontBite(Mathf.Lerp(0.4f, 0.1f, u));
        else
            mouth.DriveMolarRoll(u, mouth.PreferRightChewSide);
        if (_t >= duration) { _started = false; return Locomotion.Narrative.BehaviorTreeStatus.Success; }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }
}
