using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches Continuum notifications at startup and logs unread ones as warnings.
/// Registers with SystemDrawerService under "ContinuumNotifications".
/// </summary>
public class ContinuumNotificationsService : MonoBehaviour
{
    private void Start()
    {
        var svc = SystemDrawerService.FindInScene();
        if (svc != null)
            svc.Register("ContinuumNotifications", this);
        StartCoroutine(FetchAndLogNotifications());
    }

    private IEnumerator FetchAndLogNotifications()
    {
        var baseUrl = ContinuumApiConfig.GetApiBaseUrl().TrimEnd('/');
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
                            Debug.LogWarning($"[Continuuum] {n.message}");
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
