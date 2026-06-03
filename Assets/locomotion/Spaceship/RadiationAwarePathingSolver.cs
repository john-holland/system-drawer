using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Locomotion.Spaceship
{
    public sealed class RadiationAwarePathingSolver
    {
        public PhysicsManifold radiationManifold;
        [Range(0f, 1f)] public float radiationVsTimeAlpha = 0.5f;
        public bool ignoreRadiation;
        public bool ignoreTime;

        public List<Vector3> FindPath(
            HierarchicalPathingSolver context,
            Vector3 start,
            Vector3 goal,
            float bandWidthMeters = 20f)
        {
            var basePath = context != null
                ? context.FindPath(start, goal, true)
                : new List<Vector3> { start, goal };
            if (basePath == null || basePath.Count < 2 || ignoreRadiation && ignoreTime)
                return basePath;

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
            float rad = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                time += seg;
                if (radiationManifold != null)
                    rad += radiationManifold.SampleRadiation(path[i]) * seg;
            }
            if (ignoreRadiation)
                return time;
            if (ignoreTime)
                return rad;
            return radiationVsTimeAlpha * rad + (1f - radiationVsTimeAlpha) * time;
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
