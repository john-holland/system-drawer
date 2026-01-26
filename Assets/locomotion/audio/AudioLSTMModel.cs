using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

namespace Locomotion.Audio
{
    /// <summary>
    /// Unity wrapper for Barracuda inference of trained LSTM model.
    /// Loads trained model and runs inference on environment + behavior tree data.
    /// </summary>
    public class AudioLSTMModel : MonoBehaviour
    {
        [Header("Model Settings")]
        [Tooltip("Path to ONNX model file (relative to Assets folder)")]
        public string modelPath = "Models/audio_lstm.onnx";

        [Tooltip("Model asset (if using NNModel)")]
        public NNModel modelAsset;

        [Header("Inference Settings")]
        [Tooltip("Input feature dimension")]
        public int inputDimension = 256;

        [Tooltip("Output DSP parameter dimension")]
        public int outputDimension = 64;

        [Tooltip("Use GPU for inference")]
        public bool useGPU = true;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

#if UNITY_BARRACUDA
        private Model runtimeModel;
        private IWorker worker;
        private bool modelLoaded = false;
#endif

        private void Awake()
        {
            LoadModel();
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA
            if (worker != null)
            {
                worker.Dispose();
            }
#endif
        }

        /// <summary>
        /// Load the trained model.
        /// </summary>
        public void LoadModel()
        {
#if UNITY_BARRACUDA
            try
            {
                if (modelAsset != null)
                {
                    runtimeModel = ModelLoader.Load(modelAsset);
                }
                else if (!string.IsNullOrEmpty(modelPath))
                {
                    // Try to load from path
                    string fullPath = System.IO.Path.Combine(Application.dataPath, "..", modelPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        runtimeModel = ModelLoader.LoadFromFile(fullPath);
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioLSTMModel] Model file not found: {fullPath}");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("[AudioLSTMModel] No model asset or path specified");
                    return;
                }

                // Create worker
                worker = WorkerFactory.CreateWorker(
                    useGPU ? WorkerFactory.Type.ComputePrecompiled : WorkerFactory.Type.CPU,
                    runtimeModel
                );

                modelLoaded = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"[AudioLSTMModel] Model loaded successfully. Input: {inputDimension}, Output: {outputDimension}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioLSTMModel] Error loading model: {e.Message}");
                modelLoaded = false;
            }
#else
            Debug.LogWarning("[AudioLSTMModel] Barracuda not available. Install Unity Barracuda package.");
            modelLoaded = false;
#endif
        }

