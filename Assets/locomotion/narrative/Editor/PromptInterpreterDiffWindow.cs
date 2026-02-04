#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Diff window for prompt interpreter: baseline vs current interpretation.
    /// Black/Blue = same, Yellow = property change, Red = deleted, Green = added.
    /// </summary>
    public class PromptInterpreterDiffWindow : EditorWindow
    {
        private NarrativeLSTMPromptInterpreter _interpreter;
        private InterpretedEventsSnapshot _baselineSnapshot;
        private List<InterpretedEventDiffEntry> _diffEntries = new List<InterpretedEventDiffEntry>();
        private Vector2 _scroll;
        private int _expandedIndex = -1;
        private string _lastPrompt = "";
        private const string PrefsBaselineKey = "PromptInterpreterDiffWindow.BaselineSnapshot";
        private const string PrefsPromptKey = "PromptInterpreterDiffWindow.LastPrompt";

        [MenuItem("Window/Locomotion/Narrative/Prompt Interpreter Diff")]
        public static void ShowWindow()
        {
            var w = GetWindow<PromptInterpreterDiffWindow>("Interpreter Diff");
            w.minSize = new Vector2(420, 320);
            w.Show();
        }

        private void OnEnable()
        {
            _baselineSnapshot = AssetDatabase.LoadAssetAtPath<InterpretedEventsSnapshot>(
                EditorPrefs.GetString(PrefsBaselineKey, ""));
            _lastPrompt = EditorPrefs.GetString(PrefsPromptKey, "");
            NarrativeLSTMPromptInterpreter.InterpretCompleted += OnInterpretCompleted;
            EditorApplication.delayCall += RefreshDiff;
        }

        private void OnDisable()
        {
            NarrativeLSTMPromptInterpreter.InterpretCompleted -= OnInterpretCompleted;
            if (_baselineSnapshot != null)
                EditorPrefs.SetString(PrefsBaselineKey, AssetDatabase.GetAssetPath(_baselineSnapshot));
            EditorPrefs.SetString(PrefsPromptKey, _lastPrompt);
        }

        private void OnInterpretCompleted(NarrativeLSTMPromptInterpreter interpreter)
        {
            if (interpreter == _interpreter)
                RefreshDiff();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawLegend();
            DrawDiffList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            _interpreter = (NarrativeLSTMPromptInterpreter)EditorGUILayout.ObjectField(_interpreter, typeof(NarrativeLSTMPromptInterpreter), true);
            if (EditorGUI.EndChangeCheck())
                RefreshDiff();

            GUILayout.Label("Prompt:", GUILayout.Width(40));
            _lastPrompt = EditorGUILayout.TextField(_lastPrompt, GUILayout.Width(140));

            GUILayout.FlexibleSpace();

            _baselineSnapshot = (InterpretedEventsSnapshot)EditorGUILayout.ObjectField(_baselineSnapshot, typeof(InterpretedEventsSnapshot), false, GUILayout.Width(180));
            if (GUILayout.Button("Save as baseline", EditorStyles.toolbarButton, GUILayout.Width(110)))
                SaveAsBaseline();
            if (GUILayout.Button("Re-run diff", EditorStyles.toolbarButton, GUILayout.Width(90)))
                RefreshDiff();
            if (GUILayout.Button("Re-interpret and diff", EditorStyles.toolbarButton, GUILayout.Width(130)))
                ReinterpretAndDiff();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Legend:", GUILayout.Width(40));
            DrawLegendSwatch(Color.black, "Same");
            DrawLegendSwatch(new Color(0.2f, 0.3f, 0.9f), "Same (alt)");
            DrawLegendSwatch(Color.yellow, "Property change");
            DrawLegendSwatch(new Color(1f, 0.4f, 0.4f), "Deleted");
            DrawLegendSwatch(new Color(0.4f, 0.9f, 0.4f), "Added");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegendSwatch(Color c, string label)
        {
            var r = GUILayoutUtility.GetRect(12, 12);
            EditorGUI.DrawRect(r, c);
            GUILayout.Label(label, GUILayout.Width(80));
        }

        private void DrawDiffList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_diffEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Assign an interpreter and a baseline snapshot, then run Interpret (e.g. from Narrative LSTM UI) or use Re-interpret and diff. Or load two snapshots and Re-run diff.",
                    MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _diffEntries.Count; i++)
                    DrawDiffRow(i);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawDiffRow(int index)
        {
            var entry = _diffEntries[index];
            Color bg = GetColorForKind(entry.Kind);
            var ev = entry.Current.HasValue ? entry.Current.Value : entry.Baseline.Value;

            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rect, bg);
            bool expanded = _expandedIndex == index;

            EditorGUILayout.BeginHorizontal();
            string title = ev.title ?? "(no title)";
            string summary = FormatSummary(ev);
            string kindStr = entry.Kind.ToString();
            if (entry.Kind == InterpretedEventDiffKind.PropertyChange && (entry.Baseline.HasValue && entry.Current.HasValue))
            {
                if (GUILayout.Button(expanded ? "▼" : "▶", GUILayout.Width(18)))
                    _expandedIndex = expanded ? -1 : index;
                GUILayout.Label($"[{kindStr}] {title} — {summary}", EditorStyles.label);
            }
            else
            {
                GUILayout.Space(18);
                GUILayout.Label($"[{kindStr}] {title} — {summary}", EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            if (expanded && entry.Kind == InterpretedEventDiffKind.PropertyChange && entry.Baseline.HasValue && entry.Current.HasValue)
            {
                var b = entry.Baseline.Value;
                var c = entry.Current.Value;
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Baseline vs Current:");
                if (b.title != c.title) EditorGUILayout.LabelField("  title", $"{b.title} → {c.title}");
                if (Mathf.Abs(b.startSeconds - c.startSeconds) >= InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  startSeconds", $"{b.startSeconds} → {c.startSeconds}");
                if (Mathf.Abs(b.durationSeconds - c.durationSeconds) >= InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  durationSeconds", $"{b.durationSeconds} → {c.durationSeconds}");
                if (Vector3.SqrMagnitude(b.center - c.center) >= InterpretedEventDiff.FloatTolerance * InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  center", $"{b.center} → {c.center}");
                if (Vector3.SqrMagnitude(b.size - c.size) >= InterpretedEventDiff.FloatTolerance * InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  size", $"{b.size} → {c.size}");
                if (Mathf.Abs(b.tMin - c.tMin) >= InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  tMin", $"{b.tMin} → {c.tMin}");
                if (Mathf.Abs(b.tMax - c.tMax) >= InterpretedEventDiff.FloatTolerance) EditorGUILayout.LabelField("  tMax", $"{b.tMax} → {c.tMax}");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private static Color GetColorForKind(InterpretedEventDiffKind kind)
        {
            switch (kind)
            {
                case InterpretedEventDiffKind.Same: return new Color(0.95f, 0.95f, 1f);
                case InterpretedEventDiffKind.PropertyChange: return new Color(1f, 1f, 0.7f);
                case InterpretedEventDiffKind.Deleted: return new Color(1f, 0.85f, 0.85f);
                case InterpretedEventDiffKind.Added: return new Color(0.85f, 1f, 0.85f);
                default: return Color.white;
            }
        }

        private static string FormatSummary(InterpretedEvent ev)
        {
            return $"{ev.startSeconds:F0}s, {ev.durationSeconds:F0}s, c={ev.center}";
        }

        private void SaveAsBaseline()
        {
            List<InterpretedEvent> current = GetCurrentEvents();
            if (current == null || current.Count == 0)
            {
                EditorUtility.DisplayDialog("Interpreter Diff", "No current events. Run Interpret first (e.g. from Narrative LSTM UI) or assign an interpreter with lastInterpretedEvents.", "OK");
                return;
            }
            if (_baselineSnapshot == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save baseline snapshot", "InterpreterDiffBaseline", "asset", "Save baseline");
                if (string.IsNullOrEmpty(path)) return;
                _baselineSnapshot = CreateInstance<InterpretedEventsSnapshot>();
                AssetDatabase.CreateAsset(_baselineSnapshot, path);
            }
            string prompt = _lastPrompt;
            string model = _interpreter != null ? _interpreter.modelPath ?? "" : "";
            _baselineSnapshot.Capture(current, prompt, model);
            EditorUtility.SetDirty(_baselineSnapshot);
            AssetDatabase.SaveAssets();
            RefreshDiff();
        }

        private List<InterpretedEvent> GetCurrentEvents()
        {
            if (_interpreter != null && _interpreter.lastInterpretedEvents != null)
                return _interpreter.lastInterpretedEvents;
            return null;
        }

        private void RefreshDiff()
        {
            _diffEntries.Clear();
            List<InterpretedEvent> baseline = _baselineSnapshot != null && _baselineSnapshot.events != null ? _baselineSnapshot.events : null;
            List<InterpretedEvent> current = GetCurrentEvents();
            if (baseline != null || (current != null && current.Count > 0))
                _diffEntries = InterpretedEventDiff.Run(baseline ?? new List<InterpretedEvent>(), current ?? new List<InterpretedEvent>());
            _expandedIndex = -1;
            Repaint();
        }

        private void ReinterpretAndDiff()
        {
            if (_interpreter == null)
            {
                EditorUtility.DisplayDialog("Interpreter Diff", "Assign a NarrativeLSTMPromptInterpreter first.", "OK");
                return;
            }
            string prompt = _lastPrompt;
            if (string.IsNullOrEmpty(prompt) && _baselineSnapshot != null && !string.IsNullOrEmpty(_baselineSnapshot.lastPrompt))
                prompt = _baselineSnapshot.lastPrompt;
            if (string.IsNullOrEmpty(prompt))
                prompt = "Add event meeting at 9am";
            _lastPrompt = prompt;
            _interpreter.Interpret(prompt);
            RefreshDiff();
        }
    }
}
#endif
