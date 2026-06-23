const test = require('node:test');
const assert = require('node:assert/strict');
const { resolveScriptPermissions, normUser } = require('../../../Scripts/continuum_api/static/shared/continuum-script-output/continuum-script-permissions.js');

test('normUser defaults to anonymous', () => {
  assert.equal(normUser(''), 'anonymous');
  assert.equal(normUser('  alice  '), 'alice');
});

test('author can edit when not in review', () => {
  const p = resolveScriptPermissions({
    draft: { createdBy: 'alice', committedAt: null },
    changeList: { workflowStatus: 'in_progress' },
    userId: 'alice',
  });
  assert.equal(p.canEditScript, true);
  assert.equal(p.canSuggestEdit, false);
  assert.equal(p.editMode, 'author');
});

test('author blocked when in review', () => {
  const p = resolveScriptPermissions({
    draft: { createdBy: 'alice' },
    changeList: { workflowStatus: 'in_review' },
    userId: 'alice',
  });
  assert.equal(p.canEditScript, false);
  assert.equal(p.inReview, true);
  assert.equal(p.editMode, 'readonly');
});

test('non-author gets suggest mode', () => {
  const p = resolveScriptPermissions({
    draft: { createdBy: 'alice' },
    changeList: { workflowStatus: 'in_progress' },
    userId: 'bob',
  });
  assert.equal(p.canSuggestEdit, true);
  assert.equal(p.canEditScript, false);
  assert.equal(p.editMode, 'suggest');
});
