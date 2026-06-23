/** Synthetic change-list acknowledgment items (in-review reminder). */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) {
    module.exports = api;
  } else {
    root.ContinuumScriptAck = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  const ACK_ID = 'ack-in-review-cycle';

  function changeListNeedsReviewAck(changeList) {
    if (!changeList) return false;
    const status = (changeList.workflowStatus || changeList.workflow_status || '').toLowerCase();
    if (status === 'in_review' || status === 'submitted') return true;
    return !!(changeList.submittedAt || changeList.submitted_at);
  }

  function buildChangeListAckItems(changeList) {
    if (!changeListNeedsReviewAck(changeList)) return [];
    return [{
      id: ACK_ID,
      severity: 'required',
      itemType: 'acknowledgment',
      description: 'This script is in review (or was recently submitted). I understand these changes may require a new review cycle.',
      userAcknowledged: false,
      _synthetic: true,
    }];
  }

  function mergeAckIntoChangeListData(data, changeList) {
    const ack = buildChangeListAckItems(changeList);
    if (!ack.length) return data;
    const required = [...ack, ...((data && data.required) || [])];
    return { ...data, required };
  }

  function unacknowledgedRequired(items) {
    return (items || []).filter((i) => !i.userAcknowledged && i.severity !== 'warning');
  }

  return {
    ACK_ID,
    changeListNeedsReviewAck,
    buildChangeListAckItems,
    mergeAckIntoChangeListData,
    unacknowledgedRequired,
  };
});
