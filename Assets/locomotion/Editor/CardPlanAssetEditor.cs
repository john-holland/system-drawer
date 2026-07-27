#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardPlanAsset))]
public sealed class CardPlanAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Card Planning Editor", GUILayout.Height(28f)))
            CardPlanningEditorWindow.ShowWindow((CardPlanAsset)target);

        EditorGUILayout.Space(4f);
        DrawDefaultInspector();
    }
}
#endif
