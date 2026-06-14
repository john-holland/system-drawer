using Planetary.Composition;
using UnityEngine;

namespace Planetary.Field
{
    /// <summary>Normalized per-chart weights from altitude and surface distance (smoothstep bands).</summary>
    public struct TransitionWeightSet
    {
        public float world;
        public float planetShell;
        public float surfaceTangent;
        public float spaceTimeMetric;
        public float narrativeTimeSlice;

        public float Sum =>
            world + planetShell + surfaceTangent + spaceTimeMetric + narrativeTimeSlice;

        public static TransitionWeightSet Compute(
            float altitudeMsl,
            float surfaceDistanceKm,
            float cloudBaseM,
            float cloudTopM,
            HorizonLodSettings settings)
        {
            float surfaceMax = settings != null ? settings.surfaceBandMaxM : 2000f;
            float tropoMax = settings != null ? settings.troposphereMaxM : 12000f;
            float upperMax = settings != null ? settings.upperAtmosphereMaxM : 80000f;
            float horizonKm = settings != null ? settings.horizonDistanceKm : 500f;

            float wSurface = 1f - SmoothStep(surfaceMax, tropoMax, altitudeMsl);
            float wAtmosphere = SmoothBell(tropoMax, upperMax, altitudeMsl);
            float wSpace = SmoothStep(upperMax * 0.85f, upperMax, altitudeMsl)
                           + SmoothStep(horizonKm * 0.6f, horizonKm, surfaceDistanceKm);
            wSpace = Mathf.Clamp01(wSpace);
            float wShell = SmoothStep(tropoMax * 0.5f, tropoMax, altitudeMsl) * (1f - wSpace);

            var set = new TransitionWeightSet
            {
                world = Mathf.Max(0.05f, wSurface * 0.35f + wAtmosphere * 0.2f),
                planetShell = wShell,
                surfaceTangent = wSurface,
                spaceTimeMetric = wSpace * 0.5f,
                narrativeTimeSlice = 0.05f
            };

            float sum = set.Sum;
            if (sum <= 1e-6f)
                return DefaultUniform();

            float inv = 1f / sum;
            set.world *= inv;
            set.planetShell *= inv;
            set.surfaceTangent *= inv;
            set.spaceTimeMetric *= inv;
            set.narrativeTimeSlice *= inv;
            return set;
        }

        static TransitionWeightSet DefaultUniform()
        {
            return new TransitionWeightSet
            {
                world = 0.4f,
                planetShell = 0.15f,
                surfaceTangent = 0.35f,
                spaceTimeMetric = 0.05f,
                narrativeTimeSlice = 0.05f
            };
        }

        static float SmoothStep(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1))
                return x >= edge1 ? 1f : 0f;
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        static float SmoothBell(float centerStart, float centerEnd, float x)
        {
            float mid = (centerStart + centerEnd) * 0.5f;
            float half = (centerEnd - centerStart) * 0.5f;
            if (half <= 1e-4f)
                return 0f;
            float d = Mathf.Abs(x - mid) / half;
            return Mathf.Clamp01(1f - d * d);
        }

        public float WeightFor(SpatiotemporalChart chart)
        {
            switch (chart)
            {
                case SpatiotemporalChart.PlanetShell: return planetShell;
                case SpatiotemporalChart.SurfaceTangent: return surfaceTangent;
                case SpatiotemporalChart.SpaceTimeMetric: return spaceTimeMetric;
                case SpatiotemporalChart.NarrativeTimeSlice: return narrativeTimeSlice;
                default: return world;
            }
        }

        public SpatiotemporalChart DominantChart()
        {
            SpatiotemporalChart best = SpatiotemporalChart.World;
            float bestW = world;
            if (planetShell > bestW) { bestW = planetShell; best = SpatiotemporalChart.PlanetShell; }
            if (surfaceTangent > bestW) { bestW = surfaceTangent; best = SpatiotemporalChart.SurfaceTangent; }
            if (spaceTimeMetric > bestW) { bestW = spaceTimeMetric; best = SpatiotemporalChart.SpaceTimeMetric; }
            if (narrativeTimeSlice > bestW) best = SpatiotemporalChart.NarrativeTimeSlice;
            return best;
        }
    }
}
