using NUnit.Framework;
using UnityEngine;

public sealed class CanalTravelTests
{
    [Test]
    public void WaterBake_SamplesSpeed_NoRebakeOnSameHash()
    {
        var spec = ScriptableObject.CreateInstance<CanalRibbonSpec>();
        spec.lengthM = 80f;
        spec.authoredSpeedMps = 1.2f;
        spec.authoredVelocityMps = 1.2f;
        spec.bakeBinM = 8f;
        var field = new CanalWaterBakeField();
        field.Bake(spec);
        Assert.Greater(field.BinCount, 2);
        Assert.AreEqual(1, field.bakeCount);
        float s = field.SampleSpeed(0.5f);
        Assert.Greater(s, 0.5f);
        field.Bake(spec);
        Assert.AreEqual(1, field.bakeCount);
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void LockSpec_RejectsOversizeShip_WaterLimits()
    {
        var lockSpec = ScriptableObject.CreateInstance<CanalLockSpec>();
        Assert.IsTrue(lockSpec.AllowsShip(40f, 10f, 3f));
        Assert.IsFalse(lockSpec.AllowsShip(90f, 10f, 3f));
        Assert.IsTrue(lockSpec.WaterInLimits(8000f));
        Assert.IsFalse(lockSpec.WaterInLimits(50f));
        Object.DestroyImmediate(lockSpec);
    }

    [Test]
    public void CannalAgent_DiamondAxes_DefaultPipeline()
    {
        var go = new GameObject("canal_ta");
        try
        {
            var agent = go.AddComponent<CannalTravelAgent>();
            Assert.AreEqual(4, CannalTravelAgent.DefaultPipeline().Count);
            Assert.AreEqual(4, agent.BlueOptimal01().Length);
            Assert.AreEqual("Speed", CannalTravelAgent.DiamondAxes[0]);
            Assert.IsFalse(agent.OverLimit());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void CannalAgent_InsertStep_AfterSelected()
    {
        var go = new GameObject("canal_insert");
        try
        {
            var agent = go.AddComponent<CannalTravelAgent>();
            agent.steps = CannalTravelAgent.DefaultPipeline();
            agent.selectedStepIndex = 1;
            var added = agent.InsertStep(CannalStepKind.Lock, 1);
            Assert.AreEqual(5, agent.steps.Count);
            Assert.AreEqual(CannalStepKind.Lock, agent.steps[2].kind);
            Assert.AreEqual(added, agent.SelectedStep);
            Assert.IsTrue(agent.DiamondInterpretation().Contains("Lock"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void WaterBake_ForceRefreshIncrementsCount()
    {
        var spec = ScriptableObject.CreateInstance<CanalRibbonSpec>();
        try
        {
            var field = new CanalWaterBakeField();
            field.Bake(spec);
            Assert.AreEqual(1, field.bakeCount);
            field.Bake(spec, true);
            Assert.AreEqual(2, field.bakeCount);
        }
        finally
        {
            Object.DestroyImmediate(spec);
        }
    }

    [Test]
    public void CanalBio_FacilitateLockCard()
    {
        var go = new GameObject("canal_bio");
        try
        {
            var bio = go.AddComponent<CanalBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = "lock", worldTarget = Vector3.one });
            Assert.Greater(cards.Count, 0);
            Assert.IsInstanceOf<CanalLockTransitCard>(cards[cards.Count - 1]);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
