/**
 * Thin REST helpers for script-output Playwright fixtures.
 * Uses the same headers as ContinuuuumUserSession in the browser.
 */

const DEFAULT_USER = 'e2e-lemma-author';

/**
 * @param {string} baseURL
 * @param {string} userId
 */
function apiHeaders(userId = DEFAULT_USER) {
  return {
    'Content-Type': 'application/json',
    'X-User-ID': userId,
  };
}

/**
 * @param {string} baseURL
 * @param {string} path
 * @param {{ method?: string, body?: unknown, userId?: string }} [opts]
 */
async function api(baseURL, path, opts = {}) {
  const res = await fetch(`${baseURL}/api${path}`, {
    method: opts.method || 'GET',
    headers: apiHeaders(opts.userId),
    body: opts.body != null ? JSON.stringify(opts.body) : undefined,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(data.error || res.statusText || `HTTP ${res.status}`);
    /** @type {any} */ (err).status = res.status;
    /** @type {any} */ (err).payload = data;
    throw err;
  }
  return data;
}

/**
 * @param {string} baseURL
 * @param {{ scriptText?: string, title?: string, userId?: string }} [opts]
 */
async function createDraftFixture(baseURL, opts = {}) {
  const userId = opts.userId || DEFAULT_USER;
  const scriptText = opts.scriptText ?? 'ALICE\nHello brave world.';
  const title = opts.title || `E2E lemma clause ${Date.now()}`;

  const draft = await api(baseURL, '/drafts/episodes', {
    method: 'POST',
    userId,
    body: { title, createdBy: userId },
  });

  await api(baseURL, `/drafts/episodes/${draft.id}/script`, {
    method: 'PUT',
    userId,
    body: { scriptText, language: 'en' },
  });

  const scriptRow = await api(baseURL, `/drafts/episodes/${draft.id}/script`, { userId });

  return {
    draftId: draft.id,
    draftScriptId: scriptRow.id,
    scriptText,
    userId,
  };
}

/**
 * Seed a reusable lemma + binding template so auto-map suggestion buttons appear.
 *
 * @param {string} baseURL
 * @param {{ term?: string, selectionText?: string, prefabId?: string, userId?: string }} [opts]
 */
async function seedLemmaSuggestionTemplate(baseURL, opts = {}) {
  const userId = opts.userId || DEFAULT_USER;
  const term = opts.term || 'ALICE';
  const selectionText = opts.selectionText || term;
  const prefabId = opts.prefabId || 'e2e-prefab-alice';

  const entryRes = await api(baseURL, '/thesaurus/entries', {
    method: 'POST',
    userId,
    body: {
      word: term,
      partOfSpeech: 'noun',
      language: 'en',
      prefabId,
      description: 'E2E lemma template for script-output clause panel tests',
    },
  });
  const entryId = entryRes.entry?.id;
  if (!entryId) throw new Error('Failed to create lemma entry for E2E template');

  const templateDraft = await createDraftFixture(baseURL, {
    userId,
    scriptText: `${selectionText}\nTemplate line.`,
    title: `E2E lemma template ${Date.now()}`,
  });

  await api(baseURL, '/thesaurus/clause-bindings', {
    method: 'POST',
    userId,
    body: {
      draftScriptId: templateDraft.draftScriptId,
      draftEpisodeId: templateDraft.draftId,
      scriptText: templateDraft.scriptText,
      charStart: 0,
      charEnd: selectionText.length,
      selectionText,
      bindingKind: 'lemma',
      propertyKey: 'entry-id',
      propertyValue: entryId,
      entryId,
      fareyLeftNum: 0,
      fareyLeftDen: Math.max(templateDraft.scriptText.length, 1),
      fareyRightNum: selectionText.length,
      fareyRightDen: Math.max(templateDraft.scriptText.length, 1),
    },
  });

  return { entryId, term, selectionText, prefabId, templateDraftId: templateDraft.draftId };
}

/**
 * @param {string} baseURL
 * @param {string} draftId
 * @param {string} [userId]
 */
async function listClauseBindings(baseURL, draftId, userId = DEFAULT_USER) {
  const data = await api(
    baseURL,
    `/thesaurus/clause-bindings?draftEpisodeId=${encodeURIComponent(draftId)}`,
    { userId },
  );
  return data.items || [];
}

module.exports = {
  DEFAULT_USER,
  api,
  createDraftFixture,
  seedLemmaSuggestionTemplate,
  listClauseBindings,
};
