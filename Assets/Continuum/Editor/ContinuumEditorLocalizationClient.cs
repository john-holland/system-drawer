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

    public async Task<LocalizationChangeListDetailRecord> GetActiveChangeListForDraftAsync(string draftEpisodeId, CancellationToken ct = default)
    {
        var r = await ContinuumEditorApiClient.RequestAsync("GET",
            $"/api/localization/change-lists?draftEpisodeId={Uri.EscapeDataString(draftEpisodeId ?? "")}", null, ct);
        if (!r.success || string.IsNullOrEmpty(r.json))
            return null;
        var detail = JsonUtility.FromJson<LocalizationChangeListDetailRecord>(r.json);
        return string.IsNullOrEmpty(detail?.id) ? null : detail;
    }

    public async Task SaveChangeListAsync(string changeListId, LocalizationChangeListItemRecord[] items = null, CancellationToken ct = default)
    {
        var payload = new SaveChangeListBody { items = BuildSaveItems(items) };
        var body = JsonUtility.ToJson(payload);
        await ContinuumEditorApiClient.RequestAsync("POST", $"/api/localization/change-lists/{Uri.EscapeDataString(changeListId ?? "")}/save", body, ct);
    }

    public async Task<LocalizationClauseBindingRecord> PostClauseBindingAsync(
        ClauseRefRecord clauseRef,
        string bindingKind,
        string propertyKey,
        string propertyValue,
        string scriptText,
        CancellationToken ct = default)
    {
        var body = JsonUtility.ToJson(new PostClauseBindingBody
        {
            bindingKind = bindingKind,
            propertyKey = propertyKey,
            propertyValue = propertyValue,
            scriptText = scriptText ?? "",
            charStart = clauseRef?.charStart ?? 0,
            charEnd = clauseRef?.charEnd ?? 0,
            selectionText = clauseRef?.selectionText ?? "",
            fareyLeftNum = clauseRef?.fareyLeftNum ?? 0,
            fareyLeftDen = clauseRef?.fareyLeftDen ?? 1,
            fareyRightNum = clauseRef?.fareyRightNum ?? 1,
            fareyRightDen = clauseRef?.fareyRightDen ?? 1,
            draftScriptId = clauseRef?.draftScriptId ?? "",
            draftEpisodeId = clauseRef?.draftEpisodeId ?? "",
            entryId = clauseRef?.entryId ?? "",
            astNodeId = clauseRef?.astNodeId ?? "",
        });
        var r = await ContinuumEditorApiClient.RequestAsync("POST", "/api/thesaurus/clause-bindings", body, ct);
        if (!r.success || string.IsNullOrEmpty(r.json))
            return null;
        return JsonUtility.FromJson<LocalizationClauseBindingRecord>(r.json);
    }

    static SaveChangeListItem[] BuildSaveItems(LocalizationChangeListItemRecord[] items)
    {
        if (items == null || items.Length == 0)
            return System.Array.Empty<SaveChangeListItem>();
        var list = new System.Collections.Generic.List<SaveChangeListItem>();
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.id))
                continue;
            list.Add(new SaveChangeListItem { id = item.id, userAcknowledged = item.userAcknowledged });
        }
        return list.ToArray();
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

    public async Task<bool> PostComponentBlueprintAsync(string entryId, ComponentMetadataPayloadDto payload, CancellationToken ct = default)
    {
        if (payload == null || string.IsNullOrEmpty(entryId))
            return false;
        payload.entryId = entryId;
        payload.source = "blueprint";
        var body = JsonUtility.ToJson(payload);
        var r = await ContinuumEditorApiClient.RequestAsync(
            "POST",
            $"/api/thesaurus/entries/{Uri.EscapeDataString(entryId)}/component-blueprint",
            body,
            ct);
        return r.success;
    }

    public async Task<bool> PostComponentReportAsync(string entryId, ComponentMetadataPayloadDto payload, CancellationToken ct = default)
    {
        if (payload == null || string.IsNullOrEmpty(entryId))
            return false;
        payload.entryId = entryId;
        payload.source = "runtime";
        var body = JsonUtility.ToJson(payload);
        var r = await ContinuumEditorApiClient.RequestAsync(
            "POST",
            $"/api/thesaurus/entries/{Uri.EscapeDataString(entryId)}/component-reports",
            body,
            ct);
        return r.success;
    }

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
    [Serializable]
    class SaveChangeListBody { public SaveChangeListItem[] items; }
    [Serializable]
    class SaveChangeListItem { public string id; public bool userAcknowledged; }
    [Serializable]
    class PostClauseBindingBody
    {
        public string bindingKind;
        public string propertyKey;
        public string propertyValue;
        public string scriptText;
        public int charStart;
        public int charEnd;
        public string selectionText;
        public int fareyLeftNum;
        public int fareyLeftDen;
        public int fareyRightNum;
        public int fareyRightDen;
        public string draftScriptId;
        public string draftEpisodeId;
        public string entryId;
        public string astNodeId;
    }
}

#endif
