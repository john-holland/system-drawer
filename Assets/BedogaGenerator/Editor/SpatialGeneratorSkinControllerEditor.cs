using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpatialGeneratorSkinController))]
public class SpatialGeneratorSkinControllerEditor : Editor
{
    private SerializedProperty skinsProp;
    private SerializedProperty activeSkinIndexProp;
    private SerializedProperty editorActiveSkinIndexProp;
    private SerializedProperty orchestratorProp;

    private void OnEnable()
    {
        skinsProp = serializedObject.FindProperty("skins");
        activeSkinIndexProp = serializedObject.FindProperty("activeSkinIndex");
        editorActiveSkinIndexProp = serializedObject.FindProperty("editorActiveSkinIndex");
        orchestratorProp = serializedObject.FindProperty("orchestrator");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var controller = (SpatialGeneratorSkinController)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skin selection", EditorStyles.boldLabel);

        int skinCount = controller.skins != null ? controller.skins.Count : 0;
        if (skinCount == 0)
        {
            EditorGUILayout.HelpBox("Add at least one SpatialGeneratorSkin to the skins list.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        string[] options = new string[skinCount];
        for (int i = 0; i < skinCount; i++)
        {
            var s = controller.skins[i];
            options[i] = s != null ? $"{i}: {s.displayName}" : $"{i}: (null)";
        }

        int activeIdx = activeSkinIndexProp.intValue;
        activeIdx = Mathf.Clamp(activeIdx, 0, skinCount - 1);
        int newActiveIdx = EditorGUILayout.Popup("Active (runtime)", activeIdx, options);
        if (newActiveIdx != activeIdx)
        {
            activeSkinIndexProp.intValue = newActiveIdx;
            if (Application.isPlaying)
                controller.ApplySkin(newActiveIdx);
        }

        int editorIdx = editorActiveSkinIndexProp.intValue;
        editorIdx = Mathf.Clamp(editorIdx, 0, skinCount - 1);
        int newEditorIdx = EditorGUILayout.Popup("Editor skin", editorIdx, options);
        if (newEditorIdx != editorIdx)
        {
            editorActiveSkinIndexProp.intValue = newEditorIdx;
            controller.ApplySkin(newEditorIdx);
        }

        if (GUILayout.Button("Apply editor skin now", GUILayout.Height(22)))
        {
            controller.ApplySkin(controller.editorActiveSkinIndex);
            EditorUtility.SetDirty(controller);
        }

        if (controller.GetActiveSkin() != null)
        {
            var skin = controller.GetActiveSkin();
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Active skin (editor)", EditorStyles.miniLabel);
            EditorGUILayout.ObjectField("3D generator", skin.spatialGenerator3D, typeof(SpatialGenerator), true);
            EditorGUILayout.ObjectField("Stylesheet", skin.stylesheet, typeof(SpatialGeneratorStylesheet), true);
            if (skin.spatialGenerator4D != null)
                EditorGUILayout.ObjectField("4D generator", skin.spatialGenerator4D, typeof(SpatialGenerator4D), true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
