using System;
using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Composition
{
    [Serializable]
    public struct RatioFieldBinding
    {
        public string id;
        public float ratio;
        public bool ratioLocked;
        public float manualOverride;
        public float CurrentValue(float anchorR) =>
            ratioLocked ? ratio * anchorR : manualOverride;
    }

    /// <summary>Anchor radius R and per-field ratio bindings for composition UI.</summary>
    [Serializable]
    public sealed class PlanetaryCompositionRatioModel
    {
        public float anchorRadius = 500f;
        public List<RatioFieldBinding> fields = new List<RatioFieldBinding>();

        public static PlanetaryCompositionRatioModel CreateLittlePrinceDefaults()
        {
            var m = new PlanetaryCompositionRatioModel { anchorRadius = 500f };
            m.fields = new List<RatioFieldBinding>
            {
                Field("core.offset", -0.75f, true),
                Field("core.thickness", 0.25f, true),
                Field("core.smooth", 0.02f, false, 10f),
                Field("core.weight", 1f, false, 1f),
                Field("mantle.offset", -0.15f, true),
                Field("mantle.thickness", 0.60f, true),
                Field("mantle.smooth", 0.03f, false, 15f),
                Field("mantle.weight", 1f, false, 1f),
                Field("lava.offset", -0.05f, true),
                Field("lava.thickness", 0.10f, true),
                Field("lava.smooth", 0.01f, false, 5f),
                Field("lava.weight", 1f, false, 0.85f),
                Field("crust.offset", 0f, true),
                Field("crust.thickness", 0.05f, true),
                Field("crust.smooth", 0.006f, false, 3f),
                Field("crust.weight", 1f, false, 1f),
                Field("atmosphere.thickness", 0.80f, true),
                Field("atmosphere.smooth", 0.04f, false, 20f),
                Field("weather.thickness", 0.30f, true),
                Field("weather.smooth", 0.02f, false, 10f),
                Field("atmos.cloudBase", 0.10f, true),
                Field("atmos.cloudTop", 0.30f, true),
                Field("atmos.troposphereTop", 0.80f, true),
                Field("atmos.pressureScaleHeight", 0.16f, true),
                Field("atmos.cloudDensityCoeff", 0.35f, false, 0.35f),
                Field("horizon.fullSimRadiusKm", 0.002f, false, 1f),
                Field("horizon.horizonDistanceKm", 0.004f, false, 2f),
                Field("sdf.nearFullSdfKm", 0.001f, false, 0.5f),
                Field("sdf.farFullSdfKm", 0.004f, false, 2f),
            };
            return m;
        }

        static RatioFieldBinding Field(string id, float ratio, bool locked, float manual = 0f) =>
            new RatioFieldBinding { id = id, ratio = ratio, ratioLocked = locked, manualOverride = manual };

        public bool TryGetField(string id, out RatioFieldBinding binding)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].id == id)
                {
                    binding = fields[i];
                    return true;
                }
            }
            binding = default;
            return false;
        }

        public void SetField(RatioFieldBinding binding)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].id == binding.id)
                {
                    fields[i] = binding;
                    return;
                }
            }
            fields.Add(binding);
        }

        public float GetValue(string id)
        {
            if (TryGetField(id, out var b))
                return b.CurrentValue(anchorRadius);
            return 0f;
        }

        public void ApplyAnchorRadius(float newR)
        {
            anchorRadius = newR;
            for (int i = 0; i < fields.Count; i++)
            {
                if (!fields[i].ratioLocked)
                    continue;
                var f = fields[i];
                f.manualOverride = f.ratio * anchorRadius;
                fields[i] = f;
            }
        }

        public void LockAllShellGeometry()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if (f.id.Contains("smooth") || f.id.Contains("weight") || f.id.StartsWith("horizon") || f.id.StartsWith("sdf"))
                    continue;
                f.ratioLocked = true;
                fields[i] = f;
            }
        }

        public void UnlockAllArtistic()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if (f.id.Contains("smooth") || f.id.Contains("weight") || f.id.Contains("cloudDensity"))
                {
                    f.ratioLocked = false;
                    f.manualOverride = f.ratioLocked ? f.ratio * anchorRadius : f.manualOverride;
                    fields[i] = f;
                }
            }
        }
    }
}
