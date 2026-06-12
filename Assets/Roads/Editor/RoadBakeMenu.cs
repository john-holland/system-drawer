using UnityEditor;
using UnityEngine;

namespace Roads.Editor
{
    public static class RoadBakeMenu
    {
        [MenuItem("Roads/Bake Selected Road")]
        static void BakeSelectedRoad()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var baker = obj.GetComponent<RoadMeshBaker>();
                if (baker != null)
                {
                    baker.Bake();
                    var erosion = obj.GetComponent<RoadErosionSystem>();
                    erosion?.BakeErosion();
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        [MenuItem("Roads/Preview Flow")]
        static void PreviewFlow()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var sampler = obj.GetComponent<RoadFlowSampler>();
                if (sampler == null)
                    continue;
                var cells = sampler.SampleFlow();
                var peak = sampler.FindPeakFlow(cells);
                Debug.Log($"Flow cells: {cells.Length}, peak intensity: {peak.intensity} at {peak.arcLength}m");
            }
        }
    }
}
