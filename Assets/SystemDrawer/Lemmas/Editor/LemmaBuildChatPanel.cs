#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Model Chat panel for lemma build refinement.</summary>
public sealed class LemmaBuildChatPanel
{
    readonly LemmaBuildChatSession _session;
    readonly LemmaBuildFormState _form;
    readonly LemmaBuildSettings _settings;

    Vector2 _historyScroll;
    string _inputText = "";
    bool _thinking;
    string _statusLine = "";
    string _applyHelpBox;

    public LemmaBuildChatPanel(LemmaBuildChatSession session, LemmaBuildFormState form, LemmaBuildSettings settings)
    {
        _session = session;
        _form = form;
        _settings = settings;
    }

    public void Draw(EditorWindow host)
    {
        EditorGUILayout.LabelField("Model Chat", EditorStyles.boldLabel);

        DrawStatusStrip();
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(_form.ContextChipLabel(), EditorStyles.helpBox);

        _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.MinHeight(180), GUILayout.ExpandHeight(true));
        if (_session.Messages.Count == 0)
            EditorGUILayout.LabelField("No messages yet. Ask about tier, composition, or properties.", EditorStyles.miniLabel);
        else
        {
            foreach (var msg in _session.Messages)
                DrawMessageBubble(msg);
        }
        if (_thinking)
            EditorGUILayout.LabelField("Thinking…", EditorStyles.boldLabel);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        EditorGUI.BeginDisabledGroup(_thinking);
        _inputText = EditorGUILayout.TextArea(_inputText, GUILayout.MinHeight(48));
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Send", GUILayout.Width(72)))
            Send(host);
        if (GUILayout.Button("Clear", GUILayout.Width(72)))
            ClearChat();
        EditorGUILayout.EndHorizontal();

        var canApply = _session.TryParseLastDescriptor(out _);
        EditorGUI.BeginDisabledGroup(_thinking || !canApply);
        if (GUILayout.Button("Apply to build form"))
            ApplyToForm();
        EditorGUI.EndDisabledGroup();
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(_applyHelpBox))
            EditorGUILayout.HelpBox(_applyHelpBox, MessageType.Info);
    }

    void DrawStatusStrip()
    {
        var model = string.IsNullOrEmpty(_session.ModelId) ? _settings.defaultModelId : _session.ModelId;
        var tokens = LemmaBuildLmStudioClient.EstimateTokenCount(_session, _form.ToSnapshot());
        var status = _thinking ? "Thinking…" : (string.IsNullOrEmpty(_statusLine) ? "Ready" : _statusLine);
        EditorGUILayout.LabelField($"Model: {model}  ·  ~{tokens} tokens  ·  {status}", EditorStyles.miniLabel);
    }

    static void DrawMessageBubble(LemmaBuildChatMessage msg)
    {
        if (msg == null)
            return;
        var isUser = string.Equals(msg.role, "user", System.StringComparison.OrdinalIgnoreCase);
        var style = isUser ? EditorStyles.helpBox : EditorStyles.textArea;
        var label = isUser ? "You" : "Assistant";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.TextArea(msg.content ?? "", style, GUILayout.ExpandHeight(false));
        EditorGUILayout.Space(6);
    }

    void Send(EditorWindow host)
    {
        if (string.IsNullOrWhiteSpace(_inputText))
            return;
        _session.ModelId = string.IsNullOrEmpty(_session.ModelId) ? _settings.defaultModelId : _session.ModelId;
        _session.AppendUser(_inputText);
        var userCopy = _inputText;
        _inputText = "";
        _thinking = true;
        _statusLine = "Waiting for LM Studio…";
        host.Repaint();

        try
        {
            if (!LemmaBuildLmStudioClient.SendChat(_session, _form.ToSnapshot(), _settings, out var response, out var error))
            {
                _statusLine = error ?? "Request failed.";
                EditorUtility.DisplayDialog("Lemma Build Chat", _statusLine, "OK");
            }
            else
            {
                _session.AppendAssistant(response);
                _statusLine = "Response received.";
            }
        }
        finally
        {
            _thinking = false;
            host.Repaint();
        }
    }

    void ClearChat()
    {
        if (!EditorUtility.DisplayDialog("Lemma Build Chat", "Clear chat history for this lemma?", "Clear", "Cancel"))
            return;
        _session.Clear();
        _applyHelpBox = "";
        _statusLine = "Chat cleared.";
    }

    void ApplyToForm()
    {
        _applyHelpBox = "";
        if (!_session.TryParseLastDescriptor(out var descriptor))
        {
            EditorUtility.DisplayDialog("Lemma Build Chat", "No LemmaMechanismDescriptor found in the last assistant message.", "OK");
            return;
        }

        _form.ApplyDescriptor(descriptor, out var warnings);
        if (warnings != null && warnings.Length > 0)
        {
            foreach (var w in warnings)
                Debug.LogWarning("[LemmaBuild] " + w);
            _applyHelpBox = "Applied descriptor from chat with warnings:\n" + string.Join("\n", warnings) +
                            "\n\nReview, then Start build or edit further.";
        }
        else
        {
            _applyHelpBox = "Applied descriptor from chat. Review, then Start build or edit further.";
        }
    }

    public void OnLemmaChanged(string lemma)
    {
        _session.SetLemmaSlug(lemma);
        _applyHelpBox = "";
    }
}
#endif
