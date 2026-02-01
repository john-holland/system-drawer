using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HierarchicalPathingSolver))]
public class HierarchicalPathingSolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Rebuild Grid"))
        {
            var solver = (HierarchicalPathingSolver)target;
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
