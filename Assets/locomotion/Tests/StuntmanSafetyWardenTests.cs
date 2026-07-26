#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class StuntmanSafetyWardenTests
{
    [Test]
    public void RiskBand_MaxSafety09_ImpliesMinRisk01()
    {
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            maxSafety01 = 0.9f,
            maxRisk01 = float.NaN,
            minRisk01 = float.NaN,
            minSafety01 = float.NaN
        };
        var band = TravelRiskBand.Resolve(in hints);
        Assert.AreEqual(0.1f, band.minRisk01, 1e-4f);
        Assert.AreEqual(1f, band.maxRisk01, 1e-4f);
        Assert.IsTrue(band.Contains(0.1f));
        Assert.IsFalse(band.Contains(0.05f));
    }

    [Test]
    public void RiskBand_MinRiskAndMaxRisk_Intersect()
    {
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            minRisk01 = 0.1f,
            maxRisk01 = 0.3f,
            minSafety01 = float.NaN,
            maxSafety01 = float.NaN
        };
        var band = TravelRiskBand.Resolve(in hints);
        Assert.IsTrue(band.Contains(0.2f));
        Assert.IsFalse(band.Contains(0.05f));
        Assert.IsFalse(band.Contains(0.5f));
    }

    [Test]
    public void SafetyWarden_RewritesOutOfBandCrash()
    {
        var go = new GameObject("WardenTest");
        var warden = go.AddComponent<SafetyWardenPlannerService>();
        var apertureGo = new GameObject("WindowAperture");
        var aperture = apertureGo.AddComponent<PathingAperture>();
        aperture.apertureId = "window1";
        aperture.passMode = PathingAperturePassMode.CrashThrough;
        aperture.tags = new List<string> { "window" };
        aperture.crowdOccupancy01 = 0.8f;

        var plan = new GenericMultiModalPathPlan();
        var crash = MultiModalSegment.FromAcrobatics(null, null, Vector3.zero, Vector3.forward * 3f);
        crash.apertureId = "window1";
        crash.runningTotals = TravelPlanRunningTotals.FromJump(0.8f, 0.8f, 0.4f);
        plan.segments.Add(crash);

        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            maxRisk01 = 0.25f,
            minRisk01 = float.NaN,
            minSafety01 = float.NaN,
            maxSafety01 = float.NaN
        };

        var ctx = new StuntDiscoveryContext
        {
            startWorld = Vector3.zero,
            goalWorld = Vector3.forward * 3f,
            apertures = new[] { aperture },
            stuntZones = System.Array.Empty<StuntZone>()
        };
        warden.EnrichDiscovery(ctx);
        var result = warden.RescoreOrRewrite(plan, in hints);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.rejectedForks);
        Assert.Greater(result.rejectedForks.Count, 0);
        // Kept segments should be in band if any
        for (int i = 0; i < result.segments.Count; i++)
        {
            float r = warden.EstimateSegmentRisk(result.segments[i], ctx);
            Assert.LessOrEqual(r, 0.25f + 0.05f);
        }

        Object.DestroyImmediate(apertureGo);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Crowding_InflatesApertureRisk()
    {
        var go = new GameObject("CrowdAp");
        var ap = go.AddComponent<PathingAperture>();
        ap.apertureId = "door";
        ap.passMode = PathingAperturePassMode.AngularPassThrough;
        ap.crowdOccupancy01 = 0f;
        var empty = ApertureCrowdSampler.GetOccupancy01(ap);
        ap.crowdOccupancy01 = 0.75f;
        Assert.Greater(ApertureCrowdSampler.GetOccupancy01(ap), empty);

        var stuntGo = new GameObject("Stunt");
        var stunt = stuntGo.AddComponent<StuntmanPlannerService>();
        var seg = MultiModalSegment.FromAcrobatics(null, null, Vector3.zero, Vector3.one);
        seg.apertureId = "door";
        seg.runningTotals = TravelPlanRunningTotals.Neutral;
        var ctx = new StuntDiscoveryContext
        {
            apertures = new[] { ap },
            stuntZones = System.Array.Empty<StuntZone>()
        };
        float r0 = stunt.EstimateSegmentRisk(seg, ctx);
        ap.crowdOccupancy01 = 0f;
        float r1 = stunt.EstimateSegmentRisk(seg, ctx);
        Assert.Greater(r0, r1);

        Object.DestroyImmediate(stuntGo);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VulnerableMarker_RaisesDamageBias()
    {
        var actor = new GameObject("Actor");
        var vuln = actor.AddComponent<RagdollSectionStrengthMarker>();
        vuln.strength = RagdollSectionStrength.Vulnerable;
        vuln.EnsureControlPoint();
        vuln.controlPoint.position = actor.transform.position;
        vuln.influenceRadius = 1f;
        vuln.capsuleWeight = 1f;

        float dmg = RagdollSectionStrengthMarker.EstimateDamageBias(actor, actor.transform.position);
        Assert.Greater(dmg, 0.2f);

        var strong = actor.AddComponent<RagdollSectionStrengthMarker>();
        strong.strength = RagdollSectionStrength.Strong;
        strong.EnsureControlPoint();
        strong.controlPoint.position = actor.transform.position;
        strong.influenceRadius = 1f;
        strong.capsuleWeight = 2f;
        Assert.IsTrue(RagdollSectionStrengthMarker.HasStrongLead(actor));

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void Runway_WithoutSpeed_FailsAdequacy()
    {
        var go = new GameObject("Runway");
        var zone = go.AddComponent<StuntZone>();
        zone.kind = StuntZoneKind.Runway;
        zone.lengthMeters = 8f;
        zone.requiredEntrySpeed01 = 0.9f;
        Assert.IsFalse(zone.HasAdequateRunwayForSpeed(0.5f));
        Assert.IsTrue(zone.HasAdequateRunwayForSpeed(20f));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SafelyLemma_AppliesBandToTravelAgent()
    {
        var go = new GameObject("TA");
        var agent = go.AddComponent<TravelAgent>();
        var props = SafetyWardenLemmaProperties.ResolveFromParams(new Dictionary<string, string>
        {
            { "riskMin", "0.1" },
            { "safetyMin", "0.9" }
        });
        ConsiderSafetyWardenHints.ApplyLemmaHints(agent, props);
        Assert.AreEqual(0.1f, agent.minRisk01, 1e-4f);
        Assert.AreEqual(0.9f, agent.minSafety01, 1e-4f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void EmergenceBuffer_PublishesBranches()
    {
        StuntPlanEmergenceBuffer.Clear();
        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3>
        {
            Vector3.zero, Vector3.forward, Vector3.forward * 2f
        }));
        plan.rejectedForks = new List<MultiModalSegment>
        {
            MultiModalSegment.FromAcrobatics(null, null, Vector3.zero, Vector3.right)
        };
        plan.rejectedForks[0].waypoints = new List<Vector3> { Vector3.zero, Vector3.right };
        StuntPlanEmergenceBuffer.Publish(plan);
        Assert.Greater(StuntPlanEmergenceBuffer.Current.Count, 0);
        StuntPlanEmergenceBuffer.Clear();
    }
}
#endif
