#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StructuredChatSpatialGenerator))]
public sealed class StructuredChatSpatialGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var gen = (StructuredChatSpatialGenerator)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Update Structured Chat for Lexicon"))
        {
            Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Update Structured Chat Lexicon Nodes");
            var result = gen.UpdateForLexicon();
            EditorUtility.SetDirty(gen);
            Debug.Log($"[StructuredChatSpatialGenerator] Sync complete: created={result.nodesCreated}, updated={result.nodesUpdated}, removed={result.nodesRemoved}");
        }
        if (GUILayout.Button("Generate Chat Layout"))
            gen.GenerateLayout();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
