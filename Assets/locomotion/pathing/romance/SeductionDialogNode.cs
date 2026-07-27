using System;
using UnityEngine;

/// <summary>Stub seduction dialog BT — pickup / letdown / day-to-day lines scaled by RomanceProfile.</summary>
public sealed class SeductionDialogNode : BehaviorTreeNode
{
    public enum LineKind { Pickup, Letdown, DayToDay, SexualTalk, Argue }

    public LineKind kind = LineKind.Pickup;
    public RomanceProfile profile;
    public string lastLine;
    public float duration = 0.5f;
    float _t;

    static readonly string[] Pickups =
    {
        "Have we met under better stars?",
        "Your thoughts keep finding me.",
        "Walk with me a while?"
    };
    static readonly string[] Letdowns =
    {
        "You're sweet — I'm not there yet.",
        "Let's keep this friendly.",
        "Not tonight."
    };
    static readonly string[] DayToDay =
    {
        "How was your morning?",
        "I saved you a seat.",
        "Tell me something small."
    };

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (profile == null && tree != null)
            profile = tree.GetComponent<RomanceProfile>();
        lastLine = PickLine();
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        return _t >= duration ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }

    string PickLine()
    {
        float x = profile != null ? profile.Explicitness01() : 0.2f;
        switch (kind)
        {
            case LineKind.Letdown:
                return Letdowns[Mathf.Clamp(Mathf.FloorToInt(x * Letdowns.Length), 0, Letdowns.Length - 1)];
            case LineKind.DayToDay:
                return DayToDay[Mathf.Clamp(Mathf.FloorToInt(x * DayToDay.Length), 0, DayToDay.Length - 1)];
            case LineKind.SexualTalk:
                return x > 0.6f ? "…about us, closer." : "I like being near you.";
            case LineKind.Argue:
                return "We need to talk about boundaries.";
            default:
                return Pickups[Mathf.Clamp(Mathf.FloorToInt(x * Pickups.Length), 0, Pickups.Length - 1)];
        }
    }
}

/// <summary>Interaction event tags for romance dialog / BT hooks.</summary>
[Serializable]
public sealed class RomanceInteractionEvent
{
    public string tag = "romance.dialog";
    public SeductionDialogNode.LineKind lineKind = SeductionDialogNode.LineKind.DayToDay;
    public RomanceGroupDynamics groupDynamics = RomanceGroupDynamics.DullRampantAcceptance;
}
