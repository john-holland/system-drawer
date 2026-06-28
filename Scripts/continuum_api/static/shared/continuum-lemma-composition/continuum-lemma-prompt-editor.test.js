const test = require('node:test');
const assert = require('node:assert/strict');

const { buildSavePayload } = require('../continuum-lemma-composition/continuum-lemma-prompt-editor.js');

test('buildSavePayload includes prompt, timing, spatial, and children', () => {
  const payload = buildSavePayload({
    promptText: '{P:oven}',
    tMin: 5,
    tMax: 100,
    centerX: 1,
    centerY: 2,
    centerZ: 3,
    sizeX: 4,
    sizeY: 5,
    sizeZ: 6,
    spatial4dId: 'sid-1',
    patchProperties: { 'non-ik-animation': 'true' },
    children: [{ entryId: 'a', term: 'oven', patchProperties: { x: '1' } }],
  });
  assert.equal(payload.lemmaPrompt, '{P:oven}');
  assert.equal(payload.timing.tMin, 5);
  assert.equal(payload.timing.tMax, 100);
  assert.equal(payload.spatial.bounds.centerX, 1);
  assert.equal(payload.spatial.spatial4dId, 'sid-1');
  assert.equal(payload.compositionChildren.length, 1);
  assert.equal(payload.compositionChildren[0].entryId, 'a');
  assert.equal(payload.patchProperties['non-ik-animation'], 'true');
});

test('buildSavePayload uses compEditor children when present', () => {
  const payload = buildSavePayload({
    promptText: 'hi',
    tMin: 0,
    tMax: 3600,
    centerX: 0,
    centerY: 0,
    centerZ: 0,
    sizeX: 1,
    sizeY: 1,
    sizeZ: 1,
    compEditor: {
      getChildren: () => [{ entryId: 'c1', term: 'car' }],
    },
  });
  assert.equal(payload.compositionChildren[0].term, 'car');
});
