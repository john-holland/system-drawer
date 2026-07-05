using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches Continuuuum notifications at startup and logs unread ones as warnings.
/// </summary>
public class ContinuuuumNotificationsService : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(FetchAndLogNotifications());
    }

    private IEnumerator FetchAndLogNotifications()
    {
        var baseUrl = ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/');
        var url = $"{baseUrl}/api/notifications?limit=10";
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("X-User-ID", "anonymous");
            req.SetRequestHeader("X-Tenant-ID", "default");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;
            try
            {
                var resp = JsonUtility.FromJson<NotificationsResponse>(req.downloadHandler.text);
                if (resp?.items != null)
                {
                    foreach (var n in resp.items)
                    {
                        if (string.IsNullOrEmpty(n.readAt))
                {
                    var label = n.type switch
                    {
                        "review_approved" => "[Review approved]",
                        "review_denied" => "[Review denied]",
                        "change_list_submitted" => "[Change list submitted]",
                        "comment_delete_requested" => "[Delete requested]",
                        "comment_delete_approved" => "[Delete approved]",
                        _ => "[Continuuuum]"
                    };
                    Debug.LogWarning($"{label} {n.message}");
                }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Continuuum] Failed to parse notifications: {ex.Message}");
            }
        }
    }
}

[Serializable]
public class NotificationsResponse { public NotificationItem[] items; public int unreadCount; }

[Serializable]
public class NotificationItem
{
    public string id;
    public string userId;
    public string type;
    public string draftId;
    public string reviewId;
    public string message;
    public string readAt;
    public string createdAt;
}
