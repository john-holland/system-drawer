using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Serializable class representing a sound reference in the actor store.
    /// Contains metadata about audio files including file path, binary ID, category, and tags.
    /// </summary>
    [Serializable]
    public class SoundReference
    {
        [Tooltip("File path relative to Assets folder")]
        public string filePath;

        [Tooltip("Binary index ID for efficient lookup")]
        public int binaryId;

        [Tooltip("Category this sound belongs to (e.g., footsteps, vocalizations)")]
        public string category;

        [Tooltip("Tags for intelligent indexing (e.g., environmental, speech, mouth_sound)")]
        public List<string> tags = new List<string>();

        [Tooltip("Duration of the sound in seconds")]
        public float duration;

        [Tooltip("Sample rate in Hz")]
        public int sampleRate;

        [Tooltip("Number of audio channels (1 = mono, 2 = stereo)")]
        public int channels;

        [Tooltip("Optional: Reference to AudioClip if loaded")]
        [System.NonSerialized]
        public AudioClip audioClip;

        [Tooltip("Origin of the sound (jaw vs behavior tree)")]
        public SoundOrigin origin = SoundOrigin.BehaviorTree;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public SoundReference()
        {
        }

        /// <summary>
        /// Create a sound reference from an AudioClip.
        /// </summary>
        public SoundReference(AudioClip clip, int binaryId, string category, SoundOrigin origin = SoundOrigin.BehaviorTree)
        {
            this.audioClip = clip;
            this.binaryId = binaryId;
            this.category = category;
            this.origin = origin;

            if (clip != null)
            {
#if UNITY_EDITOR
                this.filePath = UnityEditor.AssetDatabase.GetAssetPath(clip);
#endif
                this.duration = clip.length;
                this.sampleRate = clip.frequency;
                this.channels = clip.channels;
            }
        }

        /// <summary>
        /// Check if this sound has a specific tag.
        /// </summary>
        public bool HasTag(string tag)
        {
            return tags != null && tags.Contains(tag);
        }

        /// <summary>
        /// Add a tag to this sound reference.
        /// </summary>
        public void AddTag(string tag)
        {
            if (tags == null)
                tags = new List<string>();

            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        /// <summary>
        /// Remove a tag from this sound reference.
        /// </summary>
        public void RemoveTag(string tag)
        {
            if (tags != null)
                tags.Remove(tag);
        }
    }

    /// <summary>
    /// Origin of a sound effect, used to determine generation strategy.
    /// </summary>
    public enum SoundOrigin
    {
        /// <summary>
        /// Sound originates from jaw component (speech, vocalizations, mouth sounds)
        /// </summary>
        Jaw,

        /// <summary>
        /// Sound originates from behavior tree node (environmental, movement, impact sounds)
        /// </summary>
        BehaviorTree
    }
}
