#if UNITY_EDITOR
using SdfMax;
using UnityEditor;
using UnityEngine;

namespace SdfMax.Editor
{
    [CustomEditor(typeof(SdfMaxCompositionAsset))]
    public sealed class SdfMaxCompositionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Open in SDF Max Editor", GUILayout.Height(22)))
                SdfMaxCompositionEditorWindow.ShowWindow(null, (SdfMaxCompositionAsset)target);
        }
    }
}
#endif
