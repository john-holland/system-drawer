using UnityEditor;
using UnityEngine;

/// <summary>Airport-focused PixelLight / CityPixelGrid designer — apron/runway/taxi layers + lane disable + detour.</summary>
public sealed class AirportPixelLightDesignerWindow : EditorWindow
{
    CityPixelGrid grid;
    PixelLightPatternAsset pattern;
    bool disableLane;
    bool detourFaceOut = true;
    GameObject detourPrefab;
    GameObject signagePrefab;
    GameObject shrubPrefab;
    MonoScript cleanupCrewBt;
    MonoScript maintenanceCrewBt;

    [MenuItem("Locomotion/Airport Pixel Light Designer")]
    public static void Open()
    {
        var w = GetWindow<AirportPixelLightDesignerWindow>();
        w.titleContent = new GUIContent("Airport PixelLight");
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Airport Pixel Light Designer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Uses City Pixel Grid bounds/layers with airport-focused Custom layer ids (apron, runway, taxiway, terminal). Assign PixelLight patterns like the timed playbook.",
            MessageType.Info);

        grid = (CityPixelGrid)EditorGUILayout.ObjectField("City Pixel Grid", grid, typeof(CityPixelGrid), false);
        pattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField("PixelLight Pattern", pattern, typeof(PixelLightPatternAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lane / detour", EditorStyles.boldLabel);
        disableLane = EditorGUILayout.Toggle("Disable airport lane", disableLane);
        detourFaceOut = EditorGUILayout.Toggle("Detour face out from street", detourFaceOut);
        detourPrefab = (GameObject)EditorGUILayout.ObjectField("Detour prefab", detourPrefab, typeof(GameObject), false);
        signagePrefab = (GameObject)EditorGUILayout.ObjectField("Signage prefab", signagePrefab, typeof(GameObject), false);
        shrubPrefab = (GameObject)EditorGUILayout.ObjectField("Shrub / plant prefab", shrubPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Crew BT sets", EditorStyles.boldLabel);
        cleanupCrewBt = (MonoScript)EditorGUILayout.ObjectField("Cleanup crew BT", cleanupCrewBt, typeof(MonoScript), false);
        maintenanceCrewBt = (MonoScript)EditorGUILayout.ObjectField("Maintenance crew BT", maintenanceCrewBt, typeof(MonoScript), false);

        if (GUILayout.Button("Ensure airport Custom layers on grid"))
            EnsureAirportLayers();

        if (GUILayout.Button("Stamp roadside decor on selection"))
            StampDecorOnSelection();
    }

    void EnsureAirportLayers()
    {
        if (grid == null)
        {
            EditorUtility.DisplayDialog("Airport PixelLight", "Assign a CityPixelGrid asset.", "OK");
            return;
        }
        EnsureLayer("apron");
        EnsureLayer("runway");
        EnsureLayer("taxiway");
        EnsureLayer("terminal");
        EditorUtility.SetDirty(grid);
        AssetDatabase.SaveAssets();
    }

    void EnsureLayer(string id)
    {
        if (grid.layers == null)
            grid.layers = new System.Collections.Generic.List<CityPixelLayer>();
        for (int i = 0; i < grid.layers.Count; i++)
            if (grid.layers[i] != null && grid.layers[i].layerId == id)
                return;
        grid.layers.Add(new CityPixelLayer
        {
            layerId = id,
            kind = CityPixelLayerKind.Custom
        });
    }

    void StampDecorOnSelection()
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            var stamp = go.GetComponent<RoadsideDecorStamp>() ?? go.AddComponent<RoadsideDecorStamp>();
            stamp.faceOutFromStreet = detourFaceOut;
            stamp.prefab = detourPrefab != null ? detourPrefab : (signagePrefab != null ? signagePrefab : shrubPrefab);
            stamp.label = disableLane ? "lane_disabled_detour" : "airport_decor";
            stamp.Apply();
            EditorUtility.SetDirty(go);
        }
    }
}
