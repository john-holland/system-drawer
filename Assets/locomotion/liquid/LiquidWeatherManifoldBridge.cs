using System;
using UnityEngine;
using Weather;

namespace Locomotion.Liquid
{
    /// <summary>Bridges drink/spill liquid to baked WeatherPhysicsManifold cells.</summary>
    public sealed class LiquidWeatherManifoldBridge : MonoBehaviour
    {
        public WeatherPhysicsManifold manifold;
        [Range(0f, 1f)] public float paintBlend = 0.35f;
        public float defaultWaterDensity = 1000f;
        public float defaultSurfaceTension = 0.02f;

        public WeatherPhysicsManifold ResolveManifold()
        {
            if (manifold != null)
                return manifold;
            manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            return manifold;
        }

        public ManifoldCellData SampleAt(Vector3 worldPos)
        {
            var m = ResolveManifold();
            if (m == null)
                return default;
            return m.GetDataAtPosition(worldPos);
        }

        public void PaintWaterSphere(Vector3 center, float radius, Vector3 velocity, float pressurePa)
        {
            var m = ResolveManifold();
            if (m == null || radius <= 0f)
                return;

            float hPa = pressurePa > 0f ? pressurePa / 100f : 1013.25f;
            var existing = m.GetDataAtPosition(center);
            var target = new ManifoldCellData
            {
                velocity = velocity,
                pressure = hPa,
                temperature = existing.temperature,
                density = defaultWaterDensity,
                mode = WeatherMode.Water,
                surfaceTensionCoeff = defaultSurfaceTension,
                surfaceFriction = 0.05f,
            };
            var blended = Blend(existing, target, paintBlend);
            m.SetDataAtPosition(center, blended);

            if (radius > m.cellResolution)
            {
                int steps = Mathf.CeilToInt(radius / Mathf.Max(m.cellResolution, 0.01f));
                for (int i = -steps; i <= steps; i++)
                {
                    for (int j = -steps; j <= steps; j++)
                    {
                        for (int k = -steps; k <= steps; k++)
                        {
                            var offset = new Vector3(i, j, k) * m.cellResolution;
                            if (offset.magnitude > radius)
                                continue;
                            var ex = m.GetDataAtPosition(center + offset);
                            m.SetDataAtPosition(center + offset, Blend(ex, target, paintBlend * 0.5f));
                        }
                    }
                }
            }
        }

        public void PaintSpillFootprint(Vector3 hit, float liters, Vector3 spread)
        {
            float radius = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(liters, 0.001f)) * 0.08f, 0.02f, 0.4f);
            PaintWaterSphere(hit, radius, spread, 101325f);
        }

        static ManifoldCellData Blend(ManifoldCellData a, ManifoldCellData b, float t)
        {
            t = Mathf.Clamp01(t);
            return new ManifoldCellData
            {
                velocity = Vector3.Lerp(a.velocity, b.velocity, t),
                pressure = Mathf.Lerp(a.pressure, b.pressure, t),
                temperature = Mathf.Lerp(a.temperature, b.temperature, t),
                density = Mathf.Lerp(a.density, b.density, t),
                mode = ChooseBlendedMode(a.mode, b.mode, t),
                surfaceTensionCoeff = Mathf.Lerp(a.surfaceTensionCoeff, b.surfaceTensionCoeff, t),
                surfaceFriction = Mathf.Lerp(a.surfaceFriction, b.surfaceFriction, t),
            };
        }

        static WeatherMode ChooseBlendedMode(WeatherMode existing, WeatherMode target, float t)
        {
            if (t <= 0f)
                return existing;
            if (t >= 1f)
                return target;
            if (existing == WeatherMode.Air && target != WeatherMode.Air)
                return target;
            if (target == WeatherMode.Air && existing != WeatherMode.Air)
                return existing;
            return t > 0.5f ? target : existing;
        }
    }
}
