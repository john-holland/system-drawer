using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roads.Features
{
    [AddComponentMenu("Roads/Features/Tree Root Bulge")]
    public class RoadTreeRootBulgeFeature : RoadFeatureBase
    {
        public float bulgeRadius = 2f;
        public float lateralBulge = 0.4f;
        public int treeSeed = 0;
        public float treeSearchRadius = 15f;
        [Tooltip("Extra influence beyond the root capsule radius.")]
        public float capsuleInfluencePadding = 0.5f;

        struct RootBulgeCapsule
        {
            public Vector3 bottom;
            public Vector3 top;
            public float radius;
            public bool valid;
        }

        public override void ApplyToSamples(RoadSplineSample[] samples)
        {
            if (samples == null)
                return;
            var trees = FindNearbyTrees();
            if (trees.Length == 0)
                return;

            var capsules = new List<RootBulgeCapsule>(trees.Length);
            for (int t = 0; t < trees.Length; t++)
            {
                if (trees[t] != null && TryGetRootCapsule(trees[t], out var cap))
                    capsules.Add(cap);
            }
            if (capsules.Count == 0)
                return;

            int seed = treeSeed != 0 ? treeSeed : gameObject.GetInstanceID();
            var rng = new System.Random(seed);

            for (int i = 0; i < samples.Length; i++)
            {
                float f = EvaluateFalloff(samples[i].distance);
                if (f <= 0f)
                    continue;

                ref RoadSplineSample sample = ref samples[i];
                for (int c = 0; c < capsules.Count; c++)
                {
                    var cap = capsules[c];
                    if (!CapsuleIntersectsRoadSample(sample, cap, out float surfaceDist, out Vector3 closestOnAxis))
                        continue;

                    float influence = cap.radius + capsuleInfluencePadding + bulgeRadius;
                    float capsuleF = 1f - Mathf.Clamp01(surfaceDist / Mathf.Max(0.01f, influence));
                    float bulge = lateralBulge * f * capsuleF;
                    bulge *= 0.5f + (float)rng.NextDouble() * 0.5f;
                    if (bulge > tolerance)
                        bulge = tolerance;

                    Vector3 pushDir = sample.position - closestOnAxis;
                    pushDir -= Vector3.Dot(pushDir, sample.tangent) * sample.tangent;
                    if (pushDir.sqrMagnitude < 1e-6f)
                    {
                        Vector3 toAxis = closestOnAxis - sample.position;
                        float side = Mathf.Sign(Vector3.Dot(toAxis, sample.binormal));
                        pushDir = sample.binormal * side;
                    }
                    else
                    {
                        pushDir.Normalize();
                    }

                    sample.position += pushDir * bulge;
                }
            }
        }

        static bool TryGetRootCapsule(GameObject tree, out RootBulgeCapsule cap)
        {
            cap = default;
            if (tree == null)
                return false;

            if (TryGetRootBounds(tree, out Bounds bounds))
            {
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                radius = Mathf.Max(radius, bounds.extents.y * 0.35f);
                Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                Vector3 top = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.35f, bounds.center.z);
                cap = new RootBulgeCapsule { bottom = bottom, top = top, radius = radius, valid = true };
                return true;
            }

            Vector3 trunk = tree.transform.position;
            cap = new RootBulgeCapsule
            {
                bottom = trunk + Vector3.down * 1.5f,
                top = trunk,
                radius = 1f,
                valid = true
            };
            return true;
        }

        static bool TryGetRootBounds(GameObject tree, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (var col in tree.GetComponentsInChildren<Collider>())
            {
                if (!IsRootLike(col.gameObject))
                    continue;
                Encapsulate(ref bounds, ref hasBounds, col.bounds);
            }

            if (!hasBounds)
            {
                foreach (var col in tree.GetComponentsInChildren<Collider>())
                    Encapsulate(ref bounds, ref hasBounds, col.bounds);
            }

            if (!hasBounds)
            {
                foreach (var rend in tree.GetComponentsInChildren<Renderer>())
                {
                    if (!IsRootLike(rend.gameObject))
                        continue;
                    Encapsulate(ref bounds, ref hasBounds, rend.bounds);
                }
            }

            if (!hasBounds)
            {
                foreach (var rend in tree.GetComponentsInChildren<Renderer>())
                    Encapsulate(ref bounds, ref hasBounds, rend.bounds);
            }

            return hasBounds;
        }

        static bool IsRootLike(GameObject go)
        {
            string n = go.name;
            return n.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("trunk", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Bounds add)
        {
            if (!hasBounds)
            {
                bounds = add;
                hasBounds = true;
                return;
            }
            bounds.Encapsulate(add.min);
            bounds.Encapsulate(add.max);
        }

        static bool CapsuleIntersectsRoadSample(RoadSplineSample sample, RootBulgeCapsule cap, out float surfaceDistance, out Vector3 closestOnAxis)
        {
            closestOnAxis = ClosestPointOnSegment(sample.position, cap.bottom, cap.top);
            surfaceDistance = Mathf.Max(0f, Vector3.Distance(sample.position, closestOnAxis) - cap.radius);

            float influence = cap.radius + 0.5f;
            if (surfaceDistance > influence)
                return false;

            float halfWidth = sample.width * 0.5f;
            float distToAxis = Vector3.Distance(sample.position, closestOnAxis);
            return distToAxis <= cap.radius + halfWidth + influence;
        }

        static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            if (ab.sqrMagnitude < 1e-8f)
                return a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        GameObject[] FindNearbyTrees()
        {
            var spline = ResolveSpline();
            if (spline == null)
                return Array.Empty<GameObject>();

            var list = new List<GameObject>();
            var seen = new HashSet<int>();
            float total = spline.GetTotalLength();
            int steps = Mathf.Max(4, Mathf.CeilToInt(total / Mathf.Max(1f, length)));
            for (int i = 0; i <= steps; i++)
            {
                float d = total * i / steps;
                Vector3 center = spline.GetSampleAtDistance(d).position;
                foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (t == null || !t.name.Contains("Tree", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (Vector3.Distance(t.position, center) > treeSearchRadius)
                        continue;
                    int id = t.gameObject.GetInstanceID();
                    if (seen.Add(id))
                        list.Add(t.gameObject);
                }
            }
            return list.ToArray();
        }

        protected override void DrawFeatureGizmo(RoadSpline3D spline)
        {
            var trees = FindNearbyTrees();
            Gizmos.color = new Color(0.4f, 0.25f, 0.1f, 0.8f);
            foreach (var tree in trees)
            {
                if (tree == null || !TryGetRootCapsule(tree, out var cap))
                    continue;
                DrawWireCapsule(cap.bottom, cap.top, cap.radius);
            }
        }

        static void DrawWireCapsule(Vector3 bottom, Vector3 top, float radius)
        {
            Gizmos.DrawLine(bottom, top);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
        }
    }
}
