#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Play-mode script karaoke view: ±5 words around the current runtime script position.</summary>
public sealed class ScriptKaraokeEditorWindow : EditorWindow
{
    const int KaraokeRadius = 5;

    string _previewScript = "";
    int _previewWordIndex;
    bool _followPlayMode = true;

    [MenuItem("Window/Continuum/Script Karaoke")]
    public static void Open()
    {
        var w = GetWindow<ScriptKaraokeEditorWindow>("Script Karaoke");
        w.minSize = new Vector2(480, 160);
        w.Show();
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            ScriptPlaybackCursor.Clear();
        Repaint();
    }

    void OnEditorUpdate()
    {
        if (Application.isPlaying && _followPlayMode)
            Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Script Karaoke", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "During Play Mode, follows the active script position from AnimationPlaybackPolicyContext (travel/narrative playback). " +
            "Shows ±5 words around the current phrase.",
            MessageType.Info);

        _followPlayMode = EditorGUILayout.Toggle("Follow Play Mode", _followPlayMode);

        if (!Application.isPlaying || !_followPlayMode)
        {
            EditorGUILayout.LabelField("Preview (Edit Mode)", EditorStyles.miniLabel);
            _previewScript = EditorGUILayout.TextArea(_previewScript, GUILayout.MinHeight(48));
            var previewTokens = ScriptTextTokenizer.Tokenize(_previewScript);
            if (previewTokens.Count > 0)
            {
                _previewWordIndex = EditorGUILayout.IntSlider("Preview word", _previewWordIndex, 0, previewTokens.Count - 1);
                DrawKaraokeStrip(_previewScript, _previewWordIndex, false, "", -1);
            }
            else
            {
                EditorGUILayout.LabelField("Enter preview script text or press Play.", EditorStyles.centeredGreyMiniLabel);
            }
            return;
        }

        if (!ScriptPlaybackCursor.IsLive)
        {
            EditorGUILayout.LabelField("Waiting for runtime script cursor…", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Ensure a scene has AnimationPlaybackPolicyContext with script text.", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.LabelField(
            $"Phrase: {(string.IsNullOrEmpty(ScriptPlaybackCursor.ActivePhrase) ? "—" : ScriptPlaybackCursor.ActivePhrase)} · " +
            $"Event {ScriptPlaybackCursor.ActiveEventIndex} · Word {ScriptPlaybackCursor.WordIndex + 1}/{Mathf.Max(1, ScriptPlaybackCursor.WordCount)}",
            EditorStyles.miniLabel);

        DrawKaraokeStrip(
            ScriptPlaybackCursor.ScriptText,
            ScriptPlaybackCursor.WordIndex,
            true,
            ScriptPlaybackCursor.ActivePhrase,
            ScriptPlaybackCursor.ActiveEventIndex);
    }

    void DrawKaraokeStrip(string scriptText, int wordIndex, bool live, string phrase, int eventIndex)
    {
        var tokens = ScriptTextTokenizer.Tokenize(scriptText);
        if (tokens.Count == 0)
        {
            EditorGUILayout.LabelField("No script loaded", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        wordIndex = Mathf.Clamp(wordIndex, 0, tokens.Count - 1);
        var win = ScriptTextTokenizer.Window(tokens, wordIndex, KaraokeRadius);

        var stripRect = GUILayoutUtility.GetRect(position.width - 16, 88);
        EditorGUI.DrawRect(stripRect, new Color(0.1f, 0.11f, 0.15f));

        var dimStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(0.72f, 0.76f, 0.84f) },
            wordWrap = false,
        };
        var currentStyle = new GUIStyle(dimStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };
        var placeholderStyle = new GUIStyle(dimStyle)
        {
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.55f, 0.72f, 1f) },
        };

        float x = stripRect.x + 12;
        float y = stripRect.y + 28;
        float maxX = stripRect.xMax - 12;

        void DrawToken(ScriptTextTokenizer.Token token, bool current)
        {
            GUIStyle s = current ? currentStyle : token.isPlaceholder ? placeholderStyle : dimStyle;
            var content = new GUIContent(token.text);
            Vector2 size = s.CalcSize(content);
            if (x + size.x > maxX && x > stripRect.x + 12)
            {
                x = stripRect.x + 12;
                y += size.y + 4;
            }
            GUI.Label(new Rect(x, y, size.x, size.y), content, s);
            x += size.x + 8;
        }

        if (win.hasMoreBefore)
        {
            GUI.Label(new Rect(x, y, 18, 22), "…", dimStyle);
            x += 22;
        }

        foreach (var t in win.before)
            DrawToken(t, false);
        if (win.current != null)
            DrawToken(win.current, true);
        foreach (var t in win.after)
            DrawToken(t, false);
        if (win.hasMoreAfter)
            GUI.Label(new Rect(x, y, 18, 22), "…", dimStyle);

        if (live)
        {
            var statusRect = new Rect(stripRect.x + 12, stripRect.y + 8, stripRect.width - 24, 18);
            GUI.Label(statusRect, "LIVE", EditorStyles.miniLabel);
        }
    }
}
#endif
