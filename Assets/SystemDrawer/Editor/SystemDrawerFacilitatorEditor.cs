using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SystemDrawerFacilitator))]
public class SystemDrawerFacilitatorEditor : Editor
{
    private string _adHocMenuPath = "";
    private Vector2 _toolboxScroll;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        _toolboxScroll = EditorGUILayout.BeginScrollView(_toolboxScroll, GUILayout.MaxHeight(420f));
        FacilitatorHubUi.DrawToolbox((SystemDrawerFacilitator)target, serializedObject, ref _adHocMenuPath);
        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();
    }
}
