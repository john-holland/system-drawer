#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;
using Locomotion.Narrative.EditorTools;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Prompt Tree Inspector: paragraph view with dashed groups per word/phrase, vertical list of words/synonyms,
    /// and per-item inspect panel with asset property view wizards.
    /// </summary>
    public class PromptTreeInspectorWindow : EditorWindow
    {
        private SpatialGenerator4D _spatialGenerator4D;
        private NarrativeLSTMPromptInterpreter _interpreter;
        private SceneObjectRegistry _registry;
        private NarrativePromptAsset _selectedAsset;

        private Vector2 _paragraphScroll;
        private Vector2 _listScroll;
        private Vector2 _mainScroll;
        private int _selectedBindingIndex = -1;
        readonly Dictionary<string, string> _bindingDialogueGoals = new Dictionary<string, string>();
        readonly Dictionary<string, string> _bindingDialogueQuoteSnippet = new Dictionary<string, string>();
        private int _scrollToBindingIndex = -1;

        private struct ParagraphSegment
        {
            public string text;
            public int bindingIndex;
            public bool isPhrase;
        }

        [MenuItem("Window/System Drawer/Narrative/Prompt Tree Inspector", false, 203)]
        public static void ShowWindow()
        {
            var w = GetWindow<PromptTreeInspectorWindow>("Prompt Tree Inspector");
            w.minSize = new Vector2(520, 500);
            w.Show();
        }

        public static void ShowWindow(SpatialGenerator4D sg4d)
        {
            var w = GetWindow<PromptTreeInspectorWindow>("Prompt Tree Inspector");
            w._spatialGenerator4D = sg4d;
            w.ResolveReferences();
            w.minSize = new Vector2(520, 500);
            w.Show();
        }

        private void OnEnable()
        {
            if (_spatialGenerator4D == null && Selection.activeGameObject != null)
            {
                _spatialGenerator4D = Selection.activeGameObject.GetComponent<SpatialGenerator4D>();
                if (_spatialGenerator4D == null)
                    _spatialGenerator4D = UnityEngine.Object.FindAnyObjectByType<SpatialGenerator4D>();
            }
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_spatialGenerator4D != null)
            {
                _interpreter = _spatialGenerator4D.promptTreeInspectorInterpreter;
                _registry = _spatialGenerator4D.promptTreeInspectorRegistry;
                if (_registry == null && _interpreter != null)
                    _registry = _interpreter.sceneObjectRegistry;
                if (_interpreter == null)
                {
                    var orch = _spatialGenerator4D.GetComponentInParent<SpatialGenerator4DOrchestrator>();
                    if (orch != null)
                    {
                        var placer = orch.GetComponentInChildren<Narrative4DPlacer>();
                        if (placer != null && placer.calendar != null)
                        {
                            var interp = UnityEngine.Object.FindAnyObjectByType<NarrativeLSTMPromptInterpreter>();
                            if (interp != null && interp.calendar == placer.calendar)
                                _interpreter = interp;
                        }
                    }
                    if (_interpreter == null)
                        _interpreter = UnityEngine.Object.FindAnyObjectByType<NarrativeLSTMPromptInterpreter>();
                }
                if (_registry == null && _interpreter != null)
                    _registry = _interpreter.sceneObjectRegistry;
                if (_selectedAsset == null && _spatialGenerator4D.lastInspectedPrompt != null)
                    _selectedAsset = _spatialGenerator4D.lastInspectedPrompt;
            }
        }

        private void OnGUI()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

            DrawToolbar();
            EditorGUILayout.Space(4);

            DrawContextSection();
            EditorGUILayout.Space(4);

            DrawPromptAssetSection();
            EditorGUILayout.Space(4);

            DrawParagraphSection();
            EditorGUILayout.Space(4);

            DrawVerticalListSection();
            EditorGUILayout.Space(4);

            if (_selectedBindingIndex >= 0)
                DrawInspectPanel();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            _spatialGenerator4D = (SpatialGenerator4D)EditorGUILayout.ObjectField("SpatialGenerator4D", _spatialGenerator4D, typeof(SpatialGenerator4D), true);
            if (EditorGUI.EndChangeCheck())
                ResolveReferences();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContextSection()
        {
            EditorGUILayout.LabelField("Context", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  Interpreter:", _interpreter != null ? _interpreter.name : "—");
            EditorGUILayout.LabelField("  Registry:", _registry != null ? _registry.name : "—");
        }

        private void DrawPromptAssetSection()
        {
            EditorGUILayout.LabelField("Prompt asset", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _selectedAsset = (NarrativePromptAsset)EditorGUILayout.ObjectField(_selectedAsset, typeof(NarrativePromptAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (_spatialGenerator4D != null)
                {
                    Undo.RecordObject(_spatialGenerator4D, "Set last inspected prompt");
                    _spatialGenerator4D.lastInspectedPrompt = _selectedAsset;
                }
            }
            if (_selectedAsset != null && _interpreter != null && GUILayout.Button("Interpret this asset", GUILayout.Height(22)))
            {
                _interpreter.Interpret(_selectedAsset);
                Repaint();
            }
            if (_selectedAsset != null && _interpreter != null && _interpreter.sceneObjectRegistry != null && GUILayout.Button("Fill missing links (retry ORM resolution)", GUILayout.Height(20)))
            {
                var result = _interpreter.GetResultForAsset(_selectedAsset);
                if (result != null)
                {
                    var registry = _interpreter.sceneObjectRegistry;
                    int filled = 0;
                    for (int i = 0; i < result.bindings.Count; i++)
                    {
                        var b = result.bindings[i];
                        if (b.status != BindingStatus.UnderstoodNoOrmMatch) continue;
                        string phrase = (b.phrase ?? "").Trim().ToLowerInvariant();
                        if (string.IsNullOrEmpty(phrase)) continue;
                        string key = registry.ResolveKey(phrase);
                        if (string.IsNullOrEmpty(key) && phrase.Contains(" "))
                        {
                            foreach (var w in phrase.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                            {
                                key = registry.ResolveKey(w);
                                if (!string.IsNullOrEmpty(key)) break;
                            }
                        }
                        if (!string.IsNullOrEmpty(key))
                        {
                            result.bindings[i] = InterpretedEventBinding.Matched(b.eventIndex, b.phrase, key);
                            filled++;
                        }
                    }
                    Repaint();
                }
            }
        }

        private void DrawParagraphSection()
        {
            EditorGUILayout.LabelField("Prompt paragraph", EditorStyles.boldLabel);
            var result = GetResult();
            if (result == null || _selectedAsset == null)
            {
                EditorGUILayout.HelpBox("Select a prompt asset and run Interpret.", MessageType.None);
                return;
            }

            string promptText = _selectedAsset.GetActivePromptText() ?? "";
            var segments = BuildParagraphSegments(promptText, result.bindings);

            _paragraphScroll = EditorGUILayout.BeginScrollView(_paragraphScroll, GUILayout.Height(100));

            var dashedStyle = new GUIStyle(EditorStyles.helpBox);
            dashedStyle.padding = new RectOffset(4, 4, 2, 2);
            dashedStyle.margin = new RectOffset(1, 1, 1, 1);
            var labelStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { wordWrap = false };

            float maxX = position.width - 50;
            float x = 0;
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                var content = new GUIContent(seg.text);
                float w = labelStyle.CalcSize(content).x + (seg.isPhrase ? 12 : 4);
                if (x + w > maxX && x > 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    x = 0;
                }
                if (seg.isPhrase)
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.95f, 0.98f, 1f);
                    EditorGUILayout.BeginVertical(dashedStyle, GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(seg.text, EditorStyles.label, GUILayout.ExpandWidth(false)))
                    {
                        _selectedBindingIndex = seg.bindingIndex;
                        _scrollToBindingIndex = seg.bindingIndex;
                    }
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = prevBg;
                }
                else
                {
                    GUILayout.Label(seg.text, labelStyle, GUILayout.ExpandWidth(false));
                }
                x += w;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private List<ParagraphSegment> BuildParagraphSegments(string promptText, List<InterpretedEventBinding> bindings)
        {
            var segments = new List<ParagraphSegment>();
            if (string.IsNullOrEmpty(promptText))
            {
                foreach (var b in bindings)
                    segments.Add(new ParagraphSegment { text = b.phrase, bindingIndex = segments.Count, isPhrase = true });
                return segments;
            }

            var phraseSpans = new List<(int start, int length, int bindingIndex)>();
            string lowerPrompt = promptText.ToLowerInvariant();
            var matchedIndices = new HashSet<int>();
            for (int i = 0; i < bindings.Count; i++)
            {
                string phrase = (bindings[i].phrase ?? "").Trim();
                if (string.IsNullOrEmpty(phrase)) continue;
                int idx = lowerPrompt.IndexOf(phrase.ToLowerInvariant(), StringComparison.Ordinal);
                if (idx >= 0)
                {
                    phraseSpans.Add((idx, phrase.Length, i));
                    matchedIndices.Add(i);
                }
            }
            phraseSpans.Sort((a, b) => a.start.CompareTo(b.start));
            var nonOverlapping = new List<(int start, int length, int bindingIndex)>();
            int lastEnd = -1;
            foreach (var s in phraseSpans)
            {
                if (s.start >= lastEnd)
                {
                    nonOverlapping.Add(s);
                    lastEnd = s.start + s.length;
                }
            }

            int pos = 0;
            foreach (var span in nonOverlapping)
            {
                if (span.start > pos)
                    segments.Add(new ParagraphSegment { text = promptText.Substring(pos, span.start - pos), bindingIndex = -1, isPhrase = false });
                segments.Add(new ParagraphSegment { text = promptText.Substring(span.start, span.length), bindingIndex = span.bindingIndex, isPhrase = true });
                pos = span.start + span.length;
            }
            if (pos < promptText.Length)
                segments.Add(new ParagraphSegment { text = promptText.Substring(pos), bindingIndex = -1, isPhrase = false });

            for (int i = 0; i < bindings.Count; i++)
            {
                if (matchedIndices.Contains(i)) continue;
                string phrase = (bindings[i].phrase ?? "").Trim();
                if (string.IsNullOrEmpty(phrase)) continue;
                if (segments.Count > 0) segments.Add(new ParagraphSegment { text = " ", bindingIndex = -1, isPhrase = false });
                segments.Add(new ParagraphSegment { text = phrase, bindingIndex = i, isPhrase = true });
            }

            if (segments.Count == 0 && !string.IsNullOrEmpty(promptText))
                segments.Add(new ParagraphSegment { text = promptText, bindingIndex = -1, isPhrase = false });

            return segments;
        }

        private void DrawVerticalListSection()
        {
            EditorGUILayout.LabelField("Words and synonyms", EditorStyles.boldLabel);
            var result = GetResult();
            if (result == null || result.bindings.Count == 0)
            {
                EditorGUILayout.HelpBox("No bindings. Run Interpret first.", MessageType.Info);
                return;
            }

            float listHeight = Mathf.Min(200, result.bindings.Count * 24 + 8);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(listHeight));

            if (_scrollToBindingIndex >= 0)
            {
                float targetY = _scrollToBindingIndex * 24f;
                _listScroll.y = Mathf.Max(0, targetY - listHeight / 2);
                _scrollToBindingIndex = -1;
            }

            for (int i = 0; i < result.bindings.Count; i++)
            {
                var b = result.bindings[i];
                string synonymsStr = "";
                if (_registry != null && !string.IsNullOrEmpty(b.resolvedOrmKey))
                {
                    var entry = _registry.GetCloneable(b.resolvedOrmKey) ?? _registry.GetReference(b.resolvedOrmKey);
                    if (entry?.synonyms != null && entry.synonyms.Count > 0)
                        synonymsStr = string.Join(", ", entry.synonyms);
                }

                bool selected = _selectedBindingIndex == i;
                var rowContent = $"  {b.phrase}  |  {b.status}  |  {(string.IsNullOrEmpty(b.resolvedOrmKey) ? "—" : b.resolvedOrmKey)}  |  {synonymsStr}";
                var rowStyle = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleLeft };
                var prevBg = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.85f, 0.92f, 1f);
                if (GUILayout.Button(rowContent, rowStyle, GUILayout.MinHeight(22)))
                {
                    _selectedBindingIndex = i;
                }
                if (selected) GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawInspectPanel()
        {
            var result = GetResult();
            if (result == null || _selectedBindingIndex < 0 || _selectedBindingIndex >= result.bindings.Count)
                return;

            var binding = result.bindings[_selectedBindingIndex];
            string key = !string.IsNullOrEmpty(binding.resolvedOrmKey) ? binding.resolvedOrmKey : binding.phrase;
            var entry = _registry != null ? (_registry.GetCloneable(key) ?? _registry.GetReference(key)) : null;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Inspect: " + binding.phrase, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  Status:", binding.status.ToString());
            EditorGUILayout.LabelField("  Resolved key:", string.IsNullOrEmpty(binding.resolvedOrmKey) ? "—" : binding.resolvedOrmKey);
            EditorGUILayout.LabelField("  Has prefab:", (entry != null && (entry.prefabForClone != null || entry.reference != null)) ? "Yes" : "No");
            EditorGUILayout.LabelField("  Synonyms:", entry?.synonyms != null ? string.Join(", ", entry.synonyms) : "—");

            EditorGUILayout.Space(4);
            AssetPropertyViewWizards.DrawRoundedPrefabProgression(_registry, entry, key);

            EditorGUILayout.Space(4);
            var sg4d = _spatialGenerator4D;
            SpatialGeneratorStylesheet stylesheet = null;
            if (sg4d != null)
            {
                var skinController = sg4d.GetComponent<SpatialGeneratorSkinController>();
                if (skinController?.skins != null && skinController.skins.Count > 0)
                {
                    int idx = Application.isPlaying ? skinController.activeSkinIndex : skinController.editorActiveSkinIndex;
                    if (idx >= 0 && idx < skinController.skins.Count)
                        stylesheet = skinController.skins[idx]?.stylesheet;
                }
            }
            AssetPropertyViewWizards.DrawFillSelectionProgression(binding, _registry, entry, stylesheet, key);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Dialogue goal (lemma branch)", EditorStyles.boldLabel);
            int goalIndex = EditorGUILayout.Popup("Goal",
                IndexOfGoal(_bindingDialogueGoals.TryGetValue(key, out var gk) ? gk : ""),
                DialogueGoalPopupLabels());
            if (goalIndex > 0)
                _bindingDialogueGoals[key] = DialogueGoalNames.All[goalIndex - 1];
            else
                _bindingDialogueGoals.Remove(key);
            _bindingDialogueQuoteSnippet[key] = EditorGUILayout.TextField("Dialogue quote snippet", _bindingDialogueQuoteSnippet.TryGetValue(key, out var qs) ? qs : binding.phrase);
        }

        static string[] DialogueGoalPopupLabels()
        {
            var labels = new string[DialogueGoalNames.All.Length + 1];
            labels[0] = "(none)";
            for (int i = 0; i < DialogueGoalNames.All.Length; i++)
                labels[i + 1] = DialogueGoalNames.All[i];
            return labels;
        }

        static int IndexOfGoal(string goal)
        {
            if (string.IsNullOrEmpty(goal)) return 0;
            for (int i = 0; i < DialogueGoalNames.All.Length; i++)
                if (DialogueGoalNames.All[i] == goal) return i + 1;
            return 0;
        }

        private InterpretationResult GetResult()
        {
            if (_interpreter == null || _selectedAsset == null) return null;
            return _interpreter.GetResultForAsset(_selectedAsset);
        }
    }
}
#endif
