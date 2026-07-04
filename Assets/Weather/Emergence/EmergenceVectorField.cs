using System.Collections.Generic;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Spatial index of active emergence vectors with corridor influence queries.</summary>
    public sealed class EmergenceVectorField
    {
        readonly List<EmergenceVector> _vectors = new List<EmergenceVector>(32);

        public IReadOnlyList<EmergenceVector> Vectors => _vectors;

        public void Clear() => _vectors.Clear();

        public void SetVectors(List<EmergenceVector> fresh)
        {
            _vectors.Clear();
            if (fresh == null)
                return;
            float now = Time.time;
            for (int i = 0; i < fresh.Count; i++)
            {
                if (!fresh[i].IsExpired(now))
                    _vectors.Add(fresh[i]);
            }
        }

        public float GetActivationWeight(Vector3 world)
        {
            if (_vectors.Count == 0)
                return 0f;

            float best = 0f;
            for (int i = 0; i < _vectors.Count; i++)
                best = Mathf.Max(best, InfluenceAt(_vectors[i], world));
            return Mathf.Clamp01(best);
        }

        public static float InfluenceAt(EmergenceVector v, Vector3 world)
        {
            if (v.influenceRadius <= 0f || v.weight <= 0f)
                return 0f;

            Vector3 dir = v.direction.sqrMagnitude > 1e-6f ? v.direction.normalized : Vector3.forward;
            Vector3 ap = world - v.origin;
            float along = Vector3.Dot(ap, dir);
            along = Mathf.Clamp(along, 0f, Mathf.Max(v.length, 0.01f));
            Vector3 closest = v.origin + dir * along;
            float dist = Vector3.Distance(world, closest);
            float radial = 1f - dist / Mathf.Max(0.01f, v.influenceRadius);
            if (radial <= 0f)
                return 0f;
            return v.weight * Mathf.Clamp01(radial);
        }

        public static int ComputeChecksum(IReadOnlyList<EmergenceVector> vectors)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < vectors.Count; i++)
                {
                    h = h * 31 + (vectors[i].sourceId?.GetHashCode() ?? 0);
                    h = h * 31 + vectors[i].origin.GetHashCode();
                }
                return h;
            }
        }
    }
}
