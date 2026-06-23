const test = require('node:test');
const assert = require('node:assert/strict');
const {
  normalizeClauseError,
  createApiError,
  pickAutoSelectLemma,
  ensureConflictActions,
  clearClauseDialogError,
  showClauseDialogError,
} = require('./continuum-clause-selector.js');

test('createApiError preserves structured API fields', () => {
  const err = createApiError('conflict', {
    code: 'builtin_conflict',
    existingEntryId: 'urn:unity:continuum:builtin:v1:/en/prep/in',
    field: 'word',
  });
  assert.equal(err.message, 'conflict');
  assert.equal(err.code, 'builtin_conflict');
  assert.equal(err.existingEntryId, 'urn:unity:continuum:builtin:v1:/en/prep/in');
  assert.equal(err.field, 'word');
});

test('normalizeClauseError parses JSON message from fetch wrapper', () => {
  const raw = createApiError('{"error":"bad","code":"builtin_conflict","existingEntryId":"x"}');
  const err = normalizeClauseError(raw);
  assert.equal(err.code, 'builtin_conflict');
  assert.equal(err.existingEntryId, 'x');
});

test('pickAutoSelectLemma selects sole exact term match', () => {
  const items = [
    { id: '1', term: 'inside', posTag: 'preposition', isBuiltIn: false },
    { id: '2', term: 'in', posTag: 'preposition', isBuiltIn: true },
  ];
  const hit = pickAutoSelectLemma(items, 'in');
  assert.equal(hit.id, '2');
});

test('pickAutoSelectLemma prefers built-in when multiple exact matches', () => {
  const items = [
    { id: 'custom', term: 'in', posTag: 'noun', isBuiltIn: false },
    { id: 'builtin', term: 'in', posTag: 'preposition', isBuiltIn: true },
  ];
  const hit = pickAutoSelectLemma(items, 'in');
  assert.equal(hit.id, 'builtin');
});

test('conflict actions stay hidden until builtin_conflict', () => {
  function mockClassList() {
    const set = new Set();
    return {
      add(c) { set.add(c); },
      remove(c) { set.delete(c); },
      contains(c) { return set.has(c); },
    };
  }
  function el(id, className) {
    return { id, className, classList: mockClassList(), hidden: true, innerHTML: '', textContent: '' };
  }
  const conflictEl = el('clause-attach-conflict-actions', 'continuum-clause-conflict-actions');
  const errEl = el('clause-attach-error', 'continuum-clause-dialog-error');
  const useBtn = { addEventListener: () => {} };
  const editBtn = { addEventListener: () => {} };
  const nodes = [errEl, conflictEl, useBtn, editBtn];
  const box = {
    querySelector(sel) {
      if (sel === '#clause-attach-error') return errEl;
      if (sel === '#clause-attach-conflict-actions') return conflictEl;
      if (sel === '#clause-attach-use-existing') return useBtn;
      if (sel === '#clause-attach-edit-entry') return editBtn;
      return null;
    },
    querySelectorAll(sel) {
      if (sel === '.continuum-clause-dialog-error') return [errEl];
      if (sel === '.continuum-clause-conflict-actions') return [conflictEl];
      if (sel === '.clause-field-error') return [];
      return [];
    },
  };

  ensureConflictActions(box, 'clause-attach', { onUseExisting: async () => {} });
  assert.ok(!conflictEl.classList.contains('is-visible'));

  showClauseDialogError(box, createApiError('built-in', {
    code: 'builtin_conflict',
    existingEntryId: 'urn:unity:continuum:builtin:v1:/en/prep/in',
  }), { onUseExisting: async () => {} });
  assert.ok(conflictEl.classList.contains('is-visible'));

  clearClauseDialogError(box);
  assert.ok(!conflictEl.classList.contains('is-visible'));
});
