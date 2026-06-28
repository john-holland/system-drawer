const test = require('node:test');
const assert = require('node:assert/strict');
globalThis.ContinuumLemmaPicker = require('../continuum-lemma-composition/continuum-lemma-picker.js');
const {
  buildSavePayload,
  mountCreateTabs,
  prepareSubpromptComposition,
} = require('../continuum-lemma-composition/continuum-lemma-composition-editor.js');
const { pickAutoSelectLemma } = require('../continuum-lemma-composition/continuum-lemma-picker.js');

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
