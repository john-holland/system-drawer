#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>WebView security spike — bundled host + C# bridge round-trip for apply-edit.</summary>
public sealed class ContinuumWebViewSpikeWindow : EditorWindow
{
    string _draftId = "draft-spike";
    string _oldText = "Hello {P:name}";
    string _newText = "Hello {P:name|formal=true}";
    string _log = "";
    ContinuumWebViewHost _host;
    bool _useWebView = true;

    [MenuItem("Window/Continuum/WebView Spike")]
    public static void Open()
    {
        var w = GetWindow<ContinuumWebViewSpikeWindow>("WebView Spike");
        w.minSize = new Vector2(640, 480);
    }

    void OnDisable() => _host?.Dispose();

    void OnGUI()
    {
        EditorGUILayout.LabelField("Continuum WebView Spike", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Loads local editor-host.html only. API calls route through ContinuumEditorBridge (no credentials in browser).",
            MessageType.Info);

        _useWebView = EditorGUILayout.Toggle("Try WebView embed", _useWebView && ContinuumWebViewHost.IsAvailable);
        if (!ContinuumWebViewHost.IsAvailable)
            EditorGUILayout.HelpBox("UnityEditor.WebView not found on this Unity build — use UIToolkit fallback in Script Editor.", MessageType.Warning);

        _draftId = EditorGUILayout.TextField("Draft ID", _draftId);
        _oldText = EditorGUILayout.TextField("Old text", _oldText);
        _newText = EditorGUILayout.TextField("New text", _newText);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bridge apply-edit (C#)"))
            RunBridgeApplyEdit();
        if (GUILayout.Button("Simulate JS → C# message"))
            SimulateJsMessage();
        EditorGUILayout.EndHorizontal();

        if (_useWebView && ContinuumWebViewHost.IsAvailable)
        {
            var hostRect = GUILayoutUtility.GetRect(position.width - 20, 220);
            if (_host == null)
            {
                _host = ContinuumWebViewHost.TryCreate(hostRect, OnWebMessage);
                _host?.LoadBundledHost();
            }
            _host?.Draw(hostRect);
        }
        else
        {
            EditorGUILayout.LabelField("Fallback preview (no WebView)", EditorStyles.miniBoldLabel);
            EditorGUILayout.TextArea(_newText, GUILayout.Height(120));
        }

        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
    }

    async void RunBridgeApplyEdit()
    {
        var body = JsonUtility.ToJson(new ApplyBody { oldText = _oldText, newText = _newText });
        var reqJson = JsonUtility.ToJson(new ContinuumEditorBridge.BridgeRequest
        {
            action = "api",
            requestId = "spike-1",
            method = "POST",
            path = $"/api/scripts/{_draftId}/apply-edit",
            body = body
        });
        var resp = await ContinuumEditorBridge.HandleAsync(reqJson);
        _log += $"apply-edit ok={resp.ok} data={resp.data} err={resp.error}\n";
        _host?.DeliverBridgeResponse(ContinuumEditorBridge.ToJson(resp));
        Repaint();
    }

    async void SimulateJsMessage()
    {
        var reqJson = JsonUtility.ToJson(new ContinuumEditorBridge.BridgeRequest
        {
            action = "api",
            requestId = "sim-1",
            method = "GET",
            path = "/api/thesaurus/property-specs"
        });
        var resp = await ContinuumEditorBridge.HandleAsync(reqJson);
        _log += $"property-specs ok={resp.ok}\n";
        Repaint();
    }

    void OnWebMessage(string json)
    {
        _log += $"JS: {json}\n";
        HandleBridgeMessage(json);
    }

    async void HandleBridgeMessage(string json)
    {
        var resp = await ContinuumEditorBridge.HandleAsync(json);
        _host?.DeliverBridgeResponse(ContinuumEditorBridge.ToJson(resp));
        Repaint();
    }

    [System.Serializable]
    class ApplyBody { public string oldText; public string newText; }
}

#endif
