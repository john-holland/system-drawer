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
    /// <summary>
    /// LSTM-based summarizer: builds calendar snapshot from NarrativeCalendarAsset (optionally filtered by time),
    /// runs ONNX model, decodes output to a short "what's going on" summary string.
    /// </summary>
    public class NarrativeLSTMSummarizer : MonoBehaviour
    {
        [Header("Model")]
        [Tooltip("Path to narrative_summarizer.onnx (relative to StreamingAssets or project root).")]
        public string modelPath = "NarrativeLSTM/narrative_summarizer.onnx";
#if UNITY_BARRACUDA
        [Tooltip("Or assign NNModel asset directly.")]
        public NNModel modelAsset;
#endif

        [Header("Vocab")]
        [Tooltip("Path to vocab.json under Resources or StreamingAssets/NarrativeLSTM.")]
        public string vocabPath = "NarrativeLSTM/vocab";
        [Tooltip("Or load vocab from this TextAsset.")]
        public TextAsset vocabAsset;

        [Header("Input")]
        [Tooltip("Calendar to summarize. If null, will try to find in scene.")]
        public NarrativeCalendarAsset calendar;
        [Tooltip("If set, only include events within this time range (narrative seconds). Null = all events.")]
        public Vector2 timeRangeSeconds = new Vector2(0, 86400 * 7f);

        [Header("Output")]
        [Tooltip("Last generated summary (read-only).")]
        [TextArea(2, 4)]
        public string lastSummary = "";

        private const int CalendarMaxLen = 256;
        private const int SummaryMaxLen = 32;

#if UNITY_BARRACUDA
        private Model _runtimeModel;
        private IWorker _worker;
#endif
        private NarrativeLSTMTokenizer _tokenizer;
        private bool _modelLoaded;

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
            if (vocabAsset != null)
                json = vocabAsset.text;
            if (string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(vocabPath))
            {
                var ta = Resources.Load<TextAsset>(vocabPath);
                if (ta != null) json = ta.text;
                if (string.IsNullOrEmpty(json))
                {
                    string path = Path.Combine(Application.streamingAssetsPath, vocabPath + ".json");
                    if (File.Exists(path))
                        json = File.ReadAllText(path);
                }
            }
            if (!string.IsNullOrEmpty(json))
            {
                _tokenizer = new NarrativeLSTMTokenizer();
                _tokenizer.LoadFromJson(json);
            }
        }

        private void LoadModel()
        {
            _modelLoaded = false;
#if UNITY_BARRACUDA
            try
            {
                if (modelAsset != null)
                    _runtimeModel = ModelLoader.Load(modelAsset);
                else if (!string.IsNullOrEmpty(modelPath))
                {
                    string path = Path.Combine(Application.streamingAssetsPath, modelPath);
                    if (!File.Exists(path))
                        path = Path.Combine(Application.dataPath, "..", modelPath);
                    if (File.Exists(path))
                        _runtimeModel = ModelLoader.LoadFromFile(path);
                }
                if (_runtimeModel == null) { Debug.LogWarning("[NarrativeLSTMSummarizer] Model not found."); return; }
                _worker = WorkerFactory.CreateWorker(_runtimeModel);
                _modelLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NarrativeLSTMSummarizer] Load failed: {e.Message}");
            }
#else
            Debug.LogWarning("[NarrativeLSTMSummarizer] Barracuda not available.");
#endif
        }

        /// <summary>Build snapshot text from calendar events (optionally filtered by time range).</summary>
        public static string BuildCalendarSnapshot(NarrativeCalendarAsset cal, float? tMin, float? tMax)
        {
            if (cal == null || cal.events == null) return "No events";
            var lines = new List<string>();
            for (int i = 0; i < cal.events.Count; i++)
            {
                var e = cal.events[i];
                if (e == null) continue;
                float t = NarrativeCalendarMath.DateTimeToSeconds(e.startDateTime);
                if (tMin.HasValue && t < tMin.Value) continue;
                if (tMax.HasValue && t > tMax.Value) continue;
                string notes = (e.notes ?? "").Length > 200 ? (e.notes ?? "").Substring(0, 200) : (e.notes ?? "");
                string tags = e.tags != null ? string.Join(" ", e.tags) : "";
                lines.Add($"Event: {e.title} | {notes} | {e.startDateTime} | {e.durationSeconds}s | {tags}");
            }
            return lines.Count > 0 ? string.Join("\n", lines) : "No events";
        }

        /// <summary>Run summarizer and set lastSummary. Returns summary string.</summary>
        public string Summarize()
        {
            if (_tokenizer == null || !_modelLoaded)
            {
                lastSummary = "(Vocab or model not loaded)";
                return lastSummary;
            }
            var cal = calendar != null ? calendar : FindAnyObjectByType<NarrativeCalendarAsset>();
            if (cal == null)
            {
                lastSummary = "No calendar.";
                return lastSummary;
            }
            float? tMin = timeRangeSeconds.x > 0 || timeRangeSeconds.y < 86400 * 365 ? (float?)timeRangeSeconds.x : null;
            float? tMax = timeRangeSeconds.y > 0 && timeRangeSeconds.y < 86400 * 365 ? (float?)timeRangeSeconds.y : null;
            string snapshot = BuildCalendarSnapshot(cal, tMin, tMax);
            int[] ids = _tokenizer.Encode(snapshot, false, CalendarMaxLen);
            float[] input = new float[CalendarMaxLen];
            for (int i = 0; i < CalendarMaxLen; i++)
                input[i] = i < ids.Length ? ids[i] : _tokenizer.PadId;
#if UNITY_BARRACUDA
            try
            {
                var inputTensor = new Tensor(1, CalendarMaxLen, input); // batch, length, data
                _worker.Execute(inputTensor);
                var outputTensor = _worker.PeekOutput();
                float[] outData = outputTensor.ToReadOnlyArray();
                inputTensor.Dispose();
                int vocabSize = _tokenizer.VocabSize;
                var summaryIds = new List<int>();
                for (int i = 0; i < SummaryMaxLen && i < outData.Length; i++)
                {
                    int idx = Mathf.Clamp(Mathf.RoundToInt(outData[i] * (vocabSize - 1)), 0, vocabSize - 1);
                    if (idx == _tokenizer.EosId) break;
                    if (idx != _tokenizer.PadId)
                        summaryIds.Add(idx);
                }
                lastSummary = _tokenizer.Decode(summaryIds.ToArray());
                outputTensor.Dispose();
                return lastSummary;
            }
            catch (Exception e)
            {
                lastSummary = $"(Error: {e.Message})";
                return lastSummary;
            }
#else
            lastSummary = "(Barracuda not available)";
            return lastSummary;
#endif
        }

        public bool IsReady => _tokenizer != null && _modelLoaded;
    }
}
