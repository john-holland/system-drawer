using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Hybrid synthesis + modulation system for realtime audio generation.
    /// Origin-aware generation distinguishes jaw-based sounds from environmental sounds.
    /// </summary>
    public class RealtimeAudioGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to LSTM model for DSP parameter generation")]
        public AudioLSTMModel lstmModel;

        [Tooltip("Reference to environment data collector")]
        public EnvironmentDataCollector environmentCollector;

        [Header("Generation Settings")]
        [Tooltip("Sample rate for generated audio")]
        public int sampleRate = 44100;

        [Tooltip("Duration of generated audio in seconds")]
        public float generationDuration = 1f;

        [Tooltip("Use synthesis for jaw sounds")]
        public bool useSynthesisForJaw = true;

        [Tooltip("Use modulation for environmental sounds")]
        public bool useModulationForEnvironmental = true;

        [Header("Synthesis Settings")]
        [Tooltip("Base waveform type for synthesis")]
        public WaveformType waveformType = WaveformType.Sine;

        [Tooltip("Number of harmonics for complex waveforms")]
        [Range(1, 10)]
        public int harmonics = 5;

        [Header("Modulation Settings")]
        [Tooltip("Base sound library for modulation")]
        public List<AudioClip> baseSoundLibrary = new List<AudioClip>();

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        private void Awake()
        {
            // Auto-find references if not assigned
            if (lstmModel == null)
            {
                lstmModel = GetComponent<AudioLSTMModel>();
                if (lstmModel == null)
                {
                    lstmModel = FindAnyObjectByType<AudioLSTMModel>();
                }
            }

            if (environmentCollector == null)
            {
                environmentCollector = GetComponent<EnvironmentDataCollector>();
                if (environmentCollector == null)
                {
                    environmentCollector = FindAnyObjectByType<EnvironmentDataCollector>();
                }
            }
        }

        /// <summary>
        /// Generate audio from DSP parameters based on origin.
        /// </summary>
        public AudioClip GenerateFromDSP(DSPParams dspParams, SoundOrigin origin)
        {
            if (dspParams == null)
            {
                Debug.LogWarning("[RealtimeAudioGenerator] DSP parameters are null");
                return null;
            }

            AudioClip clip = null;

            if (origin == SoundOrigin.Jaw)
            {
                if (useSynthesisForJaw)
                {
                    clip = SynthesizeSpeech(dspParams);
                }
                else
                {
                    // Fallback to modulation
                    clip = ModulateSound(null, dspParams);
                }
            }
            else
            {
                if (useModulationForEnvironmental)
                {
                    // Find appropriate base sound
                    AudioClip baseSound = FindBaseSound(dspParams);
                    clip = ModulateSound(baseSound, dspParams);
                }
                else
                {
                    // Fallback to synthesis
                    clip = SynthesizeSpeech(dspParams);
                }
            }

            return clip;
        }

        /// <summary>
        /// Synthesize jaw-based speech sounds from DSP parameters.
        /// </summary>
        public AudioClip SynthesizeSpeech(DSPParams dspParams)
        {
            int sampleCount = Mathf.RoundToInt(sampleRate * generationDuration);
            float[] samples = new float[sampleCount];

            float frequency = dspParams.baseFrequency;
            float phase = 0f;
            float phaseIncrement = frequency / sampleRate * 2f * Mathf.PI;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float sample = 0f;

                // Generate waveform
                switch (waveformType)
                {
                    case WaveformType.Sine:
                        sample = Mathf.Sin(phase);
                        break;
                    case WaveformType.Square:
                        sample = Mathf.Sign(Mathf.Sin(phase));
                        break;
                    case WaveformType.Sawtooth:
                        sample = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f));
                        break;
                    case WaveformType.Triangle:
                        sample = 2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f))) - 1f;
                        break;
                }

                // Add harmonics for complex waveforms
                if (harmonics > 1)
                {
                    for (int h = 2; h <= harmonics; h++)
                    {
                        float harmonicAmplitude = 1f / h;
                        sample += harmonicAmplitude * Mathf.Sin(phase * h);
                    }
                }

                // Apply amplitude envelope (ADSR)
                float envelope = CalculateEnvelope(time, dspParams.amplitudeEnvelope);
                sample *= envelope;

                // Apply modulation
                float modulation = Mathf.Sin(time * dspParams.modulationRate * 2f * Mathf.PI) * dspParams.modulationDepth;
                sample *= (1f + modulation);

                // Apply filter (simplified low-pass)
                sample = ApplyFilter(sample, dspParams.filterCutoff, dspParams.filterResonance, time);

                samples[i] = Mathf.Clamp(sample, -1f, 1f);

                phase += phaseIncrement;
                if (phase > 2f * Mathf.PI)
                {
                    phase -= 2f * Mathf.PI;
                }
            }

            // Create AudioClip
            AudioClip clip = AudioClip.Create("GeneratedSpeech", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);

            if (enableDebugLogging)
            {
                Debug.Log($"[RealtimeAudioGenerator] Synthesized speech: {sampleCount} samples, {generationDuration}s");
            }

            return clip;
        }

        /// <summary>
        /// Generate environmental sound effects using modulation.
        /// </summary>
        public AudioClip GenerateEnvironmentalSFX(DSPParams dspParams, SoundReference baseSound)
        {
            AudioClip baseClip = null;

            if (baseSound != null && baseSound.audioClip != null)
            {
                baseClip = baseSound.audioClip;
            }
            else if (baseSoundLibrary != null && baseSoundLibrary.Count > 0)
            {
                // Select random base sound
                baseClip = baseSoundLibrary[UnityEngine.Random.Range(0, baseSoundLibrary.Count)];
            }

            return ModulateSound(baseClip, dspParams);
        }

        /// <summary>
        /// Apply DSP modulation to a base sound.
        /// </summary>
        public AudioClip ModulateSound(AudioClip baseClip, DSPParams dspParams)
        {
            if (baseClip == null)
            {
                // Generate noise if no base clip
                return GenerateNoise(dspParams);
            }

            // Extract samples from base clip
            float[] baseSamples = new float[baseClip.samples * baseClip.channels];
            baseClip.GetData(baseSamples, 0);

            int sampleCount = baseSamples.Length;
            float[] modulatedSamples = new float[sampleCount];
            float sampleRate = baseClip.frequency;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / sampleRate;
                float sample = baseSamples[i];

                // Apply amplitude envelope
                float envelope = CalculateEnvelope(time, dspParams.amplitudeEnvelope);
                sample *= envelope;

                // Apply modulation
                float modulation = Mathf.Sin(time * dspParams.modulationRate * 2f * Mathf.PI) * dspParams.modulationDepth;
                sample *= (1f + modulation);

                // Apply filter
                sample = ApplyFilter(sample, dspParams.filterCutoff, dspParams.filterResonance, time);

                // Apply reverb (simplified)
                if (dspParams.reverbAmount > 0f)
                {
                    sample = ApplyReverb(sample, dspParams.reverbAmount, i, modulatedSamples);
                }

                modulatedSamples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            // Create AudioClip
            AudioClip clip = AudioClip.Create("ModulatedSound", baseClip.samples, baseClip.channels, (int)sampleRate, false);
            clip.SetData(modulatedSamples, 0);

            if (enableDebugLogging)
            {
                Debug.Log($"[RealtimeAudioGenerator] Modulated sound: {sampleCount} samples");
            }

            return clip;
        }

        /// <summary>
        /// Find appropriate base sound from library based on DSP parameters.
        /// </summary>
        private AudioClip FindBaseSound(DSPParams dspParams)
        {
            if (baseSoundLibrary == null || baseSoundLibrary.Count == 0)
                return null;

            // Simple selection based on frequency range
            // Could be enhanced with more sophisticated matching
            return baseSoundLibrary[UnityEngine.Random.Range(0, baseSoundLibrary.Count)];
        }

        /// <summary>
        /// Generate noise for environmental sounds.
        /// </summary>
        private AudioClip GenerateNoise(DSPParams dspParams)
        {
            int sampleCount = Mathf.RoundToInt(sampleRate * generationDuration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float sample = UnityEngine.Random.Range(-1f, 1f);

                // Apply filter to shape noise
                sample = ApplyFilter(sample, dspParams.filterCutoff, dspParams.filterResonance, time);

                // Apply envelope
                float envelope = CalculateEnvelope(time, dspParams.amplitudeEnvelope);
                sample *= envelope;

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("GeneratedNoise", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);

            return clip;
        }

        /// <summary>
        /// Calculate amplitude envelope (ADSR).
        /// </summary>
        private float CalculateEnvelope(float time, Vector4 adsr)
        {
            float attack = adsr.x;
            float decay = adsr.y;
            float sustain = adsr.z;
            float release = adsr.w;
            float totalDuration = attack + decay + release;

            if (time < attack)
            {
                return time / attack;
            }
            else if (time < attack + decay)
            {
                float t = (time - attack) / decay;
                return 1f - (1f - sustain) * t;
            }
            else if (time < totalDuration - release)
            {
                return sustain;
            }
            else
            {
                float t = (time - (totalDuration - release)) / release;
                return sustain * (1f - t);
            }
        }

        /// <summary>
        /// Apply simplified low-pass filter.
        /// </summary>
        private float ApplyFilter(float sample, float cutoff, float resonance, float time)
        {
            // Simplified filter implementation
            // In a real implementation, this would use proper filter algorithms
            float normalizedCutoff = cutoff / (sampleRate / 2f);
            float filtered = sample * normalizedCutoff;
            return filtered;
        }

        /// <summary>
        /// Apply simplified reverb.
        /// </summary>
        private float ApplyReverb(float sample, float amount, int index, float[] previousSamples)
        {
            // Simplified reverb using delay line
            int delaySamples = Mathf.RoundToInt(sampleRate * 0.1f); // 100ms delay
            if (index >= delaySamples)
            {
                float delayed = previousSamples[index - delaySamples];
                return sample + delayed * amount;
            }
            return sample;
        }
    }

    /// <summary>
    /// Waveform types for synthesis.
    /// </summary>
    public enum WaveformType
    {
        Sine,
        Square,
        Sawtooth,
        Triangle
    }
}
