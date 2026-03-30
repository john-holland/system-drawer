#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches Continuuum notifications on editor startup and periodically.
/// Shows script draft review and generic notifications.
/// </summary>
[InitializeOnLoad]
public static class ContinuumNotificationsEditor
{
    private static double _lastPollTime;
    private const double PollIntervalSeconds = 60;

    static ContinuumNotificationsEditor()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying)
            return;
        var now = EditorApplication.timeSinceStartup;
        if (now - _lastPollTime < PollIntervalSeconds)
            return;
        _lastPollTime = now;
        EditorApplication.delayCall += FetchNotifications;
    }

    private static void FetchNotifications()
    {
        var baseUrl = ContinuumSettings.GetApiBaseUrl().TrimEnd('/');
        var url = $"{baseUrl}/api/notifications?limit=5";
        var req = new UnityWebRequest(url) { downloadHandler = new DownloadHandlerBuffer() };
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", ContinuumSettings.GetTenant());
        var op = req.SendWebRequest();
        op.completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
                return;
            try
            {
                var resp = JsonUtility.FromJson<NotificationsResponse>(req.downloadHandler.text);
                if (resp?.items != null)
                {
                    var unread = new List<NotificationItem>();
                    foreach (var n in resp.items)
                    {
                        if (string.IsNullOrEmpty(n.readAt))
                            unread.Add(n);
                    }
                    if (unread.Count > 0)
                        ShowNotificationDialog(unread, baseUrl);
                }
            }
            catch { }
            req.Dispose();
        };
    }

    private static void ShowNotificationDialog(List<NotificationItem> items, string baseUrl)
    {
        var msg = string.Join("\n", items.ConvertAll(n => "• " + n.message));
        if (msg.Length > 500)
            msg = msg.Substring(0, 497) + "...";
        var ok = EditorUtility.DisplayDialog(
            "Continuuum Notifications",
            $"You have {items.Count} unread notification(s):\n\n{msg}\n\nOpen Continuuum UI to view and manage.",
            "Open Continuuum",
            "Dismiss");
        if (ok)
            Application.OpenURL(baseUrl + "/ui");
    }
}
#endif
