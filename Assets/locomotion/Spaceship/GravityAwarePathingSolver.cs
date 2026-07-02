using System.Collections.Generic;
using Planetary;
using Planetary.Celestial;
using UnityEngine;

namespace Locomotion.Spaceship
{
    public sealed class GravityAwarePathingSolver
    {
        public GalacticGravitySampleProvider gravityProvider;
        public PhysicalManifoldRelativitySolver relativity;
        [Range(0f, 1f)] public float gravityVsTimeAlpha = 0.5f;
        public bool ignoreGravity;
        public bool ignoreTime;

        public List<Vector3> FindPath(
            HierarchicalPathingSolver context,
            Vector3 start,
            Vector3 goal,
            float bandWidthMeters = 20f)
        {
            var basePath = PhysicalPathingSolverRegistry.FindPathForMedium(
                PhysicalPathingMedium.Space,
                context,
                start,
                goal,
                true);
            if (basePath == null || basePath.Count < 2)
                basePath = new List<Vector3> { start, goal };
            if (ignoreGravity && ignoreTime)
                return basePath;

            if (gravityProvider == null)
                gravityProvider = new GalacticGravitySampleProvider();
            if (relativity == null)
                relativity = Object.FindAnyObjectByType<PhysicalManifoldRelativitySolver>();

            var best = new List<Vector3>(basePath);
            float bestCost = EvaluateCost(basePath);
            for (int pass = 0; pass < 8; pass++)
            {
                var candidate = OffsetPath(basePath, bandWidthMeters * (pass + 1) * 0.1f);
                float c = EvaluateCost(candidate);
                if (c < bestCost)
                {
                    bestCost = c;
                    best = candidate;
                }
            }
            return best;
        }

        float EvaluateCost(List<Vector3> path)
        {
            float time = 0f;
            float grav = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                time += seg;
                Vector3 mid = (path[i - 1] + path[i]) * 0.5f;
                var sample = gravityProvider.Sample(mid);
                grav += sample.strength * seg;
                if (relativity != null)
                {
                    Vector3 dir = (path[i] - path[i - 1]).normalized;
                    float metric = relativity.SampleMetricFactor(mid, dir);
                    grav += (1f - metric) * seg * 10f;
                }
            }
            if (ignoreGravity)
                return time;
            if (ignoreTime)
                return grav;
            return gravityVsTimeAlpha * grav + (1f - gravityVsTimeAlpha) * time;
        }

        static List<Vector3> OffsetPath(List<Vector3> path, float offset)
        {
            var result = new List<Vector3>(path.Count);
            Vector3 side = Vector3.Cross((path[path.Count - 1] - path[0]).normalized, Vector3.up);
            if (side.sqrMagnitude < 1e-6f)
                side = Vector3.right;
            for (int i = 0; i < path.Count; i++)
                result.Add(path[i] + side * offset);
            return result;
        }
    }
}
