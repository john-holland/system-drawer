using UnityEngine;
using UnityEditor;
using Locomotion.Audio;

namespace Locomotion.Audio.Editor
{
    /// <summary>
    /// Editor window for sound cache generation and management.
    /// </summary>
    public class SoundCacheGeneratorWindow : EditorWindow
    {
        private SoundEffectCache soundCache;
        private ActorSoundStore soundStore;
        private Vector2 scrollPosition;

        [MenuItem("Window/Locomotion/Audio/Sound Cache Generator")]
        public static void ShowWindow()
        {
            GetWindow<SoundCacheGeneratorWindow>("Sound Cache Generator");
        }

        private void OnEnable()
        {
            // Auto-find sound cache
            soundCache = FindObjectOfType<SoundEffectCache>();
            if (soundCache == null)
            {
                GameObject go = new GameObject("SoundEffectCache");
                soundCache = go.AddComponent<SoundEffectCache>();
            }

            // Auto-find sound store
            soundStore = FindObjectOfType<ActorSoundStore>();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Sound Cache Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Sound Cache Reference
            soundCache = (SoundEffectCache)EditorGUILayout.ObjectField(
                "Sound Cache",
                soundCache,
                typeof(SoundEffectCache),
                true
            );

            // Sound Store Reference
            soundStore = (ActorSoundStore)EditorGUILayout.ObjectField(
                "Sound Store",
                soundStore,
                typeof(ActorSoundStore),
                true
            );

            EditorGUILayout.Space();

            if (soundCache == null)
            {
                EditorGUILayout.HelpBox("Please assign a SoundEffectCache component.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            // Cache Settings
            EditorGUILayout.LabelField("Cache Settings", EditorStyles.boldLabel);
            soundCache.assetDirectoryPath = EditorGUILayout.TextField("Asset Directory", soundCache.assetDirectoryPath);
            soundCache.cacheDirectoryPath = EditorGUILayout.TextField("Cache Directory", soundCache.cacheDirectoryPath);
            soundCache.cacheManifestPath = EditorGUILayout.TextField("Cache Manifest Path", soundCache.cacheManifestPath);
            soundCache.maxCacheDuration = EditorGUILayout.Slider("Max Cache Duration", soundCache.maxCacheDuration, 0.1f, 10f);
            soundCache.minCacheDuration = EditorGUILayout.Slider("Min Cache Duration", soundCache.minCacheDuration, 0.01f, 1f);
            soundCache.targetSampleRate = EditorGUILayout.IntField("Target Sample Rate", soundCache.targetSampleRate);

            EditorGUILayout.Space();

            // Actions
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Scan Asset Directory"))
            {
                var clips = soundCache.ScanAssetDirectory();
                EditorUtility.DisplayDialog("Scan Complete", $"Found {clips.Count} audio files", "OK");
            }

            if (GUILayout.Button("Prebake Samples"))
            {
                var clips = soundCache.ScanAssetDirectory();
                if (clips.Count > 0)
                {
                    soundCache.PrebakeSamples(clips, soundStore);
                    EditorUtility.DisplayDialog("Prebake Complete", $"Prebaked {clips.Count} samples", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("No Files", "No audio files found. Please scan first.", "OK");
                }
            }

            if (GUILayout.Button("Generate Cache Manifest"))
            {
                soundCache.GenerateCacheManifest();
                EditorUtility.DisplayDialog("Manifest Generated", "Cache manifest generated successfully", "OK");
            }

            if (GUILayout.Button("Load Cache Manifest"))
            {
                soundCache.LoadCacheManifest();
                EditorUtility.DisplayDialog("Manifest Loaded", "Cache manifest loaded successfully", "OK");
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
