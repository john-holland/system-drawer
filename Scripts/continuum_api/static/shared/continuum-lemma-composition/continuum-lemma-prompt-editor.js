/* Unified lemma prompt + composition modal */
(function (global) {
  const CompEditor = global.ContinuumLemmaCompositionEditor;

  function escHtml(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function defaultCallApi(method, path, body) {
    return fetch(path, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: body != null ? JSON.stringify(body) : undefined,
    }).then(async (res) => {
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.error || res.statusText);
      return data;
    });
  }

  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  function buildSavePayload(state) {
    const children = (state.compEditor ? state.compEditor.getChildren() : state.children || []).map((c, i) => ({
      entryId: c.entryId,
      term: c.term,
      sortOrder: i,
      patchProperties: c.patchProperties || {},
      timingOverride: c.timingOverride || {},
    }));
    return {
      lemmaPrompt: state.promptText,
      compositionChildren: children,
      patchProperties: state.patchProperties || {},
      timing: { tMin: state.tMin, tMax: state.tMax },
      spatial: {
        spatial4dId: state.spatial4dId || undefined,
        bounds: {
          centerX: state.centerX,
          centerY: state.centerY,
          centerZ: state.centerZ,
          sizeX: state.sizeX,
          sizeY: state.sizeY,
          sizeZ: state.sizeZ,
        },
      },
      draftEpisodeId: state.draftEpisodeId,
    };
  }

  function mountPromptModalShell(container, opts) {
    const callApi = opts.callApi || defaultCallApi;
    const state = {
      entryId: opts.entryId,
      promptText: '',
      children: [],
      patchProperties: {},
      tMin: 0,
      tMax: 3600,
      centerX: 0,
      centerY: 0,
      centerZ: 0,
      sizeX: 1,
      sizeY: 1,
      sizeZ: 1,
      spatial4dId: null,
      isBuiltIn: false,
      usesOverlay: false,
      draftEpisodeId: opts.draftEpisodeId,
      compEditor: null,
      aceEditor: null,
    };

    container.innerHTML =
      '<div class="continuum-prompt-tabs">' +
      ['Prompt', 'Children', 'Spatial', 'Timing', 'Preview'].map((t, i) =>
        `<button type="button" data-tab="${i}" class="${i === 0 ? 'active' : ''}">${t}</button>`,
      ).join('') +
      '</div>' +
      '<div class="continuum-prompt-body">' +
      '<div class="continuum-prompt-panel active" data-panel="0">' +
      '<div class="continuum-prompt-ace" id="lemma-prompt-ace"></div>' +
      '<div class="continuum-prompt-chips" id="lemma-prompt-chips"></div>' +
      '</div>' +
      '<div class="continuum-prompt-panel" data-panel="1"><div id="lemma-prompt-children-host"></div></div>' +
      '<div class="continuum-prompt-panel" data-panel="2">' +
      '<div class="continuum-prompt-grid" id="lemma-prompt-spatial-grid"></div>' +
      '<p class="muted" style="font-size:12px;margin-top:8px">Spatial 4D id: <code id="lemma-spatial-id">—</code></p>' +
      '</div>' +
      '<div class="continuum-prompt-panel" data-panel="3">' +
      '<div class="continuum-prompt-grid">' +
      '<label>tMin (s)<input type="number" id="lemma-timing-tmin" step="any"></label>' +
      '<label>tMax (s)<input type="number" id="lemma-timing-tmax" step="any"></label>' +
      '</div>' +
      '<div id="lemma-builtin-patch" style="margin-top:12px"></div>' +
      '</div>' +
      '<div class="continuum-prompt-panel" data-panel="4">' +
      '<div class="continuum-prompt-preview" id="lemma-prompt-preview">Loading…</div>' +
      '<div class="continuum-prompt-issues" id="lemma-prompt-issues"></div>' +
      '<table class="preview" id="lemma-prompt-props" style="margin-top:8px;font-size:12px"><tbody></tbody></table>' +
      '</div>' +
      '</div>' +
      '<div class="continuum-prompt-actions">' +
      '<button type="button" class="prompt-save primary">Save</button>' +
      '<button type="button" class="prompt-compile-dialogue secondary">Compile Dialogue</button>' +
      '<button type="button" class="prompt-close secondary">Close</button>' +
      '<span class="prompt-msg" style="font-size:13px;margin-left:8px"></span>' +
      '</div>';

    const panels = container.querySelectorAll('.continuum-prompt-panel');
    container.querySelectorAll('.continuum-prompt-tabs button').forEach((btn) => {
      btn.onclick = () => {
        container.querySelectorAll('.continuum-prompt-tabs button').forEach((b) => b.classList.remove('active'));
        btn.classList.add('active');
        panels.forEach((p) => p.classList.toggle('active', p.dataset.panel === btn.dataset.tab));
        if (btn.dataset.tab === '4') refreshPreview();
      };
    });

    function renderSpatialGrid() {
      const grid = container.querySelector('#lemma-prompt-spatial-grid');
      const fields = [
        ['centerX', 'Center X'], ['centerY', 'Center Y'], ['centerZ', 'Center Z'],
        ['sizeX', 'Size X'], ['sizeY', 'Size Y'], ['sizeZ', 'Size Z'],
      ];
      grid.innerHTML = fields.map(([k, label]) =>
        `<label>${label}<input type="number" data-field="${k}" step="any" value="${state[k]}"></label>`,
      ).join('');
      grid.querySelectorAll('input').forEach((inp) => {
        inp.onchange = () => { state[inp.dataset.field] = parseFloat(inp.value) || 0; };
      });
    }

    function renderPatchEditor() {
      const host = container.querySelector('#lemma-builtin-patch');
      if (!state.isBuiltIn) {
        host.innerHTML = '';
        return;
      }
      host.innerHTML = '<strong>Web overlay patch properties</strong>' +
        '<div id="patch-rows"></div>' +
        '<button type="button" class="secondary" id="patch-add" style="margin-top:6px">Add property</button>';
      const rows = host.querySelector('#patch-rows');
      function draw() {
        const entries = Object.entries(state.patchProperties || {});
        rows.innerHTML = entries.length
          ? entries.map(([k, v], i) =>
            `<div class="continuum-prompt-patch-row">` +
            `<input data-idx="${i}" data-part="k" value="${escHtml(k)}"> = ` +
            `<input data-idx="${i}" data-part="v" value="${escHtml(v)}"> ` +
            `<button type="button" data-rm="${i}">×</button></div>`,
          ).join('')
          : '<p class="muted">No patch overrides.</p>';
        rows.querySelectorAll('input').forEach((inp) => {
          inp.onchange = () => {
            const keys = Object.keys(state.patchProperties);
            const key = inp.dataset.part === 'k' ? inp.value : keys[parseInt(inp.dataset.idx, 10)];
            const valInp = rows.querySelector(`input[data-idx="${inp.dataset.idx}"][data-part="v"]`);
            const keyInp = rows.querySelector(`input[data-idx="${inp.dataset.idx}"][data-part="k"]`);
            if (keyInp && valInp) {
              const oldKey = keys[parseInt(inp.dataset.idx, 10)];
              delete state.patchProperties[oldKey];
              state.patchProperties[keyInp.value] = valInp.value;
              draw();
            }
          };
        });
        rows.querySelectorAll('button[data-rm]').forEach((b) => {
          b.onclick = () => {
            const keys = Object.keys(state.patchProperties);
            delete state.patchProperties[keys[parseInt(b.dataset.rm, 10)]];
            draw();
          };
        });
      }
      host.querySelector('#patch-add').onclick = () => {
        state.patchProperties = state.patchProperties || {};
        state.patchProperties['property-key'] = 'value';
        draw();
      };
      draw();
    }

    function renderChips() {
      const chips = container.querySelector('#lemma-prompt-chips');
      const kids = state.compEditor ? state.compEditor.getChildren() : state.children;
      chips.innerHTML = (kids || []).map((c) =>
        `<button type="button" class="secondary" data-term="${escHtml(c.term || c.entryId)}">{P:${escHtml(c.term || c.entryId)}}</button>`,
      ).join('');
      chips.querySelectorAll('button').forEach((b) => {
        b.onclick = () => {
          const insert = `{P:${b.dataset.term}}`;
          if (state.aceEditor) {
            state.aceEditor.session.insert(state.aceEditor.getCursorPosition(), insert);
            state.promptText = state.aceEditor.getValue();
          } else {
            state.promptText += (state.promptText ? ' ' : '') + insert;
          }
        };
      });
    }

    function initAce() {
      const el = container.querySelector('#lemma-prompt-ace');
      if (!el || !global.ace) return;
      state.aceEditor = global.ace.edit(el);
      state.aceEditor.setTheme('ace/theme/textmate');
      state.aceEditor.setOptions({ useWorker: false });
      state.aceEditor.setValue(state.promptText || '', -1);
      state.aceEditor.session.on('change', debounce(() => {
        state.promptText = state.aceEditor.getValue();
      }, 200));
    }

    const refreshPreview = debounce(async () => {
      const prev = container.querySelector('#lemma-prompt-preview');
      const iss = container.querySelector('#lemma-prompt-issues');
      const propsTbl = container.querySelector('#lemma-prompt-props tbody');
      if (!state.entryId) return;
      try {
        const data = await callApi('POST', `/api/thesaurus/entries/${encodeURIComponent(state.entryId)}/expand-prompt`, {});
        prev.textContent = data.expandedText || '(empty)';
        const issues = data.issues || [];
        iss.textContent = issues.length
          ? issues.map((i) => `${i.code}: ${i.message}`).join('\n')
          : '';
        const props = data.mergedProperties || {};
        propsTbl.innerHTML = Object.entries(props).slice(0, 20).map(([k, v]) =>
          `<tr><td>${escHtml(k)}</td><td>${escHtml(v)}</td></tr>`,
        ).join('');
      } catch (e) {
        prev.textContent = 'Preview failed: ' + (e.message || e);
      }
    }, 400);

    async function loadBundle() {
      const data = await callApi('GET', `/api/thesaurus/entries/${encodeURIComponent(state.entryId)}/prompt`);
      state.promptText = data.lemmaPrompt || '';
      state.children = data.compositionChildren || [];
      state.isBuiltIn = !!data.isBuiltIn;
      state.usesOverlay = !!data.usesOverlay;
      state.patchProperties = { ...(data.patchProperties || {}) };
      state.tMin = (data.timing && data.timing.tMin != null) ? data.timing.tMin : 0;
      state.tMax = (data.timing && data.timing.tMax != null) ? data.timing.tMax : 3600;
      const sp = data.spatial || {};
      const b = sp.bounds || {};
      state.centerX = b.centerX != null ? b.centerX : 0;
      state.centerY = b.centerY != null ? b.centerY : 0;
      state.centerZ = b.centerZ != null ? b.centerZ : 0;
      state.sizeX = b.sizeX != null ? b.sizeX : 1;
      state.sizeY = b.sizeY != null ? b.sizeY : 1;
      state.sizeZ = b.sizeZ != null ? b.sizeZ : 1;
      state.spatial4dId = sp.spatial4dId || null;
      container.querySelector('#lemma-timing-tmin').value = state.tMin;
      container.querySelector('#lemma-timing-tmax').value = state.tMax;
      container.querySelector('#lemma-spatial-id').textContent = state.spatial4dId || '—';
      renderSpatialGrid();
      renderPatchEditor();
      initAce();
      if (CompEditor && CompEditor.mountInline) {
        const host = container.querySelector('#lemma-prompt-children-host');
        state.compEditor = CompEditor.mountInline(host, {
          callApi,
          parentEntryId: state.entryId,
          initialChildren: state.children,
          draftEpisodeId: state.draftEpisodeId,
          scriptText: opts.scriptText,
          onSaved: () => { renderChips(); },
        });
      }
      renderChips();
    }

    container.querySelector('#lemma-timing-tmin').onchange = (ev) => {
      state.tMin = parseFloat(ev.target.value) || 0;
    };
    container.querySelector('#lemma-timing-tmax').onchange = (ev) => {
      state.tMax = parseFloat(ev.target.value) || 3600;
    };

    const msgEl = container.querySelector('.prompt-msg');
    container.querySelector('.prompt-save').onclick = async () => {
      if (state.aceEditor) state.promptText = state.aceEditor.getValue();
      msgEl.textContent = 'Saving…';
      msgEl.style.color = '';
      try {
        const payload = buildSavePayload(state);
        const saved = await callApi(
          'PUT',
          `/api/thesaurus/entries/${encodeURIComponent(state.entryId)}/prompt`,
          payload,
        );
        msgEl.textContent = 'Saved.';
        msgEl.style.color = '#2e7d32';
        if (opts.onSaved) opts.onSaved(saved);
      } catch (e) {
        msgEl.textContent = e.message || 'Save failed';
        msgEl.style.color = '#c62828';
      }
    };

    container.querySelector('.prompt-compile-dialogue').onclick = async () => {
      if (state.aceEditor) state.promptText = state.aceEditor.getValue();
      msgEl.textContent = 'Compiling dialogue…';
      msgEl.style.color = '';
      try {
        const data = await callApi(
          'POST',
          `/api/thesaurus/entries/${encodeURIComponent(state.entryId)}/compile-dialogue`,
          { text: state.promptText, persist: true },
        );
        const issues = (data.compiled && data.compiled.issues) || [];
        const errs = issues.filter((i) => i.level === 'error');
        msgEl.textContent = errs.length
          ? 'Compile errors: ' + errs.map((e) => e.message).join('; ')
          : 'Dialogue compiled (set ' + (data.compiled.setId || '') + ').';
        msgEl.style.color = errs.length ? '#c62828' : '#2e7d32';
      } catch (e) {
        msgEl.textContent = e.message || 'Compile failed';
        msgEl.style.color = '#c62828';
      }
    };

    loadBundle()
      .then(async () => {
        if (opts.seedPhrase && CompEditor && CompEditor.prepareSubpromptComposition) {
          try {
            const prep = await CompEditor.prepareSubpromptComposition(
              callApi,
              state.entryId,
              opts.seedPhrase,
            );
            state.promptText = prep.lemmaPrompt;
            state.children = prep.compositionChildren;
            if (state.aceEditor) state.aceEditor.setValue(state.promptText, -1);
            if (state.compEditor) state.compEditor.setChildren(state.children);
            renderChips();
          } catch (e) {
            msgEl.textContent = e.message || 'Could not seed composition';
            msgEl.style.color = '#c62828';
          }
        }
      })
      .catch((e) => {
        msgEl.textContent = e.message || 'Load failed';
        msgEl.style.color = '#c62828';
      });

    return { state, refreshPreview };
  }

  function openModal(opts) {
    opts = opts || {};
    if (!opts.entryId && opts.parentEntryId) opts.entryId = opts.parentEntryId;
    const overlay = document.createElement('div');
    overlay.className = 'continuum-prompt-overlay';
    const modal = document.createElement('div');
    modal.className = 'continuum-prompt-modal';
    modal.innerHTML = '<h3>Lemma composition</h3>';
    const banner = document.createElement('div');
    banner.className = 'continuum-prompt-banner';
    banner.style.display = 'none';
    banner.textContent = 'Web overlay — patches merge at read time for built-in lemmas.';
    modal.appendChild(banner);
    const host = document.createElement('div');
    modal.appendChild(host);
    overlay.appendChild(modal);
    document.body.appendChild(overlay);

    overlay.addEventListener('click', (ev) => { if (ev.target === overlay) overlay.remove(); });

    const callApi = opts.callApi || defaultCallApi;
    callApi('GET', `/api/thesaurus/entries/${encodeURIComponent(opts.entryId)}/prompt`)
      .then((data) => {
        if (data.isBuiltIn) banner.style.display = '';
      })
      .catch(() => {});

    mountPromptModalShell(host, opts);

    const closeHandler = () => overlay.remove();
    host.querySelector('.prompt-close')?.addEventListener('click', closeHandler);
  }

  const ContinuumLemmaPromptEditor = {
    openModal,
    buildSavePayload,
    mountPromptModalShell,
  };

  global.ContinuumLemmaPromptEditor = ContinuumLemmaPromptEditor;

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = ContinuumLemmaPromptEditor;
  }
})(typeof globalThis !== 'undefined' ? globalThis : typeof window !== 'undefined' ? window : global);
