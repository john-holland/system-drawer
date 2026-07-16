using UnityEngine;

/// <summary>BT node: waits for a gambit input trigger using unscaled time awareness.</summary>
public sealed class WaitForGambitInputTriggerNode : BehaviorTreeNode
{
    public GambitInputTriggerBuffer inputBuffer;
    public GambitInputTriggerKind waitFor = GambitInputTriggerKind.MouseClickConfirm;
    public float timeoutUnscaledSeconds = 0f;

    float _elapsed;

    public override void OnEnter(BehaviorTree tree)
    {
        _elapsed = 0f;
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (inputBuffer == null)
            return BehaviorTreeStatus.Failure;

        _elapsed += Time.unscaledDeltaTime;
        if (timeoutUnscaledSeconds > 0f && _elapsed >= timeoutUnscaledSeconds)
            return BehaviorTreeStatus.Failure;

        if (!inputBuffer.TryConsume(out var kind, out _))
            return BehaviorTreeStatus.Running;

        if (kind == waitFor)
            return BehaviorTreeStatus.Success;
        if (kind == GambitInputTriggerKind.MouseClickCancel &&
            waitFor == GambitInputTriggerKind.MouseClickConfirm)
            return BehaviorTreeStatus.Failure;

        return BehaviorTreeStatus.Running;
    }
}
