#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Editor Continuum client — uses ContinuumEditorApiClient and ContinuumSettings headers.</summary>
public sealed class ContinuumEditorLocalizationClient : IContinuumLocalizationClient, IContinuumNotificationClient
{
    public static readonly ContinuumEditorLocalizationClient Instance = new ContinuumEditorLocalizationClient();

    public async Task<LocalizationPropertySpecRecord[]> GetPropertySpecsAsync(CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET", "/api/thesaurus/property-specs", null, ct);
        return r.success ? ParseItems<LocalizationPropertySpecRecord>(r.json) : Array.Empty<LocalizationPropertySpecRecord>();
    }

    public async Task<LocalizationClauseBindingRecord[]> GetClauseBindingsAsync(string draftEpisodeId, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET",
            $"/api/thesaurus/clause-bindings?draftEpisodeId={Uri.EscapeDataString(draftEpisodeId ?? "")}", null, ct);
        return r.success ? ParseItems<LocalizationClauseBindingRecord>(r.json) : Array.Empty<LocalizationClauseBindingRecord>();
    }

    public async Task<ScriptApplyEditResult> ApplyScriptEditAsync(string draftEpisodeId, string oldText, string newText, CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new ApplyEditBody { oldText = oldText, newText = newText });
        var r = await ContinuumEditorApiClient.RequestAsync("POST", $"/api/scripts/{Uri.EscapeDataString(draftEpisodeId ?? "")}/apply-edit", body, ct);
        if (!r.success || string.IsNullOrEmpty(r.json))
            return ScriptApplyEditResult.Empty;
        return JsonUtility.FromJson<ScriptApplyEditResult>(r.json) ?? ScriptApplyEditResult.Empty;
    }

    public async Task<LocalizationChangeListRecord> GetChangeListAsync(string changeListId, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET", $"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}", null, ct);
        return r.success ? JsonUtility.FromJson<LocalizationChangeListDetailRecord>(r.json) : null;
    }

    public async Task AcknowledgeChangeListItemAsync(string itemId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("PATCH", $"/api/localization/change-list-items/{Uri.EscapeDataString(itemId ?? "")}",
            "{\"userAcknowledged\":true}", ct);
    }

    public async Task SaveChangeListAsync(string changeListId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("POST", $"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}/save", "{}", ct);
    }

    public async Task SubmitChangeListForReviewAsync(string changeListId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("POST", $"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}/submit-for-review", "{}", ct);
    }

    public async Task<ReviewerCommentArchiveRecord[]> GetArchivedReviewCommentsAsync(string reviewId, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET", $"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/archive", null, ct);
        return r.success ? ParseItems<ReviewerCommentArchiveRecord>(r.json) : Array.Empty<ReviewerCommentArchiveRecord>();
    }

    public async Task RequestCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("PATCH", $"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/{Uri.EscapeDataString(commentId ?? "")}",
            "{\"requestDelete\":true}", ct);
    }

    public async Task ApproveCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("PATCH", $"/api/reviews/{Uri.EscapeDataString(reviewId ?? "")}/comments/{Uri.EscapeDataString(commentId ?? "")}",
            "{\"approveDelete\":true}", ct);
    }

    public async Task<DraftScriptRecord> GetDraftScriptAsync(string draftEpisodeId, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET", $"/api/drafts/episodes/{Uri.EscapeDataString(draftEpisodeId ?? "")}/script", null, ct);
        return r.success ? JsonUtility.FromJson<DraftScriptRecord>(r.json) : null;
    }

    public async Task PutDraftScriptAsync(string draftEpisodeId, string scriptText, string language = "en", CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new PutDraftBody { scriptText = scriptText, language = language });
        await ContinuumEditorApiClient.RequestAsync("PUT", $"/api/drafts/episodes/{Uri.EscapeDataString(draftEpisodeId ?? "")}/script", body, ct);
    }

    public async Task<NotificationsResponse> GetNotificationsAsync(int limit = 20, bool unreadOnly = false, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET", $"/api/notifications?limit={limit}", null, ct);
        if (!r.success)
            return new NotificationsResponse { items = Array.Empty<NotificationItem>(), unreadCount = 0 };
        return JsonUtility.FromJson<NotificationsResponse>(r.json) ?? new NotificationsResponse();
    }

    public async Task MarkReadAsync(string notificationId, CancellationToken ct = default)
    {
        await ContinuumEditorApiClient.RequestAsync("POST", $"/api/notifications/{Uri.EscapeDataString(notificationId ?? "")}/read", "{}", ct);
    }

    public async Task<ThesaurusEntryPropertyRecord[]> GetEntryPropertiesAsync(string entryId = null, CancellationToken ct = default)
    {
        string path = string.IsNullOrEmpty(entryId)
            ? "/api/thesaurus/entry-properties"
            : $"/api/thesaurus/entry-properties?entryId={Uri.EscapeDataString(entryId)}";
        var r = await ContinuumEditorApiClient.RequestAsync("GET", path, null, ct);
        return r.success ? ParseItems<ThesaurusEntryPropertyRecord>(r.json) : Array.Empty<ThesaurusEntryPropertyRecord>();
    }

    public async Task PutEntryPropertyAsync(string entryId, string propertyKey, string propertyValue, CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new EntryPropertyBody { entryId = entryId, propertyKey = propertyKey, propertyValue = propertyValue });
        await ContinuumEditorApiClient.RequestAsync("PUT", "/api/thesaurus/entry-properties", body, ct);
    }

    public Task DeleteEntryPropertyAsync(string entryId, string propertyKey, CancellationToken ct = default)
    {
        string path = $"/api/thesaurus/entry-properties?entryId={Uri.EscapeDataString(entryId ?? "")}&propertyKey={Uri.EscapeDataString(propertyKey ?? "")}";
        return ContinuumEditorApiClient.RequestAsync("DELETE", path, null, ct);
    }

    public Task<ApiCallResult> CallRawAsync(string method, string path, string body, CancellationToken ct = default) =>
        ContinuumEditorApiClient.RequestAsync(method, path, body, ct);

    static T[] ParseItems<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<T>();
        var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(json);
        return wrapper?.items ?? Array.Empty<T>();
    }

    [Serializable]
    class ApplyEditBody { public string oldText; public string newText; }
    [Serializable]
    class PutDraftBody { public string scriptText; public string language; }
    [Serializable]
    class EntryPropertyBody { public string entryId; public string propertyKey; public string propertyValue; }
    [Serializable]
    class JsonArrayWrapper<T> { public T[] items; }
}

#endif
