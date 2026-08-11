(function () {
  const Session = () => window.ContinuuuumUserSession;

  const api = (path, opts = {}) => {
    const headers = Session() && Session().getHeaders
      ? Session().getHeaders({ 'Content-Type': 'application/json' })
      : { 'Content-Type': 'application/json', 'X-User-ID': 'anonymous' };
    return fetch(path, {
      ...opts,
      headers: { ...headers, ...(opts.headers || {}) },
      body: opts.body && typeof opts.body !== 'string' ? JSON.stringify(opts.body) : opts.body,
    }).then(async (res) => {
      const text = await res.text();
      let data = null;
      try { data = JSON.parse(text); } catch (_) { data = text; }
      if (!res.ok) throw new Error((data && data.error) || text || res.statusText);
      return data;
    });
  };

  const app = document.getElementById('app');
  const modal = document.getElementById('md-modal');
  const modalBody = document.getElementById('md-modal-body');
  let view = 'browse';
  let uploadTab = 'lemma';
  let registry = [];
  let loadout = [];
  let lemmaTargets = [];
  let episodeTargets = [];
  let episodeMeta = null;
  let editState = null;
  let editTab = 'lemma';

  function syncUserLabel() {
    const el = document.getElementById('user-label');
    if (el) el.textContent = (Session() && Session().getUserId()) || 'anonymous';
  }

  function setView(v) {
    view = v;
    render();
  }

  document.querySelectorAll('[data-view]').forEach((a) => {
    a.addEventListener('click', (e) => {
      e.preventDefault();
      setView(a.getAttribute('data-view'));
    });
  });

  document.getElementById('episode-id')?.addEventListener('change', () => {
    if (view === 'upload') renderUpload();
    else if (editState && !modal.hidden) paintEditModal().catch((e) => alert(e.message));
  });

  if (Session() && Session().onChange) {
    Session().onChange(syncUserLabel);
  }

  async function loadRegistry() {
    const data = await api('/api/mods/registry');
    registry = data.items || [];
  }

  async function loadTargets() {
    const ep = document.getElementById('episode-id')?.value?.trim() || '';
    const [lemma, episode] = await Promise.all([
      api('/api/mods/moddable-targets?targetKind=lemma_prompt&sync=1'),
      ep
        ? api(`/api/mods/moddable-targets?draftEpisodeId=${encodeURIComponent(ep)}&targetKind=episode_section&sync=1`)
        : Promise.resolve({ items: [], meta: {} }),
    ]);
    lemmaTargets = lemma.items || [];
    episodeTargets = episode.items || [];
    episodeMeta = (episode.meta && episode.meta.episode) || null;
  }

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function overrideMapFromDetail(detail) {
    const map = {};
    (detail.lemmaOverrides || []).forEach((o) => {
      if (o.targetId) map[o.targetId] = o.overrideText || '';
    });
    (detail.episodeOverrides || []).forEach((o) => {
      if (o.targetId) map[o.targetId] = o.overrideText || '';
    });
    return map;
  }

  function targetRow(t, kind, opts) {
    opts = opts || {};
    const checked = opts.checked ? ' checked' : '';
    const value = opts.value != null ? opts.value : '';
    const idPrefix = opts.idPrefix || '';
    return `<label class="md-target-row">
      <input type="checkbox" data-target="${esc(t.id)}" data-kind="${esc(kind)}"${checked} />
      <span><strong>${esc(t.slotKey)}</strong> — ${esc(t.label || t.targetKind)}
      <small>[${esc(t.charStart)}, ${esc(t.charEnd)})</small></span>
      <textarea data-override="${esc(t.id)}" id="${esc(idPrefix)}ov-${esc(t.id)}"
        placeholder="Override text for this slot" rows="2">${esc(value)}</textarea></label>`;
  }

  function slotPanelsHtml(activeTab, opts) {
    opts = opts || {};
    const selected = opts.selected || {};
    const idPrefix = opts.idPrefix || '';
    const ep = document.getElementById('episode-id')?.value?.trim() || '';
    const lemmaEmpty = !lemmaTargets.length
      ? `<p class="md-empty">No lemma slots yet. Add one below, or put <code>{M:slotKey}</code> in a lemma prompt and click Refresh.</p>`
      : '';
    const episodeEmpty = !ep
      ? `<p class="md-empty">Set <strong>Episode ID</strong> in the header (draft episode id) to load or create episode sections.</p>`
      : !episodeTargets.length
        ? `<p class="md-empty">No episode sections for this draft${
            episodeMeta && episodeMeta.found
              ? ` (script ${episodeMeta.scriptLength} chars; no <code>{M:…}</code> markers found)`
              : ' (draft script not found)'
          }. Add a section below.</p>`
        : '';

    return `
      <div class="md-tabs" role="tablist">
        <button type="button" class="md-tab ${activeTab === 'lemma' ? 'active' : ''}" data-tab="lemma">Lemma slots (${lemmaTargets.length})</button>
        <button type="button" class="md-tab ${activeTab === 'episode' ? 'active' : ''}" data-tab="episode">Episode sections (${episodeTargets.length})</button>
        <button type="button" class="md-tab-secondary" data-refresh-targets>Refresh</button>
      </div>
      <div class="md-tab-panel" data-panel="lemma" ${activeTab === 'lemma' ? '' : 'hidden'}>
        ${lemmaEmpty}
        <div class="md-target-list">${lemmaTargets.map((t) => targetRow(t, 'lemma', {
          idPrefix,
          checked: Object.prototype.hasOwnProperty.call(selected, t.id),
          value: selected[t.id] || '',
        })).join('')}</div>
        <div class="md-add-slot">
          <h3>Add lemma slot</h3>
          <label>Slot key <input data-new-lemma-key placeholder="greeting" /></label>
          <label>Label <input data-new-lemma-label placeholder="Greeting line" /></label>
          <button type="button" data-add-lemma-slot>Add slot</button>
        </div>
      </div>
      <div class="md-tab-panel" data-panel="episode" ${activeTab === 'episode' ? '' : 'hidden'}>
        ${episodeEmpty}
        <div class="md-target-list">${episodeTargets.map((t) => targetRow(t, 'episode', {
          idPrefix,
          checked: Object.prototype.hasOwnProperty.call(selected, t.id),
          value: selected[t.id] || '',
        })).join('')}</div>
        <div class="md-add-slot">
          <h3>Add episode section</h3>
          <label>Slot key <input data-new-ep-key placeholder="opening" ${ep ? '' : 'disabled'} /></label>
          <label>Label <input data-new-ep-label placeholder="Opening beat" ${ep ? '' : 'disabled'} /></label>
          <label>Char start <input data-new-ep-start type="number" min="0" value="0" ${ep ? '' : 'disabled'} /></label>
          <label>Char end <input data-new-ep-end type="number" min="0" value="${episodeMeta && episodeMeta.scriptLength ? episodeMeta.scriptLength : 0}" ${ep ? '' : 'disabled'} /></label>
          <button type="button" data-add-ep-slot ${ep ? '' : 'disabled'}>Add section</button>
        </div>
      </div>`;
  }

  function collectOverrides(root) {
    const lemmaOverrides = [];
    const episodeOverrides = [];
    if (!root) return { lemmaOverrides, episodeOverrides };
    root.querySelectorAll('input[data-target]:checked').forEach((cb) => {
      const id = cb.getAttribute('data-target');
      const text = root.querySelector(`textarea[data-override="${id}"]`)?.value || '';
      const row = { targetId: id, overrideText: text };
      if (cb.getAttribute('data-kind') === 'lemma') lemmaOverrides.push(row);
      else episodeOverrides.push(row);
    });
    return { lemmaOverrides, episodeOverrides };
  }

  function snapshotSelected(root) {
    const selected = {};
    if (!root) return selected;
    root.querySelectorAll('textarea[data-override]').forEach((ta) => {
      const id = ta.getAttribute('data-override');
      const cb = root.querySelector(`input[data-target="${id}"]`);
      if (cb && cb.checked) selected[id] = ta.value;
    });
    return selected;
  }

  function wireSlotPanels(root, getTab, setTab, onRefresh) {
    if (!root) return;
    root.querySelectorAll('[data-tab]').forEach((btn) => {
      btn.addEventListener('click', () => {
        const selected = snapshotSelected(root);
        setTab(btn.getAttribute('data-tab'));
        onRefresh(selected);
      });
    });
    root.querySelector('[data-refresh-targets]')?.addEventListener('click', () => {
      onRefresh(snapshotSelected(root));
    });
    root.querySelector('[data-add-lemma-slot]')?.addEventListener('click', () => {
      addLemmaSlot(root, () => onRefresh(snapshotSelected(root))).catch((e) => alert(e.message));
    });
    root.querySelector('[data-add-ep-slot]')?.addEventListener('click', () => {
      addEpisodeSlot(root, () => onRefresh(snapshotSelected(root))).catch((e) => alert(e.message));
    });
  }

  function closeModal() {
    editState = null;
    if (modal) modal.hidden = true;
  }

  async function paintEditModal(preserveSelected) {
    if (!editState) return;
    await loadTargets();
    const selected = preserveSelected || editState.selected || {};
    editState.selected = selected;
    modalBody.innerHTML = `
      <label>Display name <input id="md-edit-name" value="${esc(editState.displayName || '')}" /></label>
      <p class="md-meta">Slug <code>${esc(editState.slug)}</code> · author <code>${esc(editState.authorUserId)}</code>
        ${editState.packageVersion ? ` · package v${esc(editState.packageVersion)}` : ' · no published package yet'}</p>
      <p class="md-meta">Check slots to include in the next published package (same controls as Upload).</p>
      ${slotPanelsHtml(editTab, { selected, idPrefix: 'edit-' })}
    `;
    wireSlotPanels(
      modalBody,
      () => editTab,
      (t) => { editTab = t; },
      (sel) => {
        editState.selected = sel;
        editState.displayName = document.getElementById('md-edit-name')?.value || editState.displayName;
        paintEditModal(sel).catch((e) => alert(e.message));
      }
    );
  }

  async function openEditModal(modId) {
    const detail = await api(`/api/mods/${encodeURIComponent(modId)}`);
    editTab = 'lemma';
    editState = {
      modId,
      displayName: detail.displayName || '',
      originalName: detail.displayName || '',
      slug: detail.slug || '',
      authorUserId: detail.authorUserId || '',
      packageVersion: (detail.latestPackage && detail.latestPackage.version) || '',
      selected: overrideMapFromDetail(detail),
      baseline: overrideMapFromDetail(detail),
    };
    await paintEditModal(editState.selected);
    modal.hidden = false;
  }

  function bumpVersion(base) {
    const parts = String(base || '1.0.0').split('.');
    const last = Number(parts[parts.length - 1]);
    if (!Number.isNaN(last)) parts[parts.length - 1] = String(last + 1);
    else parts.push('1');
    return parts.join('.');
  }

  function overridesChanged(lemmaOverrides, episodeOverrides, baseline) {
    const next = {};
    lemmaOverrides.concat(episodeOverrides).forEach((o) => {
      next[o.targetId] = o.overrideText || '';
    });
    const baseKeys = Object.keys(baseline || {});
    const nextKeys = Object.keys(next);
    if (baseKeys.length !== nextKeys.length) return true;
    return nextKeys.some((k) => (baseline[k] || '') !== next[k]);
  }

  async function saveEditModal() {
    if (!editState) return;
    const name = document.getElementById('md-edit-name')?.value?.trim();
    if (!name) { alert('Display name required'); return; }
    const { lemmaOverrides, episodeOverrides } = collectOverrides(modalBody);
    const hasSelections = lemmaOverrides.length || episodeOverrides.length;
    const nameChanged = name !== editState.originalName;
    const packageChanged =
      hasSelections &&
      (!editState.packageVersion ||
        overridesChanged(lemmaOverrides, episodeOverrides, editState.baseline));

    if (!nameChanged && !packageChanged) {
      closeModal();
      return;
    }
    if (packageChanged && !hasSelections) {
      alert('Select at least one slot to include in the package');
      return;
    }

    if (nameChanged) {
      await api(`/api/mods/${encodeURIComponent(editState.modId)}`, {
        method: 'PATCH',
        body: { displayName: name },
      });
    }

    if (packageChanged) {
      await api('/api/mods/packages', {
        method: 'POST',
        body: {
          modId: editState.modId,
          version: bumpVersion(editState.packageVersion || '1.0.0'),
          publish: true,
          lemmaOverrides,
          episodeOverrides,
        },
      });
    }
    closeModal();
    await renderBrowse();
  }

  async function enableLatest(modId) {
    const detail = await api(`/api/mods/${encodeURIComponent(modId)}`);
    const pkgId = detail.latestPackage && detail.latestPackage.id;
    if (!pkgId) {
      alert('No published package for this mod');
      return;
    }
    if (!loadout.includes(pkgId)) loadout.push(pkgId);
    await api('/api/mods/enabled', { method: 'PUT', body: { packageIds: loadout } });
    alert('Enabled package ' + pkgId);
    await renderBrowse();
  }

  async function renderBrowse() {
    await loadRegistry();
    app.innerHTML = `<section class="md-panel"><h2>Published mods</h2>
      ${registry.map((m) => `<div class="md-card">
        <h3>${esc(m.displayName)}</h3>
        <p>${esc(m.slug)} · v${esc(m.latestVersion || '—')}</p>
        <div class="md-card-actions">
          <button type="button" data-enable="${esc(m.id)}">Enable latest</button>
          <button type="button" class="md-btn-secondary" data-edit="${esc(m.id)}">Edit</button>
        </div>
      </div>`).join('') || '<p>No published mods yet.</p>'}
      <h2>Your loadout</h2><pre id="loadout">${esc(JSON.stringify(loadout, null, 2))}</pre>
      <button type="button" id="save-loadout">Save loadout</button></section>`;

    app.querySelectorAll('[data-enable]').forEach((btn) => {
      btn.addEventListener('click', () => {
        enableLatest(btn.getAttribute('data-enable')).catch((e) => alert(e.message));
      });
    });
    app.querySelectorAll('[data-edit]').forEach((btn) => {
      btn.addEventListener('click', () => {
        openEditModal(btn.getAttribute('data-edit')).catch((e) => alert(e.message));
      });
    });
    app.querySelector('#save-loadout')?.addEventListener('click', async () => {
      await api('/api/mods/enabled', { method: 'PUT', body: { packageIds: loadout } });
      alert('Loadout saved');
    });
  }

  let uploadSelected = {};

  async function renderUpload(preserveSelected) {
    try {
      await loadTargets();
    } catch (err) {
      app.innerHTML = `<section class="md-panel"><p class="md-error">${esc(err.message)}</p></section>`;
      return;
    }
    const selected = preserveSelected || uploadSelected || {};
    uploadSelected = selected;
    const nameVal = document.getElementById('mod-name')?.value || '';
    app.innerHTML = `<section class="md-panel"><h2>Upload mod package</h2>
      <label>Display name<input id="mod-name" placeholder="My mod" value="${esc(nameVal)}" /></label>
      ${slotPanelsHtml(uploadTab, { selected, idPrefix: 'up-' })}
      <div class="md-actions">
        <button type="button" id="submit-mod">Create &amp; publish</button>
      </div></section>`;

    wireSlotPanels(
      app,
      () => uploadTab,
      (t) => { uploadTab = t; },
      (sel) => {
        uploadSelected = sel;
        renderUpload(sel);
      }
    );
    app.querySelector('#submit-mod')?.addEventListener('click', () => submitMod().catch((e) => alert(e.message)));
  }

  async function addLemmaSlot(root, onDone) {
    const slotKey = root?.querySelector('[data-new-lemma-key]')?.value?.trim();
    const label = root?.querySelector('[data-new-lemma-label]')?.value?.trim();
    if (!slotKey) { alert('Slot key required'); return; }
    await api('/api/mods/moddable-targets', {
      method: 'POST',
      body: { targetKind: 'lemma_prompt', slotKey, label: label || slotKey },
    });
    if (root === modalBody) editTab = 'lemma';
    else uploadTab = 'lemma';
    if (onDone) await onDone();
  }

  async function addEpisodeSlot(root, onDone) {
    const ep = document.getElementById('episode-id')?.value?.trim();
    if (!ep) { alert('Episode ID required in header'); return; }
    const slotKey = root?.querySelector('[data-new-ep-key]')?.value?.trim();
    const label = root?.querySelector('[data-new-ep-label]')?.value?.trim();
    const charStart = Number(root?.querySelector('[data-new-ep-start]')?.value || 0);
    const charEnd = Number(root?.querySelector('[data-new-ep-end]')?.value || 0);
    if (!slotKey) { alert('Slot key required'); return; }
    const namespaced = slotKey.startsWith('ep-') ? slotKey : `ep-${ep.slice(0, 8)}-${slotKey}`;
    await api('/api/mods/moddable-targets', {
      method: 'POST',
      body: {
        targetKind: 'episode_section',
        draftEpisodeId: ep,
        slotKey: namespaced,
        label: label || slotKey,
        charStart,
        charEnd,
      },
    });
    if (root === modalBody) editTab = 'episode';
    else uploadTab = 'episode';
    if (onDone) await onDone();
  }

  async function submitMod() {
    const name = document.getElementById('mod-name')?.value?.trim();
    if (!name) { alert('Mod name required'); return; }
    const { lemmaOverrides, episodeOverrides } = collectOverrides(app);
    if (!lemmaOverrides.length && !episodeOverrides.length) {
      alert('Select at least one slot and enter override text');
      return;
    }
    const mod = await api('/api/mods', { method: 'POST', body: { displayName: name } });
    const pkg = await api('/api/mods/packages', {
      method: 'POST',
      body: { modId: mod.id, publish: true, lemmaOverrides, episodeOverrides },
    });
    loadout.push(pkg.packageId);
    uploadSelected = {};
    alert(`Published package ${pkg.packageId}`);
    setView('browse');
  }

  function render() {
    syncUserLabel();
    if (view === 'settings' && window.MayorDogModUploadSettings) {
      window.MayorDogModUploadSettings.render(app, api);
      return;
    }
    if (view === 'upload') renderUpload();
    else renderBrowse();
  }

  document.getElementById('md-modal-cancel')?.addEventListener('click', closeModal);
  document.getElementById('md-modal-save')?.addEventListener('click', () => {
    saveEditModal().catch((e) => alert(e.message));
  });
  modal?.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
  });

  function boot() {
    const start = () => {
      syncUserLabel();
      render();
    };
    if (Session() && Session().ensurePresent) {
      Session().ensurePresent({ title: 'Mayor Dog Mods — sign in required' }).then(start);
    } else {
      start();
    }
  }

  boot();
  window.MayorDogModPortal = { api, setView };
})();
