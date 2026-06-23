using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

namespace Locomotion.Camera.Inference
{
    /// <summary>LSTM topology hint inference (Barracuda ONNX). Input 72 = 64 topology + 7 mode one-hot + 1 salience. Output 9 = 8 hint biases + memorability.</summary>
    public sealed class CameraTopologyLSTM : MonoBehaviour
    {
        public const int TopologyDim = FrustumAlignedOctreeBasis.TopologyDim;
        public const int ModeCount = 7;
        public const int InputDim = TopologyDim + ModeCount + 1;
        public const int OutputDim = 9;

        public string modelPath = "CameraLSTM/camera_topology_lstm.onnx";
#if UNITY_BARRACUDA
        public NNModel modelAsset;
        private Model _runtimeModel;
        private IWorker _worker;
#endif

        public float[] lastHintBias = new float[8];
        public float lastMemorabilityScore;

        void OnEnable()
        {
#if UNITY_BARRACUDA
            LoadModel();
#endif
        }

        void OnDisable()
        {
#if UNITY_BARRACUDA
            _worker?.Dispose();
            _worker = null;
#endif
        }

#if UNITY_BARRACUDA
        void LoadModel()
        {
            if (modelAsset != null)
                _runtimeModel = ModelLoader.Load(modelAsset);
            else
            {
                string full = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);
                if (System.IO.File.Exists(full))
                    _runtimeModel = ModelLoader.Load(full);
            }
            if (_runtimeModel != null)
                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _runtimeModel);
        }
#endif

        public bool TryPredict(
            UnityEngine.Camera cam,
            IReadOnlyList<HierarchicalPathingOctTree.Leaf> leaves,
            CameraFocusMode mode,
            float actorVisionSalience,
            out float[] hintBias,
            out float memorabilityScore)
        {
            hintBias = lastHintBias;
            memorabilityScore = lastMemorabilityScore;

            float[] input = BuildInput(cam, leaves, mode, actorVisionSalience);
            float[] output = RunInference(input);
            if (output == null || output.Length < OutputDim)
                return false;

            for (int i = 0; i < 8; i++)
                lastHintBias[i] = output[i];
            lastMemorabilityScore = Mathf.Clamp01(output[8]);
            hintBias = lastHintBias;
            memorabilityScore = lastMemorabilityScore;
            return true;
        }

        public static float[] BuildInput(
            UnityEngine.Camera cam,
            IReadOnlyList<HierarchicalPathingOctTree.Leaf> leaves,
            CameraFocusMode mode,
            float actorVisionSalience)
        {
            var input = new float[InputDim];
            float[] topo = FrustumAlignedOctreeBasis.BuildTopologyVector(cam, leaves);
            Array.Copy(topo, input, Math.Min(topo.Length, TopologyDim));
            int modeIdx = (int)mode;
            if (modeIdx >= 0 && modeIdx < ModeCount)
                input[TopologyDim + modeIdx] = 1f;
            input[TopologyDim + ModeCount] = Mathf.Clamp01(actorVisionSalience);
            return input;
        }

        float[] RunInference(float[] input)
        {
#if UNITY_BARRACUDA
            if (_worker == null || input == null) return StubOutput(input);
            using var tensor = new Tensor(1, InputDim, input);
            _worker.Execute(tensor);
            var peek = _worker.PeekOutput();
            var output = peek.ToReadOnlyArray();
            peek.Dispose();
            return output;
#else
            return StubOutput(input);
#endif
        }

        static float[] StubOutput(float[] input)
        {
            var output = new float[OutputDim];
            if (input == null) return output;
            for (int i = 0; i < 8; i++)
                output[i] = (i % 3) * 0.05f;
            output[8] = 0.5f;
            return output;
        }
    }
}
