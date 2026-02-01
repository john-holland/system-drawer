using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Jaw component with DSP audio processing for speech and jaw movement modulation.
    /// Similar to ModulatingSoundComponent but specifically for jaw articulation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RagdollJaw : RagdollBodyPart
    {
        [Header("Audio Settings")]
        [Tooltip("List of sounds the jaw can play")]
        public List<AudioClip> soundList = new List<AudioClip>();

        [Tooltip("Index of currently playing sound (-1 = none)")]
        private int currentSoundIndex = -1;

        [Tooltip("Audio source component (auto-required)")]
        private AudioSource audioSource;

        [Tooltip("Play first sound on start")]
        public bool playOnStart = false;

        [Tooltip("Loop current sound")]
        public bool loop = false;

        [Header("Jaw Movement")]
        [Tooltip("Current jaw opening amount (0-1, derived from DSP)")]
        [Range(0f, 1f)]
        public float jawOpenAmount = 0f;

        [Tooltip("Maximum jaw opening distance")]
        public float maxJawOpen = 0.1f;

        [Tooltip("Smoothing factor for jaw movement")]
        [Range(0f, 1f)]
        public float jawSmoothing = 0.1f;

        [Header("DSP Settings")]
        [Tooltip("Sensitivity of jaw movement to audio amplitude")]
        [Range(0f, 10f)]
        public float amplitudeSensitivity = 2f;

        [Tooltip("Minimum amplitude threshold to trigger jaw movement")]
        [Range(0f, 1f)]
        public float amplitudeThreshold = 0.01f;

        [Header("Realtime Audio Generation")]
        [Tooltip("Enable realtime generation of additional actor sound effects")]
        public bool curateRealtimeAdditionalActorSoundFX = false;

        [Tooltip("Reference to realtime audio generator")]
        public Locomotion.Audio.RealtimeAudioGenerator audioGenerator;

        [Tooltip("Reference to environment data collector")]
        public Locomotion.Audio.EnvironmentDataCollector environmentCollector;

        [Tooltip("Reference to behavior tree for embedding")]
        public BehaviorTree behaviorTree;

        [Header("Head Bobble (for non-animated jaws)")]
        [Tooltip("Enable head bobble when speaking (for ragdolls without animated jaws)")]
        public bool enableHeadBobble = false;

        [Tooltip("Head bobble intensity (rotation in degrees)")]
        [Range(0f, 10f)]
        public float bobbleIntensity = 2f;

        [Tooltip("Head bobble frequency (oscillations per second)")]
        [Range(0.5f, 10f)]
        public float bobbleFrequency = 3f;

        [Tooltip("Head bobble smoothing")]
        [Range(0f, 1f)]
        public float bobbleSmoothing = 0.1f;

        [Tooltip("Reference to head component (auto-found if null)")]
        public RagdollHead headComponent;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        // DSP processing state
        private float[] audioBuffer;
        private float smoothedAmplitude = 0f;
        private float currentAmplitude = 0f;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;

        // Head bobble state
        private Vector3 headOriginalPosition;
        private Quaternion headOriginalRotation;
        private float bobbleTime = 0f;
        private bool headBobbleInitialized = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Store original transform
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;

            // Auto-find head component if not assigned
            if (headComponent == null)
            {
                headComponent = GetComponentInParent<RagdollHead>();
                if (headComponent == null)
                {
                    var ragdollSystem = GetComponentInParent<RagdollSystem>();
                    if (ragdollSystem != null)
                    {
                        headComponent = ragdollSystem.headComponent;
                    }
                }
            }

            // Auto-find audio generator if not assigned
            if (audioGenerator == null && curateRealtimeAdditionalActorSoundFX)
            {
                audioGenerator = GetComponentInParent<Locomotion.Audio.RealtimeAudioGenerator>();
                if (audioGenerator == null)
                {
                    audioGenerator = FindAnyObjectByType<Locomotion.Audio.RealtimeAudioGenerator>();
                }
            }

            // Auto-find environment collector if not assigned
            if (environmentCollector == null && curateRealtimeAdditionalActorSoundFX)
            {
                environmentCollector = GetComponentInParent<Locomotion.Audio.EnvironmentDataCollector>();
                if (environmentCollector == null)
                {
                    environmentCollector = FindObjectOfType<Locomotion.Audio.EnvironmentDataCollector>();
                }
            }

            // Auto-find behavior tree if not assigned
            if (behaviorTree == null && curateRealtimeAdditionalActorSoundFX)
            {
                behaviorTree = GetComponentInParent<BehaviorTree>();
                if (behaviorTree == null)
                {
                    behaviorTree = FindAnyObjectByType<BehaviorTree>();
                }
            }
        }

        private void Start()
        {
            if (playOnStart && soundList != null && soundList.Count > 0)
            {
                PlaySound(0);
            }
        }

        private void Update()
        {
            // Update jaw transform based on opening amount
            if (jawOpenAmount > 0f)
            {
                // Rotate jaw down based on opening amount
                float rotationAngle = jawOpenAmount * 45f; // Max 45 degrees rotation
                Quaternion targetRotation = originalLocalRotation * Quaternion.Euler(rotationAngle, 0f, 0f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, jawSmoothing);

                // Optionally move jaw down slightly
                Vector3 targetPosition = originalLocalPosition + Vector3.down * (jawOpenAmount * maxJawOpen);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, jawSmoothing);
            }
            else
            {
                // Return to original position
                transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, jawSmoothing);
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, jawSmoothing);
            }

            // Update loop setting
            if (audioSource != null)
            {
                audioSource.loop = loop;
            }

            // Handle head bobble for non-animated jaws
            if (enableHeadBobble && headComponent != null)
            {
                UpdateHeadBobble();
            }

            // Generate realtime vocalizations if enabled
            if (curateRealtimeAdditionalActorSoundFX && audioGenerator != null)
            {
                GenerateRealtimeVocalization();
            }
        }

        /// <summary>
        /// DSP processing for jaw movement based on audio amplitude.
        /// </summary>
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels == 0 || data == null || data.Length == 0)
                return;

            int dataLength = data.Length / channels;

            // Allocate buffer if needed
            if (audioBuffer == null || audioBuffer.Length != dataLength)
            {
                audioBuffer = new float[dataLength];
            }

            // Calculate RMS (Root Mean Square) amplitude for jaw movement
            float sumSquares = 0f;
            for (int i = 0; i < dataLength; i++)
            {
                float sample = 0f;
                for (int c = 0; c < channels; c++)
                {
                    sample += data[i * channels + c];
                }
                sample /= channels; // Average across channels
                audioBuffer[i] = sample;
                sumSquares += sample * sample;
            }

            // Calculate RMS amplitude
            float rms = Mathf.Sqrt(sumSquares / dataLength);
            currentAmplitude = rms * amplitudeSensitivity;

            // Smooth amplitude to avoid jitter
            smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, currentAmplitude, 1f - jawSmoothing);

            // Map amplitude to jaw opening (0-1)
            if (smoothedAmplitude > amplitudeThreshold)
            {
                jawOpenAmount = Mathf.Clamp01(smoothedAmplitude);
            }
            else
            {
                // Decay jaw opening when below threshold
                jawOpenAmount = Mathf.Lerp(jawOpenAmount, 0f, jawSmoothing);
            }

            if (enableDebugLogging && Time.frameCount % 100 == 0)
            {
                Debug.Log($"[RagdollJaw] DSP - RMS: {rms:F4}, Amplitude: {currentAmplitude:F4}, Smoothed: {smoothedAmplitude:F4}, JawOpen: {jawOpenAmount:F4}");
            }
        }

        /// <summary>
        /// Play sound from sound list by index.
        /// </summary>
        public void PlaySound(int index)
        {
            if (soundList == null || index < 0 || index >= soundList.Count)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[RagdollJaw] Invalid sound index: {index}");
                }
                return;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null && soundList[index] != null)
            {
                audioSource.clip = soundList[index];
                audioSource.Play();
                currentSoundIndex = index;

                if (enableDebugLogging)
                {
                    Debug.Log($"[RagdollJaw] Playing sound {index}: {soundList[index].name}");
                }
            }
        }

        /// <summary>
        /// Play specific sound clip.
        /// </summary>
        public void PlaySound(AudioClip clip)
        {
            if (clip == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning("[RagdollJaw] Cannot play null AudioClip");
                }
                return;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                currentSoundIndex = -1; // Not from list

                if (enableDebugLogging)
                {
                    Debug.Log($"[RagdollJaw] Playing sound: {clip.name}");
                }
            }
        }

        /// <summary>
        /// Stop current sound.
        /// </summary>
        public void Stop()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                currentSoundIndex = -1;
            }
        }

        /// <summary>
        /// Get current jaw opening amount (0-1).
        /// </summary>
        public float GetJawOpenAmount()
        {
            return jawOpenAmount;
        }

        /// <summary>
        /// Manually set jaw opening amount.
        /// </summary>
        public void SetJawOpenAmount(float amount)
        {
            jawOpenAmount = Mathf.Clamp01(amount);
        }

        /// <summary>
        /// Check if jaw is currently playing a sound.
        /// </summary>
        public bool IsPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        /// <summary>
        /// Update head bobble animation when speaking.
        /// </summary>
        private void UpdateHeadBobble()
        {
            if (headComponent == null || headComponent.transform == null)
                return;

            // Initialize head original transform on first use
            if (!headBobbleInitialized)
            {
                headOriginalPosition = headComponent.transform.localPosition;
                headOriginalRotation = headComponent.transform.localRotation;
                headBobbleInitialized = true;
            }

            // Only bobble when playing sound and jaw isn't animated (jawOpenAmount is low)
            bool shouldBobble = IsPlaying() && jawOpenAmount < 0.1f;

            if (shouldBobble)
            {
                // Update bobble time
                bobbleTime += Time.deltaTime * bobbleFrequency;

                // Calculate bobble amount based on audio amplitude (if available) or simple oscillation
                float bobbleAmount = 0f;
                if (smoothedAmplitude > amplitudeThreshold)
                {
                    // Use audio amplitude to drive bobble
                    bobbleAmount = smoothedAmplitude * bobbleIntensity;
                }
                else
                {
                    // Use simple sine wave oscillation
                    bobbleAmount = (Mathf.Sin(bobbleTime * Mathf.PI * 2f) * 0.5f + 0.5f) * bobbleIntensity;
                }

                // Apply subtle rotation bobble (nodding motion)
                float rotationX = Mathf.Sin(bobbleTime * Mathf.PI * 2f) * bobbleAmount;
                Quaternion targetRotation = headOriginalRotation * Quaternion.Euler(rotationX, 0f, 0f);
                headComponent.transform.localRotation = Quaternion.Lerp(
                    headComponent.transform.localRotation,
                    targetRotation,
                    bobbleSmoothing
                );

                // Optional: subtle position bobble (up/down)
                Vector3 targetPosition = headOriginalPosition + Vector3.up * (Mathf.Sin(bobbleTime * Mathf.PI * 2f) * bobbleAmount * 0.01f);
                headComponent.transform.localPosition = Vector3.Lerp(
                    headComponent.transform.localPosition,
                    targetPosition,
                    bobbleSmoothing
                );
            }
            else
            {
                // Return head to original position when not speaking
                headComponent.transform.localRotation = Quaternion.Lerp(
                    headComponent.transform.localRotation,
                    headOriginalRotation,
                    bobbleSmoothing
                );
                headComponent.transform.localPosition = Vector3.Lerp(
                    headComponent.transform.localPosition,
                    headOriginalPosition,
                    bobbleSmoothing
                );

                // Reset bobble time when not speaking
                if (!IsPlaying())
                {
                    bobbleTime = 0f;
                }
            }
        }

        /// <summary>
        /// Generate realtime vocalization based on environment and behavior tree.
        /// </summary>
        private void GenerateRealtimeVocalization()
        {
            if (audioGenerator == null || environmentCollector == null || behaviorTree == null)
                return;

            // Only generate if not already playing a sound
            if (IsPlaying())
                return;

            // Collect environment data
            Vector3 position = transform.position;
            Locomotion.Audio.EnvironmentData envData = environmentCollector.CollectEnvironmentData(position);

            // Generate behavior tree embedding
            float[] behaviorTreeEmbedding = Locomotion.Audio.BehaviorTreeEmbedder.EmbedBehaviorTree(behaviorTree);

            // Get sound tags (jaw origin)
            float[] soundTags = new float[10];
            soundTags[0] = 1f; // jaw_origin
            soundTags[2] = 1f; // speech
            soundTags[3] = 1f; // mouth_sound

            // Run LSTM inference to get DSP parameters
            var lstmModel = audioGenerator.lstmModel;
            if (lstmModel != null && lstmModel.IsModelLoaded())
            {
                Locomotion.Audio.DSPParams dspParams = lstmModel.RunInference(envData, behaviorTreeEmbedding, soundTags);

                // Generate audio
                AudioClip generatedClip = audioGenerator.GenerateFromDSP(dspParams, Locomotion.Audio.SoundOrigin.Jaw);

                if (generatedClip != null)
                {
                    // Play generated audio
                    PlaySound(generatedClip);

                    if (enableDebugLogging)
                    {
                        Debug.Log($"[RagdollJaw] Generated and playing realtime vocalization");
                    }
                }
            }
        }

        /// <summary>
        /// Request audio generation from the audio generator.
        /// </summary>
        public void RequestAudioGeneration(Locomotion.Audio.SoundOrigin origin, Locomotion.Audio.DSPParams dspParams)
        {
            if (audioGenerator == null)
                return;

            AudioClip generatedClip = audioGenerator.GenerateFromDSP(dspParams, origin);
            if (generatedClip != null)
            {
                PlaySound(generatedClip);
            }
        }
    }
}
