#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Unified Continuum script editor — load/save draft, overlays, change-list workflow.</summary>
public sealed class ContinuumScriptEditorWindow : EditorWindow
{
    string _draftId = "";
    string _draftScriptId = "";
    string _reviewId = "";
    string _loadedText = "";
    string _originalText = "";
    bool _readOnly;
    bool _useWebView;
    ContinuumWebViewHost _webHost;
    readonly ContinuumRichScriptEditor _richEditor = new ContinuumRichScriptEditor();
    LocalizationClauseBindingRecord[] _bindings = Array.Empty<LocalizationClauseBindingRecord>();
    ReviewerCommentRecord[] _comments = Array.Empty<ReviewerCommentRecord>();
    LocalizationPropertySpecRecord[] _specs = Array.Empty<LocalizationPropertySpecRecord>();
    string _changeListId = "";
    LocalizationChangeListItemRecord[] _cachedRequired = Array.Empty<LocalizationChangeListItemRecord>();
    LocalizationChangeListItemRecord[] _cachedWarnings = Array.Empty<LocalizationChangeListItemRecord>();
    Vector2 _sideScroll;
    DraftEpisodeRecord[] _draftEpisodes = Array.Empty<DraftEpisodeRecord>();
    int _draftPopupIndex;
    bool _showManualDraftId;

    public static void Open(string draftId = null, string reviewId = null)
    {
        var w = GetWindow<ContinuumScriptEditorWindow>("Script Editor");
        w.minSize = new Vector2(900, 560);
        if (!string.IsNullOrEmpty(draftId)) w._draftId = draftId;
        if (!string.IsNullOrEmpty(reviewId)) w._reviewId = reviewId;
        w.Show();
        _ = w.RefreshDraftListAsync();
        w.LoadDraft();
    }

    [MenuItem("Window/Continuum/Script Editor")]
    public static void OpenMenu() => Open();

    [MenuItem("Window/Continuum/Script Editor + Lemma Properties")]
    public static void OpenWithLemmaViewer()
    {
        Open();
        VocabularyLemmaPropertyEditorWindow.Open();
    }

    [MenuItem("Window/Continuum/Change Lists")]
    public static void OpenChangeListAlias()
    {
        Open();
    }

    void OnEnable() => _ = RefreshDraftListAsync();

    async Task RefreshDraftListAsync()
    {
        try
        {
            var all = await ContinuumEditorLocalizationClient.Instance.GetDraftEpisodesAsync();
            _draftEpisodes = all.Where(d => d != null && !d.IsCommitted).OrderByDescending(d => d.updatedAt).ToArray();
            if (!string.IsNullOrEmpty(_draftId))
            {
                var idx = Array.FindIndex(_draftEpisodes, d => d.id == _draftId);
                if (idx >= 0) _draftPopupIndex = idx;
            }
            else if (_draftEpisodes.Length > 0 && string.IsNullOrEmpty(_draftId))
            {
                _draftPopupIndex = 0;
                _draftId = _draftEpisodes[0].id;
            }
            Repaint();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Script Editor: failed to list drafts — {ex.Message}");
        }
    }

