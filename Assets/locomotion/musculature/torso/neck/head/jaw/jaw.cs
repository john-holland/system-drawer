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

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        // DSP processing state
        private float[] audioBuffer;
        private float smoothedAmplitude = 0f;
        private float currentAmplitude = 0f;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;

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
    }
}
