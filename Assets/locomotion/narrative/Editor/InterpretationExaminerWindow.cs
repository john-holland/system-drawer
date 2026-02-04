#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Examination window: prompt asset selector, original vs procedural, events and bindings with status, Fill missing links.
    /// </summary>
    public class InterpretationExaminerWindow : EditorWindow
    {
        private NarrativeLSTMPromptInterpreter _interpreter;
        private NarrativePromptAsset _selectedAsset;
        private Vector2 _scroll;
        private Vector2 _scrollText;

        [MenuItem("Window/Locomotion/Narrative/Interpretation Examiner")]
        public static void ShowWindow()
        {
            var w = GetWindow<InterpretationExaminerWindow>("Interpretation Examiner");
            w.minSize = new Vector2(480, 400);
            w.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawPromptAssetSection();
            EditorGUILayout.Space(4);
            DrawOriginalVsProcedural();
            EditorGUILayout.Space(4);
            DrawEventsAndBindings();
            EditorGUILayout.Space(4);
            DrawGenerationRequests();
            EditorGUILayout.Space(4);
            DrawFillMissingLinks();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            _interpreter = (NarrativeLSTMPromptInterpreter)EditorGUILayout.ObjectField("Interpreter", _interpreter, typeof(NarrativeLSTMPromptInterpreter), true);
            if (EditorGUI.EndChangeCheck())
                Repaint();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPromptAssetSection()
        {
            EditorGUILayout.LabelField("Prompt asset", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _selectedAsset = (NarrativePromptAsset)EditorGUILayout.ObjectField(_selectedAsset, typeof(NarrativePromptAsset), false);
            if (EditorGUI.EndChangeCheck())
                Repaint();
            if (_selectedAsset != null && _interpreter != null && GUILayout.Button("Interpret this asset", GUILayout.Height(22)))
            {
                _interpreter.Interpret(_selectedAsset);
                Repaint();
            }
        }

        private void DrawOriginalVsProcedural()
        {
            EditorGUILayout.LabelField("Original vs procedural", EditorStyles.boldLabel);
            if (_selectedAsset == null)
            {
                EditorGUILayout.HelpBox("Select a NarrativePromptAsset.", MessageType.None);
                return;
            }
            _scrollText = EditorGUILayout.BeginScrollView(_scrollText, GUILayout.Height(120));
            EditorGUILayout.LabelField("Original (authored):", EditorStyles.miniLabel);
            EditorGUILayout.TextArea(_selectedAsset.originalText ?? "", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Procedural (generated):", EditorStyles.miniLabel);
            EditorGUILayout.TextArea(_selectedAsset.proceduralText ?? "", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void DrawEventsAndBindings()
        {
            EditorGUILayout.LabelField("Events and bindings", EditorStyles.boldLabel);
            if (_interpreter == null || _selectedAsset == null)
            {
                EditorGUILayout.HelpBox("Assign interpreter and prompt asset, then Interpret.", MessageType.None);
                return;
            }
            var result = _interpreter.GetResultForAsset(_selectedAsset);
            if (result == null || (result.events.Count == 0 && result.bindings.Count == 0))
            {
                EditorGUILayout.HelpBox("No result for this asset. Run Interpret(asset) first.", MessageType.Info);
                return;
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180));
            for (int i = 0; i < result.events.Count; i++)
            {
                var ev = result.events[i];
                var binding = i < result.bindings.Count ? result.bindings[i] : default;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Event {i}: {ev.title}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Status: {binding.status}  Resolved key: {(string.IsNullOrEmpty(binding.resolvedOrmKey) ? "—" : binding.resolvedOrmKey)}");
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGenerationRequests()
        {
            EditorGUILayout.LabelField("(GENERATE) requests", EditorStyles.boldLabel);
            if (_selectedAsset == null || _selectedAsset.generationRequests == null || _selectedAsset.generationRequests.Count == 0)
            {
                EditorGUILayout.LabelField("None.");
                return;
            }
            foreach (var gr in _selectedAsset.generationRequests)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{gr.start}, +{gr.length}]", GUILayout.Width(80));
                EditorGUILayout.LabelField(string.IsNullOrEmpty(gr.description) ? "(no description)" : gr.description);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFillMissingLinks()
        {
            EditorGUILayout.LabelField("Fill missing links", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("For UnderstoodNoOrmMatch: retries resolution with normalized phrase (lowercase, single words). MarkedGenerate items are listed; use an LLM or generator to produce assets and register in ORM.", MessageType.Info);
            if (GUILayout.Button("Fill missing links (retry ORM resolution)", GUILayout.Height(22)))
            {
                if (_interpreter != null && _selectedAsset != null && _interpreter.sceneObjectRegistry != null)
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
                                var words = phrase.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                                foreach (var w in words)
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
                        Debug.Log($"[Interpretation Examiner] Fill missing links: updated {filled} binding(s) to OrmMatched.");
                        Repaint();
                    }
                }
                else if (_interpreter == null || _selectedAsset == null)
                    Debug.LogWarning("[Interpretation Examiner] Assign interpreter and prompt asset first.");
                else if (_interpreter.sceneObjectRegistry == null)
                    Debug.LogWarning("[Interpretation Examiner] Assign interpreter's Scene Object Registry to resolve keys.");
            }
        }
    }
}
#endif
