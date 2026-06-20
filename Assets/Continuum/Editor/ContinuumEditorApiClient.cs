#if UNITY_EDITOR
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Editor API client — all Continuum HTTP from Unity Editor uses this (WebView never calls network directly).</summary>
public static class ContinuumEditorApiClient
{
    public static async Task<ApiCallResult> RequestAsync(
        string method,
        string path,
        string jsonBody = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
            return ApiCallResult.Fail("path required");
        if (!path.StartsWith("/"))
            path = "/" + path;

        string url = ContinuumEditorSession.ApiBaseUrl + path;
        using var req = new UnityWebRequest(url, method.ToUpperInvariant());
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(jsonBody) && method != "GET")
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        req.SetRequestHeader("X-User-ID", ContinuumEditorSession.UserId);
        req.SetRequestHeader("X-Tenant-ID", ContinuumEditorSession.TenantId);

        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
            {
                req.Abort();
                return ApiCallResult.Fail("cancelled");
            }
            await Task.Yield();
        }

        if (req.result != UnityWebRequest.Result.Success)
            return ApiCallResult.Fail(req.error ?? $"HTTP {(long)req.responseCode}");

        return ApiCallResult.Ok(req.downloadHandler.text);
    }
}

public readonly struct ApiCallResult
{
    public bool success { get; }
    public string json { get; }
    public string error { get; }

    ApiCallResult(bool ok, string data, string err)
    {
        success = ok;
        json = data;
        error = err;
    }

    public static ApiCallResult Ok(string json) => new ApiCallResult(true, json ?? "", null);
    public static ApiCallResult Fail(string error) => new ApiCallResult(false, null, error);
}

#endif
