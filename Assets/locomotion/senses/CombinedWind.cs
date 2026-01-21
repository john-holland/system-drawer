using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Locomotion.Senses
{
    /// <summary>
    /// Utility for sampling a combined wind vector from:
    /// - Your Weather system (`Weather.Wind`)
    /// - Unity built-in `WindZone` components
    ///
    /// Used by smell advection (and optionally audio heuristics).
    /// </summary>
    public static class CombinedWind
    {
        /// <summary>
        /// Tune how much each wind system contributes.
        /// </summary>
        [System.Serializable]
        public struct Weights
        {
            public float weatherWindWeight;
            public float unityWindWeight;

            public static Weights Default => new Weights
            {
                weatherWindWeight = 1f,
                unityWindWeight = 1f
            };
        }

        private static Wind cachedWeatherWind;
        private static WindZone[] cachedWindZones;
        private static float lastWindZoneScanTime = -999f;
        private const float WindZoneScanIntervalSeconds = 1.0f;

        /// <summary>
        /// Returns a world-space wind vector (m/s-ish) at a position.
        /// </summary>
        public static Vector3 GetWindAtPosition(
            Vector3 worldPosition,
            Weights weights,
            Wind explicitWeatherWind = null)
        {
            Vector3 wind = Vector3.zero;

            // 1) Weather.Wind
            if (weights.weatherWindWeight != 0f)
            {
                Wind w = explicitWeatherWind != null ? explicitWeatherWind : GetCachedWeatherWind();
                if (w != null)
                {
                    float altitude = worldPosition.y;
                    wind += w.GetWindAtPosition(worldPosition, altitude) * weights.weatherWindWeight;
                }
            }

            // 2) Unity WindZone(s)
            if (weights.unityWindWeight != 0f)
            {
                wind += SampleUnityWindZones(worldPosition) * weights.unityWindWeight;
            }

            return wind;
        }

        private static Wind GetCachedWeatherWind()
        {
            if (cachedWeatherWind == null)
            {
                cachedWeatherWind = Object.FindObjectOfType<Wind>();
            }

            return cachedWeatherWind;
        }

        private static Vector3 SampleUnityWindZones(Vector3 worldPosition)
        {
            WindZone[] zones = GetCachedWindZones();
            if (zones == null || zones.Length == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;

            for (int i = 0; i < zones.Length; i++)
            {
                WindZone z = zones[i];
                // WindZone doesn't derive from Behaviour in some Unity profiles, so it may not expose isActiveAndEnabled.
                if (z == null || z.gameObject == null || !z.gameObject.activeInHierarchy)
                    continue;

                // Unity's WindZone is not a physically rigorous wind model; treat it as an art-direction field.
                // We'll map it to a world-space vector in the direction of the zone.
                Vector3 dir;
                float strength = Mathf.Max(0f, z.windMain);

                // Mild turbulence contribution; stable but non-deterministic noise is overkill for now.
                strength += Mathf.Max(0f, z.windTurbulence) * 0.2f;

                switch (z.mode)
                {
                    case WindZoneMode.Directional:
                        dir = z.transform.forward;
                        sum += dir.normalized * strength;
                        break;

                    case WindZoneMode.Spherical:
                        // Radial wind, falling off with radius.
                        Vector3 toPoint = worldPosition - z.transform.position;
                        float distance = toPoint.magnitude;
                        if (distance < 0.0001f)
                            break;

                        float radius = Mathf.Max(0.0001f, z.radius);
                        float t = Mathf.Clamp01(1f - (distance / radius)); 
                        // lol reversing these (distance / radius) to track towards infinity is a funny way of target seeking
                        dir = toPoint / distance; // normalized
                        sum += dir * (strength * t);
                        break;
                }
            }

            return sum;
        }

        private static WindZone[] GetCachedWindZones()
        {
            // Keep this lightweight; it’s OK to miss a WindZone for up to ~1s.
            if (cachedWindZones == null || Time.time - lastWindZoneScanTime >= WindZoneScanIntervalSeconds)
            {
                cachedWindZones = Object.FindObjectsOfType<WindZone>();
                lastWindZoneScanTime = Time.time;
            }

            return cachedWindZones;
        }
    }
}

