using System.Collections.Generic;
using UnityEngine;

namespace Weather.Lod
{
    /// <summary>Tracks all active player weather egg zones.</summary>
    public sealed class PlayerWeatherEggRegistry
    {
        readonly List<PlayerWeatherEggZone> _eggs = new List<PlayerWeatherEggZone>(8);

        public IReadOnlyList<PlayerWeatherEggZone> Eggs => _eggs;

        public void Register(PlayerWeatherEggZone zone)
        {
            if (zone == null || _eggs.Contains(zone))
                return;
            _eggs.Add(zone);
        }

        public void Unregister(PlayerWeatherEggZone zone)
        {
            if (zone == null)
                return;
            _eggs.Remove(zone);
        }

        public Bounds GetCombinedBounds()
        {
            if (_eggs.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);
            Bounds b = WeatherEggBounds.GetAabb(_eggs[0].Center, _eggs[0].Radii);
            for (int i = 1; i < _eggs.Count; i++)
            {
                PlayerWeatherEggZone egg = _eggs[i];
                if (egg == null)
                    continue;
                b.Encapsulate(WeatherEggBounds.GetAabb(egg.Center, egg.Radii));
            }
            return b;
        }

        public float ComputeOverlapWeight(PlayerWeatherEggZone a, PlayerWeatherEggZone b, Vector3 world)
        {
            if (a == null || b == null)
                return 0.5f;
            return WeatherEggBounds.OverlapGradientWeight(a.Center, a.Radii, b.Center, b.Radii, world);
        }
    }
}
