#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Lemma Build tab: form + queue stub + model chat.</summary>
public sealed class LemmaBuildTabController
{
    readonly LemmaBuildFormState _form = new LemmaBuildFormState();
    readonly LemmaBuildSettings _settings;
    readonly LemmaBuildChatSession _chatSession = new LemmaBuildChatSession();
    LemmaBuildChatPanel _chatPanel;

    Vector2 _formScroll;
    Vector2 _queueScroll;
    bool _showAdmin;
    string _lastLemmaSlug = "";

    public LemmaBuildTabController()
    {
        _settings = LemmaBuildSettings.LoadOrCreate();
        _chatSession.ModelId = _settings.defaultModelId;
        _chatSession.Load();
        _chatPanel = new LemmaBuildChatPanel(_chatSession, _form, _settings);
    }

    public void Draw(EditorWindow host)
    {
        SyncLemmaSession();

        var stacked = host.position.width < 820f;
        if (stacked)
        {
            _formScroll = EditorGUILayout.BeginScrollView(
                _formScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawBuildForm();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(8);
            _chatPanel.Draw(host);
        }
        else
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            _formScroll = EditorGUILayout.BeginScrollView(
                _formScroll,
                GUILayout.Width(host.position.width * 0.48f),
                GUILayout.ExpandHeight(true));
            DrawBuildForm();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _chatPanel.Draw(host);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }

    void SyncLemmaSession()
    {
        var slug = LemmaBuildSessionPaths.Slugify(_form.lemma);
        if (slug == _lastLemmaSlug)
            return;
        _lastLemmaSlug = slug;
        _chatPanel.OnLemmaChanged(_form.lemma);
    }

    void DrawBuildForm()
    {
        EditorGUILayout.LabelField("Lemma Build", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Explore composition-first tiers in Model Chat, then Apply to populate this form. Start build materializes bundles (future).",
            MessageType.Info);

        _form.lemma = EditorGUILayout.TextField("Lemma / phrase", _form.lemma);
        _form.posTag = EditorGUILayout.TextField("Part of speech", _form.posTag);
        _form.mechanicalRole = (LemmaMechanicalRole)EditorGUILayout.EnumPopup("Mechanical role", _form.mechanicalRole);
        _form.outputTier = EditorGUILayout.IntSlider("Output tier", _form.outputTier, 0, 2);
        _form.functionalDescription = EditorGUILayout.TextField("Functional description", _form.functionalDescription);
        _form.mechanismPrompt = EditorGUILayout.TextArea(_form.mechanismPrompt, GUILayout.MinHeight(48));
        _form.synonymsCsv = EditorGUILayout.TextField("Synonyms (CSV)", _form.synonymsCsv);

        DrawCompositionFields();
        DrawPropertiesFields();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Build queue", EditorStyles.boldLabel);
        _queueScroll = EditorGUILayout.BeginScrollView(_queueScroll, GUILayout.MaxHeight(80));
        EditorGUILayout.LabelField("No jobs queued.", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
        GUI.enabled = false;
        GUILayout.Button("Start build");
        GUI.enabled = true;

        DrawAdminSettings();

        if (!string.IsNullOrEmpty(_form.ApplyStatusMessage))
            EditorGUILayout.HelpBox(_form.ApplyStatusMessage, MessageType.Info);
    }

    void DrawCompositionFields()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Composition children", EditorStyles.boldLabel);
        var children = _form.compositionChildren ?? System.Array.Empty<LemmaCompositionChildPutDto>();
        if (children.Length == 0)
            EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
        for (int i = 0; i < children.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            children[i].entryId = EditorGUILayout.TextField(children[i].entryId ?? "");
            children[i].sortOrder = EditorGUILayout.IntField(children[i].sortOrder, GUILayout.Width(48));
            EditorGUILayout.EndHorizontal();
        }
        _form.compositionChildren = children;
        if (GUILayout.Button("Add composition child"))
        {
            var list = new System.Collections.Generic.List<LemmaCompositionChildPutDto>(children)
            {
                new LemmaCompositionChildPutDto { entryId = "", sortOrder = children.Length }
            };
            _form.compositionChildren = list.ToArray();
        }
    }

    void DrawPropertiesFields()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
        var props = _form.properties ?? System.Array.Empty<ThesaurusEntryPropertyRecord>();
        if (props.Length == 0)
            EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
        for (int i = 0; i < props.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            props[i].propertyKey = EditorGUILayout.TextField(props[i].propertyKey ?? "", GUILayout.Width(140));
            props[i].propertyValue = EditorGUILayout.TextField(props[i].propertyValue ?? "");
            EditorGUILayout.EndHorizontal();
        }
        _form.properties = props;
        if (GUILayout.Button("Add property"))
        {
            var list = new System.Collections.Generic.List<ThesaurusEntryPropertyRecord>(props)
            {
                new ThesaurusEntryPropertyRecord { propertyKey = "", propertyValue = "" }
            };
            _form.properties = list.ToArray();
        }
    }

    void DrawAdminSettings()
    {
        EditorGUILayout.Space(8);
        _showAdmin = EditorGUILayout.Foldout(_showAdmin, "Admin settings", true);
        if (!_showAdmin)
            return;

        EditorGUI.indentLevel++;
        _settings.maxConcurrentBuilds = EditorGUILayout.IntSlider("Max concurrent builds", _settings.maxConcurrentBuilds, 0, 16);
        _settings.lmStudioBaseUrl = EditorGUILayout.TextField("LM Studio base URL", _settings.lmStudioBaseUrl);
        _settings.defaultModelId = EditorGUILayout.TextField("Default model id", _settings.defaultModelId);
        _chatSession.ModelId = _settings.defaultModelId;
        if (GUILayout.Button("Save admin settings"))
            _settings.SaveOverrides();
        EditorGUI.indentLevel--;
    }
}
#endif
