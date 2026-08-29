#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class RelationshipWardenTests
{
    [Test]
    public void ConsentBlend_ThreatHigh_LowersConsent()
    {
        float high = ConsentWarden.Blend01(1f / 3f, 1f, true, 1f / 3f, 1f, true, 1f / 3f, 1f, true, 0f, 1f, false);
        float low = ConsentWarden.Blend01(1f / 3f, 0f, true, 1f / 3f, 1f, true, 1f / 3f, 1f, true, 0f, 1f, false);
        Assert.Less(high, low);
        Assert.Less(high, 0.7f);
    }

    [Test]
    public void ConsentBlend_MissingRights_Renormalizes()
    {
        float withGap = ConsentWarden.Blend01(1f / 3f, 0f, true, 1f / 3f, 1f, true, 1f / 3f, 1f, true, 0.25f, 0f, false);
        Assert.AreEqual(1f, withGap, 0.001f);
    }

    [Test]
    public void ConsentBlend_RightsLow_Junta_TightensWhenWeighted()
    {
        float rightsHigh = ConsentWarden.Blend01(0.25f, 0f, true, 0.25f, 1f, true, 0.25f, 1f, true, 0.25f, 1f, true);
        float rightsLow = ConsentWarden.Blend01(0.25f, 0f, true, 0.25f, 1f, true, 0.25f, 1f, true, 0.25f, 0f, true);
        Assert.Less(rightsLow, rightsHigh);
        float equalShare = ConsentWarden.Blend01(1f / 3f, 0f, true, 1f / 3f, 1f, true, 1f / 3f, 1f, true, 0f, 0f, true);
        Assert.Less(equalShare, 1f);
    }

    [Test]
    public void JusticeAllow_UsesPrisonWhenRightsMissing_AndRightsWhenAssigned()
    {
        var go = new GameObject("justice");
        try
        {
            var prison = go.AddComponent<PrisonWarden>();
            prison.lastScore01 = 0.8f;
            var justice = go.AddComponent<JusticeWarden>();
            justice.prisonWarden = prison;
            float prisonOnly = justice.Allow01();
            Assert.AreEqual(0.2f, prisonOnly, 0.02f);

            var rights = go.AddComponent<RightsWarden>();
            rights.lastScore01 = 1f;
            justice.rightsWarden = rights;
            float withRights = justice.Allow01();
            Assert.Greater(withRights, prisonOnly);

            var junta = go.AddComponent<JuntaRuntime>();
            junta.canSuspendConstitution = true;
            rights.junta = junta;
            float suspended = justice.Allow01();
            Assert.Less(suspended, withRights);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Theocratic_AllowCounselForbid_AndNamedMaps()
    {
        var go = new GameObject("theo");
        try
        {
            var theo = go.AddComponent<TheocraticWarden>();
            Assert.AreEqual(TheocraticWardenAction.Allow, theo.SetDoctrineScore(0.9f));
            Assert.Greater(theo.Allow01(), 0.66f);
            Assert.AreEqual(TheocraticWardenAction.Counsel, theo.SetDoctrineScore(0.5f));
            Assert.AreEqual(TheocraticWardenAction.Forbid, theo.SetDoctrineScore(0.1f));
            theo.SetAction(TheocraticWardenAction.Forbid);
            Assert.LessOrEqual(theo.Allow01(), 0.32f);

            theo.SetSg3d("altar", new Vector3(1f, 2f, 3f));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), theo.GetSg3d("altar"));
            var b = new Bounds4(Vector3.one, Vector3.one * 2f, 0f, 10f);
            theo.SetSg4d("nave", b);
            var got = theo.GetSg4d("nave");
            Assert.AreEqual(b.center, got.center);
            Assert.AreEqual(b.size, got.size);
            Assert.AreEqual(b.tMin, got.tMin);
            Assert.AreEqual(b.tMax, got.tMax);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ResolvePath_TwoHumans_ManAndLamp_ThreeSubjects()
    {
        var host = new GameObject("rel-agent");
        var a = new GameObject("human-a");
        var b = new GameObject("human-b");
        var lamp = new GameObject("lamp");
        var fly = new GameObject("fly");
        a.transform.position = new Vector3(0f, 0f, 0f);
        b.transform.position = new Vector3(4f, 0f, 0f);
        lamp.transform.position = new Vector3(2f, 1f, 0f);
        fly.transform.position = new Vector3(1f, 0f, 3f);
        try
        {
            var agent = host.AddComponent<RelationshipTravelAgent>();
            var two = agent.ResolvePath(new[] { a, b }, RomanceSeverity.GoingOut);
            Assert.AreEqual(3, two.Count);
            Assert.AreEqual(RelationshipStationKind.Approach, two[0].station);
            Assert.AreEqual(a.transform.position, two[0].predictedWorld);
            Assert.AreEqual(RelationshipStationKind.ShareSpace, two[1].station);
            Assert.AreEqual(RelationshipStationKind.DialogColumn, two[2].station);
            Assert.IsNull(a.GetComponent<RagdollSystem>());

            var manLamp = agent.ResolvePath(new[] { a, lamp }, RomanceSeverity.Crush);
            Assert.GreaterOrEqual(manLamp.Count, 3);
            Assert.AreEqual(a.transform.position, manLamp[0].predictedWorld);
            Assert.AreEqual(2, agent.subjects.Count);

            var three = agent.ResolvePath(new[] { a, b, fly }, RomanceSeverity.GoingSteady);
            Assert.GreaterOrEqual(three.Count, 3);
            Assert.AreEqual(3, agent.subjects.Count);
            bool foundIntimacy = false;
            for (int i = 0; i < three.Count; i++)
                if (three[i].station == RelationshipStationKind.Intimacy)
                    foundIntimacy = true;
            Assert.IsTrue(foundIntimacy);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(lamp);
            Object.DestroyImmediate(fly);
        }
    }

    [Test]
    public void DialogColumn_Edge()
    {
        var tree = ScriptableObject.CreateInstance<RelationshipDialogTree>();
        tree.EnsureDefaultColumns();
        tree.nodes.Add(new RelationshipDialogNode { id = "n0", columnIndex = 0, title = "hello" });
        tree.nodes.Add(new RelationshipDialogNode { id = "n1", columnIndex = 1, title = "ask" });
        tree.AddEdge("n0", "n1");
        Assert.IsTrue(tree.HasEdge("n0", "n1"));
        Assert.IsFalse(tree.HasEdge("n1", "n0"));
        Assert.AreEqual("n0", tree.PickInColumn(0).id);
        Object.DestroyImmediate(tree);
    }

    [Test]
    public void Diamond_MissingWarden_IsNeutralHalf_GreenRedWhite()
    {
        float[] green = RelationshipPowerDiamond.GreenExpected01(null, null, null);
        Assert.AreEqual(4, green.Length);
        Assert.AreEqual(RelationshipPowerDiamond.NeutralMissing, green[0], 0.001f);
        float[] white = RelationshipPowerDiamond.WhiteActual01(null, null, null, null, null, null, null, null, null);
        Assert.AreEqual(RelationshipPowerDiamond.NeutralMissing, white[0], 0.001f);
        Assert.AreEqual(RelationshipPowerDiamond.NeutralMissing, white[1], 0.001f);
        Assert.AreEqual(RelationshipPowerDiamond.NeutralMissing, white[2], 0.001f);
        Assert.AreEqual(RelationshipPowerDiamond.NeutralMissing, white[3], 0.001f);
        Assert.AreEqual("Affection", RelationshipPowerDiamond.Axes[0]);
        Assert.AreEqual("Consent", RelationshipPowerDiamond.Axes[1]);
        Assert.AreEqual("Doctrine", RelationshipPowerDiamond.Axes[2]);
        Assert.AreEqual("Safety", RelationshipPowerDiamond.Axes[3]);

        var step = new RelationshipStep
        {
            expected01 = new[] { 0.8f, 0.7f, 0.6f, 0.5f },
            fireLimit01 = new[] { 0.95f, 0.9f, 0.85f, 0.8f }
        };
        float[] g2 = RelationshipPowerDiamond.GreenExpected01(step, null, null);
        Assert.AreEqual(0.8f, g2[0], 0.001f);
        float[] red = RelationshipPowerDiamond.RedLimit01(step, null, null, null, null, null);
        Assert.AreEqual(0.95f, red[0], 0.001f);
    }

    [Test]
    public void PlannerSoftGate_ReadsConsentWarden()
    {
        var go = new GameObject("planner");
        try
        {
            var planner = go.AddComponent<ConsentWardenPlannerService>();
            planner.maxPhysicality01 = 0.95f;
            var consent = go.AddComponent<ConsentWarden>();
            var threat = go.AddComponent<ThreatWarden>();
            threat.SetLevels("kitchen", ThreatAlertLevel.AllClear, ThreatLevel.None, 0f, 1f);
            consent.threatWarden = threat;
            consent.wThreat = 1f;
            consent.wTheo = 0f;
            consent.wJust = 0f;
            consent.wRights = 0f;
            var card = new LoveCard { physicality01 = 0.9f, requiresConsent = false };
            planner.SoftGate(card);
            Assert.Less(card.physicality01, 0.2f);
            Assert.IsTrue(card.requiresConsent);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void FeatureBudget_RelationshipRank36_LegalCourt35()
    {
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        FeatureBudgetEntry rel = null;
        FeatureBudgetEntry legal = null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].featureId == FeatureBudgetIds.Relationship) rel = entries[i];
            if (entries[i].featureId == FeatureBudgetIds.LegalCourt) legal = entries[i];
        }
        Assert.IsNotNull(rel);
        Assert.IsNotNull(legal);
        Assert.AreEqual(36, rel.importanceRank);
        Assert.AreEqual(35, legal.importanceRank);
        Assert.IsTrue(System.Array.Exists(rel.perfScopePrefixes, p => p == "LoveWarden"));
        Assert.IsTrue(System.Array.Exists(rel.perfScopePrefixes, p => p == "TheocraticWarden"));
        Assert.IsTrue(System.Array.Exists(legal.perfScopePrefixes, p => p == "JusticeWarden"));
        Assert.IsTrue(System.Array.Exists(legal.perfScopePrefixes, p => p == "GenevaConventionWarden"));
    }

    [Test]
    public void ThreatWarden_IsTorture_ConsultsConsentRightsJusticeRomance()
    {
        Assert.IsTrue(ThreatWarden.IsTorture(0f, ThreatKind.Torture, null, null, null, null, null));
        Assert.IsFalse(ThreatWarden.IsTorture(0.6f, ThreatKind.Generic, null, null, null, null, null));
        Assert.IsTrue(ThreatWarden.IsTorture(0.6f, ThreatKind.Generic, 0.2f, null, null, null, null));
        Assert.IsTrue(ThreatWarden.IsTorture(0.6f, ThreatKind.Generic, null, true, null, null, null));
        Assert.IsTrue(ThreatWarden.IsTorture(0.6f, ThreatKind.Generic, null, null, 0.2f, null, null));
        Assert.IsTrue(ThreatWarden.IsTorture(
            0.6f, ThreatKind.Generic, null, null, null, JusticeWardenAction.Restrain, null));
        Assert.IsTrue(ThreatWarden.IsTorture(0.6f, ThreatKind.Generic, 0.2f, null, null, null, 0.7f));
        Assert.IsFalse(ThreatWarden.IsTorture(0.2f, ThreatKind.Generic, 0.2f, true, 0.1f, JusticeWardenAction.Restrain, 0.9f));

        var go = new GameObject("torture-consult");
        try
        {
            var threat = go.AddComponent<ThreatWarden>();
            Assert.IsFalse(threat.IsTorture());
            var consent = go.AddComponent<ConsentWarden>();
            consent.threatWarden = threat;
            threat.consentWarden = consent;
            threat.SetLevels(ThreatAgencyId.Security, ThreatAlertLevel.Elevated, ThreatLevel.ActiveThreat, 0.75f, 0.75f);
            Assert.IsTrue(threat.IsTorture());
            threat.RaiseThreat(ThreatKind.Torture);
            Assert.IsTrue(threat.IsTorture());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PrebakeCalendar_TagsRomanceLoveBiorhythm()
    {
        var go = new GameObject("cal");
        var a = new GameObject("a");
        var b = new GameObject("b");
        try
        {
            var agent = go.AddComponent<RelationshipTravelAgent>();
            agent.bioRhythm = go.AddComponent<RelationshipBioRhythm>();
            agent.ResolvePath(new[] { a, b }, RomanceSeverity.Crush);
            var cal = go.AddComponent<NarrativeCalendarAsset>();
            int n = agent.PrebakeCalendar(cal);
            Assert.Greater(n, 0);
            Assert.IsTrue(cal.events[0].tags.Contains("romance"));
            Assert.IsTrue(cal.events[0].tags.Contains("love"));
            Assert.IsTrue(cal.events[0].tags.Contains("biorhythm"));
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
#endif
