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

  function shiftSpan(charStart, charEnd, edit) {
    const s = charStart;
    const e = charEnd;
    const editEnd = edit.offset + edit.oldLen;
    if (edit.oldLen === 0) {
      if (edit.offset >= e) return { charStart: s, charEnd: e, overlapped: false, shifted: false };
      if (edit.offset < s) {
        return { charStart: s + edit.delta, charEnd: e + edit.delta, overlapped: false, shifted: true };
      }
      if (edit.offset < e) return { charStart: s, charEnd: e, overlapped: true, shifted: false };
      return { charStart: s, charEnd: e, overlapped: false, shifted: false };
    }
    if (editEnd <= s) return { charStart: s, charEnd: e, overlapped: false, shifted: false };
    if (edit.offset >= e) {
      return { charStart: s + edit.delta, charEnd: e + edit.delta, overlapped: false, shifted: true };
    }
    return { charStart: s, charEnd: e, overlapped: true, shifted: false };
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

  function displayBindingSpans(snapshotText, currentText, bindings) {
    const regions = computeEditRegions(snapshotText || '', currentText || '');
    return (bindings || []).map((binding) => {
      let span = resolveBindingCharSpan(snapshotText || '', binding);
      let cs = span.charStart;
      let ce = span.charEnd;
      for (const edit of regions) {
        const next = shiftSpan(cs, ce, edit);
        cs = next.charStart;
        ce = next.charEnd;
      }
      return { ...binding, charStart: cs, charEnd: ce };
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
    shiftSpan,
    resolveBindingCharSpan,
    fareyToCharSpan,
    displayBindingSpans,
    parsePromptSpans,
    buildOverlaySpans,
    spansOverlap,
    bindingsAtRange,
  };
});
