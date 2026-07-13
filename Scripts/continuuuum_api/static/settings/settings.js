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

  function renderPanelContent() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;
    if (activeGroup === 'script-output') {
      panel.innerHTML = renderScriptOutputPanel();
      bindScriptOutputPanel();
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
  render();
})();
