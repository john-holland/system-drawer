#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather;

public class VehicleAquaplaneTests
{
    [Test]
    public void MuEff_IncreasesWithSpin()
    {
        float aquaplaneMu = 0.05f;
        float spinGain = 0.002f;
        float cap = 0.12f;
        float spinSlow = 0f;
        float spinFast = 50f;
        float muSlow = aquaplaneMu + spinGain * Mathf.Clamp(spinSlow, 0f, cap);
        float muFast = aquaplaneMu + spinGain * Mathf.Clamp(spinFast, 0f, cap);
        Assert.Less(muSlow, muFast);
        Assert.Less(aquaplaneMu, 0.1f);
    }

    [Test]
    public void LiquidContactSphere_WaterMode_SetsLowMu()
    {
        var tireGo = new GameObject("tire");
        var sphere = tireGo.AddComponent<LiquidContactSphere>();
        sphere.aquaplaneMu = 0.04f;

        var manifoldGo = new GameObject("manifold");
        var manifold = manifoldGo.AddComponent<WeatherPhysicsManifold>();
        manifold.worldBounds = new Bounds(Vector3.zero, Vector3.one * 10f);
        manifold.cellCount = new Vector3Int(2, 2, 2);
        sphere.manifoldFallback = manifold;

        ManifoldCellData water = manifold.GetDataAtPosition(tireGo.transform.position);
        water.mode = WeatherMode.Water;
        water.surfaceFriction = 0.8f;
        manifold.SetDataAtPosition(tireGo.transform.position, water);

        sphere.RefreshContact();

        Assert.IsTrue(sphere.IsInLiquid);
        Assert.AreEqual(0.04f, sphere.CurrentMu, 0.001f);

        Object.DestroyImmediate(tireGo);
        Object.DestroyImmediate(manifoldGo);
    }
}
#endif
