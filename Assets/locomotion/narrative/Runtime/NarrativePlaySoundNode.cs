using System;
using UnityEngine;
using Locomotion.Audio;
using Locomotion.Narrative;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Narrative action spec for playing sounds from the actor store.
    /// Uses narrative timeline to predict timing for sound effects.
    /// </summary>
    [Serializable]
    public class PlaySoundActionSpec : NarrativeActionSpec
    {
        [Tooltip("Reference to sound in actor store (by binary ID)")]
        public int soundBinaryId = -1;

        [Tooltip("Direct sound reference (optional, overrides binary ID)")]
        [System.NonSerialized]
        public SoundReference soundReference;

        [Tooltip("Use narrative timeline for timing prediction")]
        public bool useTimelinePrediction = true;

        [Tooltip("Predicted time from timeline (calculated)")]
        public float predictedTime = 0f;

        [Tooltip("Reference to actor sound store")]
        [System.NonSerialized]
        public ActorSoundStore soundStore;

        [Tooltip("Reference to narrative calendar for timing")]
        [System.NonSerialized]
        public NarrativeCalendarAsset narrativeCalendar;

        [Tooltip("Volume multiplier (0-1)")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Wait for sound to finish before succeeding")]
        public bool waitForCompletion = true;

        [Tooltip("Estimated duration in seconds")]
        public float estimatedDuration = 0f;

        // Execution state
        [System.NonSerialized]
        private bool soundStarted = false;

        [System.NonSerialized]
        private float soundStartTime = 0f;

        [System.NonSerialized]
        private AudioClip currentClip = null;

        [System.NonSerialized]
        private AudioSource audioSource = null;

        /// <summary>
        /// Predict timing from narrative calendar.
        /// </summary>
        public void PredictTiming(NarrativeCalendarAsset calendar)
        {
            if (calendar == null || !useTimelinePrediction)
            {
                predictedTime = 0f;
                return;
            }

            // Use behavior tree timing predictor if available
            var predictor = UnityEngine.Object.FindAnyObjectByType<BehaviorTreeTimingPredictor>();
            if (predictor != null)
            {
                // Try to get behavior tree from context or find it using reflection
                // Note: NarrativeActionSpec is not a MonoBehaviour, so we can't use GetComponentInParent
                var behaviorTreeType = System.Type.GetType("BehaviorTree, Locomotion.Runtime");
                if (behaviorTreeType == null)
                {
                    behaviorTreeType = System.Type.GetType("BehaviorTree, Assembly-CSharp");
                }
                if (behaviorTreeType != null)
                {
                    var behaviorTree = UnityEngine.Object.FindAnyObjectByType(behaviorTreeType);
                    if (behaviorTree != null)
                    {
                        predictedTime = predictor.PredictSoundTiming(behaviorTree, calendar);
                    }
                }
            }
            else
            {
                // Simple prediction based on estimated duration
                predictedTime = estimatedDuration;
            }
        }

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            // Initialize if needed
            if (!soundStarted)
            {
                Initialize();
                PlaySound();
            }

            // Check if still playing
            if (waitForCompletion && IsPlaying())
            {
                // Check for timeout
                if (Time.time - soundStartTime > GetEstimatedDuration() + 1f)
                {
                    return BehaviorTreeStatus.Success; // Assume completed
                }
                return BehaviorTreeStatus.Running;
            }

            return BehaviorTreeStatus.Success;
        }

        /// <summary>
        /// Play the sound action.
        /// </summary>
        private void PlaySound()
        {
            // Get sound reference
            SoundReference sound = null;
            if (soundReference != null)
            {
                sound = soundReference;
            }
            else if (soundBinaryId >= 0 && soundStore != null)
            {
                sound = soundStore.GetSoundByBinaryId(soundBinaryId);
            }

            if (sound == null)
            {
                Debug.LogWarning("[PlaySoundActionSpec] No valid sound reference");
                return;
            }

            // Load audio clip if needed
            if (sound.audioClip == null && !string.IsNullOrEmpty(sound.filePath))
            {
#if UNITY_EDITOR
                sound.audioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sound.filePath);
#endif
            }

            if (sound.audioClip == null)
            {
                Debug.LogWarning($"[PlaySoundActionSpec] Could not load audio clip: {sound.filePath}");
                return;
            }

            // Get or create audio source (need to find a GameObject to attach to)
            // Since this is a NarrativeActionSpec, we need to find a GameObject
            GameObject targetObject = null;

            if (targetObject == null)
            {
                // Fallback: find or create a temporary audio source
                var existingSource = UnityEngine.Object.FindAnyObjectByType<AudioSource>();
                if (existingSource != null)
                {
                    audioSource = existingSource;
                }
                else
                {
                    GameObject tempGO = new GameObject("TempAudioSource");
                    audioSource = tempGO.AddComponent<AudioSource>();
                }
            }
            else
            {
                audioSource = targetObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = targetObject.AddComponent<AudioSource>();
                }
            }

            // Play sound
            audioSource.clip = sound.audioClip;
            audioSource.volume = volume;
            audioSource.Play();

            soundStarted = true;
            soundStartTime = Time.time;
            currentClip = sound.audioClip;
        }

        /// <summary>
        /// Check if sound is still playing.
        /// </summary>
        public bool IsPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        /// <summary>
        /// Stop current sound.
        /// </summary>
        public void Stop()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            soundStarted = false;
            currentClip = null;
        }

        /// <summary>
        /// Get estimated duration from sound reference.
        /// </summary>
        public float GetEstimatedDuration()
        {
            if (soundReference != null && soundReference.duration > 0f)
            {
                return soundReference.duration;
            }

            if (currentClip != null)
            {
                return currentClip.length;
            }

            return estimatedDuration;
        }

        /// <summary>
        /// Initialize the action spec (called when action is created).
        /// </summary>
        public void Initialize()
        {
            // Auto-find sound store if not assigned
            if (soundStore == null)
            {
                soundStore = UnityEngine.Object.FindAnyObjectByType<ActorSoundStore>();
            }

            // Auto-find narrative calendar if not assigned
            if (narrativeCalendar == null)
            {
                narrativeCalendar = UnityEngine.Object.FindAnyObjectByType<NarrativeCalendarAsset>();
            }

            // Predict timing on initialize
            if (narrativeCalendar != null)
            {
                PredictTiming(narrativeCalendar);
            }
        }
    }
}
