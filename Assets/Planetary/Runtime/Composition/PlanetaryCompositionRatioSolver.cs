using Planetary.Rendering;
using UnityEngine;

namespace Planetary.Composition
{
    public static class PlanetaryCompositionRatioSolver
    {
        public static void CaptureRatiosFromProfile(
            PlanetaryCompositionRatioModel model,
            PlanetBody body,
            PlanetaryCompositionProfile composition,
            AtmosphereRegressionProfile atmosphere,
            HorizonLodSettings horizon,
            PlanetarySdfLodProfile sdfLod)
        {
            if (model == null)
                return;
            float r = body != null ? body.planetRadius : model.anchorRadius;
            model.anchorRadius = Mathf.Max(0.01f, r);

            if (composition != null)
            {
                foreach (var layer in composition.layers)
                {
                    string prefix = LayerPrefix(layer.layer);
                    CaptureField(model, $"{prefix}.offset", layer.shellOffsetMeters, r);
                    CaptureField(model, $"{prefix}.thickness", layer.shellThicknessMeters, r);
                    CaptureField(model, $"{prefix}.smooth", layer.smoothRadius, r, false);
                    CaptureField(model, $"{prefix}.weight", layer.weight, r, false);
                }
            }

            if (atmosphere != null)
            {
                CaptureField(model, "atmos.cloudBase", atmosphere.cloudBaseM, r);
                CaptureField(model, "atmos.cloudTop", atmosphere.cloudTopM, r);
                CaptureField(model, "atmos.troposphereTop", atmosphere.troposphereTopM, r);
                CaptureField(model, "atmos.pressureScaleHeight", atmosphere.pressureScaleHeightM, r);
                CaptureField(model, "atmos.cloudDensityCoeff", atmosphere.cloudDensityCoeff, r, false);
            }

            if (horizon != null)
            {
                CaptureField(model, "horizon.fullSimRadiusKm", horizon.fullSimRadiusKm, r, false);
                CaptureField(model, "horizon.horizonDistanceKm", horizon.horizonDistanceKm, r, false);
            }

            if (sdfLod != null)
            {
                CaptureField(model, "sdf.nearFullSdfKm", sdfLod.nearFullSdfKm, r, false);
                CaptureField(model, "sdf.farFullSdfKm", sdfLod.farFullSdfKm, r, false);
            }
        }

        static string LayerPrefix(PlanetaryCompositionLayer layer)
        {
            switch (layer)
            {
                case PlanetaryCompositionLayer.Core: return "core";
                case PlanetaryCompositionLayer.Mantle: return "mantle";
                case PlanetaryCompositionLayer.Lava: return "lava";
                case PlanetaryCompositionLayer.Crust: return "crust";
                case PlanetaryCompositionLayer.Water: return "water";
                case PlanetaryCompositionLayer.Atmosphere: return "atmosphere";
                case PlanetaryCompositionLayer.Weather: return "weather";
                default: return "layer";
            }
        }

        static void CaptureField(PlanetaryCompositionRatioModel model, string id, float value, float r, bool defaultLocked = true)
        {
            float ratio = r > 1e-6f ? value / r : value;
            if (model.TryGetField(id, out var existing))
            {
                existing.ratio = ratio;
                if (existing.ratioLocked)
                    existing.manualOverride = value;
                else
                    existing.manualOverride = value;
                model.SetField(existing);
            }
            else
            {
                model.SetField(new RatioFieldBinding
                {
                    id = id,
                    ratio = ratio,
                    ratioLocked = defaultLocked,
                    manualOverride = value
                });
            }
        }

        public static void WriteToProfile(
            PlanetaryCompositionRatioModel model,
            PlanetBody body,
            PlanetaryCompositionProfile composition,
            AtmosphereRegressionProfile atmosphere,
            HorizonLodSettings horizon,
            PlanetarySdfLodProfile sdfLod)
        {
            if (model == null)
                return;
            float r = model.anchorRadius;
            if (body != null)
                body.planetRadius = r;

            if (composition != null)
            {
                for (int i = 0; i < composition.layers.Length; i++)
                {
                    var layer = composition.layers[i];
                    string prefix = LayerPrefix(layer.layer);
                    layer.shellOffsetMeters = model.GetValue($"{prefix}.offset");
                    layer.shellThicknessMeters = model.GetValue($"{prefix}.thickness");
                    layer.smoothRadius = model.GetValue($"{prefix}.smooth");
                    layer.weight = model.GetValue($"{prefix}.weight");
                    composition.layers[i] = layer;
                }
            }

            if (atmosphere != null)
            {
                atmosphere.cloudBaseM = model.GetValue("atmos.cloudBase");
                atmosphere.cloudTopM = model.GetValue("atmos.cloudTop");
                atmosphere.troposphereTopM = model.GetValue("atmos.troposphereTop");
                atmosphere.pressureScaleHeightM = model.GetValue("atmos.pressureScaleHeight");
                atmosphere.cloudDensityCoeff = model.GetValue("atmos.cloudDensityCoeff");
            }

            if (horizon != null)
            {
                horizon.fullSimRadiusKm = model.GetValue("horizon.fullSimRadiusKm");
                horizon.horizonDistanceKm = model.GetValue("horizon.horizonDistanceKm");
                horizon.fullSimAltitudeMaxM = model.GetValue("atmos.troposphereTop");
                horizon.troposphereMaxM = model.GetValue("atmos.troposphereTop");
                horizon.surfaceBandMaxM = model.GetValue("crust.thickness") * 6f;
            }

            if (sdfLod != null)
            {
                sdfLod.nearFullSdfKm = model.GetValue("sdf.nearFullSdfKm");
                sdfLod.farFullSdfKm = model.GetValue("sdf.farFullSdfKm");
                sdfLod.sdfHorizonMinAltM = model.GetValue("atmos.cloudBase");
                sdfLod.sdfHorizonFullAltM = model.GetValue("atmos.troposphereTop");
            }
        }

        public static void ApplyAnchorRadius(PlanetaryCompositionRatioModel model, float newR)
        {
            model?.ApplyAnchorRadius(newR);
        }
    }
}
