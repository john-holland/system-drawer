using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpatialGenerator))]
public class SpatialGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SpatialGenerator generator = (SpatialGenerator)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
        if (GUILayout.Button("Recalculate / Refresh Gizmos", GUILayout.Height(22)))
        {
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Run Test", GUILayout.Height(30)))
        {
            generator.RunTest();
            EditorUtility.SetDirty(generator);
        }
        
        if (GUILayout.Button("Open Location Assertion Test Window", GUILayout.Height(25)))
        {
            LocationAssertionTestWindow.ShowWindow();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);
        
        SerializedProperty testResultsProp = serializedObject.FindProperty("testResults");
        if (testResultsProp != null)
        {
            EditorGUILayout.PropertyField(testResultsProp, GUIContent.none);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
