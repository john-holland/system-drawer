using UnityEngine;

namespace Roads
{
    /// <summary>Hooks road bake into SpatialGenerator4DOrchestrator Apply cycle without Bedoga→Roads asmdef cycle.</summary>
    [AddComponentMenu("Roads/Road Generator Orchestrator Bridge")]
    public class RoadGeneratorOrchestratorBridge : MonoBehaviour
    {
        public SpatialGenerator4DOrchestrator orchestrator;
        public RoadSpline4D[] roadSplines4D;
        public RoadMeshBaker[] roadBakers;
        public bool bakeOnApply = true;

        void Start()
        {
            if (orchestrator == null)
                orchestrator = FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
            BakeAllRoads();
        }

        public void BakeAllRoads()
        {
            if (roadSplines4D == null || roadSplines4D.Length == 0)
                roadSplines4D = FindObjectsByType<RoadSpline4D>(FindObjectsSortMode.None);
            foreach (var rs4d in roadSplines4D)
            {
                if (rs4d == null)
                    continue;
                var snap = rs4d.ExportSnapshot();
                var rs3d = rs4d.GetComponent<RoadSpline3D>() ?? rs4d.gameObject.AddComponent<RoadSpline3D>();
                rs4d.BakeTo3D(rs3d);
                rs3d.ApplySnapshot(snap);
            }

            if (roadBakers == null || roadBakers.Length == 0)
                roadBakers = FindObjectsByType<RoadMeshBaker>(FindObjectsSortMode.None);
            foreach (var baker in roadBakers)
            {
                if (baker == null)
                    continue;
                baker.Bake();
                var erosion = baker.GetComponent<RoadErosionSystem>();
                erosion?.BakeErosion();
            }
        }

        void LateUpdate()
        {
            if (!bakeOnApply || orchestrator == null)
                return;
        }
    }
}
