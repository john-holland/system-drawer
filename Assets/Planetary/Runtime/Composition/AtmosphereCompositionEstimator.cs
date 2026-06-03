using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Planetary.Composition
{
    public sealed class AtmosphereCompositionEstimator
    {
        const float CloudCoverDeltaThreshold = 0.05f;

        float _lastCloudCover;

        public AtmosphereRegressionProfile Estimate(PlanetBody body, WeatherPhysicsManifold manifold)
        {
            var profile = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            var clouds = Object.FindObjectsByType<Cloud>(FindObjectsSortMode.None);
            float baseSum = 0f;
            float topSum = 0f;
            float coverSum = 0f;
            float densitySum = 0f;
            int n = 0;
            for (int i = 0; i < clouds.Length; i++)
            {
                var c = clouds[i];
                if (c == null)
                    continue;
                baseSum += c.altitude.x;
                topSum += c.altitude.y;
                coverSum += c.coverage;
                densitySum += c.density;
                n++;
            }
            if (manifold != null)
            {
                var bounds = manifold.worldBounds;
                Vector3 step = new Vector3(
                    bounds.size.x / Mathf.Max(1, manifold.cellCount.x),
                    bounds.size.y / Mathf.Max(1, manifold.cellCount.y),
                    bounds.size.z / Mathf.Max(1, manifold.cellCount.z));
                for (int z = 0; z < manifold.cellCount.z; z += 4)
                for (int y = 0; y < manifold.cellCount.y; y += 4)
                for (int x = 0; x < manifold.cellCount.x; x += 4)
                {
                    Vector3 p = bounds.min + new Vector3(x * step.x, y * step.y, z * step.z);
                    var d = manifold.GetDataAtPosition(p);
                    if (d.mode == WeatherMode.Cloud)
                    {
                        coverSum += d.density * 10f;
                        densitySum += d.density;
                        n++;
                    }
                }
            }
            if (n > 0)
            {
                profile.cloudBaseM = baseSum / Mathf.Max(1, clouds.Length);
                profile.cloudTopM = topSum / Mathf.Max(1, clouds.Length);
                profile.cloudDensityCoeff = Mathf.Clamp01((coverSum / n) / 100f) * (densitySum / n);
            }
            var met = Object.FindFirstObjectByType<Meteorology>();
            if (met != null)
            {
                profile.pressureScaleHeightM = 8500f * (met.pressure / 1013.25f);
                _lastCloudCover = met.GetCloudCoverPercentage();
            }
            if (body != null)
                profile.troposphereTopM = Mathf.Max(profile.cloudTopM + 2000f, 12000f);
            return profile;
        }

        public bool ShouldRefresh(float newCloudCover) =>
            Mathf.Abs(newCloudCover - _lastCloudCover) > CloudCoverDeltaThreshold;
    }
}
