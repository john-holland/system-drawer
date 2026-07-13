// @ts-check
const { test, expect } = require('@playwright/test');
const {
  DEFAULT_USER,
  createDraftFixture,
  seedLemmaSuggestionTemplate,
  listClauseBindings,
} = require('./helpers/continuuuum-api');
const {
  selectScriptText,
  setScriptText,
  replaceInScript,
  getLemmaClauseCardCount,
  getLemmaClauseLemmaCount,
  openScriptOutputDraft,
  clickAttachClause,
  dismissChangeListModal,
  lemmaPanelHeading,
} = require('./helpers/script-editor');

const LEMMA_TERM = 'ALICE';

test.describe('Lemma clause panel — script output', () => {
  /** @type {{ draftId: string, draftScriptId: string, scriptText: string, entryId: string }} */
  let fixture;

  test.beforeAll(async ({ request, baseURL }) => {
    if (!baseURL) throw new Error('baseURL is required');
    await seedLemmaSuggestionTemplate(baseURL, {
      term: LEMMA_TERM,
      selectionText: LEMMA_TERM,
      prefabId: 'e2e-prefab-alice',
      userId: DEFAULT_USER,
    });
  });

  test.beforeEach(async ({ page, baseURL }) => {
    await page.addInitScript((userId) => {
      localStorage.setItem('continuuuumUserId', userId);
    }, DEFAULT_USER);
    fixture = await createDraftFixture(baseURL, {
      userId: DEFAULT_USER,
      scriptText: `${LEMMA_TERM}\nHello brave world.`,
      title: `E2E lemma clause ${Date.now()}`,
    });
    await openScriptOutputDraft(page, fixture.draftId, DEFAULT_USER);
  });

  test('auto-map suggestion shows lemma in clause panel without script save', async ({ page, baseURL }) => {
    await selectScriptText(page, LEMMA_TERM);

    const suggestion = page.locator('.continuuuum-clause-suggestion-btn').filter({ hasText: /^Apply:/ }).first();
    await expect(suggestion).toBeVisible({ timeout: 20_000 });

    const bindingsBefore = await listClauseBindings(baseURL, fixture.draftId);
    const lemmaBefore = bindingsBefore.filter((b) => b.bindingKind === 'lemma').length;

    await suggestion.click();

    await expect.poll(async () => listClauseBindings(baseURL, fixture.draftId).then((items) => items.length))
      .toBeGreaterThan(bindingsBefore.length);

    const bindingsAfter = await listClauseBindings(baseURL, fixture.draftId);
    expect(bindingsAfter.filter((b) => b.bindingKind === 'lemma').length).toBe(lemmaBefore + 1);

    // UI regression: lemma clause section should list the new binding at selection.
    await expect.poll(() => getLemmaClauseLemmaCount(page)).toBeGreaterThan(0);
    await expect(page.locator('#so-lemma-panel .continuuuum-clause-kind-lemma').first()).toBeVisible();
    await expect.poll(() => lemmaPanelHeading(page)).toMatch(/Clauses at selection/);
  });

  test('create-lemma attach modal shows lemma in clause panel without script save', async ({ page, baseURL }) => {
    const uniqueTerm = `E2E${Date.now().toString(36)}`;
    const scriptText = `${uniqueTerm}\nHello brave world.`;
    await setScriptText(page, scriptText);
    await selectScriptText(page, uniqueTerm);

    const bindingsBefore = await listClauseBindings(baseURL, fixture.draftId);

    await clickAttachClause(page);
    const overlay = page.locator('.continuuuum-clause-overlay').last();
    await overlay.getByRole('button', { name: 'Lemma' }).click();
    await overlay.locator('#clause-lemma-word').fill(uniqueTerm);
    await overlay.locator('#clause-lemma-pos').selectOption('noun');
    await overlay.locator('#clause-lemma-prefab').fill('e2e-prefab-created');
    await overlay.getByRole('button', { name: 'Attach' }).click();
    await overlay.waitFor({ state: 'hidden' });

    await expect.poll(async () => listClauseBindings(baseURL, fixture.draftId).then((items) => items.length))
      .toBeGreaterThan(bindingsBefore.length);

    await expect.poll(() => getLemmaClauseLemmaCount(page)).toBeGreaterThan(0);
  });

  test('lemma clause panel after script save (change list save)', async ({ page, baseURL }) => {
    await selectScriptText(page, LEMMA_TERM);

    const suggestion = page.locator('.continuuuum-clause-suggestion-btn').filter({ hasText: /^Apply:/ }).first();
    await expect(suggestion).toBeVisible({ timeout: 20_000 });
    await suggestion.click();

    await expect.poll(() => listClauseBindings(baseURL, fixture.draftId).then((items) => items.length))
      .toBeGreaterThan(0);

    // Touch script text so save produces a change list (unsaved edit vs snapshot).
    await replaceInScript(page, 'brave', 'kind');
    await selectScriptText(page, LEMMA_TERM);

    await page.locator('#save-btn').click();
    const changeListModal = page.locator('body > div').filter({ has: page.locator('#cl-save') });
    if (await changeListModal.count()) {
      await dismissChangeListModal(page, 'Save');
    } else {
      // No diff items — script PUT may still have persisted via a no-op save path.
      await page.waitForTimeout(500);
    }

    await selectScriptText(page, LEMMA_TERM);
    await expect.poll(() => getLemmaClauseLemmaCount(page)).toBeGreaterThan(0);
    await expect(page.locator('#so-lemma-panel .continuuuum-clause-card').first()).toBeVisible();
  });

  test('lemma clause panel after submit for review', async ({ page, baseURL }) => {
    const reviewFixture = await createDraftFixture(baseURL, {
      userId: DEFAULT_USER,
      scriptText: `${LEMMA_TERM}\nReview path line.`,
      title: `E2E review lemma ${Date.now()}`,
    });

    await openScriptOutputDraft(page, reviewFixture.draftId, DEFAULT_USER);
    await selectScriptText(page, LEMMA_TERM);

    const suggestion = page.locator('.continuuuum-clause-suggestion-btn').filter({ hasText: /^Apply:/ }).first();
    await expect(suggestion).toBeVisible({ timeout: 20_000 });
    await suggestion.click();

    await expect.poll(async () => listClauseBindings(baseURL, reviewFixture.draftId).then((items) => items.length))
      .toBeGreaterThan(0);

    await replaceInScript(page, 'Review', 'Submitted');
    await selectScriptText(page, LEMMA_TERM);

    await page.locator('#save-btn').click();
    await dismissChangeListModal(page, 'Submit for review');

    await openScriptOutputDraft(page, reviewFixture.draftId, DEFAULT_USER);
    await expect(page.locator('#so-header')).toContainText(/in_review|submitted/i);
    await selectScriptText(page, LEMMA_TERM);

    await expect.poll(() => getLemmaClauseCardCount(page)).toBeGreaterThan(0);
    await expect.poll(() => getLemmaClauseLemmaCount(page)).toBeGreaterThan(0);
  });

  test('lemma panel after auto-map click without re-selecting (selection loss)', async ({ page, baseURL }) => {
    await selectScriptText(page, LEMMA_TERM);

    const suggestion = page.locator('.continuuuum-clause-suggestion-btn').filter({ hasText: /^Apply:/ }).first();
    await expect(suggestion).toBeVisible({ timeout: 20_000 });

    await suggestion.click();

    await expect.poll(async () => listClauseBindings(baseURL, fixture.draftId).then((items) => items.length))
      .toBeGreaterThan(0);

    // Do not re-select — mimics focus leaving Ace when clicking the suggestion chip.
    await expect.poll(() => getLemmaClauseLemmaCount(page)).toBeGreaterThan(0);
  });
});

