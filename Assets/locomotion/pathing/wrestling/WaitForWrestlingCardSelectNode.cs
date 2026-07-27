using UnityEngine;

/// <summary>BT node: waits for wrestling card confirm using unscaled time (gambit clone).</summary>
public sealed class WaitForWrestlingCardSelectNode : BehaviorTreeNode
{
    public WrestlingCardSelectionSession session;
    public GambitInputTriggerBuffer inputBuffer;
    public float timeoutUnscaledSeconds;

    float _elapsed;

    public override void OnEnter(BehaviorTree tree)
    {
        _elapsed = 0f;
        status = BehaviorTreeStatus.Running;
        if (session == null && tree != null)
            session = tree.GetComponent<WrestlingCardSelectionSession>();
        if (inputBuffer == null && session != null)
            inputBuffer = session.inputBuffer;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (session == null)
            return BehaviorTreeStatus.Failure;

        _elapsed += Time.unscaledDeltaTime;
        if (timeoutUnscaledSeconds > 0f && _elapsed >= timeoutUnscaledSeconds)
        {
            session.Cancel();
            return BehaviorTreeStatus.Failure;
        }

        if (inputBuffer != null && inputBuffer.TryConsume(out var kind, out _))
        {
            if (kind == GambitInputTriggerKind.MouseClickConfirm)
            {
                if (session.selectedCard != null || session.TryConfirmHovered())
                {
                    session.Commit();
                    return BehaviorTreeStatus.Success;
                }
            }
            if (kind == GambitInputTriggerKind.MouseClickCancel)
            {
                session.Cancel();
                return BehaviorTreeStatus.Failure;
            }
        }

        return BehaviorTreeStatus.Running;
    }
}
