#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TravelPathReverseLimitsTests
{
    [Test]
    public void ResolveDefault_499m_ReturnsOne()
    {
        Assert.AreEqual(1f, TravelPathReverseLimits.ResolveDefaultReverseLegLimit01(499f));
    }

    [Test]
    public void ResolveDefault_500m_ReturnsHalf()
    {
        Assert.AreEqual(0.5f, TravelPathReverseLimits.ResolveDefaultReverseLegLimit01(500f));
    }

    [Test]
    public void LimitZero_NoReverseBudget()
    {
        Assert.AreEqual(0f, TravelPathReverseLimits.ReverseBudgetMeters(0f, 1200f));
        Assert.IsFalse(TravelPathReverseLimits.AllowsReverse(0f));
    }

    [Test]
    public void LimitOne_BudgetEqualsTotal()
    {
        Assert.AreEqual(750f, TravelPathReverseLimits.ReverseBudgetMeters(1f, 750f), 0.001f);
    }

    [Test]
    public void FormatDistanceLabel_UsesMetersAndKm()
    {
        string shortPath = TravelPathReverseLimits.FormatDistanceLabel(375f, 750f);
        StringAssert.Contains("375", shortPath);
        StringAssert.Contains("750", shortPath);

        string km = TravelPathReverseLimits.FormatDistanceLabel(500f, 1000f);
        StringAssert.Contains("km", km);
    }

    [Test]
    public void KinematicsProfile_LimitZero_NoReverseSamples()
    {
        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new System.Collections.Generic.List<Vector3>
        {
            Vector3.zero, Vector3.forward * 10f, Vector3.forward * 20f
        }));

        TravelPathKinematicsProfile profile = TravelPathKinematicsProfile.Build(plan, 0f, 2f);
        foreach (TravelPathSample s in profile.Samples)
            Assert.IsFalse(s.reverse);
    }

    [Test]
    public void ReversePlayback_OnlyWhenAllowsReverse()
    {
        var go = new GameObject("ReverseGate");
        var agent = go.AddComponent<TravelAgent>();
        agent.reverseLegLimit01 = 0f;
        var controller = go.AddComponent<ReversePlaybackController>();
        controller.travelAgent = agent;

        controller.EnterReverse(TravelExecutionContext.Build(
            null, null, new MultiModalSegment { reverseLeg = true }, 0,
            TravelLegMode.Walk, false, TravelLegMode.Walk, TravelLegMode.Walk,
            agent, 5f, true));

        Assert.AreEqual(1, controller.PlayDirection);
        Object.DestroyImmediate(go);
    }
}
#endif
