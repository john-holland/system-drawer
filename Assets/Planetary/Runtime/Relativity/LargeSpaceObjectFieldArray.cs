using System.Collections.Generic;
using SpatialVolumes;
using UnityEngine;

namespace Planetary
{
    public sealed class LargeSpaceObjectFieldArray : MonoBehaviour
    {
        public float binSizeMeters = 25000f;
        public PhysicalManifoldRelativitySolver relativitySolver;
        public AngularGradientSearchTargetCachingService angularCache = new AngularGradientSearchTargetCachingService();

        readonly Dictionary<long, List<TrackedObject>> _gravityBins = new Dictionary<long, List<TrackedObject>>();

        struct TrackedObject
        {
            public Transform Transform;
            public float Mass;
            public Vector3 Velocity;
        }

        public void Track(Transform t, float mass)
        {
            if (t == null)
                return;
            long key = GravityBinKey(t.position, mass);
            if (!_gravityBins.TryGetValue(key, out var list))
            {
                list = new List<TrackedObject>();
                _gravityBins[key] = list;
            }
            var rb = t.GetComponent<Rigidbody>();
            list.Add(new TrackedObject
            {
                Transform = t,
                Mass = mass,
                Velocity = rb != null ? rb.velocity : Vector3.zero
            });
        }

        long GravityBinKey(Vector3 pos, float mass)
        {
            long spatial = AngularGradientSearchTargetCachingService.BinKeyFromPosition(pos, binSizeMeters);
            int gBin = Mathf.FloorToInt(Mathf.Log10(Mathf.Max(1f, mass)));
            return spatial ^ ((long)gBin << 48);
        }

        void LateUpdate()
        {
            foreach (var kv in _gravityBins)
            {
                Vector3 avgLanding = Vector3.zero;
                int count = 0;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i].Transform == null)
                        continue;
                    avgLanding += kv.Value[i].Transform.position + kv.Value[i].Velocity * 0.5f;
                    count++;
                }
                if (count > 0)
                {
                    avgLanding /= count;
                    angularCache.SetBinGradient(kv.Key, avgLanding.normalized);
                }
            }
        }

        public List<Transform> QueryArrivalCandidates(Vector3 queryPos, Vector3 direction, int maxCount = 32)
        {
            var result = new List<Transform>();
            var scored = new List<(float score, Transform t)>();
            foreach (var kv in _gravityBins)
            {
                float sort = angularCache.SortKey(kv.Key, direction);
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i].Transform == null)
                        continue;
                    float dist = Vector3.Distance(queryPos, kv.Value[i].Transform.position);
                    float score = sort - dist * 0.0001f;
                    if (relativitySolver != null)
                        score *= relativitySolver.SampleMetricFactor(kv.Value[i].Transform.position, direction);
                    scored.Add((score, kv.Value[i].Transform));
                }
            }
            scored.Sort((a, b) => b.score.CompareTo(a.score));
            for (int i = 0; i < scored.Count && i < maxCount; i++)
                result.Add(scored[i].t);
            return result;
        }

        public bool BroadPhaseIntersects(SpatialVolumeProvider nebulaVolume, Vector3 worldPoint)
        {
            if (nebulaVolume == null)
                return false;
            return nebulaVolume.TrySample(worldPoint, 0f, out _, out bool inside) && inside;
        }
    }
}