test.describe('Auto add single lemmas', () => {
  test('settings page swaps duplicate priority slot values', async ({ page, baseURL }) => {
    if (!baseURL) throw new Error('baseURL is required');
    await page.addInitScript(() => {
      localStorage.setItem('continuuuumSettings', JSON.stringify({
        scriptOutput: {
          autoAddPriority: ['builtin', 'prefab', 'localization', 'mod_slot', 'prompt_placeholder', 'new_lemma'],
          newLemmaRequired: false,
        },
      }));
    });
    await page.goto(`${baseURL}/settings#script-output`);
    await expect(page.locator('#cs-priority-0')).toBeVisible();
    await page.locator('#cs-priority-0').selectOption('prefab');
    await expect(page.locator('#cs-priority-0')).toHaveValue('prefab');
    const values = await page.locator('select[data-slot]').evaluateAll((els) => els.map((el) => /** @type {HTMLSelectElement} */ (el).value));
    expect(values.filter((v) => v === 'prefab').length).toBe(1);
    expect(values.filter((v) => v === 'builtin').length).toBe(1);
  });

  test('bulk auto-add attaches single-suggestion span without manual save', async ({ page, baseURL }) => {
    if (!baseURL) throw new Error('baseURL is required');
    await page.addInitScript((userId) => {
      localStorage.setItem('continuuuumUserId', userId);
    }, DEFAULT_USER);
    await seedLemmaSuggestionTemplate(baseURL, {
      term: 'ALICE',
      selectionText: 'ALICE',
      userId: DEFAULT_USER,
    });
    const fixture = await createDraftFixture(baseURL, {
      userId: DEFAULT_USER,
      scriptText: 'ALICE\nSecond line only.',
      title: `E2E auto-add ${Date.now()}`,
    });
    await openScriptOutputDraft(page, fixture.draftId, DEFAULT_USER);
    const bindingsBefore = await listClauseBindings(baseURL, fixture.draftId);
    const dialogPromise = page.waitForEvent('dialog').then((dialog) => dialog.accept());
    await page.locator('#auto-add-lemmas-btn').click();
    await dialogPromise;
    await expect.poll(async () => listClauseBindings(baseURL, fixture.draftId).then((items) => items.length))
      .toBeGreaterThan(bindingsBefore.length);
    const bindingsAfter = await listClauseBindings(baseURL, fixture.draftId);
    const aliceBinding = bindingsAfter.find((b) => (b.selectionText || '').toUpperCase() === 'ALICE');
    expect(aliceBinding).toBeTruthy();
  });
});
