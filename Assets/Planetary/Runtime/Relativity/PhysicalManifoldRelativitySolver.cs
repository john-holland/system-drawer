using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public sealed class PhysicalManifoldRelativitySolver : MonoBehaviour
    {
        public float identityEpsilon = 0.01f;
        public AngularGradientSearchTargetCachingService angularCache = new AngularGradientSearchTargetCachingService();

        readonly List<SpacetimeObjectEntry> _objects = new List<SpacetimeObjectEntry>();

        struct SpacetimeObjectEntry
        {
            public Transform Transform;
            public float Mass;
            public float InfluenceRadius;
            public float[] DeflectionByAzimuth;
            public bool UseIdentity;
        }

        public void RegisterObject(Transform t, float mass, float influenceRadius)
        {
            _objects.Add(new SpacetimeObjectEntry
            {
                Transform = t,
                Mass = mass,
                InfluenceRadius = influenceRadius,
                DeflectionByAzimuth = new float[16],
                UseIdentity = mass < identityEpsilon
            });
        }

        public void UnregisterObject(Transform t)
        {
            if (t == null)
                return;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i].Transform == t)
                    _objects.RemoveAt(i);
            }
        }

        public float GetNarrativeTime() => Time.time;

        void Update()
        {
            float t = GetNarrativeTime();
            for (int i = 0; i < _objects.Count; i++)
            {
                var e = _objects[i];
                if (e.Transform == null)
                    continue;
                float speed = 0f;
                var rb = e.Transform.GetComponent<Rigidbody>();
                if (rb != null)
                    speed = rb.linearVelocity.magnitude;
                bool degrade = e.Mass < identityEpsilon || speed < identityEpsilon;
                e.UseIdentity = degrade;
                if (!degrade)
                    RefreshAngularCurve(ref e, t);
                _objects[i] = e;
            }
        }

        static void RefreshAngularCurve(ref SpacetimeObjectEntry e, float t)
        {
            for (int a = 0; a < e.DeflectionByAzimuth.Length; a++)
            {
                float az = a / (float)e.DeflectionByAzimuth.Length * Mathf.PI * 2f;
                e.DeflectionByAzimuth[a] = Mathf.Sin(az + t) * e.Mass * 0.001f;
            }
        }

        public float SampleMetricFactor(Vector3 world, Vector3 direction)
        {
            float factor = 1f;
            for (int i = 0; i < _objects.Count; i++)
            {
                var e = _objects[i];
                if (e.UseIdentity || e.Transform == null)
                    continue;
                float dist = Vector3.Distance(world, e.Transform.position);
                if (dist > e.InfluenceRadius)
                    continue;
                float falloff = 1f - dist / Mathf.Max(e.InfluenceRadius, 0.01f);
                int az = Mathf.Abs(Mathf.FloorToInt(Mathf.Atan2(direction.z, direction.x) / (Mathf.PI * 2f) * e.DeflectionByAzimuth.Length)) % e.DeflectionByAzimuth.Length;
                factor += e.DeflectionByAzimuth[az] * falloff;
            }
            long bin = AngularGradientSearchTargetCachingService.BinKeyFromPosition(world, 10000f);
            factor *= 1f + angularCache.SortKey(bin, direction) * 0.1f;
            return Mathf.Max(0.01f, factor);
        }
    }
}
