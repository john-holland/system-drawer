/**
 * Browser-side helpers for the Ace script editor on script-output.
 */

/**
 * @param {import('@playwright/test').Page} page
 * @param {string} text
 */
async function setScriptText(page, text) {
  await page.evaluate((value) => {
    const inst = window.ContinuuuumScriptOutput?._state?.editorInst;
    if (!inst?.editor?.setValue) throw new Error('Ace editor not ready');
    inst.editor.setValue(value, -1);
    inst.editor.clearSelection();
    window.ContinuuuumScriptEditor.renderClausePanel(inst);
    window.ContinuuuumScriptEditor.renderClauseSuggestions(inst);
  }, text);
}

/**
 * @param {import('@playwright/test').Page} page
 * @param {string} needle
 * @param {string} replacement
 */
async function replaceInScript(page, needle, replacement) {
  await page.evaluate(({ from, to }) => {
    const inst = window.ContinuuuumScriptOutput?._state?.editorInst;
    if (!inst?.editor?.setValue) throw new Error('Ace editor not ready');
    const next = inst.editor.getValue().replace(from, to);
    inst.editor.setValue(next, -1);
    inst.editor.clearSelection();
  }, { from: needle, to: replacement });
}

/**
 * @param {import('@playwright/test').Page} page
 * @param {string} needle
 */
async function selectScriptText(page, needle) {
  const found = await page.evaluate((text) => {
    const out = window.ContinuuuumScriptOutput?._state?.editorInst;
    if (!out?.editor?.session) return { ok: false, reason: 'editor not mounted' };
    const doc = out.editor.session.getDocument();
    const full = doc.getValue();
    const idx = full.indexOf(text);
    if (idx < 0) return { ok: false, reason: `text not found: ${text}`, full: full.slice(0, 80) };
    const start = doc.indexToPosition(idx);
    const end = doc.indexToPosition(idx + text.length);
    const Range = window.ace.require('ace/range').Range;
    out.editor.focus();
    out.editor.selection.setRange(new Range(start.row, start.column, end.row, end.column));
    out.editor.renderer.scrollToLine(start.row, true, true, () => {});
    return { ok: true, charStart: idx, charEnd: idx + text.length };
  }, needle);
  if (!found.ok) {
    throw new Error(`selectScriptText: ${found.reason}`);
  }
  return found;
}

/**
 * @param {import('@playwright/test').Page} page
 */
async function getLemmaClauseCardCount(page) {
  return page.locator('#so-lemma-panel .continuuuum-clause-card').count();
}

/**
 * @param {import('@playwright/test').Page} page
 */
async function getLemmaClauseLemmaCount(page) {
  return page.locator('#so-lemma-panel .continuuuum-clause-kind-lemma').count();
}

/**
 * @param {import('@playwright/test').Page} page
 */
async function waitForLemmaPanelNotEmpty(page) {
  await page.locator('#so-lemma-panel .continuuuum-clause-card').first().waitFor({ state: 'visible' });
}

/**
 * @param {import('@playwright/test').Page} page
 */
async function lemmaPanelHeading(page) {
  return page.locator('#so-lemma-panel .continuuuum-clause-panel-heading').innerText();
}

/**
 * @param {import('@playwright/test').Page} page
 * @param {string} draftId
 * @param {string} [userId]
 */
async function openScriptOutputDraft(page, draftId, userId = 'e2e-lemma-author') {
  const q = new URLSearchParams({ draftId, userId });
  await page.goto(`/script-output?${q.toString()}`);
  await page.waitForFunction(() => {
    const st = window.ContinuuuumScriptOutput?._state;
    return st?.editorInst?.editor && st.activeId;
  });
  await page.locator('#status').waitFor({ state: 'visible' });
}

/**
 * @param {import('@playwright/test').Page} page
 */
async function clickAttachClause(page) {
  await page.locator('#editor-host .continuuuum-script-toolbar button')
    .filter({ hasText: 'Attach clause' })
    .click();
}

/**
 * Ack all required change-list checkboxes and click a modal button.
 *
 * @param {import('@playwright/test').Page} page
 * @param {'Save'|'Submit for review'} actionLabel
 */
async function dismissChangeListModal(page, actionLabel) {
  const modalRoot = page.locator('body > div').filter({ has: page.locator('#cl-save') }).last();
  await modalRoot.waitFor({ state: 'visible' });
  const boxes = modalRoot.locator('#cl-required input[type="checkbox"]');
  const n = await boxes.count();
  for (let i = 0; i < n; i += 1) {
    await boxes.nth(i).check();
  }
  await modalRoot.getByRole('button', { name: actionLabel, exact: true }).click();
  await page.locator('#cl-save').waitFor({ state: 'hidden' });
}

module.exports = {
  setScriptText,
  replaceInScript,
  selectScriptText,
  getLemmaClauseCardCount,
  getLemmaClauseLemmaCount,
  waitForLemmaPanelNotEmpty,
  lemmaPanelHeading,
  openScriptOutputDraft,
  clickAttachClause,
  dismissChangeListModal,
};
