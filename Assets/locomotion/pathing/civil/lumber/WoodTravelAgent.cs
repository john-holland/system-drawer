using System;
using System.Collections.Generic;
using UnityEngine;

public enum WoodMillStepKind
{
    Receive = 0,
    Debark = 1,
    Grade = 2,
    Lathe = 3,
    Convey = 4,
    Pile = 5,
    Bind = 6,
    Ship = 7,
    PlankCut = 8,
    Section = 9
}

[Serializable]
public sealed class WoodMillStep
{
    public WoodMillStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float size01 = 0.5f;
    [Range(0f, 1f)] public float weight01 = 0.5f;
    [Range(0f, 1f)] public float grade01 = 0.7f;
    [Range(0f, 1f)] public float moisture01 = 0.4f;
    [Range(0f, 1f)] public float optimalSize01 = 0.45f;
    [Range(0f, 1f)] public float optimalWeight01 = 0.4f;
    [Range(0f, 1f)] public float optimalGrade01 = 0.85f;
    [Range(0f, 1f)] public float optimalMoisture01 = 0.3f;
    [Range(0f, 1f)] public float limitSize01 = 0.9f;
    [Range(0f, 1f)] public float limitWeight01 = 0.9f;
    [Range(0f, 1f)] public float limitGrade01 = 0.2f;
    [Range(0f, 1f)] public float limitMoisture01 = 0.85f;
    public int kerfCount;
    public int plankCount;
    public int sectionCount;
}

/// <summary>Mill line: receive → debark / lathe → plank cut → section → convey → grade → convey → pile → bind → ship.</summary>
[AddComponentMenu("Locomotion/Travel/Wood Travel Agent")]
public sealed class WoodTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Size", "Weight", "Grade", "Moisture" };

    public List<WoodMillStep> steps = new List<WoodMillStep>();
    public int selectedStepIndex;
    public WoodMillBioRhythm mill;
    public LatheSpec lathe;
    public WoodPileSpec pile;
    public string deliveryMechanism = "vehicle";
    public CanalRibbonSpec canalRibbon;
    public ThreatWarden threatWarden;
    public string threatAgencyId = ThreatAgencyId.BuildingMaintenance;

    public WoodMillStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static readonly WoodMillStepKind[] PipelineOrder =
    {
        WoodMillStepKind.Receive,
        WoodMillStepKind.Debark,
        WoodMillStepKind.Lathe,
        WoodMillStepKind.PlankCut,
        WoodMillStepKind.Section,
        WoodMillStepKind.Convey,
        WoodMillStepKind.Grade,
        WoodMillStepKind.Convey,
        WoodMillStepKind.Pile,
        WoodMillStepKind.Bind,
        WoodMillStepKind.Ship
    };

    public static string DefaultSgId(WoodMillStepKind kind, int pipelineIndex)
    {
        if (kind != WoodMillStepKind.Convey)
            return kind.ToString().ToLowerInvariant();
        for (int i = pipelineIndex + 1; i < PipelineOrder.Length; i++)
        {
            if (PipelineOrder[i] == WoodMillStepKind.Grade)
                return "convey_grade";
        }
        return "convey_pile";
    }

    public static List<WoodMillStep> DefaultPipeline()
    {
        var list = new List<WoodMillStep>();
        for (int i = 0; i < PipelineOrder.Length; i++)
        {
            var k = PipelineOrder[i];
            list.Add(new WoodMillStep { kind = k, sgInstanceId = DefaultSgId(k, i) });
        }
        return list;
    }

    public void EnsurePipeline()
    {
        if (steps == null || steps.Count == 0)
        {
            steps = DefaultPipeline();
            return;
        }
        var unused = new List<WoodMillStep>(steps);
        var next = new List<WoodMillStep>(PipelineOrder.Length);
        for (int i = 0; i < PipelineOrder.Length; i++)
        {
            var k = PipelineOrder[i];
            WoodMillStep found = null;
            for (int j = 0; j < unused.Count; j++)
            {
                if (unused[j] != null && unused[j].kind == k)
                {
                    found = unused[j];
                    unused.RemoveAt(j);
                    break;
                }
            }
            if (found == null)
                found = new WoodMillStep { kind = k, sgInstanceId = DefaultSgId(k, i) };
            next.Add(found);
        }
        steps = next;
    }

    /// <summary>Odd PixelLight kerfs through a log → plank count. Section further cuts those to length.</summary>
    public void ApplyLatheKerfsToPlankSteps()
    {
        int kerfs = MillKerfCuts.OddCutCount(lathe != null ? lathe.millCutCount : 3);
        int planks = MillKerfCuts.PlankCount(kerfs);
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            if (s.kind == WoodMillStepKind.PlankCut)
            {
                s.kerfCount = kerfs;
                s.plankCount = planks;
                s.sgInstanceId = "plank_cut_" + kerfs + "kerf";
            }
            if (s.kind == WoodMillStepKind.Section)
            {
                s.kerfCount = kerfs;
                s.plankCount = planks;
                s.sectionCount = planks;
                s.sgInstanceId = "section_" + planks;
            }
        }
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        else
            EnsurePipeline();
        if (mill == null)
            mill = GetComponent<WoodMillBioRhythm>();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? ThreatWarden.Instance;
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.45f, 0.4f, 0.85f, 0.7f };
        return new[] { s.optimalSize01, s.optimalWeight01, s.optimalGrade01, 1f - s.optimalMoisture01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.9f, 0.9f, 0.2f, 0.15f };
        return new[] { s.limitSize01, s.limitWeight01, s.limitGrade01, 1f - s.limitMoisture01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.5f, 0.7f, 0.6f };
        return new[] { s.size01, s.weight01, s.grade01, 1f - s.moisture01 };
    }

    public float ThreatHalo01()
    {
        if (threatWarden == null) return 0f;
        return threatWarden.GetAgency(threatAgencyId).threatScore01;
    }

    public bool OverLimit()
    {
        var s = SelectedStep;
        if (s == null) return false;
        return s.size01 > s.limitSize01 + 1e-4f
               || s.weight01 > s.limitWeight01 + 1e-4f
               || s.grade01 < s.limitGrade01 - 1e-4f
               || s.moisture01 > s.limitMoisture01 + 1e-4f
               || ThreatHalo01() > 0.75f;
    }

    public TAVehicleDeliveryCard DeliveryCard(DispatchRequest request)
    {
        var card = TAVehicleDeliveryCard.Generate(request, deliveryMechanism);
        if (request != null)
            card.goalWorld = request.worldTarget;
        return card;
    }
}
