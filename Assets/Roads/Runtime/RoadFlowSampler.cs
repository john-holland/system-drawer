using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Roads
{
    [System.Serializable]
    public struct RoadFlowCell
    {
        public float arcLength;
        public float lateralPos;
        public Vector3 flowDir;
        public float intensity;
    }

    /// <summary>Samples water flow intensity along road surface for erosion.</summary>
    public class RoadFlowSampler : MonoBehaviour
    {
        public RoadSpline3D spline;
        public SplinePathMeshSampler sampler;
        public Water water;
        public MeshTerrainSampler meshTerrainSampler;
        public float flowThreshold = 0.1f;

        public RoadFlowCell[] SampleFlow()
        {
            if (spline == null)
                spline = GetComponent<RoadSpline3D>();
            if (sampler == null)
                sampler = GetComponent<SplinePathMeshSampler>();
            if (spline == null)
                return System.Array.Empty<RoadFlowCell>();

            spline.RebuildBakedSamples(sampler != null ? sampler.sampleSpacingMeters : 1f);
            var baked = spline.BakedSamples;
            if (baked == null || baked.Count == 0)
                return System.Array.Empty<RoadFlowCell>();

            var cells = new List<RoadFlowCell>(baked.Count * 3);
            float baseFlow = water != null ? water.flowRate : 1f;

            foreach (var s in baked)
            {
                Vector3 flowDir = ComputeFlowDirection(s.position, s.normal);
                float intensity = baseFlow * (1f + Mathf.Max(0f, -Vector3.Dot(flowDir, s.normal)));
                cells.Add(new RoadFlowCell
                {
                    arcLength = s.distance,
                    lateralPos = 0f,
                    flowDir = flowDir,
                    intensity = intensity
                });
                cells.Add(new RoadFlowCell { arcLength = s.distance, lateralPos = -0.35f, flowDir = flowDir, intensity = intensity * 0.7f });
                cells.Add(new RoadFlowCell { arcLength = s.distance, lateralPos = 0.35f, flowDir = flowDir, intensity = intensity * 0.7f });
            }
            return cells.ToArray();
        }

        Vector3 ComputeFlowDirection(Vector3 pos, Vector3 normal)
        {
            if (water != null && water.rivers != null && water.rivers.Count > 0)
            {
                var river = water.rivers[0];
                if (river != null && river.riverSplines != null && river.riverSplines.Count > 0)
                    return river.riverSplines[0].GetFlowDirectionAtDistance(0f);
            }
            if (meshTerrainSampler != null)
            {
                float hL = meshTerrainSampler.SampleHeight(pos + Vector3.left);
                float hR = meshTerrainSampler.SampleHeight(pos + Vector3.right);
                float hD = meshTerrainSampler.SampleHeight(pos + Vector3.forward);
                float hU = meshTerrainSampler.SampleHeight(pos + Vector3.back);
                Vector3 grad = new Vector3(hR - hL, 0f, hD - hU);
                if (grad.sqrMagnitude > 1e-6f)
                    return -grad.normalized;
            }
            return Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        }

        public RoadFlowCell FindPeakFlow(RoadFlowCell[] cells)
        {
            RoadFlowCell peak = default;
            float best = flowThreshold;
            if (cells == null)
                return peak;
            foreach (var c in cells)
            {
                if (c.intensity > best)
                {
                    best = c.intensity;
                    peak = c;
                }
            }
            return peak;
        }
    }
}
