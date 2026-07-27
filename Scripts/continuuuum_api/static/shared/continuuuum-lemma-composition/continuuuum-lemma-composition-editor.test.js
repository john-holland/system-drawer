const test = require('node:test');
const assert = require('node:assert/strict');
globalThis.ContinuuuumLemmaPicker = require('../continuuuum-lemma-composition/continuuuum-lemma-picker.js');
const {
  buildSavePayload,
  mountCreateTabs,
  prepareSubpromptComposition,
} = require('../continuuuum-lemma-composition/continuuuum-lemma-composition-editor.js');
const { pickAutoSelectLemma } = require('../continuuuum-lemma-composition/continuuuum-lemma-picker.js');

test('buildSavePayload maps children to ordered entry ids', () => {
  const payload = buildSavePayload([
    { entryId: 'a', term: 'Alpha' },
    { entryId: 'b', term: 'Beta' },
  ]);
  assert.deepEqual(payload, {
    children: [
      { entryId: 'a', sortOrder: 0 },
      { entryId: 'b', sortOrder: 1 },
    ],
  });
});

test('mountCreateTabs defaults to prefab mode', () => {
  function mockContainer() {
    const buttons = [
      { dataset: { mode: 'prefab' }, classList: { toggle: () => {} }, onclick: null },
      { dataset: { mode: 'composition' }, classList: { toggle: () => {} }, onclick: null },
    ];
    return {
      innerHTML: '',
      querySelectorAll(sel) {
        return sel === 'button' ? buttons : [];
      },
      querySelector() { return null; },
    };
  }
  const container = mockContainer();
  const prefab = { style: { display: '' } };
  const composition = { style: { display: '' } };
  const tabs = mountCreateTabs(container, {
    prefabPanel: prefab,
    compositionHost: composition,
  });
  assert.equal(tabs.getMode(), 'prefab');
  assert.equal(prefab.style.display, '');
  assert.equal(composition.style.display, 'none');
});

test('pickAutoSelectLemma prefers built-in exact match', () => {
  const items = [
    { id: 'custom', term: 'in', isBuiltIn: false },
    { id: 'builtin', term: 'in', isBuiltIn: true },
  ];
  const hit = pickAutoSelectLemma(items, 'in');
  assert.equal(hit.id, 'builtin');
});

test('prepareSubpromptComposition builds sole child and P placeholder', async () => {
  const calls = [];
  const callApi = async (method, path, body) => {
    calls.push({ method, path, body });
    if (method === 'GET' && path.includes('entries?q=')) {
      return { items: [{ id: 'child-1', term: 'oven', posTag: 'noun', languageCode: 'en' }] };
    }
    throw new Error('unexpected ' + method + ' ' + path);
  };
  const prep = await prepareSubpromptComposition(callApi, 'parent-1', 'oven');
  assert.equal(prep.lemmaPrompt, '{P:oven}');
  assert.equal(prep.compositionChildren.length, 1);
  assert.equal(prep.compositionChildren[0].entryId, 'child-1');
});

test('prepareSubpromptComposition skips when seed is the parent lemma', async () => {
  const callApi = async (method, path) => {
    if (method === 'GET' && path.includes('entries?q=')) {
      return {
        items: [{ id: 'builtin:en:verb:action', term: 'action', isBuiltIn: true, posTag: 'verb' }],
      };
    }
    throw new Error('unexpected ' + method + ' ' + path);
  };
  const prep = await prepareSubpromptComposition(
    callApi,
    'builtin:en:verb:action',
    'action',
  );
  assert.equal(prep.skippedSelf, true);
  assert.equal(prep.lemmaPrompt, '');
  assert.deepEqual(prep.compositionChildren, []);
});

test('resolveOrCreateLemmaEntry reuses existing on builtin_conflict 409', async () => {
  const { resolveOrCreateLemmaEntry } = require('../continuuuum-lemma-composition/continuuuum-lemma-picker.js');
  const calls = [];
  const callApi = async (method, path, body) => {
    calls.push({ method, path, body });
    if (method === 'GET' && path.includes('entries?q=')) {
      // Substring hits without exact term on first page (pre-sort-boost simulation)
      return {
        items: [
          { id: 'a1', term: 'abstraction', isBuiltIn: false },
          { id: 'a2', term: 'faction', isBuiltIn: false },
        ],
      };
    }
    if (method === 'POST' && path === '/api/thesaurus/entries') {
      const err = new Error('This word matches a built-in lemma for this language.');
      err.code = 'builtin_conflict';
      err.existingEntryId = 'builtin:en:verb:action';
      err.status = 409;
      throw err;
    }
    if (method === 'GET' && path.includes('entryId=')) {
      return { id: 'builtin:en:verb:action', term: 'action', isBuiltIn: true, posTag: 'verb' };
    }
    throw new Error('unexpected ' + method + ' ' + path);
  };
  const entry = await resolveOrCreateLemmaEntry(callApi, 'action', []);
  assert.equal(entry.id, 'builtin:en:verb:action');
  assert.equal(entry.term, 'action');
  assert.ok(calls.some((c) => c.method === 'POST'));
  assert.ok(calls.some((c) => c.method === 'GET' && String(c.path).includes('entryId=')));
});
