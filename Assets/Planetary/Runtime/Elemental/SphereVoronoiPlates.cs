using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Elemental
{
    /// <summary>Voronoi on sphere from seed directions; boundary stress from mineral incompatibility.</summary>
    public static class SphereVoronoiPlates
    {
        public static int FindPlateId(Vector3 direction, IReadOnlyList<PlateDefinition> plates)
        {
            if (plates == null || plates.Count == 0)
                return 0;
            int best = 0;
            float bestDot = float.NegativeInfinity;
            Vector3 d = direction.normalized;
            for (int i = 0; i < plates.Count; i++)
            {
                float dot = Vector3.Dot(d, plates[i].seedDirection.normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = plates[i].plateId;
                }
            }
            return best;
        }

        public static float MineralCompatibility(MineralStack a, MineralStack b)
        {
            if (a?.weights == null || b?.weights == null)
                return 0.5f;
            float dot = 0f;
            float na = 0f;
            float nb = 0f;
            var mapB = new Dictionary<string, float>();
            for (int i = 0; i < b.weights.Length; i++)
                mapB[b.weights[i].mineralId] = b.weights[i].weight;
            for (int i = 0; i < a.weights.Length; i++)
            {
                float wa = a.weights[i].weight;
                na += wa * wa;
                if (mapB.TryGetValue(a.weights[i].mineralId, out float wb))
                    dot += wa * wb;
            }
            for (int i = 0; i < b.weights.Length; i++)
                nb += b.weights[i].weight * b.weights[i].weight;
            float denom = Mathf.Sqrt(na * nb);
            return denom > 1e-6f ? dot / denom : 0.5f;
        }
    }
}
