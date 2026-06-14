#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Planetary.Bridges;

/// <summary>
/// Discover and validate physics bridge components (roads, planet, ragdoll ventures).
/// </summary>
public partial class PhysicsBridgeEditorWindow : EditorWindow
{
    Vector2 _scroll;
    Vector3 _probeWorld;
    bool _hasProbe;

    [MenuItem("Window/System Drawer/Physics/Physics Bridge Editor", false, 120)]
    public static void ShowWindow()
    {
        var w = GetWindow<PhysicsBridgeEditorWindow>("Physics Bridge");
        w.minSize = new Vector2(480f, 360f);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Physics Bridge Editor", EditorStyles.boldLabel);

        if (GUILayout.Button("Fit all planet bridges to planet bounds"))
        {
            foreach (PlanetPhysicsManifoldBridge bridge in FindObjectsByType<PlanetPhysicsManifoldBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (bridge != null)
                    bridge.FitManifoldBoundsToPlanet();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sample probe", EditorStyles.boldLabel);
        _probeWorld = EditorGUILayout.Vector3Field("World position", _probeWorld);
        if (GUILayout.Button("Probe cell data"))
        {
            _hasProbe = true;
            LogProbe(_probeWorld);
        }

        DrawCanonicalFieldPanels();
        DrawShellGridPanels();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Active bridges", EditorStyles.boldLabel);
        IReadOnlyList<PhysicsBridgeRegistry.BridgeRow> rows = PhysicsBridgeRegistry.DiscoverActiveBridges();
        if (rows.Count == 0)
            EditorGUILayout.HelpBox("No bridge components found in open scenes.", MessageType.Info);

        foreach (PhysicsBridgeRegistry.BridgeRow row in rows)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(row.sourceTypeName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("From frame", row.fromFrame.ToString());
            EditorGUILayout.LabelField("Target manifold", row.targetManifold != null ? row.targetManifold.name : "(none)");
            EditorGUILayout.LabelField("Last stamp", row.lastStampLabel);
            EditorGUILayout.Toggle("Active", row.active);

            bool ok = PhysicsBridgeRegistry.ValidateRow(row, out string msg);
            EditorGUILayout.LabelField("Validate", ok ? msg : msg, ok ? EditorStyles.miniLabel : EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (row.source != null && GUILayout.Button("Select"))
            {
                Selection.activeObject = row.source;
                EditorGUIUtility.PingObject(row.source);
            }

            if (row.source is PlanetPhysicsManifoldBridge pb && GUILayout.Button("Stamp planet"))
                pb.StampFromCompositionBake();
            if (row.source is Roads.RoadPhysicsManifoldBridge rb && GUILayout.Button("Stamp road"))
                rb.StampFromBake();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Ragdoll Fitting Wizard"))
            EditorApplication.ExecuteMenuItem("Window/System Drawer/Ragdoll/Fitting Wizard");

        EditorGUILayout.EndScrollView();
    }

    static void LogProbe(Vector3 world)
    {
        if (PhysicalMediumVolumeIndex.TryResolveMedium(world, out PhysicalPathingMedium medium))
            Debug.Log($"[Physics Bridge] Medium at {world}: {medium}");
        else
            Debug.Log($"[Physics Bridge] Medium at {world}: Unspecified");

        foreach (Weather.WeatherPhysicsManifold m in FindObjectsByType<Weather.WeatherPhysicsManifold>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m == null)
                continue;
            var data = m.GetDataAtPosition(world);
            Debug.Log($"[Physics Bridge] Manifold '{m.name}' friction={data.surfaceFriction:F2} porosity={data.surfacePorosity:F2} mode={data.mode}");
        }

        foreach (Planetary.PlanetBody planet in FindObjectsByType<Planetary.PlanetBody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (planet != null && planet.TrySampleHeightAtWorld(world, out float h, out float slope))
                Debug.Log($"[Physics Bridge] Planet height={h:F1} m slope={slope:F1}°");
        }
    }
}
#endif
