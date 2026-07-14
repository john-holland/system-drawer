#if UNITY_EDITOR
using Locomotion.Audio;
using NUnit.Framework;
using UnityEngine;

public class DigitalEffectsMachineTests
{
    [Test]
    public void InsertBefore_OrdersGraph()
    {
        var go = new GameObject("fx");
        var budget = go.AddComponent<AudioPowerBudget>();
        var fx = go.AddComponent<DigitalEffectsMachine>();
        fx.powerBudget = budget;
        fx.machineId = "t1";
        var a = new DigitalEffectNode { id = "a", label = "A" };
        var b = new DigitalEffectNode { id = "b", label = "B" };
        fx.graph.Add(a);
        fx.InsertBefore("a", b);
        Assert.AreEqual("b", fx.graph[0].id);
        Assert.AreEqual("a", fx.graph[1].id);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PowerBudget_WarnsWithoutMuting()
    {
        var go = new GameObject("pwr");
        var budget = go.AddComponent<AudioPowerBudget>();
        budget.maxWatts = 10f;
        budget.Register(new AudioPowerRequirement { componentId = "x", watts = 50f, channels = 1, dspLoad01 = 0.1f });
        Assert.IsTrue(budget.UnrealisticOutputQualityWarning);
        Assert.IsFalse(string.IsNullOrEmpty(budget.WarningMessage));
        Object.DestroyImmediate(go);
    }
}
#endif
