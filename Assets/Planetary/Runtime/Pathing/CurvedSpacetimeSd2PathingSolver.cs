using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public sealed class CurvedSpacetimeSd2PathingSolver : IPhysicalPathingSolver
    {
        public PhysicalPathingMedium Medium => PhysicalPathingMedium.Space;
        public PhysicalManifoldRelativitySolver Relativity;
        public PlanetPathingBackend PlanetBackend = new PlanetPathingBackend();

        public bool TryFindPath(
            HierarchicalPathingSolver context,
            Vector3 startWorld,
            Vector3 goalWorld,
            bool returnBestEffortPathWhenNoPath,
            out List<Vector3> path)
        {
            path = new List<Vector3> { startWorld };
            if (context == null)
                return false;

            int steps = 24;
            Vector3 center = PlanetBackend?.ResolvePlanetCenter() ?? Vector3.zero;
            float radius = PlanetBackend?.ResolvePlanetRadius() ?? Vector3.Distance(startWorld, center);
            Vector3 startDir = radius > 1e-3f ? (startWorld - center).normalized : (goalWorld - startWorld).normalized;
            Vector3 goalDir = radius > 1e-3f ? (goalWorld - center).normalized : startDir;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 p;
                if (radius > 1e-3f)
                    p = center + Vector3.Slerp(startDir, goalDir, t) * radius;
                else
                    p = Vector3.Lerp(startWorld, goalWorld, t);
                if (Relativity != null)
                {
                    Vector3 dir = (goalWorld - startWorld).normalized;
                    float metric = Relativity.SampleMetricFactor(p, dir);
                    p = Vector3.Lerp(startWorld, goalWorld, Mathf.Clamp01(t * metric));
                }
                path.Add(p);
            }

            return path.Count > 1;
        }
    }
}
