using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpatialGenerator4DOrchestrator))]
public class SpatialGenerator4DOrchestratorEditor : Editor
{
    private SerializedProperty listProp;
    private SerializedProperty legacy4DProp;

    private void OnEnable()
    {
        listProp = serializedObject.FindProperty("spatialGenerators");
        legacy4DProp = serializedObject.FindProperty("spatialGenerator4D");
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
            for (int i = 0; i < orch.spatialGenerators.Count; i++)
            {
                var g = orch.spatialGenerators[i];
                string badge = g == null ? "—" : (g is SpatialGenerator4D ? "4D" : "3D");
                EditorGUILayout.LabelField($"  Element {i}: {badge}", EditorStyles.miniLabel);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
