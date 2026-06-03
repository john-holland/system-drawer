using System.Collections.Generic;
using Planetary.Lava;
using SdfMax;
using UnityEngine;

namespace Planetary.Tectonics
{
    public struct VolcanoSite
    {
        public Vector3 worldPosition;
        public float radiusMeters;
        public float gasPressure;
    }

    public sealed class VolcanoStressSolver : MonoBehaviour
    {
        public LavaPhysicsManifold lava;
        public int maxVolcanoesPerFrame = 4;
        readonly List<VolcanoSite> _sites = new List<VolcanoSite>();

        public IReadOnlyList<VolcanoSite> Sites => _sites;

        public void RefreshNearestPlayerFirst(Vector3 playerWorld)
        {
            _sites.Clear();
            if (lava == null)
                return;
            var candidates = new List<VolcanoCandidate>(lava.Candidates);
            candidates.Sort((a, b) =>
                Vector3.Distance(a.worldPosition, playerWorld).CompareTo(Vector3.Distance(b.worldPosition, playerWorld)));
            int n = Mathf.Min(maxVolcanoesPerFrame, candidates.Count);
            for (int i = 0; i < n; i++)
            {
                var c = candidates[i];
                float tension = lava.surfaceTensionCoeff;
                float r = Mathf.Max(10f, (c.stress + c.gasPressure * 0.01f) * (1f - tension));
                _sites.Add(new VolcanoSite { worldPosition = c.worldPosition, radiusMeters = r, gasPressure = c.gasPressure });
            }
        }

        public static int AppendVolcanoConeToGraph(SdfMaxCompositionAsset asset, int rootIdx, VolcanoSite site, float planetRadius)
        {
            int coneIdx = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                sphereRadius = site.radiusMeters,
                weight = 1f
            });
            int blend = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.SmoothMax,
                childIndexA = rootIdx,
                childIndexB = coneIdx,
                smoothRadius = site.radiusMeters * 0.25f
            });
            return blend;
        }
    }
}
