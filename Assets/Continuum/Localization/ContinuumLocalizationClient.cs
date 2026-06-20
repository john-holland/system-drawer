using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Live Continuum API client for localization endpoints.</summary>
public sealed class ContinuumLocalizationClient : IContinuumLocalizationClient, IContinuumNotificationClient
{
    readonly string _baseUrl;

    public ContinuumLocalizationClient(string baseUrl = null)
    {
        _baseUrl = (baseUrl ?? ContinuumApiConfig.GetApiBaseUrl()).TrimEnd('/');
    }

    public async Task<LocalizationPropertySpecRecord[]> GetPropertySpecsAsync(CancellationToken ct = default)
    {
        string json = await GetRaw("/api/thesaurus/property-specs", ct);
        return ParseItems<LocalizationPropertySpecRecord>(json);
    }

    public async Task<LocalizationClauseBindingRecord[]> GetClauseBindingsAsync(string draftEpisodeId, CancellationToken ct = default)
    {
        string path = $"/api/thesaurus/clause-bindings?draftEpisodeId={Uri.EscapeDataString(draftEpisodeId ?? "")}";
        string json = await GetRaw(path, ct);
        return ParseItems<LocalizationClauseBindingRecord>(json);
    }

    public async Task<ScriptApplyEditResult> ApplyScriptEditAsync(string draftEpisodeId, string oldText, string newText, CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new ApplyEditBody { oldText = oldText, newText = newText });
        string json = await PostJson($"/api/scripts/{Uri.EscapeDataString(draftEpisodeId ?? "")}/apply-edit", body, ct);
        if (string.IsNullOrEmpty(json))
            return ScriptApplyEditResult.Empty;
        return JsonUtility.FromJson<ScriptApplyEditResult>(json) ?? ScriptApplyEditResult.Empty;
    }

    public async Task<LocalizationChangeListRecord> GetChangeListAsync(string changeListId, CancellationToken ct = default)
    {
        string json = await GetRaw($"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}", ct);
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<LocalizationChangeListRecord>(json);
    }

    public Task AcknowledgeChangeListItemAsync(string itemId, CancellationToken ct = default) =>
        PatchJson($"/api/localization/change-list-items/{Uri.EscapeDataString(itemId ?? "")}", "{\"userAcknowledged\":true}", ct);

    public Task SaveChangeListAsync(string changeListId, CancellationToken ct = default) =>
        PostJson($"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}/save", "{}", ct);

    public Task SubmitChangeListForReviewAsync(string changeListId, CancellationToken ct = default) =>
        PostJson($"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}/submit-for-review", "{}", ct);

    public async Task<ReviewerCommentArchiveRecord[]> GetArchivedReviewCommentsAsync(string reviewId, CancellationToken ct = default)
    {
        string json = await GetRaw($"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/archive", ct);
        return ParseItems<ReviewerCommentArchiveRecord>(json);
    }

    public Task RequestCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default) =>
        PatchJson($"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/{Uri.EscapeDataString(commentId ?? "")}", "{\"requestDelete\":true}", ct);

    public Task ApproveCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default) =>
        PatchJson($"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/{Uri.EscapeDataString(commentId ?? "")}", "{\"approveDelete\":true}", ct);

    public async Task<DraftScriptRecord> GetDraftScriptAsync(string draftEpisodeId, CancellationToken ct = default)
    {
        string json = await GetRaw($"/api/drafts/episodes/{Uri.EscapeDataString(draftEpisodeId ?? "")}/script", ct);
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<DraftScriptRecord>(json);
    }

    public async Task PutDraftScriptAsync(string draftEpisodeId, string scriptText, string language = "en", CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new PutDraftScriptBody { scriptText = scriptText, language = language });
        await PutJson($"/api/drafts/episodes/{Uri.EscapeDataString(draftEpisodeId ?? "")}/script", body, ct);
    }

    public async Task<NotificationsResponse> GetNotificationsAsync(int limit = 20, bool unreadOnly = false, CancellationToken ct = default)
    {
        string path = $"/api/notifications?limit={limit}";
        string json = await GetRaw(path, ct);
        if (string.IsNullOrEmpty(json))
            return new NotificationsResponse { items = Array.Empty<NotificationItem>(), unreadCount = 0 };
        return JsonUtility.FromJson<NotificationsResponse>(json) ?? new NotificationsResponse();
    }

    public Task MarkReadAsync(string notificationId, CancellationToken ct = default) =>
        PostJson($"/api/notifications/{Uri.EscapeDataString(notificationId ?? "")}/read", "{}", ct);

    public async Task<ThesaurusEntryPropertyRecord[]> GetEntryPropertiesAsync(string entryId = null, CancellationToken ct = default)
    {
        string path = string.IsNullOrEmpty(entryId)
            ? "/api/thesaurus/entry-properties"
            : $"/api/thesaurus/entry-properties?entryId={Uri.EscapeDataString(entryId)}";
        string json = await GetRaw(path, ct);
        return ParseItems<ThesaurusEntryPropertyRecord>(json);
    }

    public async Task PutEntryPropertyAsync(string entryId, string propertyKey, string propertyValue, CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new EntryPropertyBody { entryId = entryId, propertyKey = propertyKey, propertyValue = propertyValue });
        await PutJson("/api/thesaurus/entry-properties", body, ct);
    }

    public async Task DeleteEntryPropertyAsync(string entryId, string propertyKey, CancellationToken ct = default)
    {
        string path = $"/api/thesaurus/entry-properties?entryId={Uri.EscapeDataString(entryId ?? "")}&propertyKey={Uri.EscapeDataString(propertyKey ?? "")}";
        using var req = new UnityWebRequest(_baseUrl + path, "DELETE");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", "default");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested) { req.Abort(); return; }
            await Task.Yield();
        }
    }

    static T[] ParseItems<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<T>();
        var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(json);
        return wrapper?.items ?? Array.Empty<T>();
    }

    async Task<string> GetRaw(string path, CancellationToken ct)
    {
        using var req = UnityWebRequest.Get(_baseUrl + path);
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", "default");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested) { req.Abort(); return null; }
            await Task.Yield();
        }
        if (req.result != UnityWebRequest.Result.Success)
            return null;
        return req.downloadHandler.text;
    }

    async Task<string> PutJson(string path, string body, CancellationToken ct)
    {
        using var req = new UnityWebRequest(_baseUrl + path, "PUT");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body ?? "{}"));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", "default");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested) { req.Abort(); return null; }
            await Task.Yield();
        }
        return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
    }

    async Task<string> PostJson(string path, string body, CancellationToken ct)
    {
        using var req = new UnityWebRequest(_baseUrl + path, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body ?? "{}"));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", "default");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested) { req.Abort(); return null; }
            await Task.Yield();
        }
        return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
    }

    async Task PatchJson(string path, string body, CancellationToken ct)
    {
        using var req = new UnityWebRequest(_baseUrl + path, "PATCH");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body ?? "{}"));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-User-ID", "anonymous");
        req.SetRequestHeader("X-Tenant-ID", "default");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested) { req.Abort(); return; }
            await Task.Yield();
        }
    }

    [Serializable]
    class PutDraftScriptBody
    {
        public string scriptText;
        public string language;
    }

    [Serializable]
    class EntryPropertyBody
    {
        public string entryId;
        public string propertyKey;
        public string propertyValue;
    }

    [Serializable]
    class ApplyEditBody
    {
        public string oldText;
        public string newText;
    }

    [Serializable]
    class JsonArrayWrapper<T>
    {
        public T[] items;
    }
}
