#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Reflection wrapper for Unity internal Editor WebView (platform/version dependent).</summary>
public sealed class ContinuumWebViewHost : IDisposable
{
    readonly object _webView;
    readonly MethodInfo _loadUrl;
    readonly MethodInfo _executeJs;
    readonly Action<string> _onMessage;

    public static bool IsAvailable { get; private set; }
    public bool IsCreated => _webView != null;

    static ContinuumWebViewHost()
    {
        IsAvailable = Type.GetType("UnityEditor.WebView, UnityEditor") != null
                      || Type.GetType("UnityEditor.MacWebView, UnityEditor") != null;
    }

    ContinuumWebViewHost(object webView, MethodInfo loadUrl, MethodInfo executeJs, Action<string> onMessage)
    {
        _webView = webView;
        _loadUrl = loadUrl;
        _executeJs = executeJs;
        _onMessage = onMessage;
    }

    public static ContinuumWebViewHost TryCreate(Rect hostRect, Action<string> onMessageFromJs)
    {
        Type webViewType = Type.GetType("UnityEditor.WebView, UnityEditor")
                           ?? Type.GetType("UnityEditor.MacWebView, UnityEditor");
        if (webViewType == null)
            return null;

        try
        {
            object instance = Activator.CreateInstance(webViewType, new object[] { hostRect });
            MethodInfo loadUrl = webViewType.GetMethod("LoadURL", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo executeJs = webViewType.GetMethod("ExecuteJavascript", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                   ?? webViewType.GetMethod("ExecuteJavaScript", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (loadUrl == null)
                return null;
            return new ContinuumWebViewHost(instance, loadUrl, executeJs, onMessageFromJs);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Continuum] WebView create failed: {ex.Message}");
            return null;
        }
    }

    public void Draw(Rect rect)
    {
        if (_webView == null) return;
        var setSize = _webView.GetType().GetMethod("SetSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        setSize?.Invoke(_webView, new object[] { (int)rect.width, (int)rect.height });
        var setHostView = _webView.GetType().GetMethod("SetHostView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        setHostView?.Invoke(_webView, null);
    }

    public void LoadBundledHost()
    {
        string path = GetBundledHostPath();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Continuum] Missing bundled editor-host.html at {path}");
            return;
        }
        string url = new Uri(path).AbsoluteUri;
        _loadUrl?.Invoke(_webView, new object[] { url });
        InjectBridgeBootstrap();
    }

    public void InjectBridgeBootstrap()
    {
        if (_executeJs == null) return;
        const string bootstrap = @"(function(){
          if(!window.unityBridge) window.unityBridge={};
          window.unityBridge.postMessage=function(msg){
            if(window.external&&window.external.invoke) window.external.invoke(msg);
          };
        })();";
        _executeJs.Invoke(_webView, new object[] { bootstrap });
    }

    public void DeliverBridgeResponse(string json)
    {
        if (_executeJs == null || string.IsNullOrEmpty(json)) return;
        string escaped = json.Replace("\\", "\\\\").Replace("'", "\\'");
        _executeJs.Invoke(_webView, new object[] { $"window.unityBridge && window.unityBridge.deliverResponse('{escaped}');" });
    }

    public void MountEditor(string scriptText, bool readOnly, string draftEpisodeId = null, string draftScriptId = null)
    {
        if (_executeJs == null) return;
        string escaped = EscapeJsString(scriptText ?? "");
        string draftEsc = EscapeJsString(draftEpisodeId ?? "");
        string scriptIdEsc = EscapeJsString(draftScriptId ?? "");
        string js = $@"window.continuumHost && window.continuumHost.mount({{
          scriptText: '{escaped}',
          draftEpisodeId: '{draftEsc}',
          draftId: '{draftEsc}',
          draftScriptId: '{scriptIdEsc}',
          readOnly: {(readOnly ? "true" : "false")},
          mode: '{(readOnly ? "review" : "edit")}'
        }});";
        _executeJs.Invoke(_webView, new object[] { js });
    }

    public void TriggerMayorDogModSlot()
    {
        if (_executeJs == null) return;
        _executeJs.Invoke(_webView, new object[] { "window.continuumHost && window.continuumHost._inst && ContinuumScriptEditor.markMayorDogModSlot(window.continuumHost._inst);" });
    }

    static string EscapeJsString(string s)
    {
        return (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
    }

    public void NotifyMessage(string json) => _onMessage?.Invoke(json);

    public static string GetBundledHostPath()
    {
        string rel = "Assets/Continuum/Editor/ScriptEditor/WebView/editor-host.html";
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), rel));
    }

    public void Dispose()
    {
        if (_webView == null) return;
        var destroy = _webView.GetType().GetMethod("Destroy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        destroy?.Invoke(_webView, null);
    }
}

#endif
