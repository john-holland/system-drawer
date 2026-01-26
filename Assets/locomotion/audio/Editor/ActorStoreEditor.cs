using UnityEngine;
using UnityEditor;
using Locomotion.Audio;

namespace Locomotion.Audio.Editor
{
    /// <summary>
    /// Custom editor for ActorSoundStore component.
    /// </summary>
    [CustomEditor(typeof(ActorSoundStore))]
    public class ActorStoreEditor : UnityEditor.Editor
    {
        private Vector2 scrollPosition;

        public override void OnInspectorGUI()
        {
            ActorSoundStore store = (ActorSoundStore)target;

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Store Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Manifest"))
            {
                store.LoadManifest();
                EditorUtility.SetDirty(store);
            }

            if (GUILayout.Button("Save Manifest"))
            {
                store.SaveManifest();
                EditorUtility.SetDirty(store);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display store contents
            EditorGUILayout.LabelField("Store Contents", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var categories = store.GetAllCategories();
            EditorGUILayout.LabelField($"Categories: {categories.Count}");

            foreach (string category in categories)
            {
                EditorGUILayout.LabelField($"Category: {category}", EditorStyles.boldLabel);
                var sounds = store.GetSoundsByCategory(category);
                EditorGUILayout.LabelField($"  Sounds: {sounds.Count}");

                for (int i = 0; i < sounds.Count && i < 5; i++) // Show first 5
                {
                    var sound = sounds[i];
                    EditorGUILayout.LabelField($"    [{sound.binaryId}] {sound.filePath}");
                }

                if (sounds.Count > 5)
                {
                    EditorGUILayout.LabelField($"    ... and {sounds.Count - 5} more");
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
