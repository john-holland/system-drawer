using NUnit.Framework;
using UnityEngine;

public sealed class HouseWindowAssemblyTests
{
    [Test]
    public void MuntinGrid_ThreeByThree_IsAtLeastSeven()
    {
        Assert.AreEqual(7, MuntinGridLayout.PixelLightMin(3));
        var size = MuntinGridLayout.PixelLightSize(3, 3, false, false, false, true, 1, 1);
        Assert.AreEqual(7, size.x);
        Assert.AreEqual(7, size.y);
    }

    [Test]
    public void MuntinGrid_SideTrimAndUnderSill()
    {
        var withSide = MuntinGridLayout.PixelLightSize(3, 3, true, false, false, true, 1, 1);
        Assert.AreEqual(9, withSide.x);
        Assert.AreEqual(7, withSide.y);
        var withSill = MuntinGridLayout.PixelLightSize(3, 3, false, true, true, true, 1, 1);
        Assert.AreEqual(7, withSill.x);
        Assert.AreEqual(9, withSill.y);
    }

    [Test]
    public void MuntinGrid_ArbitrarySizing_IgnoresMins()
    {
        var size = MuntinGridLayout.PixelLightSize(3, 3, true, true, true, false, 4, 5);
        Assert.AreEqual(4, size.x);
        Assert.AreEqual(5, size.y);
    }

    [Test]
    public void MuntinGrid_TrimRunExcludesElbows()
    {
        Assert.AreEqual(4, MuntinGridLayout.ElbowCount);
        Assert.AreEqual(5, MuntinGridLayout.TrimRunLength(7));
        Assert.IsTrue(MuntinGridLayout.TryParseSillToken("cill"));
        Assert.IsTrue(MuntinGridLayout.TryParseSillToken("sill"));
    }

    [Test]
    public void PaneMuntinWorld_IndependentOfGrid()
    {
        MuntinGridLayout.PaneMuntinWorldSizes(new Vector2(1.2f, 1.2f), 3, 3, 0.03f, out var a, out float barA);
        MuntinGridLayout.PaneMuntinWorldSizes(new Vector2(1.2f, 1.2f), 3, 3, 0.03f, out var b, out float barB);
        Assert.AreEqual(a.x, b.x, 0.0001f);
        Assert.AreEqual(barA, barB, 0.0001f);
        Assert.Greater(a.x, 0.1f);
    }

    [Test]
    public void WindowSpec_ApplyAutoFit_AndSlots()
    {
        var spec = ScriptableObject.CreateInstance<WindowAssemblySpec>();
        spec.paneCountX = 3;
        spec.paneCountY = 3;
        spec.sideTrim = false;
        spec.underSillTrim = false;
        spec.sillOccupiesRow = false;
        spec.autoFitPixelLightGrid = true;
        spec.ApplyAutoFit();
        Assert.AreEqual(7, spec.pixelLightGridW);
        Assert.AreEqual(7, spec.pixelLightGridH);
        spec.autoFitPixelLightGrid = false;
        spec.pixelLightGridW = 3;
        spec.ApplyAutoFit();
        Assert.AreEqual(3, spec.pixelLightGridW);
        var req = BuildingRequirementSpec.CreateDefault("house", CivilSystemKind.House);
        Assert.IsTrue(req.slots.Exists(s => s.slotId == "windows"));
        Assert.IsTrue(req.slots.Exists(s => s.slotId == "window_sill"));
        Assert.IsTrue(req.slots.Exists(s => s.slotId == "window_shade"));
        Object.DestroyImmediate(req);
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void DoubleGlazing_TwoPanes_CavityHasNoHit()
    {
        var spec = ScriptableObject.CreateInstance<WindowAssemblySpec>();
        spec.glazing = WindowGlazingKind.DoubleVacuum;
        var host = new GameObject("igu");
        int n = WindowGlazingBinder.BindPanes(host, spec);
        Assert.AreEqual(2, n);
        Assert.IsFalse(WindowGlazingBinder.CavityHasCollider(host));
        Assert.IsNotNull(host.transform.Find("pane_outer"));
        Assert.IsNotNull(host.transform.Find("pane_inner"));
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void Pulley_ProxyPulse_AndCurveLerp()
    {
        var go = new GameObject("shade");
        var pulley = go.AddComponent<PulleySurfaceRagdoll>();
        pulley.slatCount = 4;
        pulley.EnsureSlats();
        Vector3 head = pulley.headrail.position;
        float far = Vector3.Distance(pulley.slats[pulley.slats.Count - 1].position, head);
        pulley.SetPull01(1f);
        float near = Vector3.Distance(pulley.slats[pulley.slats.Count - 1].position, head);
        Assert.Less(near, far);
        pulley.SetPull01(0.2f);
        pulley.ApplyPullImpulse(1f, 0.2f);
        Assert.Greater(pulley.pull01, 0.2f);
        Assert.AreEqual(0.5f, pulley.LerpPull(0f, 1f, 0.5f), 0.05f);
        var proxy = go.AddComponent<VehicleInstrumentPhysicsProxy>();
        Assert.IsTrue(proxy.TryResolve(PulleySurfaceRagdoll.PullStringId, out var surface));
        surface.ApplyImpulse(1f, 0.1f);
        Assert.Greater(pulley.pull01, 0.2f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PulleyPullNode_ReachesTarget()
    {
        var go = new GameObject("shade");
        var pulley = go.AddComponent<PulleySurfaceRagdoll>();
        var node = go.AddComponent<PulleyPullNode>();
        node.pulley = pulley;
        node.duration = 0f;
        node.raise = true;
        Assert.AreEqual(BehaviorTreeStatus.Success, node.Execute(null));
        Assert.AreEqual(1f, pulley.pull01, 0.01f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TrimRun_IndependentOfMuntinWidth()
    {
        var spec = ScriptableObject.CreateInstance<WindowAssemblySpec>();
        spec.paneCountX = 3;
        spec.paneCountY = 3;
        spec.autoFitPixelLightGrid = true;
        spec.sideTrim = false;
        spec.underSillTrim = false;
        spec.sillOccupiesRow = false;
        spec.muntinWidth = 0.02f;
        spec.ApplyAutoFit();
        int a = spec.TrimRunLengthAlongX();
        spec.muntinWidth = 0.08f;
        spec.ApplyAutoFit();
        Assert.AreEqual(a, spec.TrimRunLengthAlongX());
        Assert.AreEqual(MuntinGridLayout.ElbowCount, 4);
        Object.DestroyImmediate(spec);
    }
}
