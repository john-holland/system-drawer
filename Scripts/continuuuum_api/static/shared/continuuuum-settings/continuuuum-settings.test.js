const test = require('node:test');
const assert = require('node:assert/strict');
const CS = require('./continuuuum-settings.js');

test('normalizePriority fills missing types in default order', () => {
  const out = CS.normalizePriority(['prefab', 'builtin']);
  assert.equal(out.length, 6);
  assert.equal(out[0], 'prefab');
  assert.equal(out[1], 'builtin');
  assert.ok(out.includes('new_lemma'));
});

test('swapPrioritySlots exchanges duplicate assignments', () => {
  const base = CS.normalizePriority(CS.AUTO_ADD_TYPES);
  const swapped = CS.swapPrioritySlots(base, 0, 'prefab');
  assert.equal(swapped[0], 'prefab');
  const prefabIndex = swapped.indexOf('builtin');
  assert.ok(prefabIndex >= 0);
  assert.notEqual(swapped[prefabIndex], 'prefab');
});

test('movePrioritySlot reorders adjacent slots', () => {
  const base = CS.normalizePriority(CS.AUTO_ADD_TYPES);
  const moved = CS.movePrioritySlot(base, 0, 1);
  assert.equal(moved[0], base[1]);
  assert.equal(moved[1], base[0]);
});

test('save and load round-trip scriptOutput settings', () => {
  const prev = global.localStorage;
  const store = new Map();
  global.localStorage = {
    getItem: (k) => (store.has(k) ? store.get(k) : null),
    setItem: (k, v) => store.set(k, v),
    removeItem: (k) => store.delete(k),
  };
  try {
    CS.save({
      scriptOutput: {
        autoAddPriority: ['prefab', 'builtin', 'localization', 'mod_slot', 'prompt_placeholder', 'new_lemma'],
        newLemmaRequired: true,
      },
    });
    const loaded = CS.getScriptOutput();
    assert.equal(loaded.newLemmaRequired, true);
    assert.equal(loaded.autoAddPriority[0], 'prefab');
  } finally {
    global.localStorage = prev;
  }
});
