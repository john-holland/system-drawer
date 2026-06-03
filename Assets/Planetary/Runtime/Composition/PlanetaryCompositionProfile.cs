using System;
using UnityEngine;

namespace Planetary.Composition
{
    [CreateAssetMenu(fileName = "PlanetaryCompositionProfile", menuName = "Planetary/Composition Profile")]
    public sealed class PlanetaryCompositionProfile : ScriptableObject
    {
        [Serializable]
        public struct LayerSettings
        {
            public PlanetaryCompositionLayer layer;
            public bool enabled;
            public float shellOffsetMeters;
            public float shellThicknessMeters;
            public float smoothRadius;
            public float weight;
        }

        public LayerSettings[] layers =
        {
            new LayerSettings { layer = PlanetaryCompositionLayer.Core, enabled = false },
            new LayerSettings { layer = PlanetaryCompositionLayer.Mantle, enabled = true, shellOffsetMeters = -50000f, shellThicknessMeters = 50000f, smoothRadius = 2000f, weight = 1f },
            new LayerSettings { layer = PlanetaryCompositionLayer.Lava, enabled = true, shellOffsetMeters = -2000f, shellThicknessMeters = 2000f, smoothRadius = 50f, weight = 0.8f },
            new LayerSettings { layer = PlanetaryCompositionLayer.Crust, enabled = true, shellOffsetMeters = 0f, shellThicknessMeters = 5000f, smoothRadius = 20f, weight = 1f },
            new LayerSettings { layer = PlanetaryCompositionLayer.Water, enabled = true, shellOffsetMeters = 0f, shellThicknessMeters = 4000f, smoothRadius = 5f, weight = 0.5f },
            new LayerSettings { layer = PlanetaryCompositionLayer.Atmosphere, enabled = true, shellOffsetMeters = 12000f, shellThicknessMeters = 80000f, smoothRadius = 500f, weight = 0.3f },
            new LayerSettings { layer = PlanetaryCompositionLayer.Weather, enabled = true, shellOffsetMeters = 2000f, shellThicknessMeters = 8000f, smoothRadius = 200f, weight = 0.4f }
        };
    }
}
