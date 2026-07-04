using System;
using UnityEngine;

namespace Weather.Scheduling
{
    [CreateAssetMenu(fileName = "WeatherSimLayerConfig", menuName = "Weather/Sim Layer Config", order = 20)]
    public sealed class WeatherSimLayerConfig : ScriptableObject
    {
        [Serializable]
        public struct LayerEntry
        {
            public WeatherSimLayerId layerId;
            [Tooltip("Seconds between ticks inside emergence corridor.")]
            public float insideInterval;
            [Tooltip("Seconds between ticks outside emergence (0 = off).")]
            public float outsideInterval;
            [Tooltip("Minimum emergence weight to tick inside corridor.")]
            public float activationMinWeight;
        }

        public LayerEntry[] layers =
        {
            new LayerEntry { layerId = WeatherSimLayerId.L0_MeteorologyGuess, insideInterval = 1f, outsideInterval = 5f, activationMinWeight = 0f },
            new LayerEntry { layerId = WeatherSimLayerId.L1_CoarseAdvection, insideInterval = 0.25f, outsideInterval = 2f, activationMinWeight = 0.1f },
            new LayerEntry { layerId = WeatherSimLayerId.L2_EggManifold, insideInterval = 0f, outsideInterval = 0f, activationMinWeight = 0.35f },
            new LayerEntry { layerId = WeatherSimLayerId.L3_NearFieldWind, insideInterval = 0.066f, outsideInterval = 0f, activationMinWeight = 0.35f },
            new LayerEntry { layerId = WeatherSimLayerId.L4_VisualClouds, insideInterval = 0.1f, outsideInterval = 0f, activationMinWeight = 0.5f },
        };

        public bool TryGet(WeatherSimLayerId id, out LayerEntry entry)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].layerId == id)
                {
                    entry = layers[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
