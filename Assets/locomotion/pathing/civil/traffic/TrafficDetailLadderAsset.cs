using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrafficDetailEmitCard
{
    None = 0,
    CopLights = 1,
    OccupyIntersection = 2,
    PullOver = 3,
    TrafficJustice = 4,
    CopDetail = 5,
    Confirm = 6
}

[Serializable]
public sealed class TrafficDetailLadderStep
{
    public string stepId = "step";
    public float durationSec = 5f;
    public string lightLemmaOrPhase = "green";
    public string dispatchKind = "traffic_detail";
    public string notes;
    public TrafficDetailEmitCard emitCard = TrafficDetailEmitCard.None;
}

/// <summary>Scriptable ladder for police traffic detail (stage → lights → pullover → clear).</summary>
[CreateAssetMenu(fileName = "TrafficDetailLadder", menuName = "Locomotion/Civil/Traffic Detail Ladder")]
public sealed class TrafficDetailLadderAsset : ScriptableObject
{
    public List<TrafficDetailLadderStep> steps = new List<TrafficDetailLadderStep>();

    public static TrafficDetailLadderAsset CreateDefaultRuntime()
    {
        var a = CreateInstance<TrafficDetailLadderAsset>();
        a.steps = new List<TrafficDetailLadderStep>
        {
            new TrafficDetailLadderStep { stepId = "stage", durationSec = 2f, emitCard = TrafficDetailEmitCard.None, notes = "stage" },
            new TrafficDetailLadderStep { stepId = "lights_on", durationSec = 3f, lightLemmaOrPhase = "green", emitCard = TrafficDetailEmitCard.CopLights },
            new TrafficDetailLadderStep { stepId = "occupy_intersection", durationSec = 4f, emitCard = TrafficDetailEmitCard.OccupyIntersection },
            new TrafficDetailLadderStep { stepId = "pullover_window", durationSec = 8f, emitCard = TrafficDetailEmitCard.PullOver },
            new TrafficDetailLadderStep { stepId = "clear", durationSec = 3f, emitCard = TrafficDetailEmitCard.TrafficJustice },
            new TrafficDetailLadderStep { stepId = "confirm", durationSec = 1f, emitCard = TrafficDetailEmitCard.Confirm }
        };
        return a;
    }
}
