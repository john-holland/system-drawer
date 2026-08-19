using System;
using System.Collections.Generic;
using UnityEngine;

public enum HouseConstructionStepKind
{
    Dig = 0,
    Foundation = 1,
    Wall = 2,
    FencePost = 3,
    FencePanel = 4,
    FenceGate = 5,
    Driveway = 6,
    GaragePad = 7,
    GarageDoor = 8,
    Finish = 9,
    MoveIn = 10
}

[Serializable]
public sealed class HouseConstructionStep
{
    public HouseConstructionStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    public Vector3 optimalWorld;
    public bool hasOptimal;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float commodities01 = 0.8f;
    [Range(0f, 1f)] public float resources01 = 0.8f;
    [Range(0f, 1f)] public float vehicleReach01 = 0.8f;
    [Range(0f, 1f)] public float blockage01;
    [Range(0f, 1f)] public float optimalCommodities01 = 1f;
    [Range(0f, 1f)] public float optimalResources01 = 1f;
    [Range(0f, 1f)] public float optimalVehicleReach01 = 1f;
    [Range(0f, 1f)] public float optimalBlockage01;
    [Range(0f, 1f)] public float limitCommodities01 = 1f;
    [Range(0f, 1f)] public float limitResources01 = 1f;
    [Range(0f, 1f)] public float limitVehicleReach01 = 1f;
    [Range(0f, 1f)] public float limitBlockage01 = 1f;
}

/// <summary>Plans RTS house-construction features with open/close topology. Not prison/rehab.</summary>
[AddComponentMenu("Locomotion/Travel/House Construction Travel Agent")]
public sealed class HouseConstructionTravelAgent : TravelAgent
{
    public List<HouseConstructionStep> steps = new List<HouseConstructionStep>();
    public int selectedStepIndex;
    public ThreatWarden threatWarden;
    public string threatAgencyId = ThreatAgencyId.BuildingMaintenance;
    public HouseConstructionPlan plan;
    public Transform siteRoot;
    public bool siteOpen = true;

    public HouseConstructionStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count ? steps[selectedStepIndex] : null;

    public static List<HouseConstructionStep> DefaultPipeline()
    {
        var list = new List<HouseConstructionStep>();
        foreach (HouseConstructionStepKind k in Enum.GetValues(typeof(HouseConstructionStepKind)))
            list.Add(new HouseConstructionStep { kind = k, sgInstanceId = k.ToString().ToLowerInvariant() });
        return list;
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? ThreatWarden.Instance;
    }

    public void SetConstructionSiteOpen(bool open)
    {
        siteOpen = open;
        if (siteRoot != null)
            siteRoot.gameObject.SetActive(open);
    }

    public HouseConstructionStep NextIncomplete()
    {
        if (steps == null) return null;
        for (int i = 0; i < steps.Count; i++)
            if (steps[i] != null && steps[i].progress01 < 1f - 1e-4f)
                return steps[i];
        return null;
    }

    public bool HardenSelected()
    {
        var step = SelectedStep;
        if (step == null) return false;
        step.progress01 = 1f;
        return true;
    }

    public float ThreatHalo01()
    {
        if (threatWarden == null) return 0f;
        var agency = threatWarden.GetAgency(threatAgencyId);
        return agency.threatScore01;
    }

    public bool OverLimit()
    {
        var s = SelectedStep;
        if (s == null) return false;
        return s.commodities01 > s.limitCommodities01 + 1e-4f
               || s.resources01 > s.limitResources01 + 1e-4f
               || s.vehicleReach01 > s.limitVehicleReach01 + 1e-4f
               || s.blockage01 > s.limitBlockage01 + 1e-4f
               || ThreatHalo01() > 0.75f;
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 1f, 1f, 1f, 0f };
        return new[] { s.optimalCommodities01, s.optimalResources01, s.optimalVehicleReach01, 1f - s.optimalBlockage01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 1f, 1f, 1f, 1f };
        return new[] { s.limitCommodities01, s.limitResources01, s.limitVehicleReach01, 1f - s.limitBlockage01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.5f, 0.5f, 0.5f };
        return new[] { s.commodities01, s.resources01, s.vehicleReach01, 1f - s.blockage01 };
    }

    public int PlanRtsFromFenceRun(int postCount)
    {
        int posts = Mathf.Max(2, postCount);
        int panels = Mathf.Max(0, posts - 1);
        if (steps == null) steps = new List<HouseConstructionStep>();
        int added = 0;
        for (int i = 0; i < posts; i++)
        {
            steps.Add(new HouseConstructionStep
            {
                kind = HouseConstructionStepKind.FencePost,
                sgInstanceId = "post_" + i
            });
            added++;
            if (i < panels)
            {
                steps.Add(new HouseConstructionStep
                {
                    kind = HouseConstructionStepKind.FencePanel,
                    sgInstanceId = "panel_" + i
                });
                added++;
            }
        }
        return added;
    }

    /// <summary>Garage pad/door only after a valid driveway (or street/sidewalk) outlet exists.</summary>
    public int PlanRtsFromLotOrder(bool drivewayReady, bool garageOutletValid)
    {
        if (steps == null) steps = new List<HouseConstructionStep>();
        int added = 0;
        if (drivewayReady)
        {
            steps.Add(new HouseConstructionStep
            {
                kind = HouseConstructionStepKind.Driveway,
                sgInstanceId = "driveway"
            });
            added++;
        }
        if (garageOutletValid && drivewayReady)
        {
            steps.Add(new HouseConstructionStep
            {
                kind = HouseConstructionStepKind.GaragePad,
                sgInstanceId = "garage_pad"
            });
            steps.Add(new HouseConstructionStep
            {
                kind = HouseConstructionStepKind.GarageDoor,
                sgInstanceId = "garage_door"
            });
            added += 2;
        }
        return added;
    }
}
