#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class WoundSutureTests
{
    [Test]
    public void CloseAmount_Full_ShowsHealedFillet()
    {
        var go = new GameObject("WoundHost");
        try
        {
            var host = go.AddComponent<WoundSiteRuntime>();
            var site = host.OpenFromDamage(new CombatDamageEvent
            {
                target = go,
                type = CombatDamageType.Slash,
                worldHit = go.transform.position,
                direction = Vector3.right,
                amount01 = 0.4f,
                limbId = "Chest"
            }, 0.4f, autoSuture: true);
            site.spec.closeAmount = 1f;
            host.TickHeal(0f);
            Assert.IsTrue(site.IsFullyClosed);
            Assert.IsTrue(site.IsHealedFilletVisible);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void StitchHoldPotential_Poles_HaveZeroRipRisk()
    {
        Assert.AreEqual(0f, WoundSiteRuntime.EffectiveRipRisk(0f));
        Assert.AreEqual(0f, WoundSiteRuntime.EffectiveRipRisk(1f));
        Assert.Greater(WoundSiteRuntime.EffectiveRipRisk(0.5f), 0f);
    }

    [Test]
    public void Slash_CreatesOpenWound_NoAutoSuture()
    {
        var go = new GameObject("Cut");
        try
        {
            var r = SlashDamageHandler.Apply(new CombatDamageEvent
            {
                target = go,
                type = CombatDamageType.Slash,
                worldHit = Vector3.zero,
                direction = Vector3.forward,
                amount01 = 0.3f,
                limbId = "Chest"
            });
            Assert.IsTrue(r.ok);
            Assert.IsNotNull(r.wound);
            Assert.IsFalse(r.wound.sutured);
            Assert.IsTrue(r.wound.spec.open);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
