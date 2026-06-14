using System.Collections.Generic;
using UnityEngine;

namespace Weather.NearField
{
    /// <summary>
    /// Actor-centric force-directed wind graph (near-field only). Merges with far-field manifold samples at boundary.
    /// </summary>
    public sealed class NearFieldWindInteractionGraph : MonoBehaviour
    {
        public Transform focus;
        public WeatherPhysicsManifold manifold;
        public List<Transform> extraNodes = new List<Transform>();
        public float nearFieldRadiusM = 30f;
        public int gridSteps = 3;
        public int relaxIterations = 2;
        public float springStrength = 4f;
        public float damping = 0.85f;

        struct Node
        {
            public Vector3 position;
            public Vector3 velocity;
        }

        readonly List<Node> _nodes = new List<Node>();
        readonly List<(int a, int b)> _edges = new List<(int, int)>();

        void Awake()
        {
            if (manifold == null)
                manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
        }

        void LateUpdate()
        {
            if (focus == null || manifold == null)
                return;
            RebuildGraphIfNeeded();
            RelaxGraph();
        }

        public Vector3 GetBlendedVelocity(Vector3 world, Vector3 farVelocity)
        {
            if (focus == null)
                return farVelocity;

            float dist = Vector3.Distance(world, focus.position);
            float wNear = 1f - Mathf.SmoothStep(0f, nearFieldRadiusM, dist);
            if (wNear <= 1e-4f || _nodes.Count == 0)
                return farVelocity;

            Vector3 graphVel = SampleGraphVelocity(world);
            return Vector3.Lerp(farVelocity, graphVel, wNear);
        }

        Vector3 SampleGraphVelocity(Vector3 world)
        {
            if (_nodes.Count == 0)
                return Vector3.zero;
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < _nodes.Count; i++)
            {
                float d = (world - _nodes[i].position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return _nodes[best].velocity;
        }

        void RebuildGraphIfNeeded()
        {
            if (_nodes.Count > 0 && (Time.frameCount % 30) != 0)
                return;

            _nodes.Clear();
            _edges.Clear();
            Vector3 origin = focus.position;
            float step = nearFieldRadiusM / Mathf.Max(1, gridSteps);

            for (int ix = -gridSteps; ix <= gridSteps; ix++)
            for (int iy = 0; iy <= gridSteps; iy++)
            for (int iz = -gridSteps; iz <= gridSteps; iz++)
            {
                Vector3 p = origin + new Vector3(ix * step, iy * step, iz * step);
                if (Vector3.Distance(p, origin) > nearFieldRadiusM)
                    continue;
                Vector3 far = manifold.GetVelocityAtPosition(p);
                _nodes.Add(new Node { position = p, velocity = far });
            }

            for (int i = 0; i < _nodes.Count; i++)
            for (int j = i + 1; j < _nodes.Count; j++)
            {
                if ((_nodes[i].position - _nodes[j].position).sqrMagnitude <= step * step * 2.5f)
                    _edges.Add((i, j));
            }

            if (extraNodes != null)
            {
                for (int i = 0; i < extraNodes.Count; i++)
                {
                    Transform t = extraNodes[i];
                    if (t == null)
                        continue;
                    _nodes.Add(new Node { position = t.position, velocity = manifold.GetVelocityAtPosition(t.position) });
                }
            }
        }

        void RelaxGraph()
        {
            for (int iter = 0; iter < relaxIterations; iter++)
            {
                for (int e = 0; e < _edges.Count; e++)
                {
                    (int a, int b) = _edges[e];
                    Node na = _nodes[a];
                    Node nb = _nodes[b];
                    Vector3 mid = (na.position + nb.position) * 0.5f;
                    Vector3 target = manifold.GetVelocityAtPosition(mid);
                    Vector3 delta = (target - na.velocity) * (springStrength * Time.deltaTime);
                    na.velocity += delta;
                    nb.velocity -= delta * 0.5f;
                    na.velocity *= damping;
                    nb.velocity *= damping;
                    _nodes[a] = na;
                    _nodes[b] = nb;
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (focus == null)
                return;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
            Gizmos.DrawWireSphere(focus.position, nearFieldRadiusM);
            Gizmos.color = Color.cyan;
            foreach (Node n in _nodes)
                Gizmos.DrawRay(n.position, n.velocity * 0.25f);
        }
#endif
    }
}
