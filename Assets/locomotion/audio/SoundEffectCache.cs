using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Locomotion.Audio
{
    /// <summary>
    /// System for scanning asset directories, prebaking short DSP samples, and generating cache manifest.
    /// Optimizes audio files for realtime DSP processing.
    /// </summary>
    public class SoundEffectCache : MonoBehaviour
    {
        [Header("Cache Settings")]
        [Tooltip("Path to scan for audio files (relative to Assets folder)")]
        public string assetDirectoryPath = "Assets/Audio";

        [Tooltip("Path to cache directory (relative to Assets folder)")]
        public string cacheDirectoryPath = "Assets/Audio/Cache";

        [Tooltip("Path to cache manifest file")]
        public string cacheManifestPath = "Assets/Audio/Cache/CacheManifest.json";

        [Tooltip("Maximum duration for cached samples (seconds)")]
        [Range(0.1f, 10f)]
        public float maxCacheDuration = 2f;

        [Tooltip("Minimum duration for cached samples (seconds)")]
        [Range(0.01f, 1f)]
        public float minCacheDuration = 0.1f;

        [Tooltip("Target sample rate for cached samples (0 = use original)")]
        public int targetSampleRate = 0;

        [Header("Cache Data")]
        [SerializeField]
        private SoundCacheManifest cacheManifest = new SoundCacheManifest();

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        private void Awake()
        {
            LoadCacheManifest();
        }

        /// <summary>
        /// Scan asset directory for audio files and return list of AudioClips.
        /// </summary>
        public List<AudioClip> ScanAssetDirectory(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = assetDirectoryPath;

            List<AudioClip> audioClips = new List<AudioClip>();

#if UNITY_EDITOR
            string fullPath = Path.Combine(Application.dataPath, "..", path);
            if (!Directory.Exists(fullPath))
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[SoundEffectCache] Directory not found: {fullPath}");
                }
                return audioClips;
            }

            // Get all audio file extensions
            string[] audioExtensions = { "*.wav", "*.mp3", "*.ogg", "*.aiff", "*.aif" };

            foreach (string extension in audioExtensions)
            {
                string[] files = Directory.GetFiles(fullPath, extension, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // Convert to Unity asset path
                    string relativePath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                    if (clip != null)
                    {
                        audioClips.Add(clip);
                    }
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[SoundEffectCache] Scanned {audioClips.Count} audio files from {path}");
            }
#endif

            return audioClips;
        }

        /// <summary>
        /// Prebake short DSP-ready samples from audio clips.
        /// </summary>
        public void PrebakeSamples(List<AudioClip> clips, ActorSoundStore soundStore = null)
        {
            if (clips == null || clips.Count == 0)
            {
                Debug.LogWarning("[SoundEffectCache] No clips provided for prebaking");
                return;
            }

            // Ensure cache directory exists
            string cacheDir = Path.Combine(Application.dataPath, "..", cacheDirectoryPath);
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            int binaryId = 0;
            int cachedCount = 0;

            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                    continue;

                // Determine binary ID from sound store if available
                if (soundStore != null)
                {
                    // Try to find existing sound reference
                    var allSounds = soundStore.GetAllSounds();
                    foreach (var soundRef in allSounds)
                    {
                        if (soundRef.audioClip == clip || soundRef.filePath == GetAssetPath(clip))
                        {
                            binaryId = soundRef.binaryId;
                            break;
                        }
                    }
                }

                // If not found, use next available ID
                if (binaryId == 0 && soundStore != null)
                {
                    binaryId = soundStore.GetNextBinaryId();
                }

                // Determine origin based on clip name/path
                SoundOrigin origin = DetermineOrigin(clip);

                // Create cached entry
                string originalPath = GetAssetPath(clip);
                string cacheFileName = $"cached_{binaryId}_{clip.name}.bytes";
                string cacheFilePath = Path.Combine(cacheDirectoryPath, cacheFileName);

                // Extract sample data (limit to maxCacheDuration)
                float[] samples = ExtractSamples(clip, maxCacheDuration);
                if (samples == null || samples.Length == 0)
                    continue;

                // Create cached entry
                CachedSoundEntry entry = new CachedSoundEntry
                {
                    binaryId = binaryId,
                    originalFilePath = originalPath,
                    cacheFilePath = cacheFilePath,
                    duration = Mathf.Min(clip.length, maxCacheDuration),
                    sampleRate = targetSampleRate > 0 ? targetSampleRate : clip.frequency,
                    channels = clip.channels,
                    sampleCount = samples.Length,
                    origin = origin
                };

                // Convert to bytes
                entry.prebakedData = ConvertSamplesToBytes(samples);

                // Save cached data to file
                string fullCachePath = Path.Combine(cacheDir, cacheFileName);
                File.WriteAllBytes(fullCachePath, entry.prebakedData);

                // Add to manifest
                cacheManifest.AddOrUpdateEntry(entry);
                cachedCount++;

                if (enableDebugLogging && cachedCount % 10 == 0)
                {
                    Debug.Log($"[SoundEffectCache] Prebaked {cachedCount}/{clips.Count} samples...");
                }
            }

            // Update manifest timestamp
            cacheManifest.generatedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Save manifest
            SaveCacheManifest();

            if (enableDebugLogging)
            {
                Debug.Log($"[SoundEffectCache] Prebaked {cachedCount} samples to cache");
            }
        }

        /// <summary>
        /// Generate cache manifest and save to file.
        /// </summary>
        public void GenerateCacheManifest()
        {
            cacheManifest.generatedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveCacheManifest();
        }

        /// <summary>
        /// Get cached sample by binary ID.
        /// </summary>
        public CachedSoundEntry GetCachedSample(int binaryId)
        {
            return cacheManifest.GetEntry(binaryId);
        }

        /// <summary>
        /// Load cached sample data and return as AudioClip (runtime).
        /// </summary>
        public AudioClip LoadCachedSample(int binaryId)
        {
            CachedSoundEntry entry = cacheManifest.GetEntry(binaryId);
            if (entry == null || entry.prebakedData == null)
                return null;

            // Convert bytes back to samples
            float[] samples = entry.GetSamples();
            if (samples == null)
                return null;

            // Create AudioClip from samples
            AudioClip clip = AudioClip.Create($"Cached_{binaryId}", 
                entry.sampleCount / entry.channels, 
                entry.channels, 
                entry.sampleRate, 
                false);

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Load cache manifest from file.
        /// </summary>
        public void LoadCacheManifest()
        {
            if (string.IsNullOrEmpty(cacheManifestPath))
                return;

            string fullPath = Path.Combine(Application.dataPath, "..", cacheManifestPath);
            if (!File.Exists(fullPath))
            {
                cacheManifest = new SoundCacheManifest();
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                cacheManifest = JsonUtility.FromJson<SoundCacheManifest>(jsonContent);
                if (cacheManifest == null)
                {
                    cacheManifest = new SoundCacheManifest();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SoundEffectCache] Error loading manifest: {e.Message}");
                cacheManifest = new SoundCacheManifest();
            }
        }

        /// <summary>
        /// Save cache manifest to file.
        /// </summary>
        public void SaveCacheManifest()
        {
            if (string.IsNullOrEmpty(cacheManifestPath))
                return;

            try
            {
                string fullPath = Path.Combine(Application.dataPath, "..", cacheManifestPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonContent = JsonUtility.ToJson(cacheManifest, true);
                File.WriteAllText(fullPath, jsonContent);

                if (enableDebugLogging)
                {
                    Debug.Log($"[SoundEffectCache] Saved manifest to {fullPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SoundEffectCache] Error saving manifest: {e.Message}");
            }
        }

        /// <summary>
        /// Extract samples from AudioClip, limiting to max duration.
        /// </summary>
        private float[] ExtractSamples(AudioClip clip, float maxDuration)
        {
            int maxSamples = Mathf.FloorToInt(maxDuration * clip.frequency * clip.channels);
            int totalSamples = clip.samples * clip.channels;
            int samplesToExtract = Mathf.Min(maxSamples, totalSamples);

            float[] samples = new float[samplesToExtract];
            clip.GetData(samples, 0);

            return samples;
        }

        /// <summary>
        /// Convert float samples to 16-bit PCM bytes.
        /// </summary>
        private byte[] ConvertSamplesToBytes(float[] samples)
        {
            byte[] bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                bytes[i * 2] = (byte)(sample & 0xFF);
                bytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            return bytes;
        }

        /// <summary>
        /// Get asset path for AudioClip.
        /// </summary>
        private string GetAssetPath(AudioClip clip)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPath(clip);
#else
            return clip.name;
#endif
        }

        /// <summary>
        /// Determine sound origin from clip name/path.
        /// </summary>
        private SoundOrigin DetermineOrigin(AudioClip clip)
        {
            string name = clip.name.ToLower();
            string path = GetAssetPath(clip).ToLower();

            // Check for jaw/speech indicators
            if (name.Contains("speech") || name.Contains("vocal") || name.Contains("mouth") || 
                name.Contains("jaw") || name.Contains("grunt") || name.Contains("exclamation") ||
                path.Contains("speech") || path.Contains("vocal") || path.Contains("mouth"))
            {
                return SoundOrigin.Jaw;
            }

            return SoundOrigin.BehaviorTree;
        }
    }
}
