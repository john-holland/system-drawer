using UnityEngine;

namespace SdfMax
{
    /// <summary>
    /// Scene debug helper for <see cref="SdfMaxSurfaceMesher"/> grid + mesh vertex sampling.
    /// Attach to any object with an <see cref="ISdfMaxVolumeHost"/> or assign composition/bounds manually.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SdfMaxSurfaceMesherDebug : MonoBehaviour
    {
        [Tooltip("Optional host (e.g. SpatialVolumeProvider). Resolved via GetComponent if null.")]
        public MonoBehaviour volumeHostBehaviour;
        public SdfMaxCompositionAsset compositionOverride;
        public SdfMaxSolverProfile profileOverride;
        public Bounds localBoundsOverride;
        public bool useLocalBoundsOverride;
        public Vector3 referenceCenter;
        public float referenceRadius = -1f;
        public bool logOnEnable;
        public bool drawGridSamples = true;
        public bool drawMeshVertices = true;
        public float gizmoPointSize = 0.25f;

        SdfMaxSurfaceMeshDebugReport _lastReport;

        ISdfMaxVolumeHost Host =>
            volumeHostBehaviour as ISdfMaxVolumeHost ?? GetComponent<ISdfMaxVolumeHost>();

        void Reset()
        {
            volumeHostBehaviour = GetComponent<MonoBehaviour>();
            referenceCenter = transform.position;
        }

        void OnEnable()
        {
            if (logOnEnable)
                CaptureAndLog();
        }

        [ContextMenu("Capture And Log Surface Mesh Debug")]
        public void CaptureAndLog()
        {
            _lastReport = CaptureReport();
            if (_lastReport != null)
                _lastReport.LogToConsole($"{name} SdfMax Surface Mesh Debug");
            else
                Debug.LogWarning($"{name}: SdfMaxSurfaceMesherDebug could not build a report.", this);
        }

        public SdfMaxSurfaceMeshDebugReport CaptureReport()
        {
            if (!TryCreateBuildInputs(out var evaluator, out var localBounds, out var localToWorld, out var isoLevel, out var gridRes))
                return null;

            var report = new SdfMaxSurfaceMeshDebugReport
            {
                ReferenceCenter = referenceCenter,
                ReferenceRadius = referenceRadius
            };
            int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(profileOverride, compositionOverride);
            SdfMaxSurfaceMesher.Build(evaluator, localBounds, localToWorld, isoLevel, gridRes, ver, true, report);
            _lastReport = report;
            return report;
        }

        bool TryCreateBuildInputs(
            out SdfMaxEvaluator evaluator,
            out Bounds localBounds,
            out Matrix4x4 localToWorld,
            out float isoLevel,
            out int gridRes)
        {
            evaluator = null;
            localBounds = default;
            localToWorld = transform.localToWorldMatrix;
            isoLevel = 0f;
            gridRes = 32;

            var host = Host;
            var composition = compositionOverride ?? host?.Composition;
            var profile = profileOverride ?? host?.Profile;
            if (composition == null)
                return false;

            if (host != null)
                localToWorld = host.HostTransform.localToWorldMatrix;

            var graph = new SdfMaxExpressionGraph(composition, profile, localToWorld);
            evaluator = new SdfMaxEvaluator(graph);
            isoLevel = profile != null ? profile.surfaceIsoLevel : 0f;
            gridRes = profile != null ? profile.surfaceGridRes : 32;

            if (useLocalBoundsOverride)
            {
                localBounds = localBoundsOverride;
            }
            else if (host != null)
            {
                Bounds worldBounds = evaluator.WorldBounds;
                localBounds = WorldBoundsToLocal(worldBounds, host.HostTransform);
            }
            else
            {
                Bounds worldBounds = evaluator.WorldBounds;
                localBounds = new Bounds(
                    localToWorld.inverse.MultiplyPoint3x4(worldBounds.center),
                    worldBounds.size);
            }

            return true;
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
                    Gizmos.DrawRay(v.WorldPos, v.Normal * gizmoPointSize * 4f);
                }
            }

            if (referenceRadius > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawWireSphere(referenceCenter, referenceRadius);
            }
        }
    }
}
