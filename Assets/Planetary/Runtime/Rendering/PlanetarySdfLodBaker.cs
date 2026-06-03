using System.Collections.Generic;
using SdfMax;
using UnityEngine;

namespace Planetary.Rendering
{
    public sealed class PlanetarySdfLodBaker
    {
        readonly List<Mesh> _tierMeshes = new List<Mesh>();
        readonly List<int> _tierVersions = new List<int>();

        public IReadOnlyList<Mesh> TierMeshes => _tierMeshes;

        public void RebuildTiers(PlanetBody body, PlanetarySdfLodProfile profile)
        {
            _tierMeshes.Clear();
            _tierVersions.Clear();
            if (body == null || profile == null || body.composition == null)
                return;
            var graph = body.CreateExpressionGraph();
            var eval = new SdfMaxEvaluator(graph);
            float r = body.PlanetRadius * 2.2f;
            var bounds = new Bounds(Vector3.zero, Vector3.one * r);
            var localToWorld = body.transform.localToWorldMatrix;
            for (int t = 0; t < profile.tierGridRes.Length; t++)
            {
                int res = profile.tierGridRes[t];
                int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(body.solverProfile, body.composition) ^ (res * 31);
                var data = SdfMaxSurfaceMesher.Build(eval, bounds, localToWorld, profile.isoLevel, res, ver, true);
                var mesh = new Mesh { name = $"PlanetSdfLod_T{t}" };
                data.ApplyToMesh(mesh, true);
                _tierMeshes.Add(mesh);
                _tierVersions.Add(ver);
            }
        }
    }
}
