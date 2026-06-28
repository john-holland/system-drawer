/** Pure span helpers for clause overlays and edit-shift preview (Node + browser). */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) {
    module.exports = api;
  } else {
    root.ContinuumScriptSpans = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  const P_RE = /\{\{?P:[^}]+\}?\}?|\{P:[^}]+\}/g;

  function computeEditRegions(oldText, newText) {
    oldText = oldText || '';
    newText = newText || '';
    if (oldText === newText) return [];
    let prefix = 0;
    const maxPrefix = Math.min(oldText.length, newText.length);
    while (prefix < maxPrefix && oldText[prefix] === newText[prefix]) prefix += 1;
    let suffix = 0;
    const maxSuffix = Math.min(oldText.length - prefix, newText.length - prefix);
    while (
      suffix < maxSuffix &&
      oldText[oldText.length - 1 - suffix] === newText[newText.length - 1 - suffix]
    ) {
      suffix += 1;
    }
    const oldLen = oldText.length - prefix - suffix;
    const newLen = newText.length - prefix - suffix;
    if (oldLen === 0 && newLen === 0) return [];
    return [{ offset: prefix, oldLen, newLen, delta: newLen - oldLen }];
  }

  function firstLetterInSelection(binding) {
    const sel = binding.selectionText || binding.selection_text || '';
    for (let i = 0; i < sel.length; i++) {
      const ch = sel[i];
      if (ch.trim() && ch !== '\n' && ch !== '\r' && ch !== '\t') {
        return { letter: ch, offsetInSelection: i };
      }
    }
    return sel.length ? { letter: sel[0], offsetInSelection: 0 } : null;
  }

  function reanchorSpanByFirstLetter(currentText, binding, shiftedStart, shiftedEnd) {
    const sel = binding.selectionText || binding.selection_text || '';
    const anchor = firstLetterInSelection(binding);
    if (!anchor || shiftedEnd <= shiftedStart || !sel) {
      return { charStart: shiftedStart, charEnd: shiftedEnd };
    }
    const spanLen = shiftedEnd - shiftedStart;
    const expectLetterPos = shiftedStart + anchor.offsetInSelection;
    const slack = Math.max(40, sel.length + 20);
    const searchFrom = Math.max(0, expectLetterPos - slack);
    const searchTo = Math.min(currentText.length, expectLetterPos + slack);
    let bestStart = shiftedStart;
    let bestScore = -1;
    for (let pos = searchFrom; pos < searchTo; pos++) {
      if (currentText[pos] !== anchor.letter) continue;
      const start = pos - anchor.offsetInSelection;
      if (start < 0) continue;
      const slice = currentText.slice(start, start + sel.length);
      let score = 0;
      const cmpLen = Math.min(slice.length, sel.length);
      for (let j = 0; j < cmpLen; j++) {
        if (slice[j] === sel[j]) score += 1;
      }
      const dist = Math.abs(start - shiftedStart);
      const combined = score * 1000 - dist;
      if (combined > bestScore) {
        bestScore = combined;
        bestStart = start;
      }
    }
    if (bestScore >= 0) {
      return { charStart: bestStart, charEnd: bestStart + spanLen };
    }
    return { charStart: shiftedStart, charEnd: shiftedEnd };
  }

  function mapPointThroughEdit(p, edit, bias) {
    const offset = edit.offset;
    const oldLen = edit.oldLen;
    const newLen = edit.newLen;
    if (oldLen === 0) {
      if (bias === 'end') {
        if (p <= offset) return p;
        return p + newLen;
      }
      if (p < offset) return p;
      return p + newLen;
    }
    const editEnd = offset + oldLen;
    if (p <= offset) return p;
    if (p >= editEnd) return p + edit.delta;
    return offset;
  }

  function shiftSpan(charStart, charEnd, edit) {
    const newStart = mapPointThroughEdit(charStart, edit, 'start');
    const newEnd = mapPointThroughEdit(charEnd, edit, 'end');
    const editEnd = edit.offset + edit.oldLen;
    const overlapped = editEnd > charStart && edit.offset < charEnd;
    const shifted = newStart !== charStart || newEnd !== charEnd;
    return { charStart: newStart, charEnd: newEnd, overlapped, shifted };
  }

  function resolveBindingCharSpan(text, binding) {
    let cs = binding.charStart ?? binding.char_start ?? 0;
    let ce = binding.charEnd ?? binding.char_end ?? 0;
    if (ce > cs) return { charStart: cs, charEnd: ce };
    const ln = binding.fareyLeftNum ?? binding.farey_left_num ?? 0;
    const ld = binding.fareyLeftDen ?? binding.farey_left_den ?? 1;
    const rn = binding.fareyRightNum ?? binding.farey_right_num ?? 1;
    const rd = binding.fareyRightDen ?? binding.farey_right_den ?? 1;
    return fareyToCharSpan(text, { fareyLeftNum: ln, fareyLeftDen: ld, fareyRightNum: rn, fareyRightDen: rd });
  }

  function fareyToCharSpan(text, binding) {
    const n = Math.max((text || '').length, 1);
    const ln = binding.fareyLeftNum ?? binding.farey_left_num ?? 0;
    const ld = binding.fareyLeftDen ?? binding.farey_left_den ?? 1;
    const rn = binding.fareyRightNum ?? binding.farey_right_num ?? 1;
    const rd = binding.fareyRightDen ?? binding.farey_right_den ?? 1;
    if (ld <= 0 || rd <= 0) return { charStart: 0, charEnd: 0 };
    const charStart = Math.max(0, Math.min(n, Math.floor((ln * n) / ld)));
    const charEnd = Math.max(charStart, Math.min(n, Math.floor((rn * n) / rd)));
    return { charStart, charEnd };
  }

  function editAllowsReanchor(edit, snapshotText, spanStart, spanEnd) {
    if (edit.oldLen === 0) return true;
    if (edit.offset >= spanEnd || edit.offset + edit.oldLen <= spanStart) return false;
    const deleted = (snapshotText || '').slice(edit.offset, edit.offset + edit.oldLen);
    return deleted.trim() === '';
  }

  function displayBindingSpans(snapshotText, currentText, bindings) {
    const regions = computeEditRegions(snapshotText || '', currentText || '');
    return (bindings || []).map((binding) => {
      let span = resolveBindingCharSpan(snapshotText || '', binding);
      let cs = span.charStart;
      let ce = span.charEnd;
      let overlapped = false;
      for (const edit of regions) {
        const next = shiftSpan(cs, ce, edit);
        if (next.overlapped) overlapped = true;
        cs = next.charStart;
        ce = next.charEnd;
      }
      if (overlapped && regions.some((edit) => editAllowsReanchor(edit, snapshotText, span.charStart, span.charEnd))) {
        const reanchored = reanchorSpanByFirstLetter(currentText || '', binding, cs, ce);
        cs = reanchored.charStart;
        ce = reanchored.charEnd;
      }
      const anchor = firstLetterInSelection(binding);
      const out = { ...binding, charStart: cs, charEnd: ce };
      if (anchor) {
        out._anchorLetter = anchor.letter;
        out._anchorOffsetInSelection = anchor.offsetInSelection;
      }
      return out;
    });
  }

  function parsePromptSpans(text) {
    const spans = [];
    if (!text) return spans;
    let m;
    P_RE.lastIndex = 0;
    while ((m = P_RE.exec(text)) !== null) {
      spans.push({ charStart: m.index, charEnd: m.index + m[0].length, kind: 'prompt', text: m[0] });
    }
    return spans;
  }

  function buildOverlaySpans(currentText, snapshotText, clauseBindings, reviewComments) {
    const shifted = displayBindingSpans(snapshotText ?? currentText, currentText, clauseBindings);
    return [
      ...parsePromptSpans(currentText),
      ...shifted.map((b) => ({
        charStart: b.charStart,
        charEnd: b.charEnd,
        kind: 'clause',
        text: (b.bindingKind || b.binding_kind || 'property') + ': ' + (b.selectionText || b.selection_text || b.propertyKey || b.property_key || ''),
      })),
      ...(reviewComments || []).map((c) => ({
        charStart: c.textSelectionStart,
        charEnd: c.textSelectionEnd,
        kind: 'comment',
        text: c.commentText,
      })),
    ];
  }

  function spansOverlap(rangeStart, rangeEnd, spanStart, spanEnd) {
    return rangeStart < spanEnd && spanStart < rangeEnd;
  }

  function bindingsAtRange(snapshotText, currentText, bindings, rangeStart, rangeEnd) {
    let rs = Number.isFinite(rangeStart) ? rangeStart : 0;
    let re = Number.isFinite(rangeEnd) ? rangeEnd : rs;
    if (re <= rs) re = rs + 1;
    return displayBindingSpans(snapshotText ?? currentText, currentText, bindings).filter(
      (b) => b.charEnd > b.charStart && spansOverlap(rs, re, b.charStart, b.charEnd),
    );
  }

  return {
    computeEditRegions,
    mapPointThroughEdit,
    shiftSpan,
    firstLetterInSelection,
    reanchorSpanByFirstLetter,
    resolveBindingCharSpan,
    fareyToCharSpan,
    displayBindingSpans,
    parsePromptSpans,
    buildOverlaySpans,
    spansOverlap,
    bindingsAtRange,
  };
});
