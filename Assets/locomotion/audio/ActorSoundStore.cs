using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Actor's sound effect store with 2D data structure (categories -> sounds) and JSON manifest.
    /// Manages file locations, binary index IDs, and sound metadata.
    /// </summary>
    public class ActorSoundStore : MonoBehaviour
    {
        [Header("Manifest Settings")]
        [Tooltip("Path to JSON manifest file (relative to Assets folder)")]
        public string soundManifestPath = "Assets/Audio/SoundManifest.json";

        [Header("Store Data")]
        [Tooltip("2D data structure: category -> list of sound references")]
        [SerializeField]
        private Dictionary<string, List<SoundReference>> soundCategories = new Dictionary<string, List<SoundReference>>();

        [Tooltip("Binary ID to sound reference mapping")]
        [SerializeField]
        private Dictionary<int, SoundReference> binaryIndexMap = new Dictionary<int, SoundReference>();

        [Tooltip("All sound references (flat list)")]
        [SerializeField]
        private List<SoundReference> allSounds = new List<SoundReference>();

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        /// <summary>
        /// JSON structure for manifest file.
        /// </summary>
        [Serializable]
        public class SoundManifest
        {
            public string version = "1.0";
            public SoundManifestEntry[] sounds = new SoundManifestEntry[0];
            public string[] categories = new string[0];
        }

        [Serializable]
        public class SoundManifestEntry
        {
            public int binaryId;
            public string filePath;
            public string category;
            public string[] tags = new string[0];
            public float duration;
            public int sampleRate;
            public int channels;
            public string origin; // "Jaw" or "BehaviorTree"
        }

        private void Awake()
        {
            // Auto-load manifest on start
            if (!string.IsNullOrEmpty(soundManifestPath))
            {
                LoadManifest();
            }
        }

        /// <summary>
        /// Load JSON manifest and populate data structures.
        /// </summary>
        public void LoadManifest()
        {
            if (string.IsNullOrEmpty(soundManifestPath))
            {
                Debug.LogWarning("[ActorSoundStore] No manifest path specified");
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, "..", soundManifestPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[ActorSoundStore] Manifest file not found: {fullPath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                SoundManifest manifest = JsonUtility.FromJson<SoundManifest>(jsonContent);

                if (manifest == null)
                {
                    Debug.LogError("[ActorSoundStore] Failed to deserialize manifest");
                    return;
                }

                // Clear existing data
                soundCategories.Clear();
                binaryIndexMap.Clear();
                allSounds.Clear();

                // Populate from manifest
                if (manifest.sounds != null)
                {
                    foreach (var entry in manifest.sounds)
                    {
                        SoundReference soundRef = new SoundReference
                        {
                            binaryId = entry.binaryId,
                            filePath = entry.filePath,
                            category = entry.category,
                            duration = entry.duration,
                            sampleRate = entry.sampleRate,
                            channels = entry.channels,
                            tags = entry.tags != null ? new List<string>(entry.tags) : new List<string>()
                        };

                    // Parse origin
                    if (Enum.TryParse<SoundOrigin>(entry.origin, out SoundOrigin origin))
                    {
                        soundRef.origin = origin;
                    }

                    // Add to categories
                    if (!soundCategories.ContainsKey(entry.category))
                    {
                        soundCategories[entry.category] = new List<SoundReference>();
                    }
                    soundCategories[entry.category].Add(soundRef);

                    // Add to binary index map
                    binaryIndexMap[entry.binaryId] = soundRef;

                        // Add to flat list
                        allSounds.Add(soundRef);
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"[ActorSoundStore] Loaded {allSounds.Count} sounds from {soundCategories.Count} categories");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActorSoundStore] Error loading manifest: {e.Message}");
            }
        }

        /// <summary>
        /// Save current store data to JSON manifest.
        /// </summary>
        public void SaveManifest()
        {
            if (string.IsNullOrEmpty(soundManifestPath))
            {
                Debug.LogWarning("[ActorSoundStore] No manifest path specified");
                return;
            }

            try
            {
                SoundManifest manifest = new SoundManifest
                {
                    version = "1.0"
                };

                // Collect all categories
                HashSet<string> categoriesSet = new HashSet<string>();

                // Convert sound references to manifest entries
                List<SoundManifestEntry> manifestEntries = new List<SoundManifestEntry>();
                foreach (var soundRef in allSounds)
                {
                    categoriesSet.Add(soundRef.category);

                    manifestEntries.Add(new SoundManifestEntry
                    {
                        binaryId = soundRef.binaryId,
                        filePath = soundRef.filePath,
                        category = soundRef.category,
                        tags = soundRef.tags != null ? soundRef.tags.ToArray() : new string[0],
                        duration = soundRef.duration,
                        sampleRate = soundRef.sampleRate,
                        channels = soundRef.channels,
                        origin = soundRef.origin.ToString()
                    });
                }

                manifest.sounds = manifestEntries.ToArray();
                manifest.categories = new List<string>(categoriesSet).ToArray();

                // Write to file
                string fullPath = Path.Combine(Application.dataPath, "..", soundManifestPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonContent = JsonUtility.ToJson(manifest, true);
                File.WriteAllText(fullPath, jsonContent);

                if (enableDebugLogging)
                {
                    Debug.Log($"[ActorSoundStore] Saved manifest to {fullPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActorSoundStore] Error saving manifest: {e.Message}");
            }
        }

        /// <summary>
        /// Retrieve sound reference by category and index.
        /// </summary>
        public SoundReference GetSoundByCategory(string category, int index)
        {
            if (soundCategories.TryGetValue(category, out List<SoundReference> sounds))
            {
                if (index >= 0 && index < sounds.Count)
                {
                    return sounds[index];
                }
            }

            if (enableDebugLogging)
            {
                Debug.LogWarning($"[ActorSoundStore] Sound not found: category={category}, index={index}");
            }

            return null;
        }

        /// <summary>
        /// Retrieve sound reference by binary ID.
        /// </summary>
        public SoundReference GetSoundByBinaryId(int binaryId)
        {
            if (binaryIndexMap.TryGetValue(binaryId, out SoundReference sound))
            {
                return sound;
            }

            if (enableDebugLogging)
            {
                Debug.LogWarning($"[ActorSoundStore] Sound not found: binaryId={binaryId}");
            }

            return null;
        }

        /// <summary>
        /// Add a new sound to the store.
        /// </summary>
        public void AddSound(string category, SoundReference sound)
        {
            if (sound == null)
            {
                Debug.LogWarning("[ActorSoundStore] Cannot add null sound reference");
                return;
            }

            // Ensure category exists
            if (!soundCategories.ContainsKey(category))
            {
                soundCategories[category] = new List<SoundReference>();
            }

            // Update category if different
            if (sound.category != category)
            {
                sound.category = category;
            }

            // Check for duplicate binary ID
            if (binaryIndexMap.ContainsKey(sound.binaryId))
            {
                Debug.LogWarning($"[ActorSoundStore] Binary ID {sound.binaryId} already exists, replacing");
                RemoveSound(sound.binaryId);
            }

            // Add to data structures
            soundCategories[category].Add(sound);
            binaryIndexMap[sound.binaryId] = sound;
            allSounds.Add(sound);

            if (enableDebugLogging)
            {
                Debug.Log($"[ActorSoundStore] Added sound: binaryId={sound.binaryId}, category={category}, path={sound.filePath}");
            }
        }

        /// <summary>
        /// Remove a sound from the store by binary ID.
        /// </summary>
        public void RemoveSound(int binaryId)
        {
            if (!binaryIndexMap.TryGetValue(binaryId, out SoundReference sound))
            {
                return;
            }

            // Remove from category
            if (soundCategories.TryGetValue(sound.category, out List<SoundReference> categorySounds))
            {
                categorySounds.Remove(sound);
                if (categorySounds.Count == 0)
                {
                    soundCategories.Remove(sound.category);
                }
            }

            // Remove from maps
            binaryIndexMap.Remove(binaryId);
            allSounds.Remove(sound);

            if (enableDebugLogging)
            {
                Debug.Log($"[ActorSoundStore] Removed sound: binaryId={binaryId}");
            }
        }

        /// <summary>
        /// Get all sounds in a category.
        /// </summary>
        public List<SoundReference> GetSoundsByCategory(string category)
        {
            if (soundCategories.TryGetValue(category, out List<SoundReference> sounds))
            {
                return new List<SoundReference>(sounds);
            }

            return new List<SoundReference>();
        }

        /// <summary>
        /// Get all categories.
        /// </summary>
        public List<string> GetAllCategories()
        {
            return new List<string>(soundCategories.Keys);
        }

        /// <summary>
        /// Get all sounds (flat list).
        /// </summary>
        public List<SoundReference> GetAllSounds()
        {
            return new List<SoundReference>(allSounds);
        }

        /// <summary>
        /// Get sounds by origin type.
        /// </summary>
        public List<SoundReference> GetSoundsByOrigin(SoundOrigin origin)
        {
            List<SoundReference> result = new List<SoundReference>();
            foreach (var sound in allSounds)
            {
                if (sound.origin == origin)
                {
                    result.Add(sound);
                }
            }
            return result;
        }

        /// <summary>
        /// Get next available binary ID.
        /// </summary>
        public int GetNextBinaryId()
        {
            int maxId = -1;
            foreach (var sound in allSounds)
            {
                if (sound.binaryId > maxId)
                {
                    maxId = sound.binaryId;
                }
            }
            return maxId + 1;
        }
    }
}
