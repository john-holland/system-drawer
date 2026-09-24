using System;
using UnityEngine;

/// <summary>One working-joint layout from RadialSlotMath.SolveWorkingJoints.</summary>
[Serializable]
public sealed class RadialSolvedConfig
{
    public int count = 1;
    public float radius;
    public float startAngleDeg;
    public float wrapAngleDeg = 360f;
    public RadialJoinKind joinKind = RadialJoinKind.Natural;
    public int sidePoseIndex;
    public float score;
    public bool matchesStartPostAnchor;
    public string label = "";

    public string DisplayLabel()
    {
        if (!string.IsNullOrEmpty(label))
            return label;
        return $"{count} @ {radius:0.##}m  start {startAngleDeg:0.#}°  wrap {wrapAngleDeg:0.#}°  {joinKind}";
    }
}
