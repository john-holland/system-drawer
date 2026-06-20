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

    public static void Open(string draftId = null, string reviewId = null)
    {
        var w = GetWindow<ContinuumScriptEditorWindow>("Script Editor");
        w.minSize = new Vector2(900, 560);
        if (!string.IsNullOrEmpty(draftId)) w._draftId = draftId;
        if (!string.IsNullOrEmpty(reviewId)) w._reviewId = reviewId;
        w.Show();
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

    void OnDisable() => _webHost?.Dispose();

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawMainEditor();
        DrawSidePanel();
        EditorGUILayout.EndHorizontal();
        ContinuumChangeListModal.DrawModal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _draftId = EditorGUILayout.TextField(_draftId, GUILayout.Width(160));
        _reviewId = EditorGUILayout.TextField(_reviewId, GUILayout.Width(120));
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
            LoadDraft();
        GUI.enabled = !_readOnly && !string.IsNullOrEmpty(_draftId);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
            SaveDraft();
        if (GUILayout.Button("Apply edit", EditorStyles.toolbarButton, GUILayout.Width(70)))
            ApplyEdit();
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
        if (string.IsNullOrEmpty(_draftId)) return;
        var draft = await ContinuumEditorLocalizationClient.Instance.GetDraftScriptAsync(_draftId);
        _loadedText = draft?.scriptText ?? "";
        _originalText = _loadedText;
        _bindings = await ContinuumEditorLocalizationClient.Instance.GetClauseBindingsAsync(_draftId);
        _readOnly = false;
        _webHost?.Dispose();
        _webHost = null;
        ContinuumNotificationConsoleSink.Log("editor", $"Loaded draft {_draftId}");
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
        await ContinuumEditorLocalizationClient.Instance.SaveChangeListAsync(id);
        await ContinuumEditorLocalizationClient.Instance.PutDraftScriptAsync(_draftId, _loadedText);
        _originalText = _loadedText;
        ContinuumNotificationConsoleSink.Log("save", $"Change list saved {id}");
    }

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
