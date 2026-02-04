using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpatialGenerator4DOrchestrator))]
public class SpatialGenerator4DOrchestratorEditor : Editor
{
    private SerializedProperty listProp;
    private SerializedProperty legacy4DProp;
    private SerializedProperty showInGameProp;
    private SerializedProperty outputPathProp;
    private SerializedProperty appendProp;
    private SerializedProperty formatProp;

    private void OnEnable()
    {
        listProp = serializedObject.FindProperty("spatialGenerators");
        legacy4DProp = serializedObject.FindProperty("spatialGenerator4D");
        showInGameProp = serializedObject.FindProperty("showInGameSpatial4DEditor");
        outputPathProp = serializedObject.FindProperty("inGameUIOutputFilePath");
        appendProp = serializedObject.FindProperty("inGameUIAppendToFile");
        formatProp = serializedObject.FindProperty("inGameUIOutputFormat");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SpatialGenerator4DOrchestrator orch = (SpatialGenerator4DOrchestrator)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reference actions", EditorStyles.boldLabel);

        bool hasLegacy = legacy4DProp != null && legacy4DProp.objectReferenceValue != null;
        if (hasLegacy)
        {
            EditorGUILayout.HelpBox("Legacy 4D reference found. Click Migrate to list to move it into the spatial generators list.", MessageType.Info);
            if (GUILayout.Button("Migrate to list", GUILayout.Height(22)))
            {
                orch.MigrateLegacyIfNeeded();
                serializedObject.Update();
                EditorUtility.SetDirty(orch);
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find generators in hierarchy", GUILayout.Height(22)))
        {
            var inChildren = orch.GetComponentsInChildren<SpatialGeneratorBase>(true);
            var inParent = orch.GetComponentsInParent<SpatialGeneratorBase>(true);
            orch.spatialGenerators.Clear();
            if (inParent != null)
                foreach (var g in inParent)
                    if (g != null && !orch.spatialGenerators.Contains(g))
                        orch.spatialGenerators.Add(g);
            if (inChildren != null)
                foreach (var g in inChildren)
                    if (g != null && !orch.spatialGenerators.Contains(g))
                        orch.spatialGenerators.Add(g);
            serializedObject.Update();
            EditorUtility.SetDirty(orch);
        }
        if (GUILayout.Button("Refresh references", GUILayout.Height(22)))
        {
            orch.ResolveReferences();
            serializedObject.Update();
            EditorUtility.SetDirty(orch);
        }
        if (GUILayout.Button("Find calendar / weather in hierarchy", GUILayout.Height(22)))
        {
            if (orch.narrativeCalendar == null)
            {
                var cal = orch.GetComponentInChildren<Locomotion.Narrative.NarrativeCalendarAsset>(true);
                if (cal == null) cal = orch.GetComponentInParent<Locomotion.Narrative.NarrativeCalendarAsset>();
                if (cal != null) { orch.narrativeCalendar = cal; EditorUtility.SetDirty(orch); }
            }
            if (orch.weatherSystemObject == null)
            {
                var wt = System.Type.GetType("Weather.WeatherSystem, Weather.Runtime");
                if (wt != null)
                {
                    var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                    foreach (var mb in all)
                        if (mb != null && wt.IsInstanceOfType(mb)) { orch.weatherSystemObject = mb.gameObject; EditorUtility.SetDirty(orch); break; }
                }
            }
            serializedObject.Update();
        }
        if (GUILayout.Button("Find bounds provider in hierarchy", GUILayout.Height(22)))
        {
            var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in all)
            {
                if (mb != null && mb is IBoundsProvider)
                {
                    orch.boundsProvider = mb;
                    EditorUtility.SetDirty(orch);
                    break;
                }
            }
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add 3D generator", GUILayout.Height(22)))
        {
            GameObject child = new GameObject("SpatialGenerator3D");
            child.transform.SetParent(orch.transform);
            var sg = child.AddComponent<SpatialGenerator>();
            orch.spatialGenerators.Add(sg);
            serializedObject.Update();
            EditorUtility.SetDirty(orch);
            Selection.activeGameObject = child;
        }
        if (GUILayout.Button("Add 4D generator", GUILayout.Height(22)))
        {
            GameObject child = new GameObject("SpatialGenerator4D");
            child.transform.SetParent(orch.transform);
            var sg4 = child.AddComponent<SpatialGenerator4D>();
            orch.spatialGenerators.Add(sg4);
            serializedObject.Update();
            EditorUtility.SetDirty(orch);
            Selection.activeGameObject = child;
        }
        EditorGUILayout.EndHorizontal();

        if (listProp != null && orch.spatialGenerators != null && orch.spatialGenerators.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generator types", EditorStyles.miniLabel);
            bool has2D3D = false;
            for (int i = 0; i < orch.spatialGenerators.Count; i++)
            {
                var g = orch.spatialGenerators[i];
                if (g is SpatialGenerator) has2D3D = true;
                string badge = g == null ? "—" : (g is SpatialGenerator4D ? "4D" : "3D");
                EditorGUILayout.LabelField($"  Element {i}: {badge}", EditorStyles.miniLabel);
            }
            if (!has2D3D)
                EditorGUILayout.HelpBox("At least one 2D/3D generator (SpatialGenerator) is recommended for the enforced structure.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4D tree mirror", EditorStyles.miniLabel);
        if (GUILayout.Button("Refresh 4D mirror", GUILayout.Height(22)))
        {
            SpatialGenerator4D sg4 = null;
            if (orch.spatialGenerators != null)
                foreach (var g in orch.spatialGenerators)
                    if (g is SpatialGenerator4D s) { sg4 = s; break; }
            if (sg4 == null)
                Debug.LogWarning("[Spatial4D] Refresh 4D mirror: No SpatialGenerator4D in list. Add a 4D generator first.");
            else
            {
                var entries = sg4.GetPlacedEntries();
                Transform mirrorRoot = orch.transform.Find("4DTreeMirror");
                if (mirrorRoot == null)
                {
                    var go = new GameObject("4DTreeMirror");
                    go.transform.SetParent(orch.transform);
                    mirrorRoot = go.transform;
                }
                while (mirrorRoot.childCount > 0)
                    Object.DestroyImmediate(mirrorRoot.GetChild(0).gameObject);
                for (int i = 0; i < entries.Count; i++)
                {
                    var (volume, payload) = entries[i];
                    var child = new GameObject("Volume_" + i + "_" + (payload != null ? payload.ToString() : ""));
                    child.transform.SetParent(mirrorRoot);
                    child.transform.position = volume.center;
                    var node = child.AddComponent<Spatial4DMirrorNode>();
                    node.SetFrom(volume, payload != null ? payload.ToString() : null);
                }
                EditorUtility.SetDirty(orch);
            }
        }

        var skinController = orch.GetComponent<SpatialGeneratorSkinController>();
        if (skinController != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skins", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SpatialGeneratorSkinController is present. Use it to switch editor/runtime skin and apply stylesheets.", MessageType.None);
            if (GUILayout.Button("Select Skin Controller", GUILayout.Height(20)))
                Selection.activeGameObject = skinController.gameObject;
        }

        if (showInGameProp != null && showInGameProp.boolValue)
        {
            EditorGUILayout.Space();
            if (orch.GetComponent<Spatial4DInGameUI>() == null && GUILayout.Button("Add In-Game UI component", GUILayout.Height(22)))
            {
                var ui = orch.gameObject.AddComponent<Spatial4DInGameUI>();
                ui.orchestrator = orch;
                serializedObject.Update();
                EditorUtility.SetDirty(orch);
            }
            if (outputPathProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse for output file", GUILayout.Width(160)))
                {
                    string dir = string.IsNullOrEmpty(orch.inGameUIOutputFilePath)
                        ? Application.dataPath
                        : System.IO.Path.GetDirectoryName(orch.inGameUIOutputFilePath);
                    string path = EditorUtility.SaveFilePanel("Spatial 4D output file", dir, "Spatial4DExpressions", "json");
                    if (!string.IsNullOrEmpty(path))
                        outputPathProp.stringValue = path;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
