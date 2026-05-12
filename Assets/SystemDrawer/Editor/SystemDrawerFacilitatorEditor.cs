using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SystemDrawerFacilitator))]
public class SystemDrawerFacilitatorEditor : Editor
{
    private string _adHocMenuPath = "";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        FacilitatorHubUi.DrawToolbox((SystemDrawerFacilitator)target, serializedObject, ref _adHocMenuPath);

        serializedObject.ApplyModifiedProperties();
    }
}
