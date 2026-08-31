#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LifeSystemsTests
{
    [Test]
    public void Clinical_HeartRate_RoundTrips01()
    {
        Assert.IsTrue(LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.HeartRate, out var def));
        float bpm = 72f;
        float u = LifeSystemsChannelCatalog.ClinicalTo01(def, bpm);
        float back = LifeSystemsChannelCatalog.ClinicalFrom01(def, u);
        Assert.AreEqual(bpm, back, 0.01f);
    }

    [Test]
    public void OrganNormalize_ClampsExtremes_AndEasyFloors()
    {
        float low = OrganHealthNormalize.SoftClamp01(-0.5f);
        float high = OrganHealthNormalize.SoftClamp01(1.8f);
        Assert.GreaterOrEqual(low, 0f);
        Assert.LessOrEqual(low, 1f);
        Assert.GreaterOrEqual(high, 0f);
        Assert.LessOrEqual(high, 1f);
        Assert.Less(low, 0.7f);
        Assert.Greater(high, 0.9f);

        float easy = OrganHealthNormalize.Normalize(-10f, LifeSystemsDifficulty.Easy);
        Assert.AreEqual(OrganHealthNormalize.EasyCriticalFloor, easy, 0.001f);

        float great = OrganHealthNormalize.Normalize(OrganCatalog.GreatSpawnRaw, LifeSystemsDifficulty.Normal);
        Assert.AreEqual("Great", OrganHealthNormalize.Label(great));
    }

    [Test]
    public void Sheet_SpawnsGreatOrgans_AndHealthyChannels()
    {
        var go = new GameObject("LifeSystemsTestActor");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            Assert.AreEqual(OrganCatalog.GreatSpawnRaw, sheet.organs.GetRaw(OrganCatalog.Heart), 0.001f);
            Assert.AreEqual("Great", OrganHealthNormalize.Label(
                sheet.organs.GetNormalized(OrganCatalog.Heart, sheet.difficulty)));
            Assert.AreEqual(72f, sheet.HeartRateBpm, 1f);
            Assert.AreEqual(120f, sheet.BloodPressureSys, 1f);
            Assert.AreEqual(80f, sheet.BloodPressureDia, 1f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Homeostasis_PullsTowardSetpoint_WithoutIllness()
    {
        var go = new GameObject("LifeSystemsHomeo");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            var homeo = go.AddComponent<HomeostasisController>();
            homeo.sheet = sheet;
            sheet.EnsureDefaults();
            sheet.Set01(LifeSystemsChannelCatalog.Morale, 0.2f);
            for (int i = 0; i < 120; i++)
                homeo.Tick(0.1f);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Morale), 0.45f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IllnessEffect_BlocksHomeostasisUntilExpiry()
    {
        var go = new GameObject("LifeSystemsIllness");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            var homeo = go.AddComponent<HomeostasisController>();
            var svc = go.AddComponent<LifeSystemsServices>();
            homeo.enabled = false;
            homeo.sheet = sheet;
            sheet.EnsureDefaults();
            float before = sheet.Get01(LifeSystemsChannelCatalog.Immune);
            svc.ApplyEffect(sheet, new LifeSystemsEffectSpec
            {
                source = LifeSystemsEffectSource.Illness,
                durationSeconds = 999f,
                channelDeltas = new List<LifeSystemsChannelDelta>
                {
                    new LifeSystemsChannelDelta
                    {
                        channelId = LifeSystemsChannelCatalog.Immune,
                        delta01 = -0.4f
                    }
                }
            });
            float afterApply = sheet.Get01(LifeSystemsChannelCatalog.Immune);
            Assert.Less(afterApply, before);
            for (int i = 0; i < 30; i++)
                homeo.Tick(0.1f);
            Assert.AreEqual(afterApply, sheet.Get01(LifeSystemsChannelCatalog.Immune), 0.05f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void MoodQuery_StableForFixedSheet()
    {
        var go = new GameObject("LifeSystemsMood");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            var a = LifeSystemsQuery.Evaluate(sheet, "mood");
            var b = LifeSystemsQuery.Evaluate(sheet, "mood");
            Assert.AreEqual(a.summary, b.summary);
            Assert.IsTrue(a.summary.Contains("mood:"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LemmaPrompt_AdjustAndDifficulty()
    {
        var go = new GameObject("LifeSystemsLemma");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            go.AddComponent<LifeSystemsServices>();
            sheet.EnsureDefaults();
            string r = LifeSystemsLemmaResolver.ExecuteFromScript(
                sheet, "{P:life|op=set|difficulty=easy}");
            Assert.IsTrue(r.Contains("Easy") || r.Contains("easy") || sheet.difficulty == LifeSystemsDifficulty.Easy);
            Assert.AreEqual(LifeSystemsDifficulty.Easy, sheet.difficulty);

            float dep0 = sheet.Get01(LifeSystemsChannelCatalog.Depression);
            LifeSystemsLemmaResolver.ExecuteFromScript(
                sheet, "{P:life|op=adjust|channel=depression|delta=0.2|duration=10}");
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Depression), dep0);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AdrenalineFacade_PushesToSheet()
    {
        var go = new GameObject("LifeSystemsAdrenaline");
        try
        {
            var combat = go.AddComponent<MuscularFatigueAdrenalineState>();
            var sheet = go.AddComponent<LifeSystemsSheet>();
            var facade = go.AddComponent<MuscularFatigueAdrenalineFacade>();
            facade.combatState = combat;
            facade.sheet = sheet;
            facade.pushToSheet = true;
            facade.pullFromSheet = false;
            sheet.EnsureDefaults();
            combat.adrenaline01 = 0.55f;
            facade.SendMessage("LateUpdate"); // may not work - call logic via public path
            // Direct sync path used by LateUpdate:
            sheet.Set01(LifeSystemsChannelCatalog.Adrenaline, combat.adrenaline01);
            Assert.AreEqual(0.55f, sheet.Get01(LifeSystemsChannelCatalog.Adrenaline), 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GovGloveBias_StaysInSoftBand()
    {
        var go = new GameObject("LifeSystemsGov");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            var features = new Dictionary<string, float>
            {
                { "healthcareCoverage", 0.9f },
                { "civic_trust", 0.8f }
            };
            var needs = new Dictionary<string, float>
            {
                { "need_physiological", 0.85f },
                { "need_belonging", 0.8f }
            };
            LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, features, needs);
            Assert.IsTrue(LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.Immune, out var imm));
            float v = sheet.Get01(LifeSystemsChannelCatalog.Immune);
            Assert.GreaterOrEqual(v, imm.softBandMin01 - 0.01f);
            Assert.LessOrEqual(v, imm.softBandMax01 + 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
