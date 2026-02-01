using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;

namespace Locomotion.Narrative.EditorTools
{
    [CustomEditor(typeof(NarrativeTreeAsset))]
    public class NarrativeTreeAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var tree = (NarrativeTreeAsset)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Tree Editor", GUILayout.Height(22)))
                NarrativeTreeEditorWindow.ShowWindow(tree);
            if (GUILayout.Button("Open Calendar Wizard", GUILayout.Height(22)))
            {
                var calendar = FindCalendarUsingTree(tree);
                NarrativeCalendarWizardWindow.ShowWindow(calendar);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            DrawDefaultInspector();
        }

        private static NarrativeCalendarAsset FindCalendarUsingTree(NarrativeTreeAsset tree)
        {
            if (tree == null) return null;
            var calendars = Object.FindObjectsByType<NarrativeCalendarAsset>(FindObjectsSortMode.None);
            foreach (var cal in calendars)
            {
                if (cal.events == null) continue;
                foreach (var evt in cal.events)
                {
                    if (evt != null && evt.tree == tree)
                        return cal;
                }
            }
            return null;
        }
    }
}
