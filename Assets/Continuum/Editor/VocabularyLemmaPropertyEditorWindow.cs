#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Editor for lemma-level localization property bags (syncs with Continuum API).</summary>
public sealed class VocabularyLemmaPropertyEditorWindow : EditorWindow
{
    const string PlaybackPolicyContextTypeName = "AnimationPlaybackPolicyContext";
    const string LocomotionRuntimeAssembly = "Locomotion.Runtime";

    LocalizationPropertySpecCatalog _catalog;
    string _entryId = "";
    string _clauseDraftId = "";
    int _clauseCharStart;
    int _clauseCharEnd;
    string _clauseSelection = "";
    ThesaurusEntryPropertyRecord[] _properties = Array.Empty<ThesaurusEntryPropertyRecord>();
    LemmaCompositionChildDto[] _compositionChildren = Array.Empty<LemmaCompositionChildDto>();
    string _compositionAddQuery = "";
    bool _nonIkAnimation;
    Vector2 _scroll;
    GameObject _pushTarget;

    [MenuItem("Window/Continuum/Lemma Properties")]
    public static void Open()
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
    }

    public static void OpenWithEntryId(string entryId)
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
        if (!string.IsNullOrEmpty(entryId))
        {
            w._entryId = entryId;
            _ = w.LoadPropertiesAsync();
            _ = w.LoadCompositionAsync();
        }
    }

    void OnEnable()
    {
        _catalog = Resources.Load<LocalizationPropertySpecCatalog>("LocalizationPropertySpecCatalog");
        if (_catalog == null)
            _catalog = LocalizationPropertySpecCatalog.CreateDefaultAsset();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Localization Property Specs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Lemma-level property bags sync with Continuum thesaurus_entry_properties.", MessageType.Info);

        _entryId = EditorGUILayout.TextField("Entry ID", _entryId);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load properties"))
        {
            _ = LoadPropertiesAsync();
            _ = LoadCompositionAsync();
        }
        if (GUILayout.Button("Save"))
            SaveProperties();
        GUI.enabled = !string.IsNullOrEmpty(_entryId);
        if (GUILayout.Button("Scan prefab components"))
            _ = ScanPrefabComponentsAsync();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Clause context (optional)", EditorStyles.boldLabel);
        _clauseDraftId = EditorGUILayout.TextField("Draft episode ID", _clauseDraftId);
        EditorGUILayout.BeginHorizontal();
        _clauseCharStart = EditorGUILayout.IntField("Char start", _clauseCharStart);
        _clauseCharEnd = EditorGUILayout.IntField("Char end", _clauseCharEnd);
        EditorGUILayout.EndHorizontal();
        _clauseSelection = EditorGUILayout.TextField("Selection text", _clauseSelection);
        GUI.enabled = !string.IsNullOrEmpty(_entryId) && !string.IsNullOrEmpty(_clauseDraftId) && _clauseCharEnd > _clauseCharStart;
        if (GUILayout.Button("Attach entry to clause span"))
            _ = AttachEntryToClauseAsync();
        GUI.enabled = true;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_catalog?.specs != null)
        {
            foreach (var spec in _catalog.specs)
            {
                if (spec == null) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(spec.key, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Type", spec.valueType);
                EditorGUILayout.LabelField("Default", spec.defaultValue);
                EditorGUILayout.LabelField("Description", spec.description ?? "", EditorStyles.wordWrappedLabel);
                if (spec.key == LocalizationPropertyKeys.NonIkAnimation)
                    _nonIkAnimation = EditorGUILayout.Toggle("Value", _nonIkAnimation);
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        _pushTarget = (GameObject)EditorGUILayout.ObjectField("Push to Travel Agent", _pushTarget, typeof(GameObject), true);
        if (GUILayout.Button("Push to runtime cache") && _pushTarget != null)
            PushToRuntime();

        if (GUILayout.Button("Sync specs from API"))
            SyncSpecs();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Open in Lemma Library") && !string.IsNullOrEmpty(_entryId))
        {
            var baseUrl = ContinuumEditorSession.ApiBaseUrl.TrimEnd('/');
            Application.OpenURL($"{baseUrl}/lemma-library#entry/{Uri.EscapeDataString(_entryId)}");
        }

        DrawCompositionPanel();
    }

    void DrawCompositionPanel()
    {
        EditorGUILayout.Space(4);
        var sepRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(sepRect, new Color(0.4f, 0.4f, 0.4f, 1f));
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Composed lemmas", EditorStyles.boldLabel);

        if (_compositionChildren == null || _compositionChildren.Length == 0)
            EditorGUILayout.LabelField("No child lemmas.", EditorStyles.miniLabel);
        else
        {
            foreach (var child in _compositionChildren)
            {
                if (child == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(child.term ?? child.entryId ?? "?", GUILayout.Width(160));
                EditorGUILayout.SelectableLabel(child.entryId ?? "", EditorStyles.textField, GUILayout.Height(18));
                if (GUILayout.Button("Remove", GUILayout.Width(64)))
                    _ = RemoveCompositionChildAsync(child.entryId);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.BeginHorizontal();
        _compositionAddQuery = EditorGUILayout.TextField("Add child lemma", _compositionAddQuery);
        if (GUILayout.Button("Search & add", GUILayout.Width(100)))
            _ = SearchAndAddCompositionChildAsync();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload composition"))
            _ = LoadCompositionAsync();
        if (GUILayout.Button("Open in Lemma Library") && !string.IsNullOrEmpty(_entryId))
        {
            var baseUrl = ContinuumEditorSession.ApiBaseUrl.TrimEnd('/');
            Application.OpenURL($"{baseUrl}/lemma-library#entry/{Uri.EscapeDataString(_entryId)}");
        }
        if (GUILayout.Button("Recombobulate spatial graph"))
            _ = RecombobulateSpatialAsync();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    async Task LoadCompositionAsync()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var data = await ContinuumEditorLocalizationClient.Instance.GetCompositionAsync(_entryId);
        _compositionChildren = data?.children ?? Array.Empty<LemmaCompositionChildDto>();
        Repaint();
    }

    async Task SearchAndAddCompositionChildAsync()
    {
        if (string.IsNullOrEmpty(_entryId) || string.IsNullOrWhiteSpace(_compositionAddQuery))
            return;
        var q = Uri.EscapeDataString(_compositionAddQuery.Trim());
        var r = await ContinuumEditorLocalizationClient.Instance.CallRawAsync("GET", $"/api/thesaurus/entries?q={q}&limit=1", null);
        if (!r.success || string.IsNullOrEmpty(r.json))
        {
            EditorUtility.DisplayDialog("Composed lemmas", "No matching lemma found.", "OK");
            return;
        }
        var wrapper = JsonUtility.FromJson<ThesaurusEntryListWrapper>(r.json);
        if (wrapper?.items == null || wrapper.items.Length == 0)
        {
            EditorUtility.DisplayDialog("Composed lemmas", "No matching lemma found.", "OK");
            return;
        }
        var hit = wrapper.items[0];
        if (hit == null || string.IsNullOrEmpty(hit.id))
            return;
        var list = (_compositionChildren ?? Array.Empty<LemmaCompositionChildDto>()).ToList();
        if (list.Any(c => c != null && c.entryId == hit.id))
        {
            EditorUtility.DisplayDialog("Composed lemmas", "Lemma already in composition.", "OK");
            return;
        }
        list.Add(new LemmaCompositionChildDto { entryId = hit.id, term = hit.term, sortOrder = list.Count });
        var put = list.Select((c, i) => new LemmaCompositionChildPutDto { entryId = c.entryId, sortOrder = i }).ToArray();
        var saved = await ContinuumEditorLocalizationClient.Instance.PutCompositionAsync(_entryId, put);
        _compositionChildren = saved?.children ?? list.ToArray();
        _compositionAddQuery = "";
        Repaint();
    }

    async Task RemoveCompositionChildAsync(string childEntryId)
    {
        if (string.IsNullOrEmpty(_entryId) || string.IsNullOrEmpty(childEntryId))
            return;
        var list = (_compositionChildren ?? Array.Empty<LemmaCompositionChildDto>())
            .Where(c => c != null && c.entryId != childEntryId)
            .Select((c, i) => new LemmaCompositionChildPutDto { entryId = c.entryId, sortOrder = i })
            .ToArray();
        var saved = await ContinuumEditorLocalizationClient.Instance.PutCompositionAsync(_entryId, list);
        _compositionChildren = saved?.children ?? Array.Empty<LemmaCompositionChildDto>();
        Repaint();
    }

    async Task RecombobulateSpatialAsync()
    {
        if (string.IsNullOrEmpty(_entryId))
            return;
        string scriptText = "";
        if (!string.IsNullOrEmpty(_clauseDraftId))
        {
            var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_clauseDraftId);
            scriptText = draft?.scriptText ?? "";
        }
        var audit = await ContinuumEditorLocalizationClient.Instance.RecombobulateSpatialAsync(
            _entryId,
            new LemmaRecombobulateRequestDto { scriptText = scriptText, draftEpisodeId = _clauseDraftId });
        if (audit?.issues == null || audit.issues.Length == 0)
        {
            EditorUtility.DisplayDialog("Recombobulate spatial graph", "No issues found.", "OK");
            await LoadCompositionAsync();
            return;
        }
        var ackIds = new List<string>();
        foreach (var issue in audit.issues)
        {
            if (issue == null) continue;
            var msg = $"{issue.code}: {issue.message}";
            if (!string.IsNullOrEmpty(issue.storedText) || !string.IsNullOrEmpty(issue.currentText))
                msg += $"\nStored: {issue.storedText}\nCurrent: {issue.currentText}";
            if (issue.requiresAck)
            {
                if (!EditorUtility.DisplayDialog("Recombobulate spatial graph", msg + "\n\nAcknowledge this fix?", "Acknowledge", "Skip"))
                    continue;
                ackIds.Add(issue.id);
            }
            else
                ackIds.Add(issue.id);
        }
        await ContinuumEditorLocalizationClient.Instance.RecombobulateSpatialAsync(
            _entryId,
            new LemmaRecombobulateRequestDto
            {
                scriptText = scriptText,
                draftEpisodeId = _clauseDraftId,
                apply = true,
                acknowledgedIssueIds = ackIds.ToArray(),
            });
        await LoadCompositionAsync();
        EditorUtility.DisplayDialog("Recombobulate spatial graph", "Repair pass completed.", "OK");
    }

    [Serializable]
    class ThesaurusEntryListWrapper
    {
        public ThesaurusEntrySummaryDto[] items;
    }

    [Serializable]
    class ThesaurusEntrySummaryDto
    {
        public string id;
        public string term;
    }

    async Task ScanPrefabComponentsAsync()
    {
        if (string.IsNullOrEmpty(_entryId))
            return;
        var ok = await LemmaComponentBlueprintScanner.ScanAndPostEntryAsync(_entryId);
        EditorUtility.DisplayDialog(
            "Lemma Properties",
            ok ? "Prefab component blueprint uploaded." : "Scan failed — check prefab-id property and Console.",
            "OK");
    }

    async Task LoadPropertiesAsync()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        _properties = await client.GetEntryPropertiesAsync(_entryId);
        foreach (var p in _properties)
        {
            if (p != null && p.propertyKey == LocalizationPropertyKeys.NonIkAnimation &&
                TryParseBool(p.propertyValue, out bool v))
                _nonIkAnimation = v;
        }
        Repaint();
    }

    async void SaveProperties()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        await client.PutEntryPropertyAsync(_entryId, LocalizationPropertyKeys.NonIkAnimation, _nonIkAnimation ? "true" : "false");
        await LoadPropertiesAsync();
    }

    async Task AttachEntryToClauseAsync()
    {
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_clauseDraftId);
        string scriptText = draft?.scriptText ?? "";
        var farey = FareySpanUtility.CharRangeToFareySpan(scriptText, _clauseCharStart, _clauseCharEnd);
        var clauseRef = new ClauseRefRecord
        {
            charStart = _clauseCharStart,
            charEnd = _clauseCharEnd,
            selectionText = _clauseSelection,
            entryId = _entryId,
            draftEpisodeId = _clauseDraftId,
            draftScriptId = draft?.id ?? "",
            fareyLeftNum = farey.ln,
            fareyLeftDen = farey.ld,
            fareyRightNum = farey.rn,
            fareyRightDen = farey.rd,
        };
        await ContinuumEditorLocalizationClient.Instance.PostClauseBindingAsync(
            clauseRef,
            LocalizationBindingKinds.Lemma,
            "entry-id",
            _entryId,
            scriptText);
        EditorUtility.DisplayDialog("Lemma Properties", "Lemma attached to clause span.", "OK");
    }

    void PushToRuntime()
    {
        var ctxType = FindLocomotionRuntimeType(PlaybackPolicyContextTypeName);
        if (ctxType == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Locomotion.Runtime is not loaded.", "OK");
            return;
        }

        var ctx = _pushTarget.GetComponent(ctxType) ?? _pushTarget.GetComponentInChildren(ctxType);
        if (ctx == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Add AnimationPlaybackPolicyContext to Travel Agent hierarchy.", "OK");
            return;
        }

        var method = ctxType.GetMethod("SetLemmaProperties", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "AnimationPlaybackPolicyContext.SetLemmaProperties not found.", "OK");
            return;
        }

        var list = _properties.ToList();
        if (!string.IsNullOrEmpty(_entryId))
        {
            list.RemoveAll(p => p != null && p.propertyKey == LocalizationPropertyKeys.NonIkAnimation && p.entryId == _entryId);
            list.Add(new ThesaurusEntryPropertyRecord
            {
                entryId = _entryId,
                propertyKey = LocalizationPropertyKeys.NonIkAnimation,
                propertyValue = _nonIkAnimation ? "true" : "false"
            });
        }

        method.Invoke(ctx, new object[] { list });
    }

    async void SyncSpecs()
    {
        var specs = await ContinuumEditorLocalizationClient.Instance.GetPropertySpecsAsync();
        if (specs != null && specs.Length > 0)
            Debug.Log($"[Lemma Properties] Loaded {specs.Length} spec(s) from API.");
    }

    static Type FindLocomotionRuntimeType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, LocomotionRuntimeAssembly, StringComparison.Ordinal))
                continue;
            var type = assembly.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;
        }
        return null;
    }

    static bool TryParseBool(string value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (bool.TryParse(value.Trim(), out result))
            return true;
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "yes":
            case "on":
                result = true;
                return true;
            case "0":
            case "no":
            case "off":
                result = false;
                return true;
            default:
                return false;
        }
    }
}
#endif
