#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Locomotion.Audio;

namespace Locomotion.EditorTools.SystemTests
{
    public class AudioSystemTestWindow : EditorWindow
    {
        private AudioPathingSolver solver;
        private Transform source;
        private Transform listener;
        private Vector2 scroll;

        [MenuItem("Window/Locomotion/System Tests/Audio System Test")]
        public static void ShowWindow()
        {
            var w = GetWindow<AudioSystemTestWindow>("Audio System Test");
            w.minSize = new Vector2(520, 420);
            w.Show();
        }

        private void OnEnable()
        {
            solver = FindAnyObjectByType<AudioPathingSolver>();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Audio System Test", EditorStyles.boldLabel);

            solver = (AudioPathingSolver)EditorGUILayout.ObjectField("AudioPathingSolver", solver, typeof(AudioPathingSolver), true);
            source = (Transform)EditorGUILayout.ObjectField("Source Transform", source, typeof(Transform), true);
            listener = (Transform)EditorGUILayout.ObjectField("Listener Transform", listener, typeof(Transform), true);

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(solver == null || source == null || listener == null))
            {
                if (GUILayout.Button("Compute Transmission", GUILayout.Height(28)))
                {
                    var r = solver.ComputeTransmission(source.position, listener.position);
                    EditorUtility.DisplayDialog(
                        "AudioPathResult",
                        $"transmission: {r.transmission:F3}\n" +
                        $"occluders: {r.occluderCount}\n" +
                        $"trackbacks: {r.trackbacks}\n" +
                        $"stdDev: {r.transmissionStdDev:F3}\n" +
                        $"echo: {r.echoEnabled} (strength {r.echoStrength:F3})\n" +
                        $"path: {r.hasTraversablePath} (fidelity {r.pathFidelity:F3}, detour {r.pathDetourRatio:F3})",
                        "OK"
                    );
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Tip: enable 'recordLastQuery' on AudioPathingSolver to watch the latest query live in the inspector.\n" +
                "This window is a quick manual sanity-check while iterating on caching/portal heuristics.",
                MessageType.Info
            );

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

