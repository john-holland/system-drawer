#if UNITY_EDITOR
using SpatialVolumes;
using UnityEditor;
using UnityEngine;

namespace SdfMax.Editor
{
    [CustomEditor(typeof(SdfMaxSolverController))]
    public sealed class SdfMaxSolverControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ctrl = (SdfMaxSolverController)target;
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Open SDF Max Editor", GUILayout.Height(22)))
            {
                ctrl.EnsureProvider();
                ctrl.SyncToProvider();
                SdfMaxCompositionEditorWindow.ShowWindow(ctrl.volumeProvider, ctrl.composition);
            }
        }
    }
}
#endif
