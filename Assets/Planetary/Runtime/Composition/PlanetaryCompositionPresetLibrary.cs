using System;
using Planetary.Rendering;
using UnityEngine;

namespace Planetary.Composition
{
    public enum CompositionPresetCategory
    {
        LittlePrince,
        SolSystem,
        SmallAsteroid,
        LargeAsteroid,
        NebulaSpaceZone
    }

    [Serializable]
    public struct PlanetaryCompositionPreset
    {
        public string id;
        public string displayName;
        public CompositionPresetCategory category;
        public float planetRadius;
        public PlanetaryCompositionProfile composition;
        public AtmosphereRegressionProfile atmosphere;
        public HorizonLodSettings horizonLod;
        public PlanetarySdfLodProfile sdfLod;
        public float proceduralNoiseAmplitude;
    }

    [CreateAssetMenu(fileName = "CompositionPresetLibrary", menuName = "Planetary/Composition Preset Library")]
    public sealed class PlanetaryCompositionPresetLibrary : ScriptableObject
    {
        public PlanetaryCompositionPreset[] presets = Array.Empty<PlanetaryCompositionPreset>();

        public static PlanetaryCompositionPresetLibrary CreateWithBuiltInPresets()
        {
            var lib = CreateInstance<PlanetaryCompositionPresetLibrary>();
            lib.presets = new[]
            {
                BuildLittlePrince(),
                BuildSolPlanet("mercury", "Mercury", 2.439e6f, 0.2f),
                BuildSolPlanet("venus", "Venus", 6.052e6f, 0.35f),
                BuildSolPlanet("earth", "Earth", 6.371e6f, 1f),
                BuildSolPlanet("mars", "Mars", 3.390e6f, 0.5f),
                BuildSolPlanet("jupiter", "Jupiter", 6.9911e7f, 2.5f),
                BuildSolPlanet("saturn", "Saturn", 5.8232e7f, 2.2f),
                BuildSolPlanet("uranus", "Uranus", 2.5362e7f, 1.8f),
                BuildSolPlanet("neptune", "Neptune", 2.4622e7f, 1.7f),
                BuildSmallAsteroid("small-asteroid-400", "Small Asteroid (400m)", 400f),
                BuildLargeAsteroid(),
                BuildNebulaZone()
            };
            return lib;
        }

