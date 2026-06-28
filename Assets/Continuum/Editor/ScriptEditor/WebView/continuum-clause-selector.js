/* Shared Continuum clause selector — unified attach for property / lemma / localization */
(function (global) {
  const BINDING_KINDS = ['property', 'lemma', 'localization', 'prompt_placeholder'];

  const FALLBACK_POS_TAGS = [
    { posTag: 'noun', segment: 'noun', label: 'Noun', category: 'Subject' },
    { posTag: 'verb', segment: 'verb', label: 'Verb', category: 'Action' },
    { posTag: 'determiner', segment: 'det', label: 'Determiner', category: 'Article' },
    { posTag: 'preposition', segment: 'prep', label: 'Preposition', category: 'Preposition' },
    { posTag: 'conjunction', segment: 'conj', label: 'Conjunction', category: 'DiscourseCausality' },
    { posTag: 'adverb', segment: 'adv', label: 'Adverb', category: 'DiscourseCausality' },
    { posTag: 'type_name', segment: 'literal', label: 'Literal type', category: 'LiteralType' },
    { posTag: 'adjective', segment: 'adj', label: 'Adjective', category: null },
    { posTag: 'pronoun', segment: 'pron', label: 'Pronoun', category: null },
    { posTag: 'interjection', segment: 'intj', label: 'Interjection', category: null },
    { posTag: 'unknown', segment: 'unknown', label: 'Unknown', category: null },
  ];

  const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  function gcd(a, b) {
    while (b) { const t = b; b = a % b; a = t; }
    return a || 1;
  }

  function escHtml(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  function bindingTemplateKey(b) {
    const kind = b.bindingKind || b.binding_kind || 'property';
    if (kind === 'lemma') {
      const eid = b.entryId || b.entry_id || b.propertyValue || b.property_value || '';
      return `lemma:${eid}`;
    }
    if (kind === 'localization') {
      return `localization:${b.propertyKey || b.property_key || ''}:${b.propertyValue || b.property_value || ''}`;
    }
    return `property:${b.propertyKey || b.property_key || ''}:${b.propertyValue || b.property_value || ''}`;
  }

  function bindingSourceKey(b) {
    const ds = b.draftScriptId || b.draft_script_id || '';
    const cs = b.charStart ?? b.char_start ?? '';
    const ce = b.charEnd ?? b.char_end ?? '';
    return `${ds}:${cs}:${ce}`;
  }

  function suggestionLabel(tpl) {
    if (tpl._label) return tpl._label;
    const kind = tpl.bindingKind || tpl.binding_kind || 'property';
    if (kind === 'lemma') return `Lemma ${tpl._term || tpl.entryId || tpl.propertyValue || 'entry'}`;
    if (kind === 'localization') {
      const pk = tpl.propertyKey || tpl.property_key || '';
      const lang = pk.startsWith('lang:') ? pk.slice(5) : pk;
      return `${lang}: ${tpl.propertyValue || tpl.property_value || ''}`;
    }
    const pk = tpl.propertyKey || tpl.property_key || 'property';
    return `${pk}`;
  }

  function suggestionTooltip(tpl) {
    if (tpl._tooltip) return tpl._tooltip;
    const kind = tpl.bindingKind || tpl.binding_kind || 'property';
    if (kind === 'lemma') {
      const eid = tpl.entryId || tpl.entry_id || tpl.propertyValue || tpl.property_value || '';
      return `Lemma binding\nEntry ID: ${eid}\nProperty key: ${tpl.propertyKey || tpl.property_key || 'entry-id'}`;
    }
    if (kind === 'localization') {
      return `Localization\nKey: ${tpl.propertyKey || tpl.property_key || ''}\nTranslation: ${tpl.propertyValue || tpl.property_value || ''}`;
    }
    const pk = tpl.propertyKey || tpl.property_key || '';
    const pv = tpl.propertyValue || tpl.property_value || '';
    return `Property\nKey: ${pk}\nValue: ${pv}`;
  }

  const FIELD_IDS = {
    word: '#clause-lemma-word',
    partOfSpeech: '#clause-lemma-pos',
    language: '#clause-lemma-lang',
    defaultProperties: '#clause-lemma-props',
    prefabId: '#clause-lemma-prefab',
  };

  function createApiError(message, parsed) {
    const err = new Error(message || 'Request failed');
    if (parsed && typeof parsed === 'object') {
      err.code = parsed.code;
      err.field = parsed.field;
      err.existingEntryId = parsed.existingEntryId;
      err.apiBody = parsed;
    }
    return err;
  }

  function lemmaLibraryEntryUrl(entryId) {
    return `/lemma-library#entry/${encodeURIComponent(entryId)}`;
  }

  function openLemmaEntryPage(entryId) {
    if (!entryId) return;
    const url = lemmaLibraryEntryUrl(entryId);
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  function clauseActionsHtml(prefix, primaryLabel) {
    return `
        <div class="continuum-clause-actions" style="margin-top:14px">
          <div style="display:flex;align-items:flex-start;gap:12px;flex-wrap:wrap">
            <div style="display:flex;gap:8px;flex-shrink:0">
              <button type="button" id="${prefix}-save">${escHtml(primaryLabel)}</button>
              <button type="button" id="${prefix}-cancel">Cancel</button>
            </div>
            <div id="${prefix}-error" class="continuum-clause-dialog-error" role="alert" hidden
              style="flex:1 1 180px;font-size:13px;line-height:1.4;color:#b00020;min-width:0"></div>
          </div>
          <div id="${prefix}-conflict-actions" class="continuum-clause-conflict-actions">
            <button type="button" id="${prefix}-use-existing">Use existing</button>
            <button type="button" id="${prefix}-edit-entry">Edit in lemma library</button>
          </div>
        </div>`;
  }

  function pickAutoSelectLemma(items, q) {
    const qLower = (q || '').trim().toLowerCase();
    if (!qLower || !items || !items.length) return null;
    const exact = items.filter((e) => (e.term || '').toLowerCase() === qLower);
    if (exact.length === 1) return exact[0];
    const builtInExact = exact.filter((e) => e.isBuiltIn);
    if (builtInExact.length === 1) return builtInExact[0];
    return null;
  }

  function ensureConflictActions(box, prefix, options) {
    options = options || {};
    const key = '_conflictCtl_' + prefix;
    if (box[key]) return box[key];
    const conflictEl = box.querySelector(`#${prefix}-conflict-actions`);
    const useBtn = box.querySelector(`#${prefix}-use-existing`);
    const editBtn = box.querySelector(`#${prefix}-edit-entry`);
    let entryId = null;
    const ctl = {
      show(id) {
        entryId = id || null;
        if (conflictEl && entryId) conflictEl.classList.add('is-visible');
      },
      hide() {
        entryId = null;
        if (conflictEl) conflictEl.classList.remove('is-visible');
      },
    };
    if (useBtn) {
      useBtn.addEventListener('click', async () => {
        if (!entryId) return;
        if (options.onUseExisting) await options.onUseExisting(entryId);
        clearClauseDialogError(box);
      });
    }
    if (editBtn) {
      editBtn.addEventListener('click', () => {
        if (entryId) openLemmaEntryPage(entryId);
      });
    }
    box[key] = ctl;
    return ctl;
  }

  function clearClauseDialogError(box) {
    if (!box) return;
    box.querySelectorAll('.continuum-clause-dialog-error').forEach((el) => {
      el.hidden = true;
      el.innerHTML = '';
    });
    box.querySelectorAll('.continuum-clause-conflict-actions').forEach((el) => {
      el.classList.remove('is-visible');
    });
    box.querySelectorAll('.clause-field-error').forEach((el) => el.classList.remove('clause-field-error'));
  }

  function normalizeClauseError(err) {
    if (!err) return err;
    if (err.code || err.existingEntryId) return err;
    const raw = String(err.message || '').trim();
    if (raw.startsWith('{')) {
      try {
        const parsed = JSON.parse(raw);
        return createApiError(parsed.error || parsed.message || raw, parsed);
      } catch (_) { /* keep original */ }
    }
    return err;
  }

  function showClauseDialogError(box, err, options) {
    options = options || {};
    if (!box) return;
    clearClauseDialogError(box);
    err = normalizeClauseError(err);
    const prefix = box.querySelector('#clause-attach-error') ? 'clause-attach' : 'clause-edit';
    const errEl = box.querySelector(`#${prefix}-error`);
    if (!errEl) return;

    const msg = (err && err.message) ? err.message : String(err);
    errEl.hidden = false;
    errEl.textContent = msg;

    const entryId = err && err.existingEntryId;
    const isBuiltinConflict = err && err.code === 'builtin_conflict' && entryId;
    if (isBuiltinConflict) {
      const prefixForCtl = box.querySelector('#clause-attach-error') ? 'clause-attach' : 'clause-edit';
      const ctl = ensureConflictActions(box, prefixForCtl, options);
      ctl.show(entryId);
    }

    const field = err && err.field;
    const fieldSel = field && FIELD_IDS[field];
    if (fieldSel) {
      const input = box.querySelector(fieldSel);
      const label = input && input.closest('label');
      if (label) label.classList.add('clause-field-error');
      if (input && input.focus) input.focus();
    }
  }

  function posOptionLabel(row) {
    const seg = row.segment ? ` · ${row.segment}` : '';
    const cat = row.category ? ` (${row.category})` : '';
    return `${row.label || row.posTag}${seg}${cat}`;
  }

  function renderPosSelectOptions(sel, items, selectedPos) {
    sel.innerHTML = items.map((row) =>
      `<option value="${escHtml(row.posTag)}">${escHtml(posOptionLabel(row))}</option>`,
    ).join('');
    const want = (selectedPos || 'noun').trim().toLowerCase();
    const match = items.find(r => (r.posTag || '').toLowerCase() === want);
    sel.value = match ? match.posTag : (items[0]?.posTag || 'noun');
  }

  function setupPosSelect(scope, callApi, selectedPos) {
    const sel = scope.querySelector('#clause-lemma-pos');
    if (!sel) return;
    callApi('GET', '/api/thesaurus/pos-tags').then((data) => {
      const items = (data && data.items && data.items.length) ? data.items : FALLBACK_POS_TAGS;
      renderPosSelectOptions(sel, items, selectedPos);
    }).catch(() => {
      renderPosSelectOptions(sel, FALLBACK_POS_TAGS, selectedPos);
    });
  }

  function lemmaFieldsHtml(propertyKey) {
    const pk = propertyKey || 'entry-id';
    return `
      <label style="display:block">Search lemma
        <input id="clause-lemma-search" type="search" autocomplete="off" placeholder="Word, definition, synonyms…" style="width:100%;box-sizing:border-box"/>
        <input id="clause-entry-id" type="hidden"/>
      </label>
      <div id="clause-lemma-results" style="max-height:160px;overflow:auto;border:1px solid #ddd;border-radius:4px;margin:4px 0;display:none"></div>
      <div id="clause-lemma-selected" style="font-size:13px;color:#555;margin:8px 0;display:none"></div>
      <details id="clause-lemma-create" open style="margin:12px 0">
        <summary style="cursor:pointer;font-weight:600">Create new lemma (if not found)</summary>
        <div style="margin-top:8px;display:flex;flex-direction:column;gap:8px">
          <label>Word <input id="clause-lemma-word" style="width:100%;box-sizing:border-box"/></label>
          <div id="clause-create-mode-tabs"></div>
          <label>Language <select id="clause-lemma-lang" style="width:100%"></select></label>
          <label>Part of speech <select id="clause-lemma-pos" style="width:100%"></select></label>
          <label>Description <textarea id="clause-lemma-desc" rows="2" style="width:100%;box-sizing:border-box"></textarea></label>
          <label>Synonyms (pipe-separated) <input id="clause-lemma-syns" placeholder="a|b" style="width:100%;box-sizing:border-box"/></label>
          <div id="clause-create-prefab-panel">
          <label>Prefab / USC asset id <input id="clause-lemma-prefab" style="width:100%;box-sizing:border-box"/></label>
          </div>
          <div id="clause-create-composition-panel" style="display:none"></div>
          <label>Default properties <input id="clause-lemma-props" placeholder="{P:walk|non-ik-animation=true}" style="width:100%;box-sizing:border-box"/></label>
        </div>
      </details>
      <label style="display:block;margin-top:8px">Property key <input id="clause-lemma-key" value="${escHtml(pk)}" style="width:100%;box-sizing:border-box"/></label>`;
  }

  function setupLemmaPicker(scope, callApi, opts) {
    opts = opts || {};
    const selectionText = (opts.selectionText || '').trim();
    const searchInp = scope.querySelector('#clause-lemma-search');
    const resultsEl = scope.querySelector('#clause-lemma-results');
    const hiddenId = scope.querySelector('#clause-entry-id');
    const selectedEl = scope.querySelector('#clause-lemma-selected');
    const createDetails = scope.querySelector('#clause-lemma-create');
    const wordInp = scope.querySelector('#clause-lemma-word');
    if (!searchInp || !hiddenId) return;

    if (wordInp && selectionText) wordInp.value = selectionText;
    let muteSearchInput = false;
    function setSearchQuery(v) {
      muteSearchInput = true;
      searchInp.value = v;
      muteSearchInput = false;
    }
    if (selectionText) setSearchQuery(selectionText);

    setupPosSelect(scope, callApi, opts.selectedPos);

    callApi('GET', '/api/thesaurus/languages').then((data) => {
      const sel = scope.querySelector('#clause-lemma-lang');
      if (!sel) return;
      const items = data.items || [];
      sel.innerHTML = items.length
        ? items.map(l => `<option value="${escHtml(l.code)}"${l.code === 'en' ? ' selected' : ''}>${escHtml(l.code)}</option>`).join('')
        : '<option value="en">en</option>';
    }).catch(() => {});

    function setSelected(entry) {
      if (entry && entry.id) {
        hiddenId.value = entry.id;
        selectedEl.style.display = 'block';
        selectedEl.innerHTML =
          `Selected: <strong>${escHtml(entry.term)}</strong> ` +
          `(${escHtml(entry.posTag || '?')}, ${escHtml(entry.languageCode || '?')}) ` +
          `<code style="font-size:11px">${escHtml(entry.id)}</code> ` +
          '<button type="button" id="clause-lemma-clear" style="font-size:11px;margin-left:4px">Clear</button>';
        if (createDetails) createDetails.open = false;
        const clearBtn = scope.querySelector('#clause-lemma-clear');
        if (clearBtn) clearBtn.onclick = () => setSelected(null);
        const langSel = scope.querySelector('#clause-lemma-lang');
        const posSel = scope.querySelector('#clause-lemma-pos');
        if (langSel && entry.languageCode) langSel.value = entry.languageCode;
        if (posSel && entry.posTag) {
          const want = String(entry.posTag).toLowerCase();
          const opt = Array.from(posSel.options).find(o => (o.value || '').toLowerCase() === want);
          if (opt) posSel.value = opt.value;
        }
      } else {
        hiddenId.value = '';
        selectedEl.style.display = 'none';
        selectedEl.innerHTML = '';
        if (createDetails) createDetails.open = true;
      }
    }

    async function runSearch(q) {
      q = (q || '').trim();
      if (!q) {
        resultsEl.innerHTML = '';
        resultsEl.style.display = 'none';
        return;
      }
      if (UUID_RE.test(q)) {
        try {
          const entry = await callApi('GET', `/api/thesaurus/entries?entryId=${encodeURIComponent(q)}`);
          if (entry && entry.id) {
            setSelected(entry);
            setSearchQuery(entry.term || q);
            resultsEl.innerHTML = '';
            resultsEl.style.display = 'none';
            return;
          }
        } catch (_) { /* fall through to text search */ }
      }
      try {
        const data = await callApi('GET', `/api/thesaurus/entries?q=${encodeURIComponent(q)}&limit=12`);
        const items = data.items || [];
        const auto = pickAutoSelectLemma(items, q);
        if (auto) {
          setSelected(auto);
          setSearchQuery(auto.term || q);
          resultsEl.innerHTML = '';
          resultsEl.style.display = 'none';
          return;
        }
        if (!items.length) {
          resultsEl.innerHTML = '<div style="padding:6px;font-size:12px;color:#666">No matches — fill create fields below.</div>';
          resultsEl.style.display = 'block';
          if (createDetails) createDetails.open = true;
          return;
        }
        resultsEl.innerHTML = items.map((e) =>
          `<button type="button" class="clause-lemma-hit" data-id="${escHtml(e.id)}" ` +
          'style="display:block;width:100%;text-align:left;padding:6px 8px;border:none;border-bottom:1px solid #eee;background:#fafafa;cursor:pointer">' +
          `<strong>${escHtml(e.term)}</strong> ` +
          `<span style="color:#666;font-size:12px">${escHtml(e.posTag || '')} · ${escHtml(e.languageCode || '')}` +
          `${e.isBuiltIn ? ' · built-in' : ''}</span></button>`,
        ).join('');
        resultsEl.style.display = 'block';
        resultsEl.querySelectorAll('.clause-lemma-hit').forEach((btn) => {
          btn.onclick = () => {
            const item = items.find(x => x.id === btn.dataset.id);
            if (!item) return;
            setSelected(item);
            setSearchQuery(item.term);
            resultsEl.innerHTML = '';
            resultsEl.style.display = 'none';
          };
        });
      } catch (_) {
        resultsEl.innerHTML = '';
        resultsEl.style.display = 'none';
      }
    }

    const debouncedSearch = debounce(() => runSearch(searchInp.value), 250);
    searchInp.addEventListener('input', () => {
      if (muteSearchInput) return;
      debouncedSearch();
    });
    searchInp.addEventListener('focus', () => {
      if (searchInp.value.trim()) runSearch(searchInp.value);
    });

    if (opts.entryId) {
      callApi('GET', `/api/thesaurus/entries?entryId=${encodeURIComponent(opts.entryId)}`).then((entry) => {
        if (entry && entry.id) {
          setSelected(entry);
          setSearchQuery(entry.term || '');
        } else {
          hiddenId.value = opts.entryId;
        }
      }).catch(() => {
        hiddenId.value = opts.entryId;
      });
    } else if (selectionText) {
      runSearch(selectionText);
    }

    if (!opts.prefabOnly && global.ContinuumLemmaCompositionEditor && scope.querySelector('#clause-create-mode-tabs')) {
      scope._compositionCreateTabs = global.ContinuumLemmaCompositionEditor.mountCreateTabs(
        scope.querySelector('#clause-create-mode-tabs'),
        {
          callApi,
          prefabPanel: scope.querySelector('#clause-create-prefab-panel'),
          compositionHost: scope.querySelector('#clause-create-composition-panel'),
        },
      );
    }

    return { setSelected, runSearch };
  }

  async function resolveLemmaEntryId(scope, callApi) {
    const hiddenId = scope.querySelector('#clause-entry-id');
    const selected = (hiddenId && hiddenId.value || '').trim();
    if (selected) return selected;

    const word = (scope.querySelector('#clause-lemma-word')?.value || '').trim();
    if (!word) throw new Error('Select an existing lemma or fill in word to create one');

    const synsRaw = (scope.querySelector('#clause-lemma-syns')?.value || '').trim();
    const posEl = scope.querySelector('#clause-lemma-pos');
    const posRaw = posEl && posEl.value ? posEl.value : 'unknown';
    const tabs = scope._compositionCreateTabs;
    const mode = tabs?.getMode?.() || 'prefab';
    const body = {
      word,
      language: scope.querySelector('#clause-lemma-lang')?.value || 'en',
      partOfSpeech: String(posRaw).trim().toLowerCase() || 'unknown',
      description: scope.querySelector('#clause-lemma-desc')?.value || '',
      prefabId: mode === 'composition' ? '' : (scope.querySelector('#clause-lemma-prefab')?.value || ''),
      defaultProperties: scope.querySelector('#clause-lemma-props')?.value || '',
    };
    if (mode === 'composition') {
      const children = tabs?.getCompositionChildren?.() || [];
      if (children.length) {
        body.composition = children.map((c, i) => ({ entryId: c.entryId, sortOrder: i }));
      }
    }
    if (synsRaw) {
      body.synonyms = synsRaw.split(/[|,;]/).map(s => s.trim()).filter(Boolean);
    }
    const data = await callApi('POST', '/api/thesaurus/entries', body);
    const entryId = data.entry && data.entry.id;
    if (!entryId) throw new Error(data.error || data.message || 'Failed to create lemma');
    if (hiddenId) hiddenId.value = entryId;
    return entryId;
  }

  const ContinuumClauseSelector = {
    fromEditorSelection(editorInst) {
      const sel = global.ContinuumScriptEditor
        ? global.ContinuumScriptEditor.getSelection(editorInst)
        : { charStart: 0, charEnd: 0, text: '' };
      const text = sel.text || '';
      const n = Math.max((global.ContinuumScriptEditor?.getValue(editorInst) || '').length, 1);
      const cs = Math.max(0, Math.min(sel.charStart, n));
      const ce = Math.max(cs, Math.min(sel.charEnd, n));
      const g1 = gcd(cs, n);
      const g2 = gcd(ce, n);
      return {
        charStart: cs,
        charEnd: ce,
        selectionText: text,
        fareyLeftNum: cs / g1,
        fareyLeftDen: n / g1,
        fareyRightNum: ce / g2,
        fareyRightDen: n / g2,
        draftScriptId: editorInst?.options?.draftScriptId,
        draftEpisodeId: editorInst?.options?.draftId,
      };
    },

    async callApi(method, path, body) {
      if (global.ContinuumScriptEditor?.callApi) {
        try {
          return await global.ContinuumScriptEditor.callApi(method, path, body);
        } catch (e) {
          throw normalizeClauseError(e);
        }
      }
      const res = await fetch(path, {
        method: method || 'GET',
        headers: { 'Content-Type': 'application/json' },
        body: body && method !== 'GET' ? JSON.stringify(body) : undefined,
      });
      const text = await res.text();
      if (!res.ok) {
        let parsed = null;
        try { parsed = JSON.parse(text); } catch (_) { /* keep null */ }
        const msg = (parsed && (parsed.error || parsed.message)) || text || res.statusText;
        throw createApiError(msg, parsed);
      }
      try { return JSON.parse(text); } catch (_) { return text; }
    },

    openAttachDialog(clauseRef, options) {
      options = options || {};
      const overlay = document.createElement('div');
      overlay.className = 'continuum-clause-overlay';
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:110;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:16px;max-width:520px;width:90%;max-height:90vh;overflow:auto;border-radius:6px;color:#222';
      box.innerHTML = `
        <h3 style="margin:0 0 8px">Attach to clause</h3>
        <p style="font-size:13px;color:#555">"${escHtml((clauseRef.selectionText || '').slice(0, 60))}" [${clauseRef.charStart}, ${clauseRef.charEnd})</p>
        <div role="tablist" style="display:flex;gap:8px;margin-bottom:12px">
          <button type="button" data-tab="property">Property</button>
          <button type="button" data-tab="lemma">Lemma</button>
          <button type="button" data-tab="localization">Localization</button>
        </div>
        <div id="clause-tab-property">
          <label>Property key <select id="clause-prop-key"><option value="">Loading…</option></select></label>
          <label style="display:block;margin-top:8px">Value <input id="clause-prop-val" style="width:100%"/></label>
        </div>
        <div id="clause-tab-lemma" hidden>${lemmaFieldsHtml()}</div>
        <div id="clause-tab-localization" hidden>
          <label>Language code <input id="clause-lang" placeholder="es" style="width:100%"/></label>
          <label style="display:block;margin-top:8px">Translation <input id="clause-loc-val" style="width:100%"/></label>
        </div>
        ${clauseActionsHtml('clause-attach', 'Attach')}`;
      overlay.appendChild(box);
      document.body.appendChild(overlay);
      box.addEventListener('mousedown', (ev) => ev.stopPropagation());
      box.addEventListener('click', (ev) => ev.stopPropagation());

      let activeTab = options.mode || 'property';
      let lemmaPickerReady = false;
      const ensureLemmaPicker = () => {
        if (lemmaPickerReady) return box._lemmaPicker;
        lemmaPickerReady = true;
        box._lemmaPicker = setupLemmaPicker(box, ContinuumClauseSelector.callApi.bind(ContinuumClauseSelector), {
          selectionText: clauseRef.selectionText || '',
          selectedPos: 'noun',
        });
        return box._lemmaPicker;
      };
      const showTab = (name) => {
        activeTab = name;
        clearClauseDialogError(box);
        if (name === 'lemma') ensureLemmaPicker();
        ['property', 'lemma', 'localization'].forEach(t => {
          const el = box.querySelector('#clause-tab-' + t);
          if (el) el.hidden = t !== name;
        });
      };
      box.querySelectorAll('[data-tab]').forEach(btn => {
        btn.onclick = () => showTab(btn.dataset.tab);
      });
      showTab(activeTab);

      const useExistingEntry = async (entryId) => {
        ensureLemmaPicker();
        const entry = await ContinuumClauseSelector.callApi(
          'GET',
          `/api/thesaurus/entries?entryId=${encodeURIComponent(entryId)}`,
        );
        if (box._lemmaPicker && box._lemmaPicker.setSelected && entry && entry.id) {
          box._lemmaPicker.setSelected(entry);
          showTab('lemma');
        }
      };
      ensureConflictActions(box, 'clause-attach', { onUseExisting: useExistingEntry });
      clearClauseDialogError(box);

      overlay.addEventListener('click', (ev) => {
        if (ev.target !== overlay) return;
        const resultsEl = box.querySelector('#clause-lemma-results');
        if (resultsEl) resultsEl.style.display = 'none';
      });

      ContinuumClauseSelector.callApi('GET', '/api/thesaurus/property-specs').then(data => {
        const sel = box.querySelector('#clause-prop-key');
        sel.innerHTML = (data.items || []).map(s => `<option value="${escHtml(s.key)}">${escHtml(s.key)}</option>`).join('') || '<option value="non-ik-animation">non-ik-animation</option>';
      }).catch(() => {});

      box.querySelector('#clause-attach-cancel').onclick = () => overlay.remove();
      box.querySelector('#clause-attach-save').onclick = async () => {
        clearClauseDialogError(box);
        try {
          let body = {
            ...clauseRef,
            draftScriptId: clauseRef.draftScriptId || options.draftScriptId,
            draftEpisodeId: clauseRef.draftEpisodeId || options.draftEpisodeId || options.draftId,
            scriptText: options.scriptText || '',
          };
          if (activeTab === 'property') {
            body.bindingKind = 'property';
            body.propertyKey = box.querySelector('#clause-prop-key').value;
            body.propertyValue = box.querySelector('#clause-prop-val').value;
          } else if (activeTab === 'lemma') {
            ensureLemmaPicker();
            body.bindingKind = 'lemma';
            body.entryId = await resolveLemmaEntryId(box, ContinuumClauseSelector.callApi.bind(ContinuumClauseSelector));
            body.propertyKey = box.querySelector('#clause-lemma-key').value || 'entry-id';
            body.propertyValue = body.entryId;
          } else {
            body.bindingKind = 'localization';
            const lang = box.querySelector('#clause-lang').value.trim();
            body.propertyKey = 'lang:' + lang;
            body.propertyValue = box.querySelector('#clause-loc-val').value;
          }
          await ContinuumClauseSelector.callApi('POST', '/api/thesaurus/clause-bindings', body);
          overlay.remove();
          if (options.onAttached) await options.onAttached();
        } catch (e) {
          showClauseDialogError(box, e, { onUseExisting: useExistingEntry });
        }
      };
    },

    openEditDialog(binding, options) {
      options = options || {};
      const kind = binding.bindingKind || binding.binding_kind || 'property';
      const scriptText = options.scriptText || '';
      const cs = binding.charStart ?? binding.char_start ?? 0;
      const ce = binding.charEnd ?? binding.char_end ?? 0;
      const overlay = document.createElement('div');
      overlay.className = 'continuum-clause-overlay';
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:110;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:16px;max-width:520px;width:90%;max-height:90vh;overflow:auto;border-radius:6px;color:#222';
      const langFromKey = (binding.propertyKey || binding.property_key || '').startsWith('lang:')
        ? (binding.propertyKey || binding.property_key).slice(5)
        : '';
      box.innerHTML = `
        <h3 style="margin:0 0 8px">Edit clause <span style="font-size:12px;color:#666">(${escHtml(kind)})</span></h3>
        <label>Char start <input id="clause-edit-start" type="number" min="0" style="width:100%"/></label>
        <label style="display:block;margin-top:8px">Char end <input id="clause-edit-end" type="number" min="0" style="width:100%"/></label>
        <p id="clause-edit-preview" style="font-size:13px;color:#555;margin:8px 0"></p>
        <div id="clause-edit-fields"></div>
        ${clauseActionsHtml('clause-edit', 'Save')}`;
      overlay.appendChild(box);
      document.body.appendChild(overlay);
      box.addEventListener('mousedown', (ev) => ev.stopPropagation());
      box.addEventListener('click', (ev) => ev.stopPropagation());

      const startInp = box.querySelector('#clause-edit-start');
      const endInp = box.querySelector('#clause-edit-end');
      const preview = box.querySelector('#clause-edit-preview');
      startInp.value = cs;
      endInp.value = ce;

      const fields = box.querySelector('#clause-edit-fields');
      if (kind === 'property') {
        fields.innerHTML = `
          <label>Property key <select id="clause-prop-key"><option value="">Loading…</option></select></label>
          <label style="display:block;margin-top:8px">Value <input id="clause-prop-val" style="width:100%"/></label>`;
        ContinuumClauseSelector.callApi('GET', '/api/thesaurus/property-specs').then(data => {
          const sel = fields.querySelector('#clause-prop-key');
          const pk = binding.propertyKey || binding.property_key || '';
          sel.innerHTML = (data.items || []).map(s =>
            `<option value="${escHtml(s.key)}"${s.key === pk ? ' selected' : ''}>${escHtml(s.key)}</option>`,
          ).join('') || `<option value="${escHtml(pk)}">${escHtml(pk)}</option>`;
        }).catch(() => {});
        fields.querySelector('#clause-prop-val').value = binding.propertyValue || binding.property_value || '';
      } else if (kind === 'lemma') {
        fields.innerHTML = lemmaFieldsHtml(binding.propertyKey || binding.property_key || 'entry-id');
        box._lemmaPicker = setupLemmaPicker(box, ContinuumClauseSelector.callApi.bind(ContinuumClauseSelector), {
          entryId: binding.entryId || binding.propertyValue || binding.property_value || '',
          selectionText: scriptText.substring(cs, ce),
        });
      } else {
        fields.innerHTML = `
          <label>Language code <input id="clause-lang" placeholder="es" style="width:100%"/></label>
          <label style="display:block;margin-top:8px">Translation <input id="clause-loc-val" style="width:100%"/></label>`;
        fields.querySelector('#clause-lang').value = langFromKey;
        fields.querySelector('#clause-loc-val').value = binding.propertyValue || binding.property_value || '';
      }

      overlay.addEventListener('click', (ev) => {
        if (ev.target !== overlay) return;
        const resultsEl = box.querySelector('#clause-lemma-results');
        if (resultsEl) resultsEl.style.display = 'none';
      });

      function updatePreview() {
        const s = parseInt(startInp.value, 10) || 0;
        const e = parseInt(endInp.value, 10) || 0;
        const slice = scriptText.substring(s, e);
        preview.textContent = slice ? `"${slice.slice(0, 60)}" [${s}, ${e})` : `[${s}, ${e})`;
      }
      startInp.oninput = updatePreview;
      endInp.oninput = updatePreview;
      updatePreview();

      const useExistingEntry = async (entryId) => {
        const entry = await ContinuumClauseSelector.callApi(
          'GET',
          `/api/thesaurus/entries?entryId=${encodeURIComponent(entryId)}`,
        );
        if (box._lemmaPicker && box._lemmaPicker.setSelected && entry && entry.id) {
          box._lemmaPicker.setSelected(entry);
        }
      };
      ensureConflictActions(box, 'clause-edit', { onUseExisting: useExistingEntry });
      clearClauseDialogError(box);

      box.querySelector('#clause-edit-cancel').onclick = () => overlay.remove();
      box.querySelector('#clause-edit-save').onclick = async () => {
        clearClauseDialogError(box);
        try {
          const draftId = options.draftEpisodeId || options.draftId;
          if (!draftId) throw new Error('Draft ID required');
          const body = {
            bindingId: binding.id,
            charStart: parseInt(startInp.value, 10) || 0,
            charEnd: parseInt(endInp.value, 10) || 0,
            scriptText,
          };
          if (kind === 'property') {
            body.propertyKey = fields.querySelector('#clause-prop-key').value;
            body.propertyValue = fields.querySelector('#clause-prop-val').value;
          } else if (kind === 'lemma') {
            body.entryId = await resolveLemmaEntryId(box, ContinuumClauseSelector.callApi.bind(ContinuumClauseSelector));
            body.propertyKey = box.querySelector('#clause-lemma-key').value || 'entry-id';
            body.propertyValue = body.entryId;
          } else {
            const lang = fields.querySelector('#clause-lang').value.trim();
            body.propertyKey = 'lang:' + lang;
            body.propertyValue = fields.querySelector('#clause-loc-val').value;
          }
          body.selectionText = scriptText.substring(body.charStart, body.charEnd);
          const result = await ContinuumClauseSelector.callApi(
            'POST',
            `/api/drafts/episodes/${encodeURIComponent(draftId)}/apply-binding-edit`,
            body,
          );
          overlay.remove();
          if (options.onEdited) await options.onEdited(result);
        } catch (e) {
          showClauseDialogError(box, e, { onUseExisting: useExistingEntry });
        }
      };
    },

    openLemmaEntryDialog(options) {
      options = options || {};
      const entryId = options.entryId;
      const binding = options.binding;
      const callApi = options.callApi || ContinuumClauseSelector.callApi.bind(ContinuumClauseSelector);
      if (!entryId && !binding) return;
      const saveLabel = binding ? 'Save' : 'Done';
      const overlay = document.createElement('div');
      overlay.className = 'continuum-clause-overlay';
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:110;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:16px;max-width:520px;width:90%;max-height:90vh;overflow:auto;border-radius:6px;color:#222';
      box.innerHTML =
        '<h3 style="margin:0 0 8px">Edit lemma</h3>' +
        '<p style="font-size:13px;color:#555;margin:0 0 8px">Search, select, or create a lemma entry.</p>' +
        '<div id="clause-edit-fields"></div>' +
        clauseActionsHtml('lemma-entry', saveLabel);
      overlay.appendChild(box);
      document.body.appendChild(overlay);
      box.addEventListener('mousedown', (ev) => ev.stopPropagation());
      box.addEventListener('click', (ev) => ev.stopPropagation());
      const fields = box.querySelector('#clause-edit-fields');
      fields.innerHTML = lemmaFieldsHtml('entry-id');
      const compPanel = box.querySelector('#clause-create-composition-panel');
      const modeTabs = box.querySelector('#clause-create-mode-tabs');
      if (options.prefabOnly !== false && compPanel) compPanel.style.display = 'none';
      if (options.prefabOnly !== false && modeTabs) modeTabs.style.display = 'none';
      box._lemmaPicker = setupLemmaPicker(box, callApi, {
        entryId: entryId || binding?.entryId || binding?.propertyValue || binding?.property_value,
        selectionText: options.selectionText || '',
        prefabOnly: options.prefabOnly !== false,
      });

      const useExistingEntry = async (existingId) => {
        const entry = await callApi(
          'GET',
          `/api/thesaurus/entries?entryId=${encodeURIComponent(existingId)}`,
        );
        if (box._lemmaPicker && box._lemmaPicker.setSelected && entry && entry.id) {
          box._lemmaPicker.setSelected(entry);
        }
      };
      ensureConflictActions(box, 'lemma-entry', { onUseExisting: useExistingEntry });
      clearClauseDialogError(box);

      box.querySelector('#lemma-entry-cancel').onclick = () => overlay.remove();
      box.querySelector('#lemma-entry-save').onclick = async () => {
        if (!binding) {
          overlay.remove();
          if (options.onSaved) options.onSaved();
          return;
        }
        clearClauseDialogError(box);
        try {
          const draftId = options.draftEpisodeId || options.draftId;
          if (!draftId) throw new Error('Draft ID required');
          const scriptText = options.scriptText || '';
          const cs = binding.charStart ?? binding.char_start ?? 0;
          const ce = binding.charEnd ?? binding.char_end ?? 0;
          const resolvedId = await resolveLemmaEntryId(box, callApi);
          const body = {
            bindingId: binding.id,
            charStart: cs,
            charEnd: ce,
            scriptText,
            entryId: resolvedId,
            propertyKey: box.querySelector('#clause-lemma-key')?.value || 'entry-id',
            propertyValue: resolvedId,
            selectionText: options.selectionText || scriptText.substring(cs, ce),
          };
          const result = await callApi(
            'POST',
            `/api/drafts/episodes/${encodeURIComponent(draftId)}/apply-binding-edit`,
            body,
          );
          overlay.remove();
          if (options.onEdited) await options.onEdited(result);
          if (options.onSaved) options.onSaved();
        } catch (e) {
          showClauseDialogError(box, e, { onUseExisting: useExistingEntry });
        }
      };
      overlay.addEventListener('click', (ev) => { if (ev.target === overlay) overlay.remove(); });
    },

    bindingTemplateKey,

    bindingSourceKey,

    suggestionLabel,

    suggestionTooltip,

    async cloneBindingTemplates(templates, clauseRef, options) {
      options = options || {};
      if (!templates || !templates.length) return;
      const bodyBase = {
        ...clauseRef,
        draftScriptId: clauseRef.draftScriptId || options.draftScriptId,
        draftEpisodeId: clauseRef.draftEpisodeId || options.draftEpisodeId || options.draftId,
        scriptText: options.scriptText || '',
      };
      for (const tpl of templates) {
        const kind = tpl.bindingKind || tpl.binding_kind || 'property';
        await ContinuumClauseSelector.callApi('POST', '/api/thesaurus/clause-bindings', {
          ...bodyBase,
          bindingKind: kind,
          propertyKey: tpl.propertyKey || tpl.property_key || '',
          propertyValue: tpl.propertyValue || tpl.property_value || '',
          entryId: tpl.entryId || tpl.entry_id || undefined,
        });
      }
    },

    lemmaEntryTemplate(entry) {
      const id = entry.id;
      return {
        bindingKind: 'lemma',
        propertyKey: 'entry-id',
        propertyValue: id,
        entryId: id,
        _label: entry.term,
        _tooltip: `Lemma: ${entry.term} (${entry.posTag || '?'}, ${entry.languageCode || '?'})\nEntry ID: ${id}`,
      };
    },
  };

  global.ContinuumClauseSelector = ContinuumClauseSelector;
  global.ContinuumClauseSelectorBindingKinds = BINDING_KINDS;

  const _testExports = {
    normalizeClauseError,
    createApiError,
    pickAutoSelectLemma,
    ensureConflictActions,
    clearClauseDialogError,
    showClauseDialogError,
  };
  if (typeof module !== 'undefined' && module.exports) {
    module.exports = _testExports;
  }
})(typeof window !== 'undefined' ? window : globalThis);
