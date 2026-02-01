#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Locomotion.Senses;
using Locomotion.Smell;

namespace Locomotion.EditorTools.SystemTests
{
    public class SmellSystemTestWindow : EditorWindow
    {
        private SmellSensor smellSensor;
        private SmellEmitter emitter;
        private HighDefinitionSmellSolver hd;
        private Vector2 scroll;

        [MenuItem("Window/Locomotion/System Tests/Smell System Test")]
        public static void ShowWindow()
        {
            var w = GetWindow<SmellSystemTestWindow>("Smell System Test");
            w.minSize = new Vector2(520, 420);
            w.Show();
        }

        private void OnEnable()
        {
            hd = FindAnyObjectByType<HighDefinitionSmellSolver>();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Smell System Test", EditorStyles.boldLabel);

            smellSensor = (SmellSensor)EditorGUILayout.ObjectField("SmellSensor", smellSensor, typeof(SmellSensor), true);
            emitter = (SmellEmitter)EditorGUILayout.ObjectField("SmellEmitter", emitter, typeof(SmellEmitter), true);
            hd = (HighDefinitionSmellSolver)EditorGUILayout.ObjectField("HighDefinitionSmellSolver", hd, typeof(HighDefinitionSmellSolver), true);

            EditorGUILayout.Space(8);

            if (smellSensor != null)
            {
                EditorGUILayout.LabelField("Sensor Config", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HD enabled: {smellSensor.enableHighDefinitionSmell}");
                EditorGUILayout.LabelField($"HD radius: {smellSensor.hdSampleRadius:F2}");
            }

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(smellSensor == null))
            {
                if (GUILayout.Button("Sample HD Concentration (at sensor)", GUILayout.Height(28)))
                {
                    if (hd == null)
                        hd = FindAnyObjectByType<HighDefinitionSmellSolver>();

                    float c = hd != null ? hd.SampleConcentration(smellSensor.transform.position, Mathf.Max(0.01f, smellSensor.hdSampleRadius)) : 0f;
                    EditorUtility.DisplayDialog("HD Smell", $"Concentration: {c:F3}", "OK");
                }
            }

            EditorGUILayout.Space(8);
            if (emitter != null && smellSensor != null)
            {
                float d = Vector3.Distance(emitter.transform.position, smellSensor.transform.position);
                EditorGUILayout.HelpBox($"Emitter→Sensor distance: {d:F2}m", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

