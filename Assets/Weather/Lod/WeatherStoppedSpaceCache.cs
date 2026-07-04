using System.Collections.Generic;
using UnityEngine;

namespace Weather.Lod
{
    /// <summary>Read-only weather queries outside active advection eggs.</summary>
    public sealed class WeatherStoppedSpaceCache
    {
        readonly Dictionary<int, SphericalHyperplaneRegression> _regressions = new Dictionary<int, SphericalHyperplaneRegression>();
        readonly List<(Vector3 center, ManifoldCellData data)> _coarseGuesses = new List<(Vector3, ManifoldCellData)>();

        static int KeyFor(Vector3 center)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + Mathf.RoundToInt(center.x * 10f);
                h = h * 31 + Mathf.RoundToInt(center.y * 10f);
                h = h * 31 + Mathf.RoundToInt(center.z * 10f);
                return h;
            }
        }

        public void StoreRegression(Vector3 eggCenter, SphericalHyperplaneRegression regression)
        {
            if (regression == null)
                return;
            _regressions[KeyFor(eggCenter)] = regression;
        }

        public void StoreCoarseGuess(Vector3 eggCenter, ManifoldCellData data)
        {
            for (int i = 0; i < _coarseGuesses.Count; i++)
            {
                if (Vector3.Distance(_coarseGuesses[i].center, eggCenter) < 0.5f)
                {
                    _coarseGuesses[i] = (eggCenter, data);
                    return;
                }
            }
            _coarseGuesses.Add((eggCenter, data));
        }

        public bool TryEvaluate(Vector3 world, out ManifoldCellData data)
        {
            data = default;
            if (_regressions.Count == 0 && _coarseGuesses.Count == 0)
                return false;

            float bestWeight = 0f;
            ManifoldCellData blended = default;
            bool any = false;
            foreach (var pair in _regressions)
            {
                SphericalHyperplaneRegression r = pair.Value;
                if (r == null)
                    continue;
                float w = 1f / (1f + Vector3.Distance(world, r.center));
                ManifoldCellData sample = r.Evaluate(world);
                if (!any)
                {
                    blended = sample;
                    bestWeight = w;
                    any = true;
                }
                else
                {
                    float t = w / (bestWeight + w);
                    blended.velocity = Vector3.Lerp(blended.velocity, sample.velocity, t);
                    blended.temperature = Mathf.Lerp(blended.temperature, sample.temperature, t);
                    blended.pressure = Mathf.Lerp(blended.pressure, sample.pressure, t);
                    bestWeight += w;
                }
            }

            data = blended;
            if (any)
                return true;

            if (_coarseGuesses.Count > 0)
            {
                float bestDist = float.MaxValue;
                ManifoldCellData best = default;
                for (int i = 0; i < _coarseGuesses.Count; i++)
                {
                    float d = Vector3.Distance(world, _coarseGuesses[i].center);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = _coarseGuesses[i].data;
                    }
                }
                data = best;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _regressions.Clear();
            _coarseGuesses.Clear();
        }
    }
}
