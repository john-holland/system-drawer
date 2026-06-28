/* Shared lemma search picker — used by clause selector and composition editor */
(function (global) {
  const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  function escHtml(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function debounce(fn, ms) {
    let t;
    return function debounced(...args) {
      clearTimeout(t);
      t = setTimeout(() => fn.apply(this, args), ms);
    };
  }

  function pickAutoSelectLemma(items, query) {
    const q = (query || '').trim().toLowerCase();
    if (!q) return null;
    const exact = items.filter((e) => (e.term || '').toLowerCase() === q);
    if (exact.length === 1) return exact[0];
    if (exact.length > 1) {
      const builtin = exact.find((e) => e.isBuiltIn);
      return builtin || exact[0];
    }
    return null;
  }

  function renderResultButtons(resultsEl, items, onPick) {
    resultsEl.innerHTML = items.map((e) =>
      `<button type="button" class="clp-hit" data-id="${escHtml(e.id)}" ` +
      'style="display:block;width:100%;text-align:left;padding:6px 8px;border:none;border-bottom:1px solid #eee;background:#fafafa;cursor:pointer">' +
      `<strong>${escHtml(e.term)}</strong> ` +
      `<span style="color:#666;font-size:12px">${escHtml(e.posTag || '')} · ${escHtml(e.languageCode || '')}` +
      `${e.isBuiltIn ? ' · built-in' : ''}</span></button>`,
    ).join('');
    resultsEl.style.display = items.length ? 'block' : 'none';
    resultsEl.querySelectorAll('.clp-hit').forEach((btn) => {
      btn.onclick = () => {
        const item = items.find((x) => x.id === btn.dataset.id);
        if (!item) return;
        if (onPick) onPick(item);
      };
    });
  }

  /**
   * Mount lemma search UI into container.
   * opts: { callApi, onSelect(entry|null), excludeIds?: string[], placeholder?: string }
   * Selection is manual: Enter/Return or confirm() — not on search match.
   */
  function mountSearch(container, opts) {
    opts = opts || {};
    const callApi = opts.callApi;
    if (!container || !callApi) return null;

    container.innerHTML =
      '<label class="clp-search-label">Search lemma' +
      `<input type="search" class="clp-search-input" autocomplete="off" placeholder="${escHtml(opts.placeholder || 'Word, definition, synonyms…')}" style="width:100%;box-sizing:border-box"/>` +
      '</label>' +
      '<p class="clp-search-hint" style="margin:4px 0 0;font-size:12px;color:#666">Press Enter or Add lemma to confirm.</p>' +
      '<div class="clp-results" style="max-height:160px;overflow:auto;border:1px solid #ddd;border-radius:4px;margin:4px 0;display:none"></div>';

    const searchInp = container.querySelector('.clp-search-input');
    const resultsEl = container.querySelector('.clp-results');
    const exclude = new Set(opts.excludeIds || []);
    let lastItems = [];

    function pickEntry(entry) {
      if (!entry || !entry.id || exclude.has(entry.id)) return false;
      if (opts.onSelect) opts.onSelect(entry);
      searchInp.value = entry.term || searchInp.value;
      resultsEl.innerHTML = '';
      resultsEl.style.display = 'none';
      lastItems = [];
      return true;
    }

    async function confirmSelection() {
      const q = (searchInp.value || '').trim();
      if (!q) return false;

      if (UUID_RE.test(q)) {
        try {
          const entry = await callApi('GET', `/api/thesaurus/entries?entryId=${encodeURIComponent(q)}`);
          if (entry && entry.id && !exclude.has(entry.id)) {
            return pickEntry(entry);
          }
        } catch (_) { /* fall through */ }
      }

      const auto = pickAutoSelectLemma(lastItems, q);
      if (auto) return pickEntry(auto);

      if (lastItems.length === 1) return pickEntry(lastItems[0]);

      const focused = resultsEl.querySelector('.clp-hit:focus') || resultsEl.querySelector('.clp-hit');
      if (focused) {
        const item = lastItems.find((x) => x.id === focused.dataset.id);
        if (item) return pickEntry(item);
      }

      return false;
    }

    async function runSearch(q) {
      q = (q || '').trim();
      if (!q) {
        resultsEl.innerHTML = '';
        resultsEl.style.display = 'none';
        lastItems = [];
        return;
      }
      if (UUID_RE.test(q)) {
        try {
          const entry = await callApi('GET', `/api/thesaurus/entries?entryId=${encodeURIComponent(q)}`);
          if (entry && entry.id && !exclude.has(entry.id)) {
            lastItems = [entry];
            renderResultButtons(resultsEl, lastItems, pickEntry);
            return;
          }
        } catch (_) { /* fall through */ }
      }
      try {
        const data = await callApi('GET', `/api/thesaurus/entries?q=${encodeURIComponent(q)}&limit=12`);
        lastItems = (data.items || []).filter((e) => !exclude.has(e.id));
        if (!lastItems.length) {
          resultsEl.innerHTML = '<div style="padding:6px;font-size:12px;color:#666">No matches.</div>';
          resultsEl.style.display = 'block';
          return;
        }
        renderResultButtons(resultsEl, lastItems, pickEntry);
      } catch (_) {
        resultsEl.innerHTML = '';
        resultsEl.style.display = 'none';
        lastItems = [];
      }
    }

    const debounced = debounce(() => runSearch(searchInp.value), 250);
    searchInp.addEventListener('input', debounced);
    searchInp.addEventListener('focus', () => {
      if (searchInp.value.trim()) runSearch(searchInp.value);
    });
    searchInp.addEventListener('keydown', (ev) => {
      if (ev.key === 'Enter') {
        ev.preventDefault();
        confirmSelection();
      }
    });

    return {
      runSearch,
      confirm: confirmSelection,
      clear: () => {
        searchInp.value = '';
        resultsEl.innerHTML = '';
        resultsEl.style.display = 'none';
        lastItems = [];
      },
    };
  }

  /**
   * Find an existing lemma for phrase or create a new custom entry.
   * opts: { language?, partOfSpeech? }
   */
  async function resolveOrCreateLemmaEntry(callApi, phrase, excludeIds, opts) {
    opts = opts || {};
    const q = String(phrase || '').trim();
    if (!q) throw new Error('Lemma phrase is required');
    const exclude = new Set(excludeIds || []);
    const data = await callApi('GET', `/api/thesaurus/entries?q=${encodeURIComponent(q)}&limit=12`);
    const items = (data.items || []).filter((e) => e.id && !exclude.has(e.id));
    const auto = pickAutoSelectLemma(items, q);
    if (auto) return auto;
    if (items.length === 1) return items[0];
    const created = await callApi('POST', '/api/thesaurus/entries', {
      word: q,
      language: opts.language || 'en',
      partOfSpeech: opts.partOfSpeech || 'noun',
      description: opts.description || '',
    });
    const entry = created.entry || created;
    if (!entry || !entry.id) throw new Error('Could not create lemma');
    return entry;
  }

  const ContinuumLemmaPicker = {
    escHtml,
    debounce,
    pickAutoSelectLemma,
    mountSearch,
    resolveOrCreateLemmaEntry,
  };

  global.ContinuumLemmaPicker = ContinuumLemmaPicker;

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = ContinuumLemmaPicker;
  }
})(typeof globalThis !== 'undefined' ? globalThis : typeof window !== 'undefined' ? window : global);
