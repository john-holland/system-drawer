/** Stub script-output permissions (client-first; server mirrors on writes). */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) {
    module.exports = api;
  } else {
    root.ContinuumScriptPermissions = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  const IN_REVIEW = new Set(['in_review', 'submitted']);

  function normUser(id) {
    return String(id || 'anonymous').trim().toLowerCase() || 'anonymous';
  }

  function resolveAuthor(ctx) {
    const draft = (ctx && ctx.draft) || {};
    const review = (ctx && ctx.review) || {};
    return normUser(
      draft.createdBy || draft.created_by ||
      review.revieweeUserId || review.reviewee_user_id ||
      'anonymous',
    );
  }

  function resolveScriptPermissions(ctx) {
    ctx = ctx || {};
    const draft = ctx.draft || {};
    const changeList = ctx.changeList || null;
    const userId = normUser(ctx.userId);
    const author = resolveAuthor(ctx);
    const isAuthor = userId === author;
    const committed = !!(draft.committedAt || draft.committed_at);
    const clStatus = (changeList && (changeList.workflowStatus || changeList.workflow_status)) || '';
    const inReview = IN_REVIEW.has(clStatus);
    const hasUser = userId.length > 0;

    const canEditScript = isAuthor && !committed && !inReview;
    const canSuggestEdit = !isAuthor && !committed;
    const canAttachClause = canEditScript || canSuggestEdit;
    const canSaveDirect = canEditScript;
    const canSubmitChangeList = isAuthor && !committed && clStatus === 'in_progress';
    const canAcceptSuggestion = isAuthor && !committed;
    const canComment = hasUser;
    const editMode = canEditScript ? 'author' : (canSuggestEdit ? 'suggest' : 'readonly');

    return {
      userId,
      isAuthor,
      committed,
      inReview,
      editMode,
      canEditScript,
      canSuggestEdit,
      canAttachClause,
      canSaveDirect,
      canSubmitChangeList,
      canAcceptSuggestion,
      canComment,
    };
  }

  return { resolveScriptPermissions, resolveAuthor, normUser };
});
