'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const src = fs.readFileSync(path.join(__dirname, 'continuuuum-lemma-entry.js'), 'utf8');
const sandbox = { window: {}, globalThis: {} };
sandbox.window = sandbox;
vm.runInNewContext(src, sandbox);
const LE = sandbox.ContinuuuumLemmaEntry;

assert.equal(LE.entryUrl('abc-123'), '/lemma-library#entry/abc-123');
assert.equal(LE.entryUrl('urn:continuuuum:lemma/en/noun/test'), '/lemma-library#entry/urn%3Acontinuuuum%3Alemma%2Fen%2Fnoun%2Ftest');

const n = LE.normalize({ entryId: 'e1', term: 'oven' });
assert.equal(n.entryId, 'e1');
assert.equal(n.term, 'oven');

assert.ok(LE.chipHtml(n).includes('continuuuum-lemma-entry-chip'));
assert.ok(LE.chipHtml(n).includes('oven'));
assert.ok(!LE.chipHtml({ term: 'x' }).includes('<script'));

console.log('continuuuum-lemma-entry.test.js: ok');
