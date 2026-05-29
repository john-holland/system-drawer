#if UNITY_EDITOR
using SpatialVolumes;
using UnityEditor;
using UnityEngine;

namespace SdfMax.Editor
{
    [CustomEditor(typeof(HierarchicalPathingSolver))]
    public sealed class HierarchicalPathingSolverSdfMaxEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4f);
            var solver = (HierarchicalPathingSolver)target;
            if (GUILayout.Button("Open SDF Max Editor", GUILayout.Height(22)))
            {
                SpatialVolumeProvider provider = null;
                if (solver.volumeProviders != null && solver.volumeProviders.Count > 0)
                    provider = solver.volumeProviders[0];
                SdfMaxCompositionEditorWindow.ShowWindow(provider);
            }

            if (GUILayout.Button("Rebuild Grid"))
            {
                if (solver.fitToTerrain && solver.fitToTerrains != null && solver.fitToTerrains.Count > 0)
                {
                    Undo.RecordObject(solver, "Rebuild Grid");
                    solver.SetWorldBoundsFromTerrains();
                }
                solver.RebuildGrid();
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
