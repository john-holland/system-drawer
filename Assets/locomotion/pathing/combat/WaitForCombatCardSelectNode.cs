using UnityEngine;

public sealed class WaitForCombatCardSelectNode : BehaviorTreeNode
{
    public CombatCardSelectionSession session;
    public float timeoutUnscaledSeconds = 12f;
    float _elapsed;

    public override void OnEnter(BehaviorTree tree)
    {
        _elapsed = 0f;
        if (session == null && tree != null)
            session = tree.GetComponent<CombatCardSelectionSession>();
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (session == null) return BehaviorTreeStatus.Failure;
        _elapsed += Time.unscaledDeltaTime;
        var buffer = session.GetComponent<GambitInputTriggerBuffer>()
                     ?? (tree != null ? tree.GetComponent<GambitInputTriggerBuffer>() : null);
        if (buffer != null && buffer.TryConsume(out var kind, out _))
        {
            if (kind == GambitInputTriggerKind.MouseClickConfirm)
            {
                if (session.TryConfirmHovered())
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
        if (!session.requirePlayerConfirm && session.candidates.Count > 0)
        {
            if (session.hoveredCard == null)
                session.SetHovered(session.candidates[0]);
            session.TryConfirmHovered();
            session.Commit();
            return BehaviorTreeStatus.Success;
        }
        if (timeoutUnscaledSeconds > 0f && _elapsed >= timeoutUnscaledSeconds)
        {
            if (session.hoveredCard != null || session.candidates.Count > 0)
            {
                if (session.hoveredCard == null)
                    session.SetHovered(session.candidates[0]);
                session.TryConfirmHovered();
                session.Commit();
                return BehaviorTreeStatus.Success;
            }
            session.Cancel();
            return BehaviorTreeStatus.Failure;
        }
        return BehaviorTreeStatus.Running;
    }
}
