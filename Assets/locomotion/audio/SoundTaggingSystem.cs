using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Intelligent tagging system for sounds based on origin and characteristics.
    /// Distinguishes jaw-based sounds (speech, vocalizations) from environmental/behavior tree sounds.
    /// </summary>
    public static class SoundTaggingSystem
    {
        /// <summary>
        /// Tag categories for intelligent indexing.
        /// </summary>
        public static class Tags
        {
            public const string JAW_ORIGIN = "jaw_origin";
            public const string BEHAVIOR_TREE_ORIGIN = "behavior_tree_origin";
            public const string SPEECH = "speech";
            public const string MOUTH_SOUND = "mouth_sound";
            public const string ENVIRONMENTAL = "environmental";
            public const string VOCALIZATION = "vocalization";
            public const string MOVEMENT = "movement";
            public const string IMPACT = "impact";
            public const string FOOTSTEP = "footstep";
            public const string GRUNT = "grunt";
            public const string EXCLAMATION = "exclamation";
        }

        /// <summary>
        /// Auto-tag a sound based on its origin and characteristics.
        /// </summary>
        public static void TagSound(SoundReference sound, SoundOrigin origin)
        {
            if (sound == null)
                return;

            // Set origin
            sound.origin = origin;

            // Clear existing tags
            sound.tags.Clear();

            // Add origin tag
            if (origin == SoundOrigin.Jaw)
            {
                sound.AddTag(Tags.JAW_ORIGIN);
            }
            else
            {
                sound.AddTag(Tags.BEHAVIOR_TREE_ORIGIN);
            }

            // Analyze file path and name for additional tags
            AnalyzeSoundName(sound);

            // Analyze audio characteristics if AudioClip is available
            if (sound.audioClip != null)
            {
                AnalyzeAudioCharacteristics(sound);
            }
        }

        /// <summary>
        /// Check if a sound is jaw-based.
        /// </summary>
        public static bool IsJawSound(SoundReference sound)
        {
            if (sound == null)
                return false;

            return sound.origin == SoundOrigin.Jaw || 
                   sound.HasTag(Tags.JAW_ORIGIN) ||
                   sound.HasTag(Tags.SPEECH) ||
                   sound.HasTag(Tags.MOUTH_SOUND) ||
                   sound.HasTag(Tags.VOCALIZATION);
        }

        /// <summary>
        /// Check if a sound is environmental (non-jaw).
        /// </summary>
        public static bool IsEnvironmentalSound(SoundReference sound)
        {
            if (sound == null)
                return false;

            return sound.origin == SoundOrigin.BehaviorTree ||
                   sound.HasTag(Tags.BEHAVIOR_TREE_ORIGIN) ||
                   sound.HasTag(Tags.ENVIRONMENTAL) ||
                   sound.HasTag(Tags.MOVEMENT) ||
                   sound.HasTag(Tags.IMPACT) ||
                   sound.HasTag(Tags.FOOTSTEP);
        }

        /// <summary>
        /// Get tags for LSTM indexing.
        /// Returns a normalized vector representation of tags.
        /// </summary>
        public static float[] GetTagsForLSTM(SoundReference sound)
        {
            if (sound == null || sound.tags == null)
                return new float[10]; // Return zero vector

            // Create tag vector (one-hot like encoding for common tags)
            float[] tagVector = new float[10];

            // Map common tags to vector positions
            Dictionary<string, int> tagMap = new Dictionary<string, int>
            {
                { Tags.JAW_ORIGIN, 0 },
                { Tags.BEHAVIOR_TREE_ORIGIN, 1 },
                { Tags.SPEECH, 2 },
                { Tags.MOUTH_SOUND, 3 },
                { Tags.ENVIRONMENTAL, 4 },
                { Tags.VOCALIZATION, 5 },
                { Tags.MOVEMENT, 6 },
                { Tags.IMPACT, 7 },
                { Tags.FOOTSTEP, 8 },
                { Tags.GRUNT, 9 }
            };

            foreach (string tag in sound.tags)
            {
                if (tagMap.TryGetValue(tag, out int pos) && pos < tagVector.Length)
                {
                    tagVector[pos] = 1f;
                }
            }

            return tagVector;
        }

        /// <summary>
        /// Analyze sound name and file path for tags.
        /// </summary>
        private static void AnalyzeSoundName(SoundReference sound)
        {
            if (string.IsNullOrEmpty(sound.filePath))
                return;

            string name = sound.filePath.ToLower();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(name).ToLower();

            // Jaw/speech indicators
            if (ContainsAny(fileName, new[] { "speech", "vocal", "mouth", "jaw", "voice", "talk", "say", "speak" }) ||
                ContainsAny(name, new[] { "/speech/", "/vocal/", "/mouth/", "/jaw/", "/voice/" }))
            {
                sound.AddTag(Tags.SPEECH);
                sound.AddTag(Tags.JAW_ORIGIN);
                sound.origin = SoundOrigin.Jaw;
            }

            // Mouth sounds
            if (ContainsAny(fileName, new[] { "grunt", "exclamation", "gasp", "sigh", "moan", "groan", "huff", "puff" }))
            {
                sound.AddTag(Tags.MOUTH_SOUND);
                sound.AddTag(Tags.VOCALIZATION);
                if (sound.origin != SoundOrigin.Jaw)
                {
                    sound.origin = SoundOrigin.Jaw;
                    sound.AddTag(Tags.JAW_ORIGIN);
                }
            }

            // Grunts and exclamations
            if (ContainsAny(fileName, new[] { "grunt", "ugh", "oof", "ow", "ah", "oh", "eh", "huh" }))
            {
                sound.AddTag(Tags.GRUNT);
                sound.AddTag(Tags.EXCLAMATION);
                sound.AddTag(Tags.MOUTH_SOUND);
                if (sound.origin != SoundOrigin.Jaw)
                {
                    sound.origin = SoundOrigin.Jaw;
                    sound.AddTag(Tags.JAW_ORIGIN);
                }
            }

            // Environmental sounds
            if (ContainsAny(fileName, new[] { "footstep", "foot", "step", "walk", "run" }) ||
                ContainsAny(name, new[] { "/footstep/", "/foot/", "/movement/" }))
            {
                sound.AddTag(Tags.FOOTSTEP);
                sound.AddTag(Tags.MOVEMENT);
                sound.AddTag(Tags.ENVIRONMENTAL);
                if (sound.origin != SoundOrigin.Jaw)
                {
                    sound.origin = SoundOrigin.BehaviorTree;
                    sound.AddTag(Tags.BEHAVIOR_TREE_ORIGIN);
                }
            }

            // Impact sounds
            if (ContainsAny(fileName, new[] { "impact", "hit", "collision", "crash", "bang", "thud", "smack" }) ||
                ContainsAny(name, new[] { "/impact/", "/hit/", "/collision/" }))
            {
                sound.AddTag(Tags.IMPACT);
                sound.AddTag(Tags.ENVIRONMENTAL);
                if (sound.origin != SoundOrigin.Jaw)
                {
                    sound.origin = SoundOrigin.BehaviorTree;
                    sound.AddTag(Tags.BEHAVIOR_TREE_ORIGIN);
                }
            }

            // General environmental
            if (ContainsAny(name, new[] { "/environmental/", "/sfx/", "/effects/", "/ambient/" }))
            {
                sound.AddTag(Tags.ENVIRONMENTAL);
                if (sound.origin != SoundOrigin.Jaw)
                {
                    sound.origin = SoundOrigin.BehaviorTree;
                    sound.AddTag(Tags.BEHAVIOR_TREE_ORIGIN);
                }
            }
        }

        /// <summary>
        /// Analyze audio characteristics for tagging.
        /// </summary>
        private static void AnalyzeAudioCharacteristics(SoundReference sound)
        {
            if (sound.audioClip == null)
                return;

            // Analyze frequency content (would require FFT, simplified here)
            // For now, use duration and sample rate as indicators

            // Short sounds are more likely to be exclamations/grunts
            if (sound.duration < 0.5f)
            {
                if (sound.origin == SoundOrigin.Jaw)
                {
                    sound.AddTag(Tags.EXCLAMATION);
                }
            }

            // Very short sounds are likely impacts
            if (sound.duration < 0.2f && sound.origin == SoundOrigin.BehaviorTree)
            {
                sound.AddTag(Tags.IMPACT);
            }
        }

        /// <summary>
        /// Check if string contains any of the given substrings.
        /// </summary>
        private static bool ContainsAny(string str, string[] substrings)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            foreach (string substring in substrings)
            {
                if (str.Contains(substring))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get all sounds with a specific tag from a store.
        /// </summary>
        public static List<SoundReference> GetSoundsByTag(ActorSoundStore store, string tag)
        {
            if (store == null)
                return new List<SoundReference>();

            List<SoundReference> result = new List<SoundReference>();
            foreach (var sound in store.GetAllSounds())
            {
                if (sound.HasTag(tag))
                {
                    result.Add(sound);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all jaw sounds from a store.
        /// </summary>
        public static List<SoundReference> GetJawSounds(ActorSoundStore store)
        {
            if (store == null)
                return new List<SoundReference>();

            return store.GetSoundsByOrigin(SoundOrigin.Jaw);
        }

        /// <summary>
        /// Get all environmental sounds from a store.
        /// </summary>
        public static List<SoundReference> GetEnvironmentalSounds(ActorSoundStore store)
        {
            if (store == null)
                return new List<SoundReference>();

            return store.GetSoundsByOrigin(SoundOrigin.BehaviorTree);
        }
    }
}
