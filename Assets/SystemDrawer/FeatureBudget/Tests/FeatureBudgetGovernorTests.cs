using NUnit.Framework;
using UnityEngine;

public class FeatureBudgetGovernorTests
{
    [Test]
    public void NormalState_ResetsAutoGranularityToOne()
    {
        var profile = CreateProfile();
        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(profile);
        var governor = new FeatureBudgetGovernor(profile, registry);
        var pathing = profile.FindEntry(FeatureBudgetIds.Pathing);
        pathing.granularityLevel = 0.35f;
        registry.SetGranularityForFeature(FeatureBudgetIds.Pathing, 0.35f);

        governor.Tick(profile.targetFrameCpuMs * 0.5f);
        Assert.AreEqual(FeatureBudgetState.Normal, governor.State);
        Assert.AreEqual(1f, pathing.granularityLevel, 0.001f);
    }

    [Test]
    public void BudgetMode_StepsLowestImportanceFirst()
    {
        var profile = CreateProfile();
        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(profile);
        var governor = new FeatureBudgetGovernor(profile, registry);
        foreach (var e in profile.entries)
        {
            e.controlMode = FeatureBudgetControlMode.Auto;
            e.granularityLevel = 1f;
        }

        governor.Tick(profile.targetFrameCpuMs * 2f);
        Assert.AreEqual(FeatureBudgetState.BudgetMode, governor.State);
        var pathing = profile.FindEntry(FeatureBudgetIds.Pathing);
        var weather = profile.FindEntry(FeatureBudgetIds.Weather);
        Assert.LessOrEqual(pathing.granularityLevel, weather.granularityLevel);
    }

    [Test]
    public void MapGranularityToLodTierOffset_MatchesSteps()
    {
        Assert.AreEqual(0, FeatureBudgetGranularityBridge.MapGranularityToLodTierOffset(1f));
        Assert.AreEqual(1, FeatureBudgetGranularityBridge.MapGranularityToLodTierOffset(0.5f));
        Assert.AreEqual(2, FeatureBudgetGranularityBridge.MapGranularityToLodTierOffset(0.2f));
        Assert.AreEqual(3, FeatureBudgetGranularityBridge.MapGranularityToLodTierOffset(0f));
    }

    static FeatureBudgetProfile CreateProfile()
    {
        var p = ScriptableObject.CreateInstance<FeatureBudgetProfile>();
        p.EnsureDefaults();
        return p;
    }
}