        public bool TryGetPreset(string id, out PlanetaryCompositionPreset preset)
        {
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].id == id)
                {
                    preset = presets[i];
                    return true;
                }
            }
            preset = default;
            return false;
        }

        static PlanetaryCompositionPreset BuildLittlePrince()
        {
            var comp = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            comp.layers = new PlanetaryCompositionProfile.LayerSettings[]
            {
                Layer(PlanetaryCompositionLayer.Core, true, -375, 125, 10, 1),
                Layer(PlanetaryCompositionLayer.Mantle, true, -75, 300, 15, 1),
                Layer(PlanetaryCompositionLayer.Lava, true, -25, 50, 5, 0.85f),
                Layer(PlanetaryCompositionLayer.Crust, true, 0, 25, 3, 1),
                Layer(PlanetaryCompositionLayer.Water, false, 0, 10, 2, 0.3f),
                Layer(PlanetaryCompositionLayer.Atmosphere, true, 0, 400, 20, 0.25f),
                Layer(PlanetaryCompositionLayer.Weather, true, 0, 150, 10, 0.4f)
            };
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = 50;
            atmos.cloudTopM = 150;
            atmos.troposphereTopM = 400;
            atmos.pressureScaleHeightM = 80;
            atmos.cloudDensityCoeff = 0.35f;
            var horizon = ScriptableObject.CreateInstance<HorizonLodSettings>();
            horizon.fullSimRadiusKm = 1f;
            horizon.horizonDistanceKm = 2f;
            horizon.fullSimAltitudeMaxM = 400f;
            horizon.surfaceBandMaxM = 150f;
            horizon.troposphereMaxM = 400f;
            var sdf = ScriptableObject.CreateInstance<PlanetarySdfLodProfile>();
            sdf.tierGridRes = new[] { 12, 24, 32 };
            sdf.nearFullSdfKm = 0.5f;
            sdf.farFullSdfKm = 2f;
            sdf.sdfHorizonMinAltM = 50f;
            sdf.sdfHorizonFullAltM = 400f;
            return new PlanetaryCompositionPreset
            {
                id = "little-prince",
                displayName = "B-612 Little Prince",
                category = CompositionPresetCategory.LittlePrince,
                planetRadius = 500f,
                composition = comp,
                atmosphere = atmos,
                horizonLod = horizon,
                sdfLod = sdf,
                proceduralNoiseAmplitude = 0.02f
            };
        }

        static PlanetaryCompositionPreset BuildSolPlanet(string id, string name, float radius, float atmosScale)
        {
            var model = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
            model.anchorRadius = radius;
            model.ApplyAnchorRadius(radius);
            var comp = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            comp.layers = DefaultEarthLikeLayers(radius, atmosScale);
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = model.GetValue("atmos.cloudBase");
            atmos.cloudTopM = model.GetValue("atmos.cloudTop");
            atmos.troposphereTopM = model.GetValue("atmos.troposphereTop");
            atmos.pressureScaleHeightM = model.GetValue("atmos.pressureScaleHeight");
            atmos.cloudDensityCoeff = 0.35f * atmosScale;
            return new PlanetaryCompositionPreset
            {
                id = id,
                displayName = name,
                category = CompositionPresetCategory.SolSystem,
                planetRadius = radius,
                composition = comp,
                atmosphere = atmos,
                proceduralNoiseAmplitude = 0.01f
            };
        }

        static PlanetaryCompositionPreset BuildSmallAsteroid(string id, string name, float radius)
        {
            var comp = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            comp.layers = new PlanetaryCompositionProfile.LayerSettings[]
            {
                Layer(PlanetaryCompositionLayer.Core, false, -radius * 0.5f, radius * 0.2f, 5, 1),
                Layer(PlanetaryCompositionLayer.Mantle, true, -radius * 0.15f, radius * 0.55f, 8, 1),
                Layer(PlanetaryCompositionLayer.Lava, false, -radius * 0.05f, radius * 0.08f, 3, 0.7f),
                Layer(PlanetaryCompositionLayer.Crust, true, 0, radius * 0.05f, 2, 1),
                Layer(PlanetaryCompositionLayer.Water, false, 0, 5, 1, 0.2f),
                Layer(PlanetaryCompositionLayer.Atmosphere, true, 0, radius * 0.5f, 10, 0.2f),
                Layer(PlanetaryCompositionLayer.Weather, true, 0, radius * 0.2f, 5, 0.3f)
            };
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = radius * 0.1f;
            atmos.cloudTopM = radius * 0.25f;
            atmos.troposphereTopM = radius * 0.6f;
            atmos.pressureScaleHeightM = radius * 0.15f;
            return new PlanetaryCompositionPreset
            {
                id = id,
                displayName = name,
                category = CompositionPresetCategory.SmallAsteroid,
                planetRadius = radius,
                composition = comp,
                atmosphere = atmos,
                proceduralNoiseAmplitude = 0.03f
            };
        }

        static PlanetaryCompositionPreset BuildLargeAsteroid()
        {
            float r = 470000f;
            var comp = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            comp.layers = new PlanetaryCompositionProfile.LayerSettings[]
            {
                Layer(PlanetaryCompositionLayer.Core, true, -r * 0.4f, r * 0.25f, 2000, 1),
                Layer(PlanetaryCompositionLayer.Mantle, true, -r * 0.12f, r * 0.65f, 5000, 1),
                Layer(PlanetaryCompositionLayer.Lava, false, -r * 0.03f, r * 0.05f, 500, 0.5f),
                Layer(PlanetaryCompositionLayer.Crust, true, 0, r * 0.03f, 200, 1),
                Layer(PlanetaryCompositionLayer.Water, false, 0, 1000, 50, 0.2f),
                Layer(PlanetaryCompositionLayer.Atmosphere, false, 0, 5000, 500, 0.1f),
                Layer(PlanetaryCompositionLayer.Weather, false, 0, 2000, 200, 0.1f)
            };
            return new PlanetaryCompositionPreset
            {
                id = "ceres-large-asteroid",
                displayName = "Ceres-like Large Asteroid",
                category = CompositionPresetCategory.LargeAsteroid,
                planetRadius = r,
                composition = comp,
                proceduralNoiseAmplitude = 0.005f
            };
        }

        static PlanetaryCompositionPreset BuildNebulaZone()
        {
            float r = 50000f;
            var comp = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            comp.layers = new PlanetaryCompositionProfile.LayerSettings[]
            {
                Layer(PlanetaryCompositionLayer.Core, true, -r * 0.1f, r * 0.08f, 500, 0.3f),
                Layer(PlanetaryCompositionLayer.Mantle, false, 0, 100, 50, 0.2f),
                Layer(PlanetaryCompositionLayer.Lava, false, 0, 50, 20, 0.1f),
                Layer(PlanetaryCompositionLayer.Crust, false, 0, 100, 30, 0.2f),
                Layer(PlanetaryCompositionLayer.Water, false, 0, 100, 20, 0.1f),
                Layer(PlanetaryCompositionLayer.Atmosphere, true, 0, r * 3f, 2000, 0.6f),
                Layer(PlanetaryCompositionLayer.Weather, true, 0, r * 1.5f, 1000, 0.8f)
            };
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            atmos.cloudBaseM = r * 0.05f;
            atmos.cloudTopM = r * 0.4f;
            atmos.troposphereTopM = r * 2f;
            atmos.cloudDensityCoeff = 0.85f;
            return new PlanetaryCompositionPreset
            {
                id = "nebula-space-zone",
                displayName = "Nebula / Space Zone",
                category = CompositionPresetCategory.NebulaSpaceZone,
                planetRadius = r,
                composition = comp,
                atmosphere = atmos,
                proceduralNoiseAmplitude = 0.08f
            };
        }

        static PlanetaryCompositionProfile.LayerSettings[] DefaultEarthLikeLayers(float r, float scale)
        {
            var m = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
            m.anchorRadius = r;
            m.ApplyAnchorRadius(r);
            return new PlanetaryCompositionProfile.LayerSettings[]
            {
                Layer(PlanetaryCompositionLayer.Core, true, m.GetValue("core.offset"), m.GetValue("core.thickness"), m.GetValue("core.smooth"), 1),
                Layer(PlanetaryCompositionLayer.Mantle, true, m.GetValue("mantle.offset"), m.GetValue("mantle.thickness"), m.GetValue("mantle.smooth"), 1),
                Layer(PlanetaryCompositionLayer.Lava, true, m.GetValue("lava.offset"), m.GetValue("lava.thickness"), m.GetValue("lava.smooth"), 0.85f),
                Layer(PlanetaryCompositionLayer.Crust, true, m.GetValue("crust.offset"), m.GetValue("crust.thickness"), m.GetValue("crust.smooth"), 1),
                Layer(PlanetaryCompositionLayer.Water, scale > 0.8f, 0, r * 0.001f * scale, 50, 0.5f),
                Layer(PlanetaryCompositionLayer.Atmosphere, true, 0, m.GetValue("atmosphere.thickness") * scale, 500 * scale, 0.3f),
                Layer(PlanetaryCompositionLayer.Weather, true, 0, m.GetValue("weather.thickness") * scale, 200 * scale, 0.4f)
            };
        }

        static PlanetaryCompositionProfile.LayerSettings Layer(
            PlanetaryCompositionLayer layer, bool enabled,
            float offset, float thickness, float smooth, float weight) =>
            new PlanetaryCompositionProfile.LayerSettings
            {
                layer = layer,
                enabled = enabled,
                shellOffsetMeters = offset,
                shellThicknessMeters = thickness,
                smoothRadius = smooth,
                weight = weight
            };
    }
}
