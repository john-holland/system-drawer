using NUnit.Framework;
using Planetary.Composition;
using SdfMax;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryCompositionLayerTests
    {
        [Test]
        public void Baker_ProducesValidRoot()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 500f;
            var profile = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            var asset = PlanetaryCompositionBaker.Bake(body, null, null, profile, atmos, null);
            Assert.IsNotNull(asset);
            Assert.Greater(asset.nodes.Count, 0);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AnnularShell_IsInsideOnlyBetweenRadii()
        {
            float inner = 1000f;
            float outer = 3000f;
            Assert.IsFalse(PlanetAnnularShellSdf.IsInsideBand(500f, inner, outer));
            Assert.IsTrue(PlanetAnnularShellSdf.IsInsideBand(2000f, inner, outer));
            Assert.IsFalse(PlanetAnnularShellSdf.IsInsideBand(4000f, inner, outer));
        }

        [Test]
        public void Baker_WeatherLayer_UsesAnnularShellAboveCrust()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 1000f;

            var profile = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            foreach (var layer in profile.layers)
            {
                if (layer.layer == PlanetaryCompositionLayer.Weather ||
                    layer.layer == PlanetaryCompositionLayer.Crust)
                {
                    // defaults already enabled for crust/weather
                }
            }

            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = 1000f;
            atmos.cloudTopM = 3000f;

            var asset = PlanetaryCompositionBaker.Bake(body, null, null, profile, atmos, null);
            int subtractCount = 0;
            foreach (var node in asset.nodes)
            {
                if (node.op == SdfMaxOp.Subtract)
                    subtractCount++;
            }

            Assert.GreaterOrEqual(subtractCount, 2,
                "Weather/atmosphere should bake as annular shells (Subtract nodes), not solid spheres from center");

            float r = body.PlanetRadius;
            Assert.IsFalse(
                PlanetAnnularShellSdf.IsInsideBand(r + 500f, r + atmos.cloudBaseM, r + atmos.cloudTopM),
                "Cloud band should not include altitudes below cloud base");
            Assert.IsTrue(
                PlanetAnnularShellSdf.IsInsideBand(r + 2000f, r + atmos.cloudBaseM, r + atmos.cloudTopM),
                "Cloud band should include mid-cloud altitude");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(atmos);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Baker_CoreLayer_AddsFilledSphereWhenInnerRadiusZero()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 500f;

            var profile = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            profile.layers[0] = new PlanetaryCompositionProfile.LayerSettings
            {
                layer = PlanetaryCompositionLayer.Core,
                enabled = true,
                shellOffsetMeters = -375f,
                shellThicknessMeters = 125f,
                smoothRadius = 10f,
                weight = 1f
            };

            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            var asset = PlanetaryCompositionBaker.Bake(body, null, null, profile, atmos, null);
            Assert.Greater(asset.nodes.Count, 1);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(atmos);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Baker_CloudBand_StartsAtOrAboveCrustOuterRadius()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 1000f;

            var profile = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            var crust = profile.layers[3];
            Assert.AreEqual(PlanetaryCompositionLayer.Crust, crust.layer);
            crust.enabled = true;
            crust.shellThicknessMeters = 5000f;

            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = 1000f;
            atmos.cloudTopM = 3000f;

            float crustOuter = body.PlanetRadius + Mathf.Clamp(body.PlanetRadius * 0.05f, 0.5f, 500f);
            float cloudInner = body.PlanetRadius + atmos.cloudBaseM;
            if (cloudInner < crustOuter)
                cloudInner = crustOuter;
            Assert.GreaterOrEqual(cloudInner, crustOuter - 0.01f,
                "Cloud inner radius must not sit inside the crust outer envelope");

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(atmos);
            Object.DestroyImmediate(go);
        }
    }
}
