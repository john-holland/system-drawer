const test = require('node:test');
const assert = require('node:assert/strict');
const {
  buildChangeListAckItems,
  mergeAckIntoChangeListData,
  changeListNeedsReviewAck,
  unacknowledgedRequired,
} = require('../../../Scripts/continuuuum_api/static/shared/continuuuum-script-output/continuuuum-script-ack.js');

test('changeListNeedsReviewAck when in_review', () => {
  assert.equal(changeListNeedsReviewAck({ workflowStatus: 'in_review' }), true);
  assert.equal(changeListNeedsReviewAck({ workflowStatus: 'in_progress' }), false);
  assert.equal(changeListNeedsReviewAck({ submittedAt: '2020-01-01' }), true);
});

test('buildChangeListAckItems adds synthetic required row', () => {
  const items = buildChangeListAckItems({ workflowStatus: 'in_review' });
  assert.equal(items.length, 1);
  assert.equal(items[0].severity, 'required');
  assert.equal(items[0]._synthetic, true);
});

test('mergeAckIntoChangeListData prepends ack', () => {
  const merged = mergeAckIntoChangeListData(
    { required: [{ id: 'r1', severity: 'required', description: 'x' }] },
    { workflowStatus: 'submitted' },
  );
  assert.equal(merged.required.length, 2);
  assert.equal(merged.required[0].id, 'ack-in-review-cycle');
});

test('unacknowledgedRequired ignores warnings', () => {
  const bad = unacknowledgedRequired([
    { severity: 'required', userAcknowledged: false },
    { severity: 'warning', userAcknowledged: false },
  ]);
  assert.equal(bad.length, 1);
});
