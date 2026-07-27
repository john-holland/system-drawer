using System;
using UnityEngine;

/// <summary>Builds a BT subtree for narrative composition (parallel to NarrativeActionSpec).</summary>
[Serializable]
public abstract class NarrativeBehaviorSpec
{
    public abstract BehaviorTreeNode BuildSubtree();
}

[Serializable]
public sealed class BiteNarrativeBehavior : NarrativeBehaviorSpec
{
    public float duration = 0.25f;
    public override BehaviorTreeNode BuildSubtree() => new BiteNode { duration = duration };
}

[Serializable]
public sealed class ChewNarrativeBehavior : NarrativeBehaviorSpec
{
    public float duration = 0.6f;
    public override BehaviorTreeNode BuildSubtree() => new ChewNode { duration = duration };
}

[Serializable]
public sealed class SwallowNarrativeBehavior : NarrativeBehaviorSpec
{
    public float duration = 0.3f;
    public override BehaviorTreeNode BuildSubtree() => new SwallowNode { duration = duration };
}

[Serializable]
public sealed class AnimationChewNarrativeBehavior : NarrativeBehaviorSpec
{
    public string animationGroupTag = "eat.chew";
    public float duration = 1f;
    public override BehaviorTreeNode BuildSubtree() =>
        new AnimationChewNode { animationGroupTag = animationGroupTag, duration = duration };
}

/// <summary>Plays arbitrary animation tag while chewing.</summary>
public sealed class AnimationChewNode : BehaviorTreeNode
{
    public string animationGroupTag = "eat.chew";
    public float duration = 1f;
    public MouthInteriorRuntime mouth;
    float _t;
    EatingAnimationDriver _driver;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
        if (tree != null)
        {
            _driver = EatingAnimationDriver.FindOrCreate(tree.gameObject);
            _driver.PlayTag(animationGroupTag, duration);
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / Mathf.Max(1e-3f, duration));
        if (mouth != null)
        {
            var cat = EatingAnimationDriver.CategoryForTag(animationGroupTag);
            if (cat == PhysicsIKTrainingCategory.Bite)
                mouth.DriveFrontBite(Mathf.PingPong(u * 2f, 1f));
            else if (cat == PhysicsIKTrainingCategory.Swallow)
                mouth.DriveFrontBite(Mathf.Lerp(0.4f, 0.1f, u));
            else
                mouth.DriveMolarRoll(u, mouth.PreferRightChewSide);
        }
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
