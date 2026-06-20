#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Polls Continuum notifications and updates the Notifications window badge.
/// High-priority types get a single non-blocking toast (no modal spam).
/// </summary>
[InitializeOnLoad]
public static class ContinuumNotificationsEditor
{
    static double _lastPollTime;
    const double PollIntervalSeconds = 60;
    static readonly string[] ToastTypes = { "review_denied", "change_list_submitted" };

    static ContinuumNotificationsEditor()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying)
            return;
        var now = EditorApplication.timeSinceStartup;
        if (now - _lastPollTime < PollIntervalSeconds)
            return;
        _lastPollTime = now;
        EditorApplication.delayCall += PollNotifications;
    }

    static async void PollNotifications()
    {
        try
        {
            var feed = new ContinuumNotificationFeedController();
            await feed.RefreshAsync(10);
            ContinuumNotificationsWindow.NotifyBadgeUpdated(feed.UnreadCount);

            var toast = feed.Items
                .Where(n => n != null && string.IsNullOrEmpty(n.readAt) && ToastTypes.Contains(n.type))
                .FirstOrDefault();
            if (toast != null)
                ShowToast(toast);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Continuum] Notification poll failed: {ex.Message}");
        }
    }

    static void ShowToast(NotificationItem item)
    {
        var label = ContinuumNotificationFeedController.TypeLabel(item.type);
        Debug.LogWarning($"[Continuum] {label}: {item.message}");
        ContinuumNotificationsWindow.Open();
    }
}
#endif
