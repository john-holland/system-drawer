using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>HTTP client for Continuuuum game/dimension prewarm and switch APIs.</summary>
public sealed class GameDimensionClient : MonoBehaviour
{
    [Tooltip("Continuuuum API base, e.g. http://127.0.0.1:5050")]
    public string apiBaseUrl = "http://127.0.0.1:5050";
    public string userId = "developer";
    public bool admin = true;
    public string gameSlug = "main";

    public string LastJson { get; private set; }

    public IEnumerator Prewarm(int dimIndex, Action<bool, string> done = null)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/api/gd/sg-prewarm";
        var body = $"{{\"game\":\"{Escape(gameSlug)}\",\"dimension\":{dimIndex}}}";
        yield return PostJson(url, body, done);
    }

    public IEnumerator SwitchDimension(int dimIndex, Action<bool, string> done = null)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/api/gd/dimension-switch";
        var body = $"{{\"game\":\"{Escape(gameSlug)}\",\"dimension\":{dimIndex}}}";
        yield return PostJson(url, body, done);
    }

    public IEnumerator GetPrewarm(int dimIndex, Action<bool, string> done = null)
    {
        var url =
            $"{apiBaseUrl.TrimEnd('/')}/api/gd/sg-prewarm?game={UnityWebRequest.EscapeURL(gameSlug)}&dimension={dimIndex}";
        using var req = UnityWebRequest.Get(url);
        ApplyHeaders(req);
        yield return req.SendWebRequest();
        LastJson = req.downloadHandler?.text;
        var ok = req.result == UnityWebRequest.Result.Success;
        done?.Invoke(ok, LastJson);
    }

    IEnumerator PostJson(string url, string body, Action<bool, string> done)
    {
        using var req = new UnityWebRequest(url, "POST");
        var raw = Encoding.UTF8.GetBytes(body ?? "{}");
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        ApplyHeaders(req);
        yield return req.SendWebRequest();
        LastJson = req.downloadHandler?.text;
        var ok = req.result == UnityWebRequest.Result.Success;
        done?.Invoke(ok, LastJson);
    }

    void ApplyHeaders(UnityWebRequest req)
    {
        req.SetRequestHeader("X-User-ID", string.IsNullOrEmpty(userId) ? "developer" : userId);
        if (admin)
            req.SetRequestHeader("X-Admin", "1");
        req.SetRequestHeader("X-Game", gameSlug ?? "main");
    }

    static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
}
