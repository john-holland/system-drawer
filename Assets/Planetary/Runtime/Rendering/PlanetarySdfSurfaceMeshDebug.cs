using Planetary;
using SdfMax;
using UnityEngine;

namespace Planetary.Rendering
{
    /// <summary>
    /// Planet-specific surface mesher debug: uses <see cref="PlanetBody.CreateExpressionGraph"/> (planar context)
    /// and the same bounds path as <see cref="PlanetarySdfLodBaker"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlanetarySdfSurfaceMeshDebug : MonoBehaviour
    {
        public PlanetBody planetBody;
        public bool useLodBakerBounds = true;
        public bool useVolumeProviderBounds;
        public int gridResOverride;
        public bool logOnEnable = true;
        public bool drawGridSamples = true;
        public bool drawMeshVertices = true;
        public float gizmoPointSize = 50f;

        SdfMaxSurfaceMeshDebugReport _lastReport;

        void Reset()
        {
            planetBody = GetComponent<PlanetBody>() ?? GetComponentInParent<PlanetBody>();
        }

        void OnEnable()
        {
            if (logOnEnable)
                CaptureAndLog();
        }

        [ContextMenu("Capture And Log Planet SDF Surface Debug")]
        public void CaptureAndLog()
        {
            _lastReport = CaptureReport();
            if (_lastReport != null)
                _lastReport.LogToConsole($"{name} Planet SDF Surface Debug");
            else
                Debug.LogWarning($"{name}: PlanetarySdfSurfaceMeshDebug could not build a report.", this);
        }

        public SdfMaxSurfaceMeshDebugReport CaptureReport()
        {
            if (planetBody == null)
                planetBody = GetComponent<PlanetBody>() ?? GetComponentInParent<PlanetBody>();
            if (planetBody == null || planetBody.composition == null)
                return null;

            var graph = planetBody.CreateExpressionGraph();
            var eval = new SdfMaxEvaluator(graph);
            var profile = planetBody.sdfLodProfile;
            float iso = profile != null ? profile.isoLevel : 0f;
            int gridRes = gridResOverride > 0
                ? gridResOverride
                : profile != null && profile.tierGridRes.Length > 0
                    ? profile.tierGridRes[profile.tierGridRes.Length - 1]
                    : planetBody.solverProfile != null
                        ? planetBody.solverProfile.surfaceGridRes
                        : 32;

            Bounds localBounds;
            if (useVolumeProviderBounds && planetBody.volumeProvider != null)
            {
                Bounds worldBounds = eval.WorldBounds;
                localBounds = WorldBoundsToLocal(worldBounds, planetBody.transform);
            }
            else if (useLodBakerBounds)
            {
                float r = planetBody.PlanetRadius * 2.2f;
                localBounds = new Bounds(Vector3.zero, Vector3.one * r);
            }
            else
            {
                Bounds worldBounds = eval.WorldBounds;
                localBounds = WorldBoundsToLocal(worldBounds, planetBody.transform);
            }

            var report = new SdfMaxSurfaceMeshDebugReport
            {
                ReferenceCenter = planetBody.PlanetCenter,
                ReferenceRadius = planetBody.PlanetRadius
            };
            int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(planetBody.solverProfile, planetBody.composition) ^ (gridRes * 31);
            SdfMaxSurfaceMesher.Build(
                eval,
                localBounds,
                planetBody.transform.localToWorldMatrix,
                iso,
                gridRes,
                ver,
                true,
                report);
            _lastReport = report;
            return report;
        }

        static Bounds WorldBoundsToLocal(Bounds world, Transform t)
        {
            Vector3 c = t.InverseTransformPoint(world.center);
            Vector3 ext = t.InverseTransformVector(world.extents);
            return new Bounds(c, ext * 2f);
        }

        void OnDrawGizmosSelected()
        {
            if (_lastReport == null)
                return;

            if (drawGridSamples && _lastReport.GridSamples != null)
            {
                for (int i = 0; i < _lastReport.GridSamples.Length; i++)
                {
                    var s = _lastReport.GridSamples[i];
                    Gizmos.color = s.Inside ? new Color(0.2f, 0.9f, 1f, 0.9f) : new Color(1f, 0.35f, 0.2f, 0.9f);
                    Gizmos.DrawSphere(s.WorldPos, gizmoPointSize);
                }
            }

            if (drawMeshVertices && _lastReport.VertexSamples != null)
            {
                for (int i = 0; i < _lastReport.VertexSamples.Length; i++)
                {
                    var v = _lastReport.VertexSamples[i];
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(v.WorldPos, gizmoPointSize * 0.75f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(v.WorldPos, v.Normal * gizmoPointSize * 3f);
                }
            }

            if (planetBody != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawWireSphere(planetBody.PlanetCenter, planetBody.PlanetRadius);
            }
        }
    }
}
