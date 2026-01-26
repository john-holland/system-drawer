using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Serializable manifest structure for cached sound samples.
    /// Maps binary IDs to cached sample data with metadata.
    /// </summary>
    [Serializable]
    public class SoundCacheManifest
    {
        [Tooltip("Version of the cache manifest")]
        public string version = "1.0";

        [Tooltip("Timestamp when cache was generated")]
        public string generatedTimestamp;

        [Tooltip("List of cached sound entries")]
        public List<CachedSoundEntry> entries = new List<CachedSoundEntry>();

        /// <summary>
        /// Get cached entry by binary ID.
        /// </summary>
        public CachedSoundEntry GetEntry(int binaryId)
        {
            foreach (var entry in entries)
            {
                if (entry.binaryId == binaryId)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Add or update a cached entry.
        /// </summary>
        public void AddOrUpdateEntry(CachedSoundEntry entry)
        {
            // Remove existing entry with same binary ID
            entries.RemoveAll(e => e.binaryId == entry.binaryId);
            entries.Add(entry);
        }
    }

    /// <summary>
    /// Entry for a single cached sound sample.
    /// </summary>
    [Serializable]
    public class CachedSoundEntry
    {
        [Tooltip("Binary ID of the sound")]
        public int binaryId;

        [Tooltip("Original file path")]
        public string originalFilePath;

        [Tooltip("Path to cached sample file")]
        public string cacheFilePath;

        [Tooltip("Duration of the cached sample in seconds")]
        public float duration;

        [Tooltip("Sample rate in Hz")]
        public int sampleRate;

        [Tooltip("Number of channels (1 = mono, 2 = stereo)")]
        public int channels;

        [Tooltip("Number of samples in the cached buffer")]
        public int sampleCount;

        [Tooltip("Prebaked audio data (compressed or raw)")]
        public byte[] prebakedData;

        [Tooltip("Category of the sound")]
        public string category;

        [Tooltip("Tags for indexing")]
        public List<string> tags = new List<string>();

        [Tooltip("Origin of the sound")]
        public SoundOrigin origin;

        /// <summary>
        /// Create a cached entry from an AudioClip.
        /// </summary>
        public static CachedSoundEntry FromAudioClip(AudioClip clip, int binaryId, string originalPath, string cachePath, SoundOrigin origin = SoundOrigin.BehaviorTree)
        {
            if (clip == null)
                return null;

            CachedSoundEntry entry = new CachedSoundEntry
            {
                binaryId = binaryId,
                originalFilePath = originalPath,
                cacheFilePath = cachePath,
                duration = clip.length,
                sampleRate = clip.frequency,
                channels = clip.channels,
                origin = origin
            };

            // Extract audio data
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            entry.sampleCount = samples.Length;

            // Convert float samples to bytes (16-bit PCM)
            entry.prebakedData = ConvertSamplesToBytes(samples);

            return entry;
        }

        /// <summary>
        /// Convert float samples to 16-bit PCM bytes.
        /// </summary>
        private static byte[] ConvertSamplesToBytes(float[] samples)
        {
            byte[] bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                // Clamp to [-1, 1] and convert to 16-bit integer
                short sample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                bytes[i * 2] = (byte)(sample & 0xFF);
                bytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            return bytes;
        }

        /// <summary>
        /// Convert bytes back to float samples.
        /// </summary>
        public float[] GetSamples()
        {
            if (prebakedData == null || prebakedData.Length == 0)
                return null;

            float[] samples = new float[prebakedData.Length / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)((prebakedData[i * 2] & 0xFF) | ((prebakedData[i * 2 + 1] & 0xFF) << 8));
                samples[i] = sample / 32767f;
            }
            return samples;
        }
    }
}
