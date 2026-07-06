using NUnit.Framework;

public class FeatureBudgetRatioRegistryTests
{
    [Test]
    public void TryUnlock_RejectsEmptyReason()
    {
        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(CreateProfile());
        Assert.IsFalse(registry.TryUnlock(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, ""));
        Assert.IsFalse(registry.TryUnlock(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, "   "));
    }

    [Test]
    public void TryUnlock_AcceptsNonEmptyReason()
    {
        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(CreateProfile());
        Assert.IsTrue(registry.TryUnlock(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, "Manual art pass"));
        Assert.IsTrue(registry.TryGetBinding(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, out var b));
        Assert.IsFalse(b.ratioLocked);
        Assert.AreEqual("Manual art pass", b.unlockReason);
    }

    [Test]
    public void GetEffectiveValue_Locked_AppliesGranularity()
    {
        var registry = new FeatureBudgetRatioRegistry();
        var profile = CreateProfile();
        registry.LoadFromProfile(profile);
        registry.SetGranularityForFeature(FeatureBudgetIds.Planet, 0.5f);
        float effective = registry.GetEffectiveValue(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm);
        Assert.AreEqual(0.002f * 500f * 0.5f, effective, 0.001f);
    }

    [Test]
    public void Relock_RestoresLockAndClearsReason()
    {
        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(CreateProfile());
        registry.TryUnlock(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, "test");
        Assert.IsTrue(registry.Relock(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm));
        Assert.IsTrue(registry.TryGetBinding(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, out var b));
        Assert.IsTrue(b.ratioLocked);
        Assert.IsEmpty(b.unlockReason);
    }

    static FeatureBudgetProfile CreateProfile()
    {
        var p = UnityEngine.ScriptableObject.CreateInstance<FeatureBudgetProfile>();
        p.EnsureDefaults();
        return p;
    }
}
