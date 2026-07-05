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
  isLemmaAnchorMismatch,
  proposeLemmaReanchor,
  mismatchedLemmaBindingsInRange,
  liveSliceAtBinding,
} = require('./continuuuum-script-spans.js');

test('computeEditRegions detects prefix insert', () => {
  const regions = computeEditRegions('hello world', 'PREFIX hello world');
  assert.equal(regions.length, 1);
  assert.equal(regions[0].offset, 0);
  assert.equal(regions[0].oldLen, 0);
  assert.equal(regions[0].newLen, 7);
});

test('shiftSpan shrinks binding when whitespace deleted inside clause', () => {
  const edit = { offset: 5, oldLen: 1, newLen: 0, delta: -1 };
  const next = shiftSpan(2, 8, edit);
  assert.equal(next.charStart, 2);
  assert.equal(next.charEnd, 7);
  assert.equal(next.overlapped, true);
});

test('shiftSpan expands binding when whitespace inserted inside clause', () => {
  const edit = { offset: 5, oldLen: 0, newLen: 1, delta: 1 };
  const next = shiftSpan(2, 8, edit);
  assert.equal(next.charStart, 2);
  assert.equal(next.charEnd, 9);
  assert.equal(next.overlapped, true);
});

test('displayBindingSpans tracks whitespace delete inside clause text', () => {
  const oldText = 'X clause Y';
  const newText = 'X cluse Y';
  const bindings = [{ id: 'b1', charStart: 2, charEnd: 8, selectionText: 'clause' }];
  const shifted = displayBindingSpans(oldText, newText, bindings);
  assert.equal(shifted[0].charStart, 2);
  assert.equal(shifted[0].charEnd, 7);
});

test('displayBindingSpans tracks whitespace insert inside clause text', () => {
  const oldText = 'X clause Y';
  const newText = 'X cla use Y';
  const bindings = [{ id: 'b1', charStart: 2, charEnd: 8, selectionText: 'clause' }];
  const shifted = displayBindingSpans(oldText, newText, bindings);
  assert.equal(shifted[0].charStart, 2);
  assert.equal(shifted[0].charEnd, 9);
});

test('buildOverlaySpans moves underline when space deleted before clause', () => {
  const oldText = 'X clause Y';
  const newText = 'Xclause Y';
  const bindings = [{ charStart: 2, charEnd: 8, selectionText: 'clause', bindingKind: 'lemma' }];
  const spans = buildOverlaySpans(newText, oldText, bindings, []);
  const clause = spans.find((s) => s.kind === 'clause');
  assert.ok(clause);
  assert.equal(clause.charStart, 1);
  assert.equal(clause.charEnd, 7);
});

test('bindingsAtRange uses shifted span after whitespace edit inside clause', () => {
  const oldText = 'X clause Y';
  const newText = 'X cla use Y';
  const bindings = [{ id: 'b1', charStart: 2, charEnd: 8, selectionText: 'clause' }];
  const hits = bindingsAtRange(oldText, newText, bindings, 4, 4);
  assert.equal(hits.length, 1);
  assert.equal(hits[0].charEnd, 9);
});

test('reanchorSpanByFirstLetter relocates clause after text inserted before first letter', () => {
  const oldText = 'AAAA clause BBBB';
  const newText = 'AAAA X clause BBBB';
  const bindings = [{ id: 'b1', charStart: 5, charEnd: 11, selectionText: 'clause' }];
  const shifted = displayBindingSpans(oldText, newText, bindings);
  assert.equal(shifted[0].charStart, 7);
  assert.equal(shifted[0].charEnd, 13);
  assert.equal(shifted[0]._anchorLetter, 'c');
});

test('reanchorSpanByFirstLetter keeps clause aligned when prefix text grows', () => {
  const oldText = 'X clause Y';
  const newText = 'LONGER X clause Y';
  const bindings = [{ id: 'b1', charStart: 2, charEnd: 8, selectionText: 'clause' }];
  const shifted = displayBindingSpans(oldText, newText, bindings);
  assert.equal(shifted[0].charStart, 9);
  assert.equal(shifted[0].charEnd, 15);
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

test('isLemmaAnchorMismatch when stored span points at wrong text', () => {
  const text = 'before clause after';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 0,
    charEnd: 6,
    selectionText: 'clause',
  };
  assert.equal(isLemmaAnchorMismatch(binding, text, text), true);
  assert.equal(liveSliceAtBinding(binding, text, text).slice, 'before');
});

test('isLemmaAnchorMismatch false when display-shifted span matches selectionText', () => {
  const oldText = 'AAAA clause BBBB';
  const newText = 'XXXX AAAA clause BBBB';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 5,
    charEnd: 11,
    selectionText: 'clause',
  };
  assert.equal(isLemmaAnchorMismatch(binding, oldText, newText), false);
  assert.equal(liveSliceAtBinding(binding, oldText, newText).slice, 'clause');
});

test('isLemmaAnchorMismatch true when script text edited in place', () => {
  const text = 'AAAA cluse BBBB';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 5,
    charEnd: 11,
    selectionText: 'clause',
  };
  assert.equal(isLemmaAnchorMismatch(binding, text, text), true);
});

test('isLemmaAnchorMismatch ignores non-lemma bindings', () => {
  const text = 'hello world';
  const binding = {
    id: 'b1',
    bindingKind: 'property',
    charStart: 0,
    charEnd: 5,
    selectionText: 'hello',
  };
  assert.equal(isLemmaAnchorMismatch(binding, text, text), false);
});

test('proposeLemmaReanchor returns new indices when fixable', () => {
  const text = 'before clause after';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 0,
    charEnd: 6,
    selectionText: 'clause',
  };
  const proposal = proposeLemmaReanchor(binding, text, text);
  assert.ok(proposal);
  assert.equal(proposal.charStart, 7);
  assert.equal(proposal.charEnd, 13);
  assert.equal(text.substring(proposal.charStart, proposal.charEnd), 'clause');
});

test('proposeLemmaReanchor returns null when selectionText absent from script', () => {
  const text = 'AAAA BBBB';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 5,
    charEnd: 11,
    selectionText: 'clause',
  };
  assert.equal(proposeLemmaReanchor(binding, text, text), null);
});

test('proposeLemmaReanchor returns null when span already correct', () => {
  const text = 'AAAA clause BBBB';
  const binding = {
    id: 'b1',
    bindingKind: 'lemma',
    charStart: 5,
    charEnd: 11,
    selectionText: 'clause',
  };
  assert.equal(proposeLemmaReanchor(binding, text, text), null);
});

test('mismatchedLemmaBindingsInRange respects overlap and ignores property bindings', () => {
  const text = 'aa ONE bb TWO cc';
  const bindings = [
    {
      id: 'b1',
      bindingKind: 'lemma',
      charStart: 3,
      charEnd: 6,
      selectionText: 'OEN',
    },
    {
      id: 'b2',
      bindingKind: 'property',
      charStart: 10,
      charEnd: 13,
      selectionText: 'TWO',
    },
    {
      id: 'b3',
      bindingKind: 'lemma',
      charStart: 10,
      charEnd: 13,
      selectionText: 'TWO',
    },
  ];
  const hits = mismatchedLemmaBindingsInRange(text, text, bindings, 3, 6);
  assert.equal(hits.length, 1);
  assert.equal(hits[0].id, 'b1');
});
