using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Roads
{
    /// <summary>
    /// Static 3D road spline: consumes 4D snapshot or direct authoring, conforms to terrain, produces bake samples.
    /// </summary>
    [AddComponentMenu("Roads/Road Spline 3D")]
    public class RoadSpline3D : RoadSplineBase
    {
        [Header("3D Bake")]
        public RoadSpline4DSnapshot snapshot;
        public bool conformToTerrain = true;
        public float terrainConformOffset = 0.05f;
        public LayerMask terrainLayers = ~0;
        public MeshTerrainSampler meshTerrainSampler;
        public List<Terrain> heightMapTerrains = new List<Terrain>();

        [Header("Identity")]
        public string roadSegmentId;

        [Header("Underside")]
        public bool closeUndersideWithLoop;

        RoadSplineSample[] _bakedSamples;

        public IReadOnlyList<RoadSplineSample> BakedSamples => _bakedSamples;

        public void ApplySnapshot(RoadSpline4DSnapshot src)
        {
            if (src == null)
                return;
            snapshot = src;
            controlPoints = new List<Vector3>(src.controlPoints);
            defaultWidth = src.defaultWidth;
            gradeSlope = src.gradeSlope;
            widthCurve = src.widthCurve != null ? new AnimationCurve(src.widthCurve.keys) : AnimationCurve.Constant(0f, 1f, 1f);
            gradeCurve = src.gradeCurve != null ? new AnimationCurve(src.gradeCurve.keys) : AnimationCurve.Constant(0f, 1f, 0f);
            bankingCurve = src.bankingCurve != null ? new AnimationCurve(src.bankingCurve.keys) : AnimationCurve.Constant(0f, 1f, 0f);
            roadSegmentId = src.roadSegmentId;
            RebuildLengthTable();
        }

        public void RebuildBakedSamples(float spacingMeters)
        {
            if (snapshot != null && controlPoints.Count == 0)
                ApplySnapshot(snapshot);

            var raw = BuildSamples(spacingMeters);
            if (!conformToTerrain)
            {
                _bakedSamples = raw;
                return;
            }

            _bakedSamples = new RoadSplineSample[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                var s = raw[i];
                float terrainY = SampleTerrainHeight(s.position);
                Vector3 grounded = new Vector3(s.position.x, terrainY + terrainConformOffset, s.position.z);
                float gradeLift = Mathf.Tan(s.gradeDegrees * Mathf.Deg2Rad) * s.width * 0.25f;
                grounded += s.normal * gradeLift;
                s.position = grounded;
                s.heightOffset = grounded.y - terrainY;
                _bakedSamples[i] = s;
            }
            RoadFeatureApplicator.ApplyFeatures(transform, _bakedSamples);
        }

        public float SampleTerrainHeight(Vector3 worldPos)
        {
            if (meshTerrainSampler != null)
                return meshTerrainSampler.SampleHeight(worldPos);

            foreach (var terrain in heightMapTerrains)
            {
                if (terrain == null || terrain.terrainData == null)
                    continue;
                float h = terrain.SampleHeight(worldPos);
                return h + terrain.transform.position.y;
            }

            if (Physics.Raycast(worldPos + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 1000f, terrainLayers, QueryTriggerInteraction.Ignore))
                return hit.point.y;

            return worldPos.y;
        }

        public bool IsOverhangAt(RoadSplineSample sample, float dropDistance = 30f)
        {
            Vector3 origin = sample.position + sample.normal * 0.1f;
            if (Physics.Raycast(origin, -sample.normal, out RaycastHit hit, dropDistance, terrainLayers, QueryTriggerInteraction.Ignore))
                return hit.distance > 2f;
            return true;
        }

        public Vector3 ProjectPointOntoSpline(Vector3 worldPoint, out float distanceAlong, out float lateralOffset)
        {
            RebuildLengthTable();
            float total = GetTotalLength();
            distanceAlong = 0f;
            lateralOffset = 0f;
            if (total <= 1e-4f)
                return worldPoint;

            float bestDistSq = float.MaxValue;
            int steps = Mathf.Max(16, Mathf.CeilToInt(total / splineResolution));
            for (int i = 0; i <= steps; i++)
            {
                float d = (float)i / steps * total;
                var s = GetSampleAtDistance(d);
                float dsq = (s.position - worldPoint).sqrMagnitude;
                if (dsq < bestDistSq)
                {
                    bestDistSq = dsq;
                    distanceAlong = d;
                    lateralOffset = Vector3.Dot(worldPoint - s.position, s.binormal);
                }
            }
            return GetSampleAtDistance(distanceAlong).position;
        }
    }
}
