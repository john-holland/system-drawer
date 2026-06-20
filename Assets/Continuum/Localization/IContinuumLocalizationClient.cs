using System.Threading;
using System.Threading.Tasks;

/// <summary>Continuum localization / change-list API contract (mirrors server.py routes).</summary>
public interface IContinuumLocalizationClient
{
    Task<LocalizationPropertySpecRecord[]> GetPropertySpecsAsync(CancellationToken ct = default);
    Task<LocalizationClauseBindingRecord[]> GetClauseBindingsAsync(string draftEpisodeId, CancellationToken ct = default);
    Task<ScriptApplyEditResult> ApplyScriptEditAsync(string draftEpisodeId, string oldText, string newText, CancellationToken ct = default);
    Task<LocalizationChangeListRecord> GetChangeListAsync(string changeListId, CancellationToken ct = default);
    Task AcknowledgeChangeListItemAsync(string itemId, CancellationToken ct = default);
    Task SaveChangeListAsync(string changeListId, LocalizationChangeListItemRecord[] items = null, CancellationToken ct = default);
    Task<LocalizationChangeListDetailRecord> GetActiveChangeListForDraftAsync(string draftEpisodeId, CancellationToken ct = default);
    Task<LocalizationClauseBindingRecord> PostClauseBindingAsync(ClauseRefRecord clauseRef, string bindingKind, string propertyKey, string propertyValue, string scriptText, CancellationToken ct = default);
    Task SubmitChangeListForReviewAsync(string changeListId, CancellationToken ct = default);
    Task<ReviewerCommentArchiveRecord[]> GetArchivedReviewCommentsAsync(string reviewId, CancellationToken ct = default);
    Task RequestCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default);
    Task ApproveCommentDeleteAsync(string reviewId, string commentId, CancellationToken ct = default);
    Task<DraftScriptRecord> GetDraftScriptAsync(string draftEpisodeId, CancellationToken ct = default);
    Task PutDraftScriptAsync(string draftEpisodeId, string scriptText, string language = "en", CancellationToken ct = default);
    Task<ThesaurusEntryPropertyRecord[]> GetEntryPropertiesAsync(string entryId = null, CancellationToken ct = default);
    Task PutEntryPropertyAsync(string entryId, string propertyKey, string propertyValue, CancellationToken ct = default);
    Task DeleteEntryPropertyAsync(string entryId, string propertyKey, CancellationToken ct = default);
}