    void OnDisable() => _webHost?.Dispose();

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawMainEditor();
        DrawSidePanel();
        EditorGUILayout.EndHorizontal();
        ContinuumChangeListModal.DrawModal();
        ContinuumClauseAttachDialog.DrawModal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (_draftEpisodes.Length > 0 && !_showManualDraftId)
        {
            var labels = _draftEpisodes.Select(d => d.DisplayLabel).ToArray();
            var next = EditorGUILayout.Popup(_draftPopupIndex, labels, GUILayout.MinWidth(200));
            if (next != _draftPopupIndex && next >= 0 && next < _draftEpisodes.Length)
            {
                _draftPopupIndex = next;
                _draftId = _draftEpisodes[next].id;
                LoadDraft();
            }
        }
        else
        {
            _draftId = EditorGUILayout.TextField(_draftId, GUILayout.Width(160));
        }
        if (GUILayout.Button(_showManualDraftId ? "▼" : "✎", EditorStyles.toolbarButton, GUILayout.Width(22)))
            _showManualDraftId = !_showManualDraftId;
        if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(22)))
            _ = RefreshDraftListAsync();
        _reviewId = EditorGUILayout.TextField(_reviewId, GUILayout.Width(120));
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
            LoadDraft();
        GUI.enabled = !_readOnly && !string.IsNullOrEmpty(_draftId);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
            SaveDraft();
        if (GUILayout.Button("Apply edit", EditorStyles.toolbarButton, GUILayout.Width(70)))
            ApplyEdit();
        GUI.enabled = !_readOnly && !string.IsNullOrEmpty(_draftId);
        if (GUILayout.Button("Attach clause", EditorStyles.toolbarButton, GUILayout.Width(90)))
            OpenAttachClauseDialog();
        GUI.enabled = true;
        if (GUILayout.Button("Submit CL", EditorStyles.toolbarButton, GUILayout.Width(70)))
            SubmitChangeList();
        if (GUILayout.Button("Hub", EditorStyles.toolbarButton, GUILayout.Width(40)))
            Application.OpenURL($"{ContinuumEditorSession.ApiBaseUrl}/#review?draftId={_draftId}");
        GUILayout.FlexibleSpace();
        _useWebView = GUILayout.Toggle(_useWebView && ContinuumWebViewHost.IsAvailable, "WebView", EditorStyles.toolbarButton);
        if (GUILayout.Button("Karaoke", EditorStyles.toolbarButton, GUILayout.Width(60)))
            ScriptKaraokeEditorWindow.Open();
        EditorGUILayout.EndHorizontal();
    }

    void DrawMainEditor()
    {
        var mainRect = GUILayoutUtility.GetRect(position.width * 0.68f, position.height - 60, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (_useWebView && ContinuumWebViewHost.IsAvailable)
        {
            if (_webHost == null)
            {
                _webHost = ContinuumWebViewHost.TryCreate(mainRect, OnWebMessage);
                _webHost?.LoadBundledHost();
                _webHost?.MountEditor(_loadedText, _readOnly);
            }
            _webHost.Draw(mainRect);
        }
        else
        {
            var spans = ContinuumScriptSpanOverlayModel.Build(_loadedText, _bindings, _comments);
            _richEditor.SetContent(_loadedText, spans, _readOnly);
            _loadedText = _richEditor.Draw(mainRect);
        }
    }

    void DrawSidePanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.28f));
        _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
        EditorGUILayout.LabelField("Property specs", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh specs"))
            RefreshSpecs();
        foreach (var s in _specs.Take(12))
            EditorGUILayout.LabelField(s?.key ?? "", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Change list", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(string.IsNullOrEmpty(_changeListId) ? "(none)" : _changeListId, EditorStyles.wordWrappedLabel);
        if (!string.IsNullOrEmpty(_changeListId) && GUILayout.Button("Open change list modal"))
            OpenChangeListModal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Review", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_readOnly ? "Read-only (review/committed)" : "Editing", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    async void LoadDraft()
    {
        if (string.IsNullOrEmpty(_draftId) && string.IsNullOrEmpty(_reviewId)) return;
        if (!string.IsNullOrEmpty(_reviewId))
            await LoadReviewOrDraft();
        else
            await LoadDraftOnly();
    }

    async Task LoadDraftOnly()
    {
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_draftId);
        _loadedText = draft?.scriptText ?? "";
        _originalText = _loadedText;
        _draftScriptId = draft?.id ?? "";
        _bindings = await ContinuumEditorLocalizationClient.Instance.GetClauseBindingsAsync(_draftId);
        _readOnly = false;
        await RefreshChangeListFromDraft();
        _webHost?.Dispose();
        _webHost = null;
        ContinuumNotificationConsoleSink.Log("editor", $"Loaded draft {_draftId}");
        Repaint();
    }

    async Task LoadReviewOrDraft()
    {
        if (string.IsNullOrEmpty(_draftId) && !string.IsNullOrEmpty(_reviewId))
        {
            var rev = await ContinuumEditorLocalizationClient.Instance.CallRawAsync("GET", $"/api/reviews/{Uri.EscapeDataString(_reviewId)}", null);
            if (rev.success && !string.IsNullOrEmpty(rev.json))
            {
                var wrapper = JsonUtility.FromJson<ReviewDraftWrapper>(rev.json);
                if (!string.IsNullOrEmpty(wrapper?.draftEpisodeId))
                    _draftId = wrapper.draftEpisodeId;
            }
        }
        if (string.IsNullOrEmpty(_draftId)) return;
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_draftId);
        _loadedText = draft?.scriptText ?? "";
        _originalText = _loadedText;
        _draftScriptId = draft?.id ?? "";
        _bindings = await ContinuumEditorLocalizationClient.Instance.GetClauseBindingsAsync(_draftId);
        var cl = await ContinuumEditorLocalizationClient.Instance.GetActiveChangeListForDraftAsync(_draftId);
        _changeListId = cl?.id ?? _changeListId;
        _readOnly = cl != null && (cl.workflowStatus == "in_review" || cl.workflowStatus == "submitted");
        if (cl?.items != null && cl.items.Length > 0)
            SplitChangeListItems(cl.items, out _cachedRequired, out _cachedWarnings);
        _webHost?.Dispose();
        _webHost = null;
        ContinuumNotificationConsoleSink.Log("editor", $"Loaded review {_reviewId} draft {_draftId}");
        Repaint();
    }

    async Task RefreshChangeListFromDraft()
    {
        if (string.IsNullOrEmpty(_draftId)) return;
        var cl = await ContinuumEditorLocalizationClient.Instance.GetActiveChangeListForDraftAsync(_draftId);
        _changeListId = cl?.id ?? "";
        if (cl?.items != null && cl.items.Length > 0)
            SplitChangeListItems(cl.items, out _cachedRequired, out _cachedWarnings);
    }

    void OpenAttachClauseDialog()
    {
        if (_readOnly || string.IsNullOrEmpty(_draftId)) return;
        if (_specs == null || _specs.Length == 0)
            RefreshSpecs();
        var (start, end, text) = _richEditor.GetSelection();
        if (end <= start)
        {
            EditorUtility.DisplayDialog("Attach clause", "Select text in the script editor first.", "OK");
            return;
        }
        var farey = FareySpanUtility.CharRangeToFareySpan(_loadedText, start, end);
        var clauseRef = new ClauseRefRecord
        {
            charStart = start,
            charEnd = end,
            selectionText = text,
            draftScriptId = _draftScriptId,
            draftEpisodeId = _draftId,
            fareyLeftNum = farey.ln,
            fareyLeftDen = farey.ld,
            fareyRightNum = farey.rn,
            fareyRightDen = farey.rd,
        };
        ContinuumClauseAttachDialog.Open(clauseRef, _loadedText, _specs, AttachClauseAsync);
    }

    async Task AttachClauseAsync(ClauseRefRecord clauseRef, string bindingKind, string propertyKey, string propertyValue, string scriptText)
    {
        await ContinuumEditorLocalizationClient.Instance.PostClauseBindingAsync(
            clauseRef, bindingKind, propertyKey, propertyValue, scriptText);
        _bindings = await ContinuumEditorLocalizationClient.Instance.GetClauseBindingsAsync(_draftId);
        ContinuumNotificationConsoleSink.Log("attach", $"Attached {bindingKind} {propertyKey}");
        Repaint();
    }

    async void RefreshSpecs()
    {
        _specs = await ContinuumEditorLocalizationClient.Instance.GetPropertySpecsAsync();
        Repaint();
    }

    async void SaveDraft()
    {
        await ContinuumEditorLocalizationClient.Instance.PutDraftScriptAsync(_draftId, _loadedText);
        _originalText = _loadedText;
        ContinuumNotificationConsoleSink.Log("save", $"Saved draft {_draftId}");
    }

    async void ApplyEdit()
    {
        var result = await ContinuumEditorLocalizationClient.Instance.ApplyScriptEditAsync(_draftId, _originalText, _loadedText);
        _changeListId = result?.changeListId ?? _changeListId;
        _cachedRequired = result?.required ?? Array.Empty<LocalizationChangeListItemRecord>();
        _cachedWarnings = result?.warnings ?? Array.Empty<LocalizationChangeListItemRecord>();
        ContinuumChangeListModal.Open(_changeListId, null, _cachedRequired, _cachedWarnings, OnChangeListSave, OnChangeListSubmit);
        ContinuumNotificationConsoleSink.Log("apply-edit", $"Apply edit → CL {_changeListId}");
        Repaint();
    }

    async void OpenChangeListModal()
    {
        var list = await ContinuumEditorLocalizationClient.Instance.GetChangeListAsync(_changeListId) as LocalizationChangeListDetailRecord;
        SplitChangeListItems(list?.items, out var required, out var warnings);
        if (required.Length > 0) _cachedRequired = required;
        if (warnings.Length > 0) _cachedWarnings = warnings;
        ContinuumChangeListModal.Open(_changeListId, list, _cachedRequired, _cachedWarnings, OnChangeListSave, OnChangeListSubmit);
    }

    static void SplitChangeListItems(LocalizationChangeListItemRecord[] items, out LocalizationChangeListItemRecord[] required, out LocalizationChangeListItemRecord[] warnings)
    {
        if (items == null || items.Length == 0)
        {
            required = Array.Empty<LocalizationChangeListItemRecord>();
            warnings = Array.Empty<LocalizationChangeListItemRecord>();
            return;
        }
        var req = new List<LocalizationChangeListItemRecord>();
        var warn = new List<LocalizationChangeListItemRecord>();
        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.severity == "required") req.Add(item);
            else warn.Add(item);
        }
        required = req.ToArray();
        warnings = warn.ToArray();
    }

    async void OnChangeListSave(string id, LocalizationChangeListRecord data)
    {
        var items = MergeChangeListItems(_cachedRequired, _cachedWarnings);
        await ContinuumEditorLocalizationClient.Instance.SaveChangeListAsync(id, items);
        await ContinuumEditorLocalizationClient.Instance.PutDraftScriptAsync(_draftId, _loadedText);
        _originalText = _loadedText;
        ContinuumNotificationConsoleSink.Log("save", $"Change list saved {id}");
    }

    static LocalizationChangeListItemRecord[] MergeChangeListItems(
        LocalizationChangeListItemRecord[] required,
        LocalizationChangeListItemRecord[] warnings)
    {
        var list = new List<LocalizationChangeListItemRecord>();
        if (required != null) list.AddRange(required);
        if (warnings != null) list.AddRange(warnings);
        return list.ToArray();
    }

    [Serializable]
    class ReviewDraftWrapper { public string draftEpisodeId; }

    async void OnChangeListSubmit(string id)
    {
        await ContinuumEditorLocalizationClient.Instance.SubmitChangeListForReviewAsync(id);
        _readOnly = true;
        ContinuumNotificationConsoleSink.Log("submit", $"Submitted {id} for review");
        Repaint();
    }

    void SubmitChangeList()
    {
        if (string.IsNullOrEmpty(_changeListId))
            ApplyEdit();
        else
            OpenChangeListModal();
    }

    async void OnWebMessage(string json)
    {
        var resp = await ContinuumEditorBridge.HandleAsync(json);
        _webHost?.DeliverBridgeResponse(ContinuumEditorBridge.ToJson(resp));
        Repaint();
    }
}

#endif
