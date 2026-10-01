using System;
using System.Collections.Generic;
using UnityEngine;

public enum SewingFeedDirection
{
    Left = 0,
    Right = 1
}

public enum ClothThreadSide
{
    Top = 0,
    Bottom = 1
}

/// <summary>
/// Needle step role in a stitch program. Serger/overlock start = <see cref="Entry"/>,
/// finish / stop = <see cref="Exit"/> (angled entry/exit degrees still apply per step).
/// </summary>
public enum SewingNeedlePhase
{
    Entry = 0,
    Exit = 1
}

public enum SewingStitchClass
{
    Lockstitch = 0,
    Overlock = 1
}

[Serializable]
public sealed class SewingNeedleStep
{
    public int index;
    public int cellX;
    public int cellY;
    public SewingFeedDirection direction;
    public ClothThreadSide clothSide;
    public SewingNeedlePhase phase = SewingNeedlePhase.Entry;
    public float entryAngleDeg = 90f;
    public float exitAngleDeg = 90f;
    [Range(0f, 1f)] public float gauge01 = 0.4f;
    public bool connectingStrand;

    public float PitchM() => 0.04f + Mathf.Clamp01(gauge01) * 0.04f;

    /// <summary>True when entry and exit angles differ (non-orthogonal / angled needle path).</summary>
    public bool IsAngled => Mathf.Abs(entryAngleDeg - exitAngleDeg) > 0.5f;
}

[Serializable]
public sealed class SewingStitchProgram
{
    public SewingStitchClass stitchClass = SewingStitchClass.Lockstitch;
    [Range(0f, 1f)] public float defaultGauge01 = 0.4f;
    public List<SewingNeedleStep> steps = new List<SewingNeedleStep>();

    public static SewingStitchProgram DefaultLockstitch()
    {
        var p = new SewingStitchProgram
        {
            stitchClass = SewingStitchClass.Lockstitch,
            defaultGauge01 = 0.4f
        };
        p.steps.Add(new SewingNeedleStep
        {
            index = 0,
            cellX = 2,
            cellY = 3,
            direction = SewingFeedDirection.Left,
            clothSide = ClothThreadSide.Top,
            phase = SewingNeedlePhase.Entry,
            entryAngleDeg = 80f,
            exitAngleDeg = 100f,
            gauge01 = 0.4f
        });
        p.steps.Add(new SewingNeedleStep
        {
            index = 1,
            cellX = 3,
            cellY = 3,
            direction = SewingFeedDirection.Right,
            clothSide = ClothThreadSide.Bottom,
            phase = SewingNeedlePhase.Exit,
            entryAngleDeg = 100f,
            exitAngleDeg = 80f,
            gauge01 = 0.4f
        });
        return p;
    }

    public static SewingStitchProgram DefaultOverlock()
    {
        var p = new SewingStitchProgram
        {
            stitchClass = SewingStitchClass.Overlock,
            defaultGauge01 = 0.45f
        };
        // Start: angled needle entry into the edge.
        p.steps.Add(new SewingNeedleStep
        {
            index = 0,
            cellX = 2,
            cellY = 2,
            direction = SewingFeedDirection.Left,
            clothSide = ClothThreadSide.Top,
            phase = SewingNeedlePhase.Entry,
            entryAngleDeg = 55f,
            exitAngleDeg = 100f,
            gauge01 = 0.45f
        });
        // Mid: looper connecting strand (angled).
        p.steps.Add(new SewingNeedleStep
        {
            index = 1,
            cellX = 3,
            cellY = 2,
            direction = SewingFeedDirection.Right,
            clothSide = ClothThreadSide.Bottom,
            phase = SewingNeedlePhase.Entry,
            entryAngleDeg = 70f,
            exitAngleDeg = 110f,
            gauge01 = 0.45f,
            connectingStrand = true
        });
        // Stop: angled exit / finish of the overlock chain.
        p.steps.Add(new SewingNeedleStep
        {
            index = 2,
            cellX = 3,
            cellY = 3,
            direction = SewingFeedDirection.Right,
            clothSide = ClothThreadSide.Bottom,
            phase = SewingNeedlePhase.Exit,
            entryAngleDeg = 115f,
            exitAngleDeg = 50f,
            gauge01 = 0.45f,
            connectingStrand = true
        });
        return p;
    }

    public SewingNeedleStep StartStep =>
        steps != null && steps.Count > 0 ? steps[0] : null;

    public SewingNeedleStep StopStep =>
        steps != null && steps.Count > 0 ? steps[steps.Count - 1] : null;

    public void Reindex()
    {
        if (steps == null)
            steps = new List<SewingNeedleStep>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null) continue;
            steps[i].index = i;
            if (steps.Count == 1)
                steps[i].phase = SewingNeedlePhase.Entry;
            else if (i == 0)
                steps[i].phase = SewingNeedlePhase.Entry;
            else if (i == steps.Count - 1)
                steps[i].phase = SewingNeedlePhase.Exit;
        }
    }

    public SewingNeedleStep AddOrSelectAt(int cellX, int cellY)
    {
        if (steps == null)
            steps = new List<SewingNeedleStep>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].cellX == cellX && steps[i].cellY == cellY)
                return steps[i];
        }
        var step = new SewingNeedleStep
        {
            index = steps.Count,
            cellX = cellX,
            cellY = cellY,
            direction = steps.Count % 2 == 0 ? SewingFeedDirection.Left : SewingFeedDirection.Right,
            clothSide = steps.Count % 2 == 0 ? ClothThreadSide.Top : ClothThreadSide.Bottom,
            entryAngleDeg = 90f,
            exitAngleDeg = 90f,
            gauge01 = defaultGauge01
        };
        steps.Add(step);
        Reindex();
        return steps[steps.Count - 1];
    }

    public bool RemoveAtCell(int cellX, int cellY)
    {
        if (steps == null) return false;
        int n = steps.RemoveAll(s => s != null && s.cellX == cellX && s.cellY == cellY);
        if (n > 0)
            Reindex();
        return n > 0;
    }

    public SewingNeedleStep StepAt(int cellX, int cellY)
    {
        if (steps == null) return null;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].cellX == cellX && steps[i].cellY == cellY)
                return steps[i];
        }
        return null;
    }

    public void PaintRowChase(int rowY, int width, SewingFeedDirection direction)
    {
        int w = Mathf.Max(1, width);
        for (int x = 0; x < w; x++)
        {
            var step = AddOrSelectAt(x, rowY);
            step.direction = direction;
            step.clothSide = direction == SewingFeedDirection.Left ? ClothThreadSide.Top : ClothThreadSide.Bottom;
        }
        Reindex();
    }

    public float ConnectingSpanM()
    {
        if (steps == null || steps.Count == 0)
            return 0.2f;
        float span = 0f;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].connectingStrand)
                span += steps[i].PitchM();
        }
        return span > 1e-4f ? span : 0.2f;
    }
}
