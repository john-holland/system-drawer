using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>In-memory localization client for EditMode tests and offline authoring.</summary>
public sealed class StubContinuuuumLocalizationClient : IContinuuuumLocalizationClient, IContinuuuumNotificationClient
{
    readonly LocalizationPropertySpecRecord[] _specs;
    readonly List<LocalizationClauseBindingRecord> _bindings = new List<LocalizationClauseBindingRecord>();
    readonly List<ThesaurusEntryPropertyRecord> _entryProperties = new List<ThesaurusEntryPropertyRecord>();

    public StubContinuuuumLocalizationClient()
    {
        _specs = LocalizationPropertySpecCatalog.BuildDefaultRecords();
    }

    public Task<LocalizationPropertySpecRecord[]> GetPropertySpecsAsync(CancellationToken ct = default) =>
        Task.FromResult(_specs);

    public Task<LocalizationClauseBindingRecord[]> GetClauseBindingsAsync(string draftEpisodeId, CancellationToken ct = default) =>
        Task.FromResult(_bindings.ToArray());

    public Task<ScriptApplyEditResult> ApplyScriptEditAsync(string draftEpisodeId, string oldText, string newText, CancellationToken ct = default) =>
        Task.FromResult(ScriptApplyEditResult.Empty);

    public Task<LocalizationChangeListRecord> GetChangeListAsync(string changeListId, CancellationToken ct = default) =>
        Task.FromResult<LocalizationChangeListRecord>(null);

    public Task AcknowledgeChangeListItemAsync(string itemId, CancellationToken ct = default) => Task.CompletedTask;

    public Task SaveChangeListAsync(string changeListId, LocalizationChangeListItemRecord[] items = null, CancellationToken ct = default) => Task.CompletedTask;

    public Task<LocalizationChangeListDetailRecord> GetActiveChangeListForDraftAsync(string draftEpisodeId, CancellationToken ct = default) =>
        Task.FromResult<LocalizationChangeListDetailRecord>(null);

    public Task<LocalizationClauseBindingRecord> PostClauseBindingAsync(
        ClauseRefRecord clauseRef,
        string bindingKind,
        string propertyKey,
        string propertyValue,
        string scriptText,
        CancellationToken ct = default) => Task.FromResult<LocalizationClauseBindingRecord>(null);

    public Task SubmitChangeListForReviewAsync(string changeListId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<ReviewerCommentArchiveRecord[]> GetArchivedReviewCommentsAsync(string reviewId, CancellationToken ct = default) =>
        Task.FromResult(System.Array.Empty<ReviewerCommentArchiveRecord>());

    public Task RequestCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default) => Task.CompletedTask;

    public Task ApproveCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<DraftScriptRecord> GetDraftScriptAsync(string draftEpisodeId, CancellationToken ct = default) =>
        Task.FromResult<DraftScriptRecord>(null);

    public Task PutDraftScriptAsync(string draftEpisodeId, string scriptText, string language = "en", CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<NotificationsResponse> GetNotificationsAsync(int limit = 20, bool unreadOnly = false, CancellationToken ct = default) =>
        Task.FromResult(new NotificationsResponse { items = Array.Empty<NotificationItem>(), unreadCount = 0 });

    public Task MarkReadAsync(string notificationId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<ThesaurusEntryPropertyRecord[]> GetEntryPropertiesAsync(string entryId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(entryId))
            return Task.FromResult(_entryProperties.ToArray());
        var filtered = new List<ThesaurusEntryPropertyRecord>();
        foreach (var p in _entryProperties)
        {
            if (p != null && string.Equals(p.entryId, entryId, System.StringComparison.OrdinalIgnoreCase))
                filtered.Add(p);
        }
        return Task.FromResult(filtered.ToArray());
    }

    public Task PutEntryPropertyAsync(string entryId, string propertyKey, string propertyValue, CancellationToken ct = default)
    {
        _entryProperties.RemoveAll(p =>
            p != null &&
            string.Equals(p.entryId, entryId, System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.propertyKey, propertyKey, System.StringComparison.OrdinalIgnoreCase));
        _entryProperties.Add(new ThesaurusEntryPropertyRecord
        {
            entryId = entryId,
            propertyKey = propertyKey,
            propertyValue = propertyValue
        });
        return Task.CompletedTask;
    }

    public Task DeleteEntryPropertyAsync(string entryId, string propertyKey, CancellationToken ct = default)
    {
        _entryProperties.RemoveAll(p =>
            p != null &&
            string.Equals(p.entryId, entryId, System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.propertyKey, propertyKey, System.StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }
}
