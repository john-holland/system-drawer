using System;
using System.Collections.Generic;
using UnityEngine;
using Weather.Lod;

namespace Planetary.Celestial
{
    [Serializable]
    public struct GalacticNightSkyCacheRecord
    {
        public string cacheId;
        public string observerBodyId;
        public float anchorLat;
        public float anchorLon;
        public float anchorAltM;
        public string localPath;
        public int starCount;
    }

    [Serializable]
    public struct GalacticSkyLatticeCell
    {
        public string cellId;
        public Vector3 centroid;
        public Vector3 eggRadii;
        public string[] cacheIds;
        public float[] weights;
    }

    /// <summary>Egg-LOD spatial index for blending night-sky cubemap caches.</summary>
    public sealed class GalacticSkyLatticeIndex
    {
        readonly List<GalacticSkyLatticeCell> _cells = new List<GalacticSkyLatticeCell>();

        public void Load(IEnumerable<GalacticSkyLatticeCell> cells)
        {
            _cells.Clear();
            if (cells == null)
                return;
            _cells.AddRange(cells);
        }

        public bool TrySampleBlend(Vector3 world, out GalacticSkyLatticeCell cell, out float weight)
        {
            cell = default;
            weight = 0f;
            if (_cells.Count == 0)
                return false;

            float bestW = -1f;
            int bestIdx = -1;
            for (int i = 0; i < _cells.Count; i++)
            {
                float w = WeatherEggBounds.ShellWeight(_cells[i].centroid, _cells[i].eggRadii, world);
                if (w > bestW)
                {
                    bestW = w;
                    bestIdx = i;
                }
            }
            if (bestIdx < 0)
                return false;
            cell = _cells[bestIdx];
            weight = bestW;
            return true;
        }

        public static float[] NormalizeWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return weights;
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++)
                sum += weights[i];
            if (sum < 1e-6f)
                return weights;
            var norm = new float[weights.Length];
            for (int i = 0; i < weights.Length; i++)
                norm[i] = weights[i] / sum;
            return norm;
        }

        public static float OverlapWeight(Vector3 world, GalacticSkyLatticeCell a, GalacticSkyLatticeCell b)
        {
            return WeatherEggBounds.OverlapGradientWeight(a.centroid, a.eggRadii, b.centroid, b.eggRadii, world);
        }
    }
}
