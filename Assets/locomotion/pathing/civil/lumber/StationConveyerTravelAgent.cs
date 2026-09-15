using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ConveyerStationStep
{
    public string stationId = "station";
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float size01 = 0.4f;
    [Range(0f, 1f)] public float weight01 = 0.4f;
    [Range(0f, 1f)] public float optimalSize01 = 0.35f;
    [Range(0f, 1f)] public float optimalWeight01 = 0.35f;
    [Range(0f, 1f)] public float limitSize01 = 0.85f;
    [Range(0f, 1f)] public float limitWeight01 = 0.85f;
}

/// <summary>Steps between mill stations. Diamond Size / Weight. Belt UV from rope wind rate.</summary>
[AddComponentMenu("Locomotion/Travel/Station Conveyer Travel Agent")]
public sealed class StationConveyerTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Size", "Weight" };

    public List<ConveyerStationStep> steps = new List<ConveyerStationStep>();
    public int selectedStepIndex;
    public Transform conveyorAnchor;
    public ConveyorScrollUvDriver scroll;
    public RopeSystem beltRope;
    public float fallbackScrollMps = 0.25f;

    public ConveyerStationStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static List<ConveyerStationStep> DefaultPipeline()
    {
        return new List<ConveyerStationStep>
        {
            new ConveyerStationStep { stationId = "infeed" },
            new ConveyerStationStep { stationId = "debark" },
            new ConveyerStationStep { stationId = "lathe" },
            new ConveyerStationStep { stationId = "grade" },
            new ConveyerStationStep { stationId = "pile" }
        };
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (scroll == null)
            scroll = GetComponent<ConveyorScrollUvDriver>();
        if (beltRope == null)
            beltRope = GetComponent<RopeSystem>();
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.35f, 0.35f };
        return new[] { s.optimalSize01, s.optimalWeight01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.85f, 0.85f };
        return new[] { s.limitSize01, s.limitWeight01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.4f, 0.4f };
        return new[] { s.size01, s.weight01 };
    }

    public bool ClampSizeWeight()
    {
        var s = SelectedStep;
        if (s == null) return false;
        bool clamped = false;
        if (s.size01 > s.limitSize01)
        {
            s.size01 = s.limitSize01;
            clamped = true;
        }
        if (s.weight01 > s.limitWeight01)
        {
            s.weight01 = s.limitWeight01;
            clamped = true;
        }
        return clamped;
    }

    public bool OverLimit()
    {
        var s = SelectedStep;
        if (s == null) return false;
        return s.size01 > s.limitSize01 + 1e-4f || s.weight01 > s.limitWeight01 + 1e-4f;
    }

    public float BeltRateMps()
    {
        if (beltRope != null)
            return beltRope.WindRateMps;
        return fallbackScrollMps;
    }
}
