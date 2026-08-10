using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Airport-focused PixelLight / CityPixelGrid designer — multi grid slots + layers.</summary>
public sealed class AirportPixelLightDesignerWindow : EditorWindow
{
    CityPixelGrid grid;
    PixelLightPatternAsset pattern;
    PixelLightMultiSlotCatalog catalog;
    AirplaneVehicleRagdoll airplane;
    Vector2 _slotsScroll;
    bool disableLane;
    bool detourFaceOut = true;
    GameObject detourPrefab;
    GameObject signagePrefab;
    GameObject shrubPrefab;
    MonoScript cleanupCrewBt;
    MonoScript maintenanceCrewBt;
    List<PixelLightGridMountGameObject> _sceneMounts = new List<PixelLightGridMountGameObject>();

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
            "Uses City Pixel Grid bounds/layers with airport-focused Custom layer ids (apron, runway, taxiway, terminal). Multi grid slots share PixelLightMultiSlotCatalog with heli/airplane.",
            MessageType.Info);

        grid = (CityPixelGrid)EditorGUILayout.ObjectField("City Pixel Grid", grid, typeof(CityPixelGrid), false);
        pattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField("PixelLight Pattern", pattern, typeof(PixelLightPatternAsset), false);
        catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "Multi-slot catalog", catalog, typeof(PixelLightMultiSlotCatalog), false);
        airplane = (AirplaneVehicleRagdoll)EditorGUILayout.ObjectField(
            "Airplane (optional)", airplane, typeof(AirplaneVehicleRagdoll), true);
        if (airplane != null)
        {
            if (catalog == null && airplane.pixelLightCatalog != null)
                catalog = airplane.pixelLightCatalog;
            airplane.pixelLightCatalog = catalog;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid slots (scroll + accordion)", EditorStyles.boldLabel);
        if (GUILayout.Button("Collect PixelLightGridMount in scene"))
        {
            _sceneMounts.Clear();
            _sceneMounts.AddRange(Object.FindObjectsByType<PixelLightGridMountGameObject>(FindObjectsSortMode.None));
            catalog?.SyncSlotsFromMounts(_sceneMounts);
            if (catalog != null) EditorUtility.SetDirty(catalog);
        }
        if (catalog == null && GUILayout.Button("Create multi-slot catalog"))
        {
            var c = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
            string path = EditorUtility.SaveFilePanelInProject(
                "PixelLight Multi Slot Catalog", "AirportPixelLightMultiSlot", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(c, path);
                catalog = c;
                if (airplane != null) airplane.pixelLightCatalog = c;
            }
        }
        PixelLightGridSlotAccordionDrawer.Draw(catalog, ref _slotsScroll, null, entry =>
        {
            if (entry?.heliSlot != null)
                Selection.activeGameObject = entry.heliSlot.gameObject;
            else if (entry?.mount != null)
                Selection.activeGameObject = entry.mount.gameObject;
        }, 280f, airplane);

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

        if (GUILayout.Button("Open Pixel Light Timed Designer"))
            PixelLightTimedDesignerWindow.Open();
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
            grid.layers = new List<CityPixelLayer>();
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
