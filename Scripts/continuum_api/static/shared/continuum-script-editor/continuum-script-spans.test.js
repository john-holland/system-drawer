const test = require('node:test');
const assert = require('node:assert/strict');
const {
  computeEditRegions,
  shiftSpan,
  fareyToCharSpan,
  displayBindingSpans,
  buildOverlaySpans,
  bindingsAtRange,
  spansOverlap,
} = require('./continuum-script-spans.js');

test('computeEditRegions detects prefix insert', () => {
  const regions = computeEditRegions('hello world', 'PREFIX hello world');
  assert.equal(regions.length, 1);
  assert.equal(regions[0].offset, 0);
  assert.equal(regions[0].oldLen, 0);
  assert.equal(regions[0].newLen, 7);
});

test('shiftSpan moves binding when text inserted before clause', () => {
  const edit = { offset: 0, oldLen: 0, newLen: 4, delta: 4 };
  const next = shiftSpan(10, 16, edit);
  assert.equal(next.charStart, 14);
  assert.equal(next.charEnd, 20);
  assert.equal(next.shifted, true);
  assert.equal(next.overlapped, false);
});

test('displayBindingSpans tracks live insert before clause', () => {
  const oldText = 'AAAA clause BBBB';
  const newText = 'XXXX AAAA clause BBBB';
  const bindings = [{ id: 'b1', charStart: 5, charEnd: 11, selectionText: 'clause' }];
  const shifted = displayBindingSpans(oldText, newText, bindings);
  assert.equal(shifted[0].charStart, 10);
  assert.equal(shifted[0].charEnd, 16);
});

test('fareyToCharSpan maps proportional interval', () => {
  const text = 'abcdefghij';
  const span = fareyToCharSpan(text, { fareyLeftNum: 1, fareyLeftDen: 5, fareyRightNum: 1, fareyRightDen: 2 });
  assert.equal(span.charStart, 2);
  assert.equal(span.charEnd, 5);
});

test('buildOverlaySpans includes shifted clause underline range', () => {
  const oldText = 'start clause end';
  const newText = 'NEW start clause end';
  const bindings = [{ charStart: 6, charEnd: 12, selectionText: 'clause', bindingKind: 'property' }];
  const spans = buildOverlaySpans(newText, oldText, bindings, []);
  const clause = spans.find((s) => s.kind === 'clause');
  assert.ok(clause);
  assert.equal(clause.charStart, 10);
  assert.equal(clause.charEnd, 16);
});

test('bindingsAtRange matches caret inside clause', () => {
  const text = 'AAAA clause BBBB';
  const bindings = [{ id: 'b1', charStart: 5, charEnd: 11, selectionText: 'clause' }];
  const hits = bindingsAtRange(text, text, bindings, 7, 7);
  assert.equal(hits.length, 1);
  assert.equal(hits[0].id, 'b1');
});

test('bindingsAtRange matches selection spanning two clauses', () => {
  const text = 'aa ONE bb TWO cc';
  const bindings = [
    { id: 'b1', charStart: 3, charEnd: 6, selectionText: 'ONE' },
    { id: 'b2', charStart: 10, charEnd: 13, selectionText: 'TWO' },
  ];
  const hits = bindingsAtRange(text, text, bindings, 2, 12);
  assert.equal(hits.length, 2);
});

test('bindingsAtRange uses shifted display spans after prefix insert', () => {
  const oldText = 'AAAA clause BBBB';
  const newText = 'XXXX AAAA clause BBBB';
  const bindings = [{ id: 'b1', charStart: 5, charEnd: 11, selectionText: 'clause' }];
  const hits = bindingsAtRange(oldText, newText, bindings, 12, 12);
  assert.equal(hits.length, 1);
  assert.equal(hits[0].charStart, 10);
});
