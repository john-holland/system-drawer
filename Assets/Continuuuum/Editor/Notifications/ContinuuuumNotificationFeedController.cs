#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Polls Continuuuum notifications and updates feed state.</summary>
public sealed class ContinuuuumNotificationFeedController
{
    public NotificationItem[] Items { get; private set; } = Array.Empty<NotificationItem>();
    public int UnreadCount { get; private set; }

    public event Action Changed;

    public async Task RefreshAsync(int limit = 30)
    {
        var resp = await ContinuuuumEditorLocalizationClient.Instance.GetNotificationsAsync(limit);
        Items = resp?.items ?? Array.Empty<NotificationItem>();
        UnreadCount = resp?.unreadCount ?? Items.Count(i => string.IsNullOrEmpty(i.readAt));
        Changed?.Invoke();
    }

    public async Task MarkReadAsync(string notificationId)
    {
        await ContinuuuumEditorLocalizationClient.Instance.MarkReadAsync(notificationId);
        await RefreshAsync();
    }

    public static string TypeLabel(string type) => type switch
    {
        "review_approved" => "Review approved",
        "review_denied" => "Review denied",
        "change_list_submitted" => "Change list submitted",
        "comment" => "Comment",
        "comment_delete_requested" => "Delete requested",
        "comment_delete_approved" => "Delete approved",
        _ => type ?? "Notification"
    };
}

#endif
