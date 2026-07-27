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

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (mouth == null && tree != null)
            mouth = tree.GetComponent<MouthInteriorRuntime>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (mouth != null)
            mouth.jawOpen01 = 0.35f + 0.2f * Mathf.Sin(_t * Mathf.PI * 4f);
        // Animation group tag is available for IK training / ABT selection.
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
