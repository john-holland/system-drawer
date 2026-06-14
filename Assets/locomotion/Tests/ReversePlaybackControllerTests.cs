using NUnit.Framework;
using UnityEngine;

public class ReversePlaybackControllerTests
{
    [Test]
    public void EnterReverse_SetsDirectionNegative()
    {
        var go = new GameObject("ReverseTest");
        var agent = go.AddComponent<TravelAgent>();
        agent.reverseLegLimit01 = 0.5f;
        typeof(TravelAgent).GetField("reverseBudgetMeters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(agent, 10f);
        typeof(TravelAgent).GetField("totalPathLengthMeters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(agent, 20f);

        var controller = go.AddComponent<ReversePlaybackController>();
        controller.travelAgent = agent;

        var ctx = TravelExecutionContext.Build(
            null, null, new MultiModalSegment { reverseLeg = true }, 0,
            TravelLegMode.Walk, false, TravelLegMode.Walk, TravelLegMode.Walk, agent,
            10f, true);

        controller.EnterReverse(ctx);
        Assert.AreEqual(-1, controller.PlayDirection);
        Assert.IsTrue(controller.InReverse);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AdvanceArcLength_ExhaustsBudgetAndFlipsDirection()
    {
        var go = new GameObject("ReverseBudget");
        var controller = go.AddComponent<ReversePlaybackController>();
        controller.EnterReverse(TravelExecutionContext.Build(
            null, null, new MultiModalSegment { reverseLeg = true }, 0,
            TravelLegMode.Walk, false, TravelLegMode.Walk, TravelLegMode.Walk,
            null, 5f, true));

        controller.AdvanceArcLength(6f);
        Assert.AreEqual(1, controller.PlayDirection);
        Assert.IsFalse(controller.InReverse);

        Object.DestroyImmediate(go);
    }
}
