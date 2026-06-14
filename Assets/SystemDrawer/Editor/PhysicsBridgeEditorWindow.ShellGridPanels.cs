#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Planetary.Bridges;

public partial class PhysicsBridgeEditorWindow
{
    bool _drawShellGridOverlay = true;

    void DrawShellGridPanels()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Planet shell grid", EditorStyles.boldLabel);
        _drawShellGridOverlay = EditorGUILayout.Toggle("Draw shell overlay in Scene", _drawShellGridOverlay);

        foreach (PlanetShellManifoldGrid grid in FindObjectsByType<PlanetShellManifoldGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (grid == null)
                continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.ObjectField("Shell grid", grid, typeof(PlanetShellManifoldGrid), true);
            EditorGUILayout.LabelField("Lat/Lon/Bands", $"{grid.latCount} x {grid.lonCount} x {grid.altitudeBandCount}");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild + sync weather"))
                grid.RebuildAndSyncToWeatherManifold();
            if (GUILayout.Button("Select"))
            {
                Selection.activeObject = grid;
                EditorGUIUtility.PingObject(grid);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        DrawUnresolvedServiceKeysPanel();
    }

    void DrawUnresolvedServiceKeysPanel()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unresolved service keys", EditorStyles.boldLabel);
        if (GUILayout.Button("Scan canonical keys"))
        {
            IReadOnlyList<string> missing = SystemDrawerSceneServices.GetUnresolvedRequiredKeys(
                SystemDrawerServiceKeys.WeatherPhysicsManifold,
                SystemDrawerServiceKeys.PlanetBody,
                SystemDrawerServiceKeys.PlanetShellGrid,
                SystemDrawerServiceKeys.HierarchicalPathingSolver);
            if (missing.Count == 0)
                Debug.Log("[Physics Bridge] All canonical keys registered.");
            else
                Debug.LogWarning("[Physics Bridge] Missing keys: " + string.Join(", ", missing));
        }
    }

    void OnSceneGUI()
    {
        if (!_drawShellGridOverlay)
            return;

        foreach (PlanetShellManifoldGrid grid in FindObjectsByType<PlanetShellManifoldGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (grid == null || grid.planet == null)
                continue;
            Handles.color = new Color(0.2f, 0.85f, 1f, 0.6f);
            float r = grid.planet.PlanetRadius;
            Vector3 c = grid.planet.PlanetCenter;
            Handles.DrawWireDisc(c + Vector3.up * r, Vector3.up, r * 0.02f);
            Handles.Label(c + Vector3.up * (r + 5f), $"Shell {grid.latCount}x{grid.lonCount}");
        }
    }
}
#endif
