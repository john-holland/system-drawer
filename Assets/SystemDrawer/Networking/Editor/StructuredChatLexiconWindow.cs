#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Load/save per-product chat lexicon from Continuuuum (not Continuuuum.Runtime).</summary>
public sealed class StructuredChatLexiconWindow : EditorWindow
{
    string _productId = "default";
    string _composeMode = "preview";
    string _status = "";
    Vector2 _scroll;
    readonly List<ChatLexiconWord> _words = new List<ChatLexiconWord>();

    [MenuItem("Window/System Drawer/Networking/Structured Chat Lexicon")]
    public static void Open()
    {
        GetWindow<StructuredChatLexiconWindow>("Chat Lexicon");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Per-Saurce-product whitelist for game multiplayer structured chat. Continuuuum editor/web chat stays unrated and is not this surface.",
            MessageType.Info);
        _productId = EditorGUILayout.TextField("Product Id", _productId);
        int modeIndex = string.Equals(_composeMode, "sendButton", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        modeIndex = EditorGUILayout.Popup("Compose Mode", modeIndex, new[] { "preview", "sendButton" });
        _composeMode = modeIndex == 1 ? "sendButton" : "preview";
        EditorGUILayout.LabelField("API", ApiBase());

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load"))
            LoadFromApi();
        if (GUILayout.Button("Save"))
            SaveToApi();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add Word"))
            _words.Add(new ChatLexiconWord { id = "word", text = "word" });

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _words.Count; i++)
        {
            var w = _words[i] ?? new ChatLexiconWord();
            EditorGUILayout.BeginHorizontal();
            w.id = EditorGUILayout.TextField(w.id ?? "", GUILayout.Width(120));
            w.text = EditorGUILayout.TextField(w.text ?? "");
            w.lemmaEntryId = EditorGUILayout.TextField(w.lemmaEntryId ?? "", GUILayout.Width(140));
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                _words.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                continue;
            }
            _words[i] = w;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.None);
    }

    void LoadFromApi()
    {
        string url = ApiBase() + "/api/chat/lexicon?productId=" + UnityWebRequest.EscapeURL(_productId ?? "");
        using var req = UnityWebRequest.Get(url);
        req.SendWebRequest();
        while (!req.isDone) { }
        if (req.result != UnityWebRequest.Result.Success)
        {
            _status = "Load failed: " + req.error + " " + req.downloadHandler.text;
            return;
        }
        var doc = JsonUtility.FromJson<LexiconDoc>(req.downloadHandler.text);
        if (doc == null)
        {
            _status = "Load failed: empty document";
            return;
        }
        _composeMode = string.IsNullOrEmpty(doc.composeMode) ? "preview" : doc.composeMode;
        _words.Clear();
        if (doc.lexicon != null && doc.lexicon.words != null)
            _words.AddRange(doc.lexicon.words);
        _status = "Loaded " + _words.Count + " words";
    }

    void SaveToApi()
    {
        var doc = new LexiconDoc
        {
            productId = _productId,
            composeMode = _composeMode,
            lexicon = new ChatLexiconData { words = _words.ToArray() }
        };
        string json = JsonUtility.ToJson(doc);
        string url = ApiBase() + "/api/chat/lexicon?productId=" + UnityWebRequest.EscapeURL(_productId ?? "");
        using var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SendWebRequest();
        while (!req.isDone) { }
        if (req.result != UnityWebRequest.Result.Success)
        {
            _status = "Save failed: " + req.error + " " + req.downloadHandler.text;
            return;
        }
        _status = "Saved";
        ApplyToSceneRagdoll();
    }

    void ApplyToSceneRagdoll()
    {
        var words = _words.ToArray();
        var ragdolls = Object.FindObjectsByType<StructuredChatRagdoll>(FindObjectsSortMode.None);
        for (int i = 0; i < ragdolls.Length; i++)
        {
            ragdolls[i].ApplyLexicon(words, _composeMode);
            EditorUtility.SetDirty(ragdolls[i]);
        }
        var gens = Object.FindObjectsByType<StructuredChatSpatialGenerator>(FindObjectsSortMode.None);
        for (int i = 0; i < gens.Length; i++)
        {
            gens[i].lexiconWords = words;
            gens[i].UpdateForLexicon();
            EditorUtility.SetDirty(gens[i]);
        }
    }

    static string ApiBase()
    {
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_api_url.txt");
        try
        {
            if (File.Exists(p))
            {
                var s = File.ReadAllText(p).Trim();
                if (!string.IsNullOrEmpty(s))
                    return s.TrimEnd('/');
            }
        }
        catch (IOException)
        {
        }
        return "http://localhost:5050";
    }

    [System.Serializable]
    sealed class LexiconDoc
    {
        public string productId;
        public string composeMode;
        public ChatLexiconData lexicon;
    }
}

#endif
