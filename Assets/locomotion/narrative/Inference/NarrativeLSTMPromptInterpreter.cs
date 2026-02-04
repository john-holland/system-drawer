using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Locomotion.Narrative.Serialization;
#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

namespace Locomotion.Narrative
{
    /// <summary>One decoded event from the prompt interpreter (before applying to calendar/4D).</summary>
    [Serializable]
    public struct InterpretedEvent
    {
        public string title;
        public float startSeconds;
        public float durationSeconds;
        public Vector3 center;
        public Vector3 size;
        public float tMin;
        public float tMax;
    }

    /// <summary>
    /// LSTM prompt interpreter: natural language prompt -> decoded events (and optional 4D).
    /// Run Interpret(prompt) to get a list of InterpretedEvent; optionally call ApplyToCalendar to add them.
    /// </summary>
    public class NarrativeLSTMPromptInterpreter : MonoBehaviour
    {
        [Header("Model")]
        public string modelPath = "NarrativeLSTM/narrative_prompt_interpreter.onnx";
#if UNITY_BARRACUDA
        public NNModel modelAsset;
#endif

        [Header("Vocab")]
        public string vocabPath = "NarrativeLSTM/vocab";
        public TextAsset vocabAsset;

        [Header("Optional targets")]
        [Tooltip("If set, ApplyToCalendar will add interpreted events here.")]
        public NarrativeCalendarAsset calendar;

        [Header("Output")]
        [Tooltip("Last interpreted events (read-only).")]
        public List<InterpretedEvent> lastInterpretedEvents = new List<InterpretedEvent>();

        private const int PromptMaxLen = 128;
        private const int EventParams = 11;
        private const int MaxEvents = 3;
        private const int OutputDim = 2 + MaxEvents * EventParams;

#if UNITY_BARRACUDA
        private Model _runtimeModel;
        private IWorker _worker;
#endif
        private NarrativeLSTMTokenizer _tokenizer;
        private bool _modelLoaded;
        private int _vocabSize;

        private void Start()
        {
            LoadVocab();
            LoadModel();
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA
            _worker?.Dispose();
#endif
        }

        private void LoadVocab()
        {
            string json = null;
            if (vocabAsset != null) json = vocabAsset.text;
            if (string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(vocabPath))
            {
                var ta = Resources.Load<TextAsset>(vocabPath);
                if (ta != null) json = ta.text;
                if (string.IsNullOrEmpty(json))
                {
                    string path = Path.Combine(Application.streamingAssetsPath, vocabPath + ".json");
                    if (File.Exists(path)) json = File.ReadAllText(path);
                }
            }
            if (!string.IsNullOrEmpty(json))
            {
                _tokenizer = new NarrativeLSTMTokenizer();
                if (_tokenizer.LoadFromJson(json))
                    _vocabSize = _tokenizer.VocabSize;
            }
        }

        private void LoadModel()
        {
#if UNITY_BARRACUDA
            try
            {
                if (modelAsset != null)
                    _runtimeModel = ModelLoader.Load(modelAsset);
                else if (!string.IsNullOrEmpty(modelPath))
                {
                    string path = Path.Combine(Application.streamingAssetsPath, modelPath);
                    if (!File.Exists(path)) path = Path.Combine(Application.dataPath, "..", modelPath);
                    if (File.Exists(path)) _runtimeModel = ModelLoader.LoadFromFile(path);
                }
                if (_runtimeModel == null) { Debug.LogWarning("[NarrativeLSTMPromptInterpreter] Model not found."); return; }
                _worker = WorkerFactory.CreateWorker(_runtimeModel);
                _modelLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NarrativeLSTMPromptInterpreter] Load failed: {e.Message}");
            }
#endif
        }

        /// <summary>Run interpreter on prompt; returns list of decoded events and sets lastInterpretedEvents.</summary>
        public List<InterpretedEvent> Interpret(string prompt)
        {
            lastInterpretedEvents.Clear();
            if (_tokenizer == null || !_modelLoaded)
                return lastInterpretedEvents;
            int[] ids = _tokenizer.Encode(prompt ?? "", false, PromptMaxLen);
            float[] input = new float[PromptMaxLen];
            for (int i = 0; i < PromptMaxLen; i++)
                input[i] = i < ids.Length ? ids[i] : _tokenizer.PadId;
#if UNITY_BARRACUDA
            try
            {
                var inputTensor = new Tensor(1, PromptMaxLen, input);
                _worker.Execute(inputTensor);
                var outputTensor = _worker.PeekOutput();
                float[] outData = outputTensor.ToReadOnlyArray();
                inputTensor.Dispose();
                outputTensor.Dispose();
                DecodeOutput(outData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NarrativeLSTMPromptInterpreter] Inference failed: {e.Message}");
            }
#endif
            return lastInterpretedEvents;
        }

        private void DecodeOutput(float[] outData)
        {
            if (outData == null || outData.Length < OutputDim) return;
            float weekSeconds = 86400f * 7f;
            int n = Mathf.Clamp(Mathf.RoundToInt(outData[0] * MaxEvents), 0, MaxEvents);
            for (int e = 0; e < n; e++)
            {
                int off = 2 + e * EventParams;
                if (off + EventParams > outData.Length) break;
                float titleNorm = outData[off];
                int titleIdx = Mathf.Clamp(Mathf.RoundToInt(titleNorm * (_vocabSize - 1)), 0, _vocabSize - 1);
                string title = _tokenizer.Decode(new[] { titleIdx });
                if (string.IsNullOrEmpty(title)) title = "Event";
                float startNorm = Mathf.Clamp01(outData[off + 1]);
                float durNorm = Mathf.Clamp01(outData[off + 2]);
                float cx = outData[off + 3], cy = outData[off + 4], cz = outData[off + 5];
                float sx = Mathf.Max(0.1f, outData[off + 6]), sy = Mathf.Max(0.1f, outData[off + 7]), sz = Mathf.Max(0.1f, outData[off + 8]);
                float tMin = Mathf.Clamp01(outData[off + 9]) * weekSeconds;
                float tMax = Mathf.Clamp01(outData[off + 10]) * weekSeconds;
                if (tMax <= tMin) tMax = tMin + 3600f;
                lastInterpretedEvents.Add(new InterpretedEvent
                {
                    title = title,
                    startSeconds = startNorm * weekSeconds,
                    durationSeconds = durNorm * 3600f,
                    center = new Vector3(cx, cy, cz),
                    size = new Vector3(sx, sy, sz),
                    tMin = tMin,
                    tMax = tMax
                });
            }
        }

        /// <summary>Add lastInterpretedEvents to the assigned calendar (or to the one passed in).</summary>
        public void ApplyToCalendar(NarrativeCalendarAsset targetCalendar = null)
        {
            var cal = targetCalendar != null ? targetCalendar : calendar;
            if (cal == null || cal.events == null) return;
            foreach (var ev in lastInterpretedEvents)
            {
                var ne = new NarrativeCalendarEvent
                {
                    title = ev.title,
                    startDateTime = NarrativeCalendarMath.SecondsToNarrativeDateTime(ev.startSeconds),
                    durationSeconds = Mathf.RoundToInt(ev.durationSeconds),
                    spatiotemporalVolume = new Bounds4(ev.center, ev.size, ev.tMin, ev.tMax)
                };
                cal.events.Add(ne);
            }
        }

        public bool IsReady => _tokenizer != null && _modelLoaded;
    }
}
