#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainMenuSpatialGenerator))]
public sealed class MainMenuSpatialGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var gen = (MainMenuSpatialGenerator)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "When Sync Network Requirements is on, managed menu nodes follow the canonical networking tree and spec-owned fields are read-only in the inspector. Turn sync off to customize spatial layout and menu fields manually.",
            MessageType.Info);

        if (GUILayout.Button("Update Main Menu for Network Requirements"))
        {
            Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Update Main Menu Network Requirements");
            var result = gen.UpdateMainMenuForNetworkRequirements();
            EditorUtility.SetDirty(gen);
            Debug.Log($"[MainMenuSpatialGenerator] Sync complete: created={result.nodesCreated}, updated={result.nodesUpdated}, removed={result.nodesRemoved}");
        }

        if (GUILayout.Button("Generate Menu Layout"))
        {
            gen.GenerateMenuLayout();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
