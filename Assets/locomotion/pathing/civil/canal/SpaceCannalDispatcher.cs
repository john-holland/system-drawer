using System;
using System.Collections.Generic;
using Planetary;
using SpatialVolumes;
using UnityEngine;

/// <summary>Canal lock/ops dispatcher. Space canals subclass this.</summary>
[AddComponentMenu("Locomotion/Civil/Canal Dispatcher")]
public class CanalDispatcher : MonoBehaviour
{
    public CanalBioRhythm bio;
    public GerPowerBus ger = new GerPowerBus();
    public CanalLockSpec lockSpec;

    protected virtual void Awake()
    {
        if (bio == null)
            bio = GetComponent<CanalBioRhythm>();
        ger ??= new GerPowerBus();
    }

    public virtual bool GateOpen()
    {
        ger ??= new GerPowerBus();
        ger.Tick();
        return ger.IsLive;
    }
}

/// <summary>Space canal dispatcher: GER gate, slip-stream volumes, relativity pathing.</summary>
[AddComponentMenu("Locomotion/Civil/Space Cannal Dispatcher")]
public sealed class SpaceCannalDispatcher : CanalDispatcher
{
    public SdfSpatiotemporalVolume slipStream;
    public CurvedSpacetimeSd2PathingSolver spacetime = new CurvedSpacetimeSd2PathingSolver();
    public Transform landingZone;
    public string[] commodities = { "fuel", "ger", "food", "wood" };

    public override bool GateOpen()
    {
        return base.GateOpen();
    }

    public bool TrySlipPath(HierarchicalPathingSolver context, Vector3 start, Vector3 goal, out List<Vector3> path)
    {
        spacetime ??= new CurvedSpacetimeSd2PathingSolver();
        return spacetime.TryFindPath(context, start, goal, true, out path);
    }
}

/// <summary>Space canal ops; inherits canal hours/commodities and requires a live GER bus.</summary>
[AddComponentMenu("Locomotion/Civil/Space Cannal Bio Rhythm")]
public sealed class SpaceCannalBioRhythm : CanalBioRhythm
{
    public SpaceCannalDispatcher dispatcher;
    public GerPowerBus ger;
    public string gerCommodity = "ger";
    public string fuelCommodity = "fuel";

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "space_cannal";
        if (dispatcher == null)
            dispatcher = GetComponent<SpaceCannalDispatcher>();
        if (ger == null && dispatcher != null)
            ger = dispatcher.ger;
        ger ??= new GerPowerBus();
        base.Awake();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        ger ??= dispatcher != null ? dispatcher.ger : ger;
        ger?.Tick();
        base.Tick(utcNow, dt);
        if (ger != null && !ger.IsLive)
            unitsAvailable01 = 0f;
    }

    public bool GateAllowsTransit()
    {
        if (dispatcher != null)
            return dispatcher.GateOpen();
        ger ??= new GerPowerBus();
        ger.Tick();
        return ger.IsLive;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = base.FacilitateCards(request);
        if (request == null) return cards;
        if (!GateAllowsTransit())
            return cards;
        cards.Add(SpaceCannalTransitCard.Generate(request));
        return cards;
    }
}

[Serializable]
public sealed class SpaceCannalTransitCard : TravelAgentCard
{
    public static SpaceCannalTransitCard Generate(DispatchRequest request)
    {
        var c = new SpaceCannalTransitCard();
        c.sectionName = "space_cannal";
        c.description = "Space canal transit";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.physicalPathingTag = "space_cannal";
        return c;
    }
}

public enum SpaceCannalStepKind
{
    Approach = 0,
    Gate = 1,
    Slipstream = 2,
    Landing = 3
}

[Serializable]
public sealed class SpaceCannalStep
{
    public SpaceCannalStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float speed01 = 0.5f;
    [Range(0f, 1f)] public float size01 = 0.4f;
    [Range(0f, 1f)] public float justice01 = 0.8f;
    [Range(0f, 1f)] public float threat01;
    [Range(0f, 1f)] public float optimalSpeed01 = 0.6f;
    [Range(0f, 1f)] public float optimalSize01 = 0.35f;
    [Range(0f, 1f)] public float optimalJustice01 = 1f;
    [Range(0f, 1f)] public float optimalThreat01;
    [Range(0f, 1f)] public float limitSpeed01 = 1f;
    [Range(0f, 1f)] public float limitSize01 = 0.8f;
    [Range(0f, 1f)] public float limitJustice01 = 0.25f;
    [Range(0f, 1f)] public float limitThreat01 = 0.7f;
}

[AddComponentMenu("Locomotion/Travel/Space Cannal Travel Agent")]
public sealed class SpaceCannalTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Speed", "Size", "Justice", "Threat" };

    public List<SpaceCannalStep> steps = new List<SpaceCannalStep>();
    public int selectedStepIndex;
    public SpaceCannalDispatcher dispatcher;
    public SpaceCannalBioRhythm bio;
    public JusticeWarden justiceWarden;
    public ThreatWarden threatWarden;
    public string threatAgencyId = ThreatAgencyId.BuildingMaintenance;

    public SpaceCannalStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static List<SpaceCannalStep> DefaultPipeline()
    {
        var list = new List<SpaceCannalStep>();
        foreach (SpaceCannalStepKind k in Enum.GetValues(typeof(SpaceCannalStepKind)))
            list.Add(new SpaceCannalStep { kind = k, sgInstanceId = k.ToString().ToLowerInvariant() });
        return list;
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (dispatcher == null)
            dispatcher = GetComponent<SpaceCannalDispatcher>();
        if (bio == null)
            bio = GetComponent<SpaceCannalBioRhythm>();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? ThreatWarden.Instance;
        if (justiceWarden == null)
            justiceWarden = GetComponent<JusticeWarden>();
    }

    public bool GateDenied()
    {
        if (dispatcher != null)
            return !dispatcher.GateOpen();
        if (bio != null)
            return !bio.GateAllowsTransit();
        return true;
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.6f, 0.35f, 1f, 1f };
        return new[] { s.optimalSpeed01, s.optimalSize01, s.optimalJustice01, 1f - s.optimalThreat01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 1f, 0.8f, 0.25f, 0.3f };
        return new[] { s.limitSpeed01, s.limitSize01, s.limitJustice01, 1f - s.limitThreat01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.4f, 0.8f, 1f };
        float justice = justiceWarden != null ? justiceWarden.Allow01() : s.justice01;
        float threat = ThreatHalo01();
        return new[] { s.speed01, s.size01, justice, 1f - Mathf.Max(s.threat01, threat) };
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
        return GateDenied()
               || s.speed01 > s.limitSpeed01 + 1e-4f
               || s.size01 > s.limitSize01 + 1e-4f
               || ThreatHalo01() > s.limitThreat01 + 1e-4f;
    }
}

/// <summary>Apartment / monarchy housing on a space-canal landing zone.</summary>
[AddComponentMenu("Locomotion/Civil/Space Cannal Housing")]
public sealed class SpaceCannalHousing : MonoBehaviour
{
    public HousingBuildingRagdoll apartments;
    public MonarchicVenueRuntime monarchy;
    public CityPixelGrid cityGrid;

    public MonarchCard AudienceCard()
    {
        return monarchy != null ? monarchy.Audience() : MonarchCard.Generate("audience");
    }

    public void EnsureApartmentLayer()
    {
        if (cityGrid == null) return;
        cityGrid.EnsureHouseLayers();
        cityGrid.PaintLayerCell(CityPixelLayerKind.Apartment, 0, 0, 0);
    }
}
