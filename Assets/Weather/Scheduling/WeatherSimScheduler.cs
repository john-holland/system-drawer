using System;
using System.Collections.Generic;
using UnityEngine;
using Weather.Activation;

namespace Weather.Scheduling
{
    /// <summary>Per-layer fidelity clocks scaled by emergence activation weight.</summary>
    public sealed class WeatherSimScheduler
    {
        readonly Dictionary<WeatherSimLayerId, float> _nextTick = new Dictionary<WeatherSimLayerId, float>();

        public WeatherSimLayerConfig config;

        public bool ShouldTick(WeatherSimLayerId layerId, float activationWeight, bool insideEgg, float now)
        {
            if (config == null || !config.TryGet(layerId, out WeatherSimLayerConfig.LayerEntry entry))
                return insideEgg && activationWeight >= 0.35f;

            if (activationWeight < entry.activationMinWeight && !insideEgg)
                return false;

            float interval = (insideEgg || activationWeight >= 0.35f)
                ? entry.insideInterval
                : entry.outsideInterval;

            if (interval <= 0f)
                return insideEgg || activationWeight >= entry.activationMinWeight;

            if (!_nextTick.TryGetValue(layerId, out float next) || now >= next)
            {
                _nextTick[layerId] = now + interval;
                return true;
            }

            return false;
        }

        public void Reset() => _nextTick.Clear();
    }
}
