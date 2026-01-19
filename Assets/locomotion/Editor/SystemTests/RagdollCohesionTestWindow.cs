#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Locomotion.Rig;

namespace Locomotion.EditorTools.SystemTests
{
    public class RagdollCohesionTestWindow : EditorWindow
    {
        private Vector2 scroll;
        private GameObject[] ragdolls;

        [MenuItem("Window/Locomotion/System Tests/Ragdoll Cohesion Test")]
        public static void ShowWindow()
        {
            var w = GetWindow<RagdollCohesionTestWindow>("Ragdoll Cohesion Test");
            w.minSize = new Vector2(620, 420);
            w.Refresh();
            w.Show();
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void Refresh()
        {
            ragdolls = Resources.FindObjectsOfTypeAll<RagdollSystem>()
                .Where(r => r != null)
                .Select(r => r.gameObject)
                .Distinct()
                .OrderBy(go => go.name)
                .ToArray();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(120)))
                Refresh();
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Ragdoll Cohesion Test", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            if (ragdolls == null || ragdolls.Length == 0)
            {
                EditorGUILayout.HelpBox("No RagdollSystem instances found.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            foreach (var go in ragdolls)
            {
                if (go == null) continue;

                var boneMap = go.GetComponent<BoneMap>();
                var nervous = go.GetComponent<NervousSystem>();
                var brain = go.GetComponentInChildren<Brain>();
                var world = go.GetComponent<WorldInteraction>();
                var anySensor = go.GetComponentInChildren<Sensor>();
                var anyEar = go.GetComponentInChildren<Locomotion.Audio.Ears>();

                bool ok = boneMap != null && nervous != null && brain != null && world != null && anySensor != null && anyEar != null;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                GUILayout.Label(ok ? "OK" : "MISSING", ok ? EditorStyles.boldLabel : EditorStyles.boldLabel, GUILayout.Width(80));
                if (GUILayout.Button("Select", GUILayout.Width(80)))
                    Selection.activeGameObject = go;
                EditorGUILayout.EndHorizontal();

                if (!ok)
                {
                    EditorGUILayout.LabelField($"BoneMap: {(boneMap != null ? "✓" : "✗")}");
                    EditorGUILayout.LabelField($"NervousSystem: {(nervous != null ? "✓" : "✗")}");
                    EditorGUILayout.LabelField($"Brain: {(brain != null ? "✓" : "✗")}");
                    EditorGUILayout.LabelField($"WorldInteraction: {(world != null ? "✓" : "✗")}");
                    EditorGUILayout.LabelField($"Sensor: {(anySensor != null ? "✓" : "✗")}");
                    EditorGUILayout.LabelField($"Ears: {(anyEar != null ? "✓" : "✗")}");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