        /// <summary>
        /// Run inference on environment + behavior tree data.
        /// </summary>
        public DSPParams RunInference(EnvironmentData envData, float[] behaviorTreeEmbedding, float[] soundTags)
        {
            if (!modelLoaded)
            {
                Debug.LogWarning("[AudioLSTMModel] Model not loaded");
                return new DSPParams();
            }

#if UNITY_BARRACUDA
            try
            {
                // Combine input features
                float[] envFeatures = envData.ToFeatureVector();
                List<float> inputFeatures = new List<float>();
                inputFeatures.AddRange(envFeatures);
                inputFeatures.AddRange(behaviorTreeEmbedding);
                inputFeatures.AddRange(soundTags);

                // Ensure correct input dimension
                if (inputFeatures.Count != inputDimension)
                {
                    // Pad or truncate
                    while (inputFeatures.Count < inputDimension)
                    {
                        inputFeatures.Add(0f);
                    }
                    if (inputFeatures.Count > inputDimension)
                    {
                        inputFeatures = inputFeatures.GetRange(0, inputDimension);
                    }
                }

                // Create input tensor
                Tensor inputTensor = new Tensor(1, 1, inputDimension, inputFeatures.ToArray());

                // Run inference
                worker.Execute(inputTensor);

                // Get output
                Tensor outputTensor = worker.PeekOutput();
                float[] outputData = outputTensor.ToReadOnlyArray();

                // Convert to DSP parameters
                DSPParams dspParams = new DSPParams();
                if (outputData.Length >= outputDimension)
                {
                    dspParams.FromArray(outputData, outputDimension);
                }
                else
                {
                    Debug.LogWarning($"[AudioLSTMModel] Output dimension mismatch. Expected {outputDimension}, got {outputData.Length}");
                }

                // Cleanup
                inputTensor.Dispose();
                outputTensor.Dispose();

                return dspParams;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioLSTMModel] Error during inference: {e.Message}");
                return new DSPParams();
            }
#else
            Debug.LogWarning("[AudioLSTMModel] Barracuda not available");
            return new DSPParams();
#endif
        }

        /// <summary>
        /// Check if model is loaded and ready.
        /// </summary>
        public bool IsModelLoaded()
        {
            return modelLoaded;
        }
    }

    /// <summary>
    /// DSP generation parameters output by the LSTM model.
    /// </summary>
    [Serializable]
    public class DSPParams
    {
        [Tooltip("Frequency range (min, max) in Hz")]
        public Vector2 frequencyRange = new Vector2(20f, 20000f);

        [Tooltip("Base frequency in Hz")]
        public float baseFrequency = 440f;

        [Tooltip("Amplitude envelope (attack, decay, sustain, release)")]
        public Vector4 amplitudeEnvelope = new Vector4(0.1f, 0.2f, 0.5f, 0.2f);

        [Tooltip("Modulation rate in Hz")]
        public float modulationRate = 1f;

        [Tooltip("Modulation depth (0-1)")]
        [Range(0f, 1f)]
        public float modulationDepth = 0.1f;

        [Tooltip("Filter cutoff frequency in Hz")]
        public float filterCutoff = 10000f;

        [Tooltip("Filter resonance (0-1)")]
        [Range(0f, 1f)]
        public float filterResonance = 0.5f;

        [Tooltip("Reverb amount (0-1)")]
        [Range(0f, 1f)]
        public float reverbAmount = 0f;

        [Tooltip("Delay time in seconds")]
        public float delayTime = 0f;

        [Tooltip("Delay feedback (0-1)")]
        [Range(0f, 1f)]
        public float delayFeedback = 0f;

        /// <summary>
        /// Convert from array (for model output).
        /// </summary>
        public void FromArray(float[] data, int dimension)
        {
            if (data == null || data.Length < dimension)
                return;

            int index = 0;

            // Frequency range (2)
            if (index + 2 <= data.Length)
            {
                frequencyRange = new Vector2(data[index], data[index + 1]);
                index += 2;
            }

            // Base frequency (1)
            if (index < data.Length)
            {
                baseFrequency = data[index++];
            }

            // Amplitude envelope (4)
            if (index + 4 <= data.Length)
            {
                amplitudeEnvelope = new Vector4(data[index], data[index + 1], data[index + 2], data[index + 3]);
                index += 4;
            }

            // Modulation (2)
            if (index + 2 <= data.Length)
            {
                modulationRate = data[index++];
                modulationDepth = Mathf.Clamp01(data[index++]);
            }

            // Filter (2)
            if (index + 2 <= data.Length)
            {
                filterCutoff = data[index++];
                filterResonance = Mathf.Clamp01(data[index++]);
            }

            // Reverb (1)
            if (index < data.Length)
            {
                reverbAmount = Mathf.Clamp01(data[index++]);
            }

            // Delay (2)
            if (index + 2 <= data.Length)
            {
                delayTime = data[index++];
                delayFeedback = Mathf.Clamp01(data[index++]);
            }
        }

        /// <summary>
        /// Convert to array (for model input/training).
        /// </summary>
        public float[] ToArray()
        {
            List<float> array = new List<float>();

            array.Add(frequencyRange.x);
            array.Add(frequencyRange.y);
            array.Add(baseFrequency);
            array.Add(amplitudeEnvelope.x);
            array.Add(amplitudeEnvelope.y);
            array.Add(amplitudeEnvelope.z);
            array.Add(amplitudeEnvelope.w);
            array.Add(modulationRate);
            array.Add(modulationDepth);
            array.Add(filterCutoff);
            array.Add(filterResonance);
            array.Add(reverbAmount);
            array.Add(delayTime);
            array.Add(delayFeedback);

            return array.ToArray();
        }
    }
}
