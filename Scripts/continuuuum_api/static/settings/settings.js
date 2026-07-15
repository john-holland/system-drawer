(function () {
  'use strict';

  const CS = window.ContinuuuumSettings;
  if (!CS) return;

  let activeGroup = 'script-output';
  let draft = CS.load();

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function setStatus(msg, isError) {
    const el = document.getElementById('cs-status');
    if (!el) return;
    el.textContent = msg || '';
    el.classList.toggle('is-error', !!isError);
  }

  function groupFromHash() {
    const h = (location.hash || '').replace(/^#/, '').trim();
    if (!h) return 'script-output';
    const g = CS.GROUPS.find((x) => x.id === h);
    return g && g.enabled ? g.id : 'script-output';
  }

  function renderGroupList() {
    const el = document.getElementById('cs-group-list');
    if (!el) return;
    el.innerHTML = CS.GROUPS.map((g) =>
      `<button type="button" class="cs-group-btn${g.id === activeGroup ? ' is-active' : ''}" ` +
      `data-group="${esc(g.id)}" ${g.enabled ? '' : 'disabled'}>${esc(g.label)}` +
      `${g.enabled ? '' : ' (soon)'}</button>`,
    ).join('');
    el.querySelectorAll('.cs-group-btn').forEach((btn) => {
      btn.onclick = () => {
        if (btn.disabled) return;
        activeGroup = btn.dataset.group;
        location.hash = activeGroup;
        render();
      };
    });
  }

  function renderScriptOutputPanel() {
    const so = draft.scriptOutput || CS.defaultSettings().scriptOutput;
    const priority = CS.normalizePriority(so.autoAddPriority);
    const rows = priority.map((typeId, idx) =>
      `<div class="cs-priority-row" data-slot="${idx}">
        <label for="cs-priority-${idx}">Priority ${idx + 1}</label>
        <select id="cs-priority-${idx}" data-slot="${idx}">
          ${CS.AUTO_ADD_TYPES.map((t) =>
        `<option value="${esc(t)}"${t === typeId ? ' selected' : ''}>${esc(CS.typeLabel(t))}</option>`,
      ).join('')}
        </select>
        <span class="cs-reorder">
          <button type="button" data-dir="-1" data-slot="${idx}" aria-label="Move up">↑</button>
          <button type="button" data-dir="1" data-slot="${idx}" aria-label="Move down">↓</button>
        </span>
      </div>`,
    ).join('');

    return `
      <h1>Script Output</h1>
      <p class="cs-hint">Configure <strong>Auto Add All Single Lemmas</strong>: scans script spans that would
      show a single Apply suggestion when selected, then attaches the highest-priority binding type below.
      Single lemma = unambiguous exact-term match, non-composed.</p>
      <section aria-labelledby="cs-auto-add-heading">
        <h2 id="cs-auto-add-heading" style="font-size:1rem;margin:0 0 12px">Auto-add priority (1 = highest)</h2>
        ${rows}
      </section>
      <div class="cs-checkbox-row">
        <label><input type="checkbox" id="cs-new-lemma-required" ${so.newLemmaRequired ? 'checked' : ''}/>
          Require new lemma when no other match</label>
      </div>
      <div class="cs-actions">
        <button type="button" id="cs-save-btn">Save settings</button>
        <span id="cs-status"></span>
      </div>`;
  }

  function bindScriptOutputPanel() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;

    panel.querySelectorAll('select[data-slot]').forEach((sel) => {
      sel.onchange = () => {
        const slot = Number(sel.dataset.slot);
        const newType = sel.value;
        draft.scriptOutput.autoAddPriority = CS.swapPrioritySlots(
          draft.scriptOutput.autoAddPriority,
          slot,
          newType,
        );
        renderPanelContent();
      };
    });

    panel.querySelectorAll('.cs-reorder button').forEach((btn) => {
      btn.onclick = () => {
        const slot = Number(btn.dataset.slot);
        const dir = Number(btn.dataset.dir);
        draft.scriptOutput.autoAddPriority = CS.movePrioritySlot(
          draft.scriptOutput.autoAddPriority,
          slot,
          dir,
        );
        renderPanelContent();
      };
    });

    const req = panel.querySelector('#cs-new-lemma-required');
    if (req) {
      req.onchange = () => {
        draft.scriptOutput.newLemmaRequired = !!req.checked;
      };
    }

    const saveBtn = panel.querySelector('#cs-save-btn');
    if (saveBtn) {
      saveBtn.onclick = () => {
        try {
          CS.save(draft);
          setStatus('Settings saved.');
        } catch (e) {
          setStatus(e.message || 'Save failed', true);
        }
      };
    }
  }

  function renderLemmaLibraryPanel() {
    const ll = draft.lemmaLibrary || CS.DEFAULT_LEMMA_LIBRARY || {};
    return `
      <h1>Lemma Library</h1>
      <p class="cs-hint">Admin settings for Web Lemma Build model proxy (Codestral / LM Studio).
      Saves to the server with <code>X-Admin: 1</code> and mirrors into local settings.</p>
      <label>Model base URL
        <input type="text" id="ll-base-url" value="${esc(ll.lmStudioBaseUrl || '')}" />
      </label>
      <label>Default model id
        <input type="text" id="ll-model-id" value="${esc(ll.defaultModelId || '')}" />
      </label>
      <div class="cs-actions" style="margin-top:0.5rem">
        <button type="button" id="ll-refresh-models">Refresh models</button>
        <select id="ll-models" style="min-width:12rem"><option value="">— models —</option></select>
      </div>
      <label>Max concurrent builds
        <input type="number" id="ll-max-c" min="0" max="16" value="${esc(ll.maxConcurrentBuilds ?? 1)}" />
      </label>
      <label>Batch output directory
        <input type="text" id="ll-batch" value="${esc(ll.batchOutputDir || '')}" />
      </label>
      <label>Default target engine
        <select id="ll-engine">
          <option value="unity"${ll.defaultEngine === 'unity' ? ' selected' : ''}>Unity</option>
          <option value="haxe"${ll.defaultEngine === 'haxe' ? ' selected' : ''}>Haxe</option>
        </select>
      </label>
      <div class="cs-actions">
        <button type="button" id="cs-save-btn">Save to server</button>
        <span id="cs-status"></span>
      </div>`;
  }

  function bindLemmaLibraryPanel() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;

    const syncDraft = () => {
      draft.lemmaLibrary = {
        ...(draft.lemmaLibrary || CS.DEFAULT_LEMMA_LIBRARY),
        lmStudioBaseUrl: panel.querySelector('#ll-base-url')?.value || '',
        defaultModelId: panel.querySelector('#ll-model-id')?.value || '',
        maxConcurrentBuilds: Number(panel.querySelector('#ll-max-c')?.value || 1),
        batchOutputDir: panel.querySelector('#ll-batch')?.value || '',
        defaultEngine: panel.querySelector('#ll-engine')?.value || 'unity',
      };
    };

    panel.querySelectorAll('input, select').forEach((el) => {
      el.addEventListener('change', syncDraft);
      el.addEventListener('input', syncDraft);
    });

    const modelsSel = panel.querySelector('#ll-models');
    if (modelsSel) {
      modelsSel.onchange = () => {
        if (modelsSel.value) {
          panel.querySelector('#ll-model-id').value = modelsSel.value;
          syncDraft();
        }
      };
    }

    const refresh = panel.querySelector('#ll-refresh-models');
    if (refresh) {
      refresh.onclick = async () => {
        setStatus('Loading models…');
        try {
          const res = await fetch('/api/lemma-build/models', { headers: { 'X-Admin': '1' } });
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || data.detail || 'models failed');
          const models = data.models || [];
          modelsSel.innerHTML =
            '<option value="">— models —</option>' +
            models.map((m) => `<option value="${esc(m)}">${esc(m)}</option>`).join('');
          setStatus(models.length ? `${models.length} models` : 'No models');
        } catch (e) {
          setStatus(e.message || 'Refresh failed', true);
        }
      };
    }

    const saveBtn = panel.querySelector('#cs-save-btn');
    if (saveBtn) {
      saveBtn.onclick = async () => {
        syncDraft();
        try {
          const body = draft.lemmaLibrary;
          const res = await fetch('/api/lemma-build/settings', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', 'X-Admin': '1' },
            body: JSON.stringify(body),
          });
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || 'Save failed');
          draft.lemmaLibrary = {
            lmStudioBaseUrl: data.lmStudioBaseUrl,
            defaultModelId: data.defaultModelId,
            maxConcurrentBuilds: data.maxConcurrentBuilds,
            batchOutputDir: data.batchOutputDir,
            defaultEngine: data.defaultEngine,
          };
          CS.saveLemmaLibrary(draft.lemmaLibrary);
          setStatus('Lemma Library settings saved.');
        } catch (e) {
          setStatus(e.message || 'Save failed', true);
        }
      };
    }
  }

  async function hydrateLemmaLibraryFromServer() {
    try {
      const res = await fetch('/api/lemma-build/settings', { headers: { 'X-Admin': '1' } });
      if (!res.ok) return;
      const data = await res.json();
      draft.lemmaLibrary = {
        lmStudioBaseUrl: data.lmStudioBaseUrl || draft.lemmaLibrary?.lmStudioBaseUrl,
        defaultModelId: data.defaultModelId || draft.lemmaLibrary?.defaultModelId,
        maxConcurrentBuilds: data.maxConcurrentBuilds ?? draft.lemmaLibrary?.maxConcurrentBuilds,
        batchOutputDir: data.batchOutputDir || draft.lemmaLibrary?.batchOutputDir,
        defaultEngine: data.defaultEngine || draft.lemmaLibrary?.defaultEngine || 'unity',
      };
    } catch (_) {
      /* ignore */
    }
  }

  function renderPanelContent() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;
    if (activeGroup === 'script-output') {
      panel.innerHTML = renderScriptOutputPanel();
      bindScriptOutputPanel();
    } else if (activeGroup === 'lemma-library') {
      panel.innerHTML = renderLemmaLibraryPanel();
      bindLemmaLibraryPanel();
    } else {
      panel.innerHTML = '<h1>Coming soon</h1><p class="cs-hint">This settings group is not available yet.</p>';
    }
  }

  function render() {
    renderGroupList();
    renderPanelContent();
  }

  window.addEventListener('hashchange', () => {
    activeGroup = groupFromHash();
    render();
  });

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'settings', theme: 'light' });
  }

  activeGroup = groupFromHash();
  draft = CS.load();
  hydrateLemmaLibraryFromServer().finally(render);
})();
