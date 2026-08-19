(function () {
  'use strict';

  const CS = window.ContinuuuumSettings;
  if (!CS) return;

  let activeGroup = 'script-output';
  let draft = CS.load();
  let modelsDraft = [];
  let modelsConcurrency = 1;
  let profilesDraft = [];
  let liveStream = null;
  let captureFile = null;
  let captureFileUrl = null;
  let recordingsPollTimer = null;

  const Session = window.ContinuuuumUserSession;

  function apiHeaders(extra) {
    extra = extra || {};
    if (Session && Session.getHeaders) {
      return Session.getHeaders(extra);
    }
    return extra;
  }

  function stopLiveWebcam() {
    if (liveStream) {
      liveStream.getTracks().forEach((t) => t.stop());
      liveStream = null;
    }
  }

  function stopRecordingsPoll() {
    if (recordingsPollTimer) {
      clearInterval(recordingsPollTimer);
      recordingsPollTimer = null;
    }
  }

  function revokeCaptureUrl() {
    if (captureFileUrl) {
      URL.revokeObjectURL(captureFileUrl);
      captureFileUrl = null;
    }
  }

  function leaveModelsConfig() {
    stopLiveWebcam();
    stopRecordingsPoll();
    revokeCaptureUrl();
    captureFile = null;
  }

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
    const admin = Session && Session.isAdmin && Session.isAdmin();
    const g = CS.GROUPS.find((x) => x.id === h);
    if (!g || !g.enabled) return 'script-output';
    if (g.adminOnly && !admin) return 'script-output';
    return g.id;
  }

  function renderGroupList() {
    const el = document.getElementById('cs-group-list');
    if (!el) return;
    const admin = Session && Session.isAdmin && Session.isAdmin();
    const groups = CS.GROUPS.filter((g) => !g.adminOnly || admin);
    el.innerHTML = groups.map((g) =>
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

  async function hydrateCivilLodFromServer() {
    try {
      const res = await fetch('/api/persona-day/settings');
      if (!res.ok) return;
      const data = await res.json();
      if (data.settings) {
        draft.civilLod = {
          ...(CS.DEFAULT_CIVIL_LOD || {}),
          ...(draft.civilLod || {}),
          ...data.settings,
          kindPriorityOrder: CS.normalizeCivilKindOrder(
            data.settings.kindPriorityOrder || draft.civilLod?.kindPriorityOrder,
          ),
        };
      }
    } catch (_) {
      /* ignore */
    }
  }

  function renderCivilLodPanel() {
    const cl = draft.civilLod || CS.DEFAULT_CIVIL_LOD || {};
    const order = CS.normalizeCivilKindOrder(cl.kindPriorityOrder);
    const rows = order
      .map(
        (kind, idx) =>
          `<div class="cs-priority-row" data-slot="${idx}">
            <label>Priority ${idx + 1}</label>
            <span>${esc(kind)}</span>
            <span class="cs-reorder">
              <button type="button" data-dir="-1" data-slot="${idx}" aria-label="Move up">↑</button>
              <button type="button" data-dir="1" data-slot="${idx}" aria-label="Move down">↓</button>
            </span>
          </div>`,
      )
      .join('');
    return `
      <h1>Civil LOD</h1>
      <p class="cs-hint">PersonaDayManager lattice: venue kind priority, developer speed bounds (log falloff),
      and FeatureBudget <code>civil_systems</code> caps. Syncs to <code>/api/persona-day/settings</code>.</p>
      <section aria-labelledby="cl-priority-heading">
        <h2 id="cl-priority-heading" style="font-size:1rem;margin:0 0 12px">Kind priority (1 = FullSim first)</h2>
        ${rows}
      </section>
      <label>Developer max speed (m/s)
        <input type="number" id="cl-vmax" step="0.1" min="0.1" value="${esc(cl.developerMaxSpeedMps ?? 12)}" />
      </label>
      <label>Log falloff base
        <input type="number" id="cl-logbase" step="0.1" min="2" value="${esc(cl.logFalloffBase ?? 10)}" />
      </label>
      <label>LOD floor
        <input type="number" id="cl-floor" step="0.01" min="0.05" max="1" value="${esc(cl.lodFloor ?? 0.15)}" />
      </label>
      <label>Max FullSim venues
        <input type="number" id="cl-max-full" min="0" max="64" value="${esc(cl.maxFullSimVenues ?? 4)}" />
      </label>
      <label>Max woken actors
        <input type="number" id="cl-max-woken" min="0" max="512" value="${esc(cl.maxWokenActors ?? 24)}" />
      </label>
      <p class="cs-hint">Feature budget id: <code>${esc(cl.featureBudgetId || 'civil_systems')}</code><br/>
      ${esc(cl.featureBudgetImportanceHint || '')}</p>
      <div class="cs-actions">
        <button type="button" id="cs-save-btn">Save Civil LOD</button>
        <span id="cs-status"></span>
      </div>`;
  }

  function bindCivilLodPanel() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;

    const syncDraft = () => {
      draft.civilLod = {
        ...(draft.civilLod || CS.DEFAULT_CIVIL_LOD),
        developerMaxSpeedMps: Number(panel.querySelector('#cl-vmax')?.value || 12),
        logFalloffBase: Number(panel.querySelector('#cl-logbase')?.value || 10),
        lodFloor: Number(panel.querySelector('#cl-floor')?.value || 0.15),
        maxFullSimVenues: Number(panel.querySelector('#cl-max-full')?.value || 4),
        maxWokenActors: Number(panel.querySelector('#cl-max-woken')?.value || 24),
        kindPriorityOrder: CS.normalizeCivilKindOrder(draft.civilLod?.kindPriorityOrder),
      };
    };

    panel.querySelectorAll('input').forEach((el) => {
      el.addEventListener('change', syncDraft);
      el.addEventListener('input', syncDraft);
    });

    panel.querySelectorAll('.cs-reorder button').forEach((btn) => {
      btn.onclick = () => {
        const slot = Number(btn.dataset.slot);
        const dir = Number(btn.dataset.dir);
        const list = CS.normalizeCivilKindOrder(draft.civilLod?.kindPriorityOrder);
        const to = slot + dir;
        if (to < 0 || to >= list.length) return;
        const tmp = list[slot];
        list[slot] = list[to];
        list[to] = tmp;
        draft.civilLod = { ...(draft.civilLod || {}), kindPriorityOrder: list };
        renderPanelContent();
      };
    });

    const saveBtn = panel.querySelector('#cs-save-btn');
    if (saveBtn) {
      saveBtn.onclick = async () => {
        syncDraft();
        try {
          CS.saveCivilLod(draft.civilLod);
          const res = await fetch('/api/persona-day/settings', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ settings: draft.civilLod }),
          });
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || 'Save failed');
          draft.civilLod = { ...draft.civilLod, ...(data.settings || {}) };
          setStatus('Civil LOD settings saved.');
        } catch (e) {
          setStatus(e.message || 'Save failed', true);
        }
      };
    }
  }

  function catalogKindOptions(selected) {
    return ['pose', 'whisper', 'music'].map(
      (k) => `<option value="${k}"${k === selected ? ' selected' : ''}>${k}</option>`,
    ).join('');
  }

  function defaultForKindOptions(selected) {
    const vals = ['', 'pose', 'whisper', 'music'];
    return vals.map(
      (k) => `<option value="${esc(k)}"${k === (selected || '') ? ' selected' : ''}>${k || '—'}</option>`,
    ).join('');
  }

  function renderCatalogRows() {
    if (!modelsDraft.length) {
      return '<tr><td colspan="6" class="cs-hint">No models yet. Add a row or save to seed defaults from the server.</td></tr>';
    }
    return modelsDraft.map((m, idx) => `
      <tr class="cs-model-row" data-idx="${idx}">
        <td><input type="text" data-f="id" value="${esc(m.id || '')}" placeholder="family@variant" /></td>
        <td><select data-f="kind">${catalogKindOptions(m.kind || 'pose')}</select></td>
        <td><input type="text" data-f="label" value="${esc(m.label || '')}" /></td>
        <td><input type="checkbox" data-f="enabled"${m.enabled !== false ? ' checked' : ''} /></td>
        <td><select data-f="defaultForKind">${defaultForKindOptions(m.defaultForKind)}</select></td>
        <td><button type="button" class="cs-model-remove" data-idx="${idx}">Remove</button></td>
      </tr>`).join('');
  }

  function poseSpecOptions(selected) {
    const enabled = modelsDraft.filter((m) => m.enabled !== false && (m.kind || 'pose') === 'pose' && m.id);
    const ids = enabled.map((m) => m.id);
    const opts = enabled.map(
      (m) => `<option value="${esc(m.id)}"${m.id === selected ? ' selected' : ''}>${esc(m.label || m.id)}</option>`,
    );
    if (selected && !ids.includes(selected)) {
      opts.unshift(`<option value="${esc(selected)}" selected>${esc(selected)}</option>`);
    }
    return opts.join('');
  }

  function renderProfileRows() {
    if (!profilesDraft.length) {
      return '<tr><td colspan="8" class="cs-hint">No detector profiles. Save catalog to seed Human / Animal defaults.</td></tr>';
    }
    return profilesDraft.map((p, idx) => `
      <tr class="cs-profile-row" data-idx="${idx}">
        <td><input type="text" data-pf="id" value="${esc(p.id || '')}" /></td>
        <td><input type="text" data-pf="label" value="${esc(p.label || '')}" /></td>
        <td><input type="checkbox" data-pf="enabled"${p.enabled !== false ? ' checked' : ''} /></td>
        <td>
          <select data-pf="poseEngine">
            <option value="mediapipe"${p.poseEngine === 'mediapipe' ? ' selected' : ''}>MediaPipe</option>
            <option value="mocapanything"${p.poseEngine === 'mocapanything' ? ' selected' : ''}>MoCapAnything</option>
          </select>
        </td>
        <td><select data-pf="mediapipeSpec">${poseSpecOptions(p.mediapipeSpec || 'mediapipe_holistic@v1')}</select></td>
        <td><select data-pf="mocapSpec">${poseSpecOptions(p.mocapSpec || 'mocapanything@v2')}</select></td>
        <td><input type="text" data-pf="defaultSpecies" value="${esc(p.defaultSpecies || '')}" placeholder="Lion" /></td>
        <td>
          <input type="text" data-pf="mocapRoot" value="${esc(p.mocapRoot || '')}" placeholder="optional root" />
          <input type="number" data-pf="mocapTimeoutSec" min="1" value="${esc(p.mocapTimeoutSec || 10)}" />
          <button type="button" class="cs-profile-remove" data-idx="${idx}">Remove</button>
        </td>
      </tr>`).join('');
  }

  function syncProfilesFromDom(panel) {
    const rows = [];
    panel.querySelectorAll('.cs-profile-row').forEach((tr) => {
      rows.push({
        id: (tr.querySelector('[data-pf="id"]')?.value || '').trim(),
        label: tr.querySelector('[data-pf="label"]')?.value || '',
        enabled: !!tr.querySelector('[data-pf="enabled"]')?.checked,
        poseEngine: tr.querySelector('[data-pf="poseEngine"]')?.value || 'mediapipe',
        mediapipeSpec: tr.querySelector('[data-pf="mediapipeSpec"]')?.value || 'mediapipe_holistic@v1',
        mocapSpec: tr.querySelector('[data-pf="mocapSpec"]')?.value || 'mocapanything@v2',
        defaultSpecies: tr.querySelector('[data-pf="defaultSpecies"]')?.value || '',
        mocapRoot: tr.querySelector('[data-pf="mocapRoot"]')?.value || '',
        mocapTimeoutSec: Number(tr.querySelector('[data-pf="mocapTimeoutSec"]')?.value || 10),
      });
    });
    profilesDraft = rows;
  }

  function derivedSpecForProfile(profileId) {
    const p = profilesDraft.find((x) => x.id === profileId && x.enabled !== false) || profilesDraft.find((x) => x.enabled !== false);
    if (!p) return '';
    return p.poseEngine === 'mocapanything' ? (p.mocapSpec || '') : (p.mediapipeSpec || '');
  }

  function profileOptions(selected) {
    const want = selected || '';
    return (profilesDraft || []).filter((p) => p.enabled !== false && p.id).map(
      (p) => `<option value="${esc(p.id)}"${p.id === want ? ' selected' : ''}>${esc(p.label || p.id)}</option>`,
    ).join('');
  }

  function modelSpecOptions(selected) {
    const mc = draft.modelsConfig || CS.DEFAULT_MODELS_CONFIG;
    const want = selected || mc.modelSpec || '';
    const enabled = modelsDraft.filter((m) => m.enabled !== false && m.id);
    const ids = enabled.map((m) => m.id);
    const custom = want && !ids.includes(want);
    const opts = enabled.map(
      (m) => `<option value="${esc(m.id)}"${m.id === want && !custom ? ' selected' : ''}>${esc(m.label || m.id)} (${esc(m.kind)})</option>`,
    );
    opts.push(`<option value="__custom__"${custom || !want ? ' selected' : ''}>Custom…</option>`);
    return opts.join('');
  }

  function renderRecordingAction(doc) {
    const status = doc.queueStatus || 'none';
    if (status === 'queued' || status === 'running') {
      return `<span class="cs-in-progress">In progress <small>(${esc(status)})</small></span>`;
    }
    if (status === 'failed') {
      return `<span class="cs-failed">${esc(doc.queueError || doc.error || 'failed')}</span>`;
    }
    const href = doc.previewUrl || '';
    return href ? `<a href="${esc(href)}" target="_blank" rel="noopener">View</a>` : '';
  }

  function renderModelsConfigPanel() {
    const mc = draft.modelsConfig || CS.DEFAULT_MODELS_CONFIG || {};
    const customSpec = (() => {
      const ids = modelsDraft.filter((m) => m.enabled !== false).map((m) => m.id);
      const want = mc.modelSpec || '';
      return want && !ids.includes(want) ? want : '';
    })();
    return `
      <h1>Models / Configuration</h1>
      <p class="cs-hint">Animation-model catalog (<code>family@variant</code>) and total concurrency for
      webcam / video IK jobs. Lemma Library stays on LM Studio / Codestral. Source of truth is
      <code>/api/webcam-animations/models</code>.</p>

      <section class="cs-section" aria-labelledby="cs-catalog-heading">
        <h2 id="cs-catalog-heading">Configuration</h2>
        <table class="cs-catalog-table">
          <thead>
            <tr>
              <th>id</th>
              <th>kind</th>
              <th>label</th>
              <th>enabled</th>
              <th>default for kind</th>
              <th></th>
            </tr>
          </thead>
          <tbody id="cs-catalog-body">${renderCatalogRows()}</tbody>
        </table>
        <div class="cs-actions" style="margin-top:0">
          <button type="button" id="cs-add-model">Add model</button>
        </div>
        <label>Total concurrency
          <input type="number" id="cs-total-concurrency" min="1" step="1" value="${esc(modelsConcurrency || 1)}" />
        </label>
        <div class="cs-actions">
          <button type="button" id="cs-save-catalog">Save catalog</button>
          <span id="cs-status"></span>
        </div>
      </section>

      <section class="cs-section" aria-labelledby="cs-profiles-heading">
        <h2 id="cs-profiles-heading">Detector profiles</h2>
        <p class="cs-hint">Named MediaPipe / MoCapAnything pins. Table-read video animation and webcam capture
        enqueue the catalog <code>@version</code> from the selected profile, not a free-typed spec.</p>
        <table class="cs-catalog-table">
          <thead>
            <tr>
              <th>id</th>
              <th>label</th>
              <th>on</th>
              <th>engine</th>
              <th>MediaPipe spec</th>
              <th>MoCap spec</th>
              <th>default species</th>
              <th>root / timeout</th>
            </tr>
          </thead>
          <tbody id="cs-profiles-body">${renderProfileRows()}</tbody>
        </table>
        <div class="cs-actions" style="margin-top:0">
          <button type="button" id="cs-add-profile">Add profile</button>
        </div>
      </section>

      <section class="cs-section" aria-labelledby="cs-capture-heading">
        <h2 id="cs-capture-heading">Capture</h2>
        <p class="cs-hint">Live preview and video file enqueue to the same recordings queue as Webcam Anim.
        Save does not wait on USC upload.</p>
        <div class="cs-webcam-preview">
          <video id="cs-webcam-live" autoplay playsinline muted></video>
        </div>
        <div class="cs-actions" style="margin-top:0">
          <button type="button" id="cs-webcam-start">Start webcam</button>
          <button type="button" id="cs-webcam-stop">Stop</button>
        </div>
        <label>Video file
          <input type="file" id="cs-capture-file" accept="video/*" />
        </label>
        <div class="cs-webcam-preview">
          <video id="cs-webcam-file" controls playsinline></video>
        </div>
        <div class="cs-capture-grid">
          <label>Kind
            <select id="cs-capture-kind">
              <option value="ambulatory"${mc.kind === 'ambulatory' ? ' selected' : ''}>Ambulatory</option>
              <option value="vehicle"${mc.kind === 'vehicle' ? ' selected' : ''}>Vehicle</option>
              <option value="dance"${mc.kind === 'dance' ? ' selected' : ''}>Dance</option>
              <option value="misc"${mc.kind === 'misc' ? ' selected' : ''}>Misc</option>
            </select>
          </label>
          <label>Detector profile
            <select id="cs-capture-profile">${profileOptions(mc.detectorProfileId || 'human-mediapipe-v1')}</select>
          </label>
          <label>Pinned model spec
            <input type="text" id="cs-capture-spec-pinned" value="${esc(derivedSpecForProfile(mc.detectorProfileId || 'human-mediapipe-v1'))}" readonly />
          </label>
          <label>Model spec (no profile)
            <select id="cs-capture-spec">${modelSpecOptions(mc.modelSpec)}</select>
          </label>
          <label>Custom model spec
            <input type="text" id="cs-capture-spec-custom" value="${esc(customSpec)}" placeholder="family@variant" />
          </label>
          <label>Subsection
            <input type="text" id="cs-capture-subsection" placeholder="takeoff_roll_0" />
          </label>
          <label>Start ms
            <input type="number" id="cs-capture-start" value="0" />
          </label>
          <label>End ms
            <input type="number" id="cs-capture-end" value="1000" />
          </label>
          <label>Granularity
            <select id="cs-capture-granularity">
              <option value="decimillisecond">decimillisecond</option>
              <option value="millisecond" selected>millisecond</option>
              <option value="centisecond">centisecond</option>
              <option value="decisecond">decisecond</option>
              <option value="second">second</option>
              <option value="decasecond">decasecond</option>
              <option value="minute">minute</option>
            </select>
          </label>
          <label>Target hint
            <input type="text" id="cs-capture-target" value="ragdoll" />
          </label>
          <label>Species (MoCapAnything)
            <input type="text" id="cs-capture-species" placeholder="Lion" />
          </label>
        </div>
        <div class="cs-actions">
          <button type="button" id="cs-capture-save">Save (enqueue)</button>
          <span id="cs-capture-status"></span>
        </div>
        <h2 style="font-size:1rem;margin:20px 0 8px">Recordings</h2>
        <ul id="cs-recordings" class="cs-recordings"></ul>
      </section>`;
  }

  function syncCatalogFromDom(panel) {
    const rows = [];
    panel.querySelectorAll('.cs-model-row').forEach((tr) => {
      rows.push({
        id: (tr.querySelector('[data-f="id"]')?.value || '').trim(),
        kind: tr.querySelector('[data-f="kind"]')?.value || 'pose',
        label: tr.querySelector('[data-f="label"]')?.value || '',
        enabled: !!tr.querySelector('[data-f="enabled"]')?.checked,
        defaultForKind: tr.querySelector('[data-f="defaultForKind"]')?.value || '',
        detectorId: '',
        notes: '',
      });
    });
    modelsDraft.forEach((prev, i) => {
      if (rows[i]) {
        rows[i].detectorId = prev.detectorId || '';
        rows[i].notes = prev.notes || '';
      }
    });
    modelsDraft = rows;
    modelsConcurrency = Math.max(1, Number(panel.querySelector('#cs-total-concurrency')?.value || 1));
  }

  function resolvedCaptureSpec(panel) {
    const sel = panel.querySelector('#cs-capture-spec');
    const custom = (panel.querySelector('#cs-capture-spec-custom')?.value || '').trim();
    if (!sel || sel.value === '__custom__') return custom;
    return sel.value;
  }

  function persistCapturePrefs(panel) {
    const profileId = panel.querySelector('#cs-capture-profile')?.value || '';
    draft.modelsConfig = {
      ...(draft.modelsConfig || CS.DEFAULT_MODELS_CONFIG),
      modelSpec: profileId ? derivedSpecForProfile(profileId) : resolvedCaptureSpec(panel),
      detectorProfileId: profileId,
      kind: panel.querySelector('#cs-capture-kind')?.value || 'ambulatory',
      totalConcurrency: modelsConcurrency,
    };
    CS.saveModelsConfig(draft.modelsConfig);
  }

  function attachMediaPreviews() {
    const live = document.getElementById('cs-webcam-live');
    if (live && liveStream) {
      live.srcObject = liveStream;
      live.play().catch(() => {});
    }
    const fileVid = document.getElementById('cs-webcam-file');
    if (fileVid && captureFileUrl) {
      fileVid.src = captureFileUrl;
    }
  }

  async function refreshSettingsRecordings() {
    const list = document.getElementById('cs-recordings');
    if (!list) return;
    try {
      const res = await fetch('/api/webcam-animations?kind=webcam_anim_recording', {
        headers: apiHeaders(),
        credentials: 'include',
      });
      const rows = await res.json();
      if (!res.ok) throw new Error(rows.error || 'list failed');
      list.innerHTML = (rows || []).map((doc) => {
        const meta = doc.type_metadata || {};
        const title = doc.subsection || meta.subsection || doc.id;
        return `<li>
          <div><strong>${esc(title)}</strong>
            <span> ${esc(doc.webcamAnimKind || '')}</span>
            <div class="cs-hint" style="margin:4px 0 0">${esc(doc.model_spec || '')} ·
              ${esc(doc.timelineStartMs || 0)}–${esc(doc.timelineEndMs || 0)} ms</div>
          </div>
          ${renderRecordingAction(doc)}
        </li>`;
      }).join('') || '<li class="cs-hint">No recordings yet.</li>';
      const busy = (rows || []).some((r) => r.queueStatus === 'queued' || r.queueStatus === 'running');
      if (busy && !recordingsPollTimer) {
        recordingsPollTimer = setInterval(() => {
          refreshSettingsRecordings().catch(() => {});
        }, 3000);
      } else if (!busy) {
        stopRecordingsPoll();
      }
    } catch (e) {
      list.innerHTML = `<li class="cs-failed">${esc(e.message || e)}</li>`;
    }
  }

  function bindModelsConfigPanel() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;

    panel.querySelectorAll('.cs-model-row input, .cs-model-row select, #cs-total-concurrency, .cs-profile-row input, .cs-profile-row select').forEach((el) => {
      el.addEventListener('change', () => {
        syncCatalogFromDom(panel);
        syncProfilesFromDom(panel);
      });
      el.addEventListener('input', () => {
        syncCatalogFromDom(panel);
        syncProfilesFromDom(panel);
      });
    });

    const addBtn = panel.querySelector('#cs-add-model');
    if (addBtn) {
      addBtn.onclick = () => {
        syncCatalogFromDom(panel);
        modelsDraft.push({
          id: '',
          kind: 'pose',
          label: '',
          enabled: true,
          defaultForKind: '',
          detectorId: '',
          notes: '',
        });
        renderPanelContent();
      };
    }

    panel.querySelectorAll('.cs-model-remove').forEach((btn) => {
      btn.onclick = () => {
        syncCatalogFromDom(panel);
        const idx = Number(btn.dataset.idx);
        modelsDraft.splice(idx, 1);
        renderPanelContent();
      };
    });

    const addProfile = panel.querySelector('#cs-add-profile');
    if (addProfile) {
      addProfile.onclick = () => {
        syncProfilesFromDom(panel);
        profilesDraft.push({
          id: 'profile-' + Date.now().toString(36),
          label: 'New profile',
          enabled: true,
          poseEngine: 'mediapipe',
          mediapipeSpec: 'mediapipe_holistic@v1',
          mocapSpec: 'mocapanything@v2',
          defaultSpecies: '',
          mocapRoot: '',
          mocapTimeoutSec: 10,
        });
        renderPanelContent();
      };
    }

    panel.querySelectorAll('.cs-profile-remove').forEach((btn) => {
      btn.onclick = () => {
        syncProfilesFromDom(panel);
        const idx = Number(btn.dataset.idx);
        profilesDraft.splice(idx, 1);
        renderPanelContent();
      };
    });

    const profileSel = panel.querySelector('#cs-capture-profile');
    const pinned = panel.querySelector('#cs-capture-spec-pinned');
    if (profileSel && pinned) {
      profileSel.addEventListener('change', () => {
        pinned.value = derivedSpecForProfile(profileSel.value);
      });
    }

    const saveCat = panel.querySelector('#cs-save-catalog');
    if (saveCat) {
        saveCat.onclick = async () => {
        syncCatalogFromDom(panel);
        syncProfilesFromDom(panel);
        persistCapturePrefs(panel);
        setStatus('Saving catalog…');
        try {
          const res = await fetch('/api/webcam-animations/models', {
            method: 'PUT',
            headers: apiHeaders({ 'Content-Type': 'application/json' }),
            credentials: 'include',
            body: JSON.stringify({
              models: modelsDraft,
              totalConcurrency: modelsConcurrency,
              detectorProfiles: profilesDraft,
            }),
          });
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || 'Save failed');
          modelsDraft = data.models || modelsDraft;
          modelsConcurrency = data.totalConcurrency || modelsConcurrency;
          profilesDraft = data.detectorProfiles || profilesDraft;
          draft.modelsConfig = {
            ...(draft.modelsConfig || {}),
            totalConcurrency: modelsConcurrency,
          };
          CS.saveModelsConfig(draft.modelsConfig);
          setStatus('Catalog saved.');
          renderPanelContent();
        } catch (e) {
          setStatus(e.message || 'Save failed', true);
        }
      };
    }

    const startBtn = panel.querySelector('#cs-webcam-start');
    if (startBtn) {
      startBtn.onclick = async () => {
        const status = panel.querySelector('#cs-capture-status');
        try {
          liveStream = await navigator.mediaDevices.getUserMedia({ video: true });
          attachMediaPreviews();
          if (status) status.textContent = 'Webcam on.';
        } catch (e) {
          if (status) status.textContent = e.message || 'Webcam failed';
        }
      };
    }

    const stopBtn = panel.querySelector('#cs-webcam-stop');
    if (stopBtn) {
      stopBtn.onclick = () => {
        stopLiveWebcam();
        const live = panel.querySelector('#cs-webcam-live');
        if (live) live.srcObject = null;
      };
    }

    const fileInput = panel.querySelector('#cs-capture-file');
    if (fileInput) {
      fileInput.onchange = () => {
        revokeCaptureUrl();
        captureFile = fileInput.files && fileInput.files[0] ? fileInput.files[0] : null;
        if (captureFile) {
          captureFileUrl = URL.createObjectURL(captureFile);
        }
        attachMediaPreviews();
      };
    }

    const saveCap = panel.querySelector('#cs-capture-save');
    if (saveCap) {
      saveCap.onclick = async () => {
        const status = panel.querySelector('#cs-capture-status');
        persistCapturePrefs(panel);
        const profileId = panel.querySelector('#cs-capture-profile')?.value || '';
        const body = {
          kind: 'webcam_anim_recording',
          webcamAnimKind: panel.querySelector('#cs-capture-kind')?.value || 'ambulatory',
          detectorProfileId: profileId,
          model_spec: profileId ? derivedSpecForProfile(profileId) : resolvedCaptureSpec(panel),
          subsection: panel.querySelector('#cs-capture-subsection')?.value || '',
          animationListIndex: 0,
          timelineStartMs: Number(panel.querySelector('#cs-capture-start')?.value || 0),
          timelineEndMs: Number(panel.querySelector('#cs-capture-end')?.value || 0),
          granularity: panel.querySelector('#cs-capture-granularity')?.value || 'millisecond',
          targetHint: panel.querySelector('#cs-capture-target')?.value || 'ragdoll',
          species: panel.querySelector('#cs-capture-species')?.value || '',
        };
        if (status) status.textContent = 'Enqueueing…';
        try {
          let res;
          if (captureFile) {
            const fd = new FormData();
            fd.append('type_metadata', JSON.stringify(body));
            fd.append('file', captureFile, captureFile.name);
            const headers = apiHeaders();
            delete headers['Content-Type'];
            res = await fetch('/api/webcam-animations', {
              method: 'POST',
              headers,
              credentials: 'include',
              body: fd,
            });
          } else {
            res = await fetch('/api/webcam-animations', {
              method: 'POST',
              headers: apiHeaders({ 'Content-Type': 'application/json' }),
              credentials: 'include',
              body: JSON.stringify(body),
            });
          }
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || 'Enqueue failed');
          if (status) status.textContent = 'Queued ' + data.id + (data.queueStatus ? ' (' + data.queueStatus + ')' : '');
          await refreshSettingsRecordings();
        } catch (e) {
          if (status) status.textContent = e.message || 'Enqueue failed';
        }
      };
    }

    attachMediaPreviews();
    refreshSettingsRecordings().catch(() => {});
  }

  function renderAssetOwnersPanel() {
    return `
      <h1>Asset owners</h1>
      <p class="cs-hint">Admin-only. Reassign USC library documents or Continuuuum assets
      (webcam recording, table-read audio, dialog set). Writes <code>asset_owner_history</code>
      and a warehouse event <code>asset_owner_reassigned</code>. Not part of Lemma Library.</p>
      <label>Kind
        <select id="ao-kind">
          <option value="usc">USC library document</option>
          <option value="continuuuum">Continuuuum asset</option>
        </select>
      </label>
      <label>Asset id
        <input type="text" id="ao-asset-id" placeholder="document id or recording / session / set id" />
      </label>
      <label>New owner user id
        <input type="text" id="ao-to-owner" />
      </label>
      <label>Reason
        <input type="text" id="ao-reason" />
      </label>
      <div class="cs-actions">
        <button type="button" id="ao-save">Save</button>
        <span id="cs-status"></span>
      </div>
      <p class="cs-hint"><a href="/sql-viewer?recipe=asset_owner_history">SQL Viewer history</a>
      · <a href="/sql-viewer?recipe=asset_owner_warehouse">Warehouse events</a></p>
      <h2 style="font-size:1rem">History</h2>
      <div id="ao-history"><p class="cs-hint">Loading…</p></div>`;
  }

  function bindAssetOwnersPanel() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;
    const saveBtn = panel.querySelector('#ao-save');
    if (saveBtn) {
      saveBtn.onclick = async () => {
        try {
          const res = await fetch('/api/admin/asset-owners', {
            method: 'POST',
            headers: apiHeaders({ 'Content-Type': 'application/json' }),
            credentials: 'include',
            body: JSON.stringify({
              assetKind: panel.querySelector('#ao-kind')?.value,
              assetId: panel.querySelector('#ao-asset-id')?.value,
              toOwner: panel.querySelector('#ao-to-owner')?.value,
              reason: panel.querySelector('#ao-reason')?.value,
            }),
          });
          const data = await res.json();
          if (!res.ok) throw new Error(data.error || 'Reassign failed');
          setStatus('Reassigned ' + (data.assetId || '') + ' → ' + (data.toOwner || ''));
          await loadAssetOwnerHistory();
        } catch (e) {
          setStatus(e.message || 'Reassign failed', true);
        }
      };
    }
    loadAssetOwnerHistory();
  }

  async function loadAssetOwnerHistory() {
    const host = document.getElementById('ao-history');
    if (!host) return;
    try {
      const res = await fetch('/api/admin/asset-owners', {
        headers: apiHeaders(),
        credentials: 'include',
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'History failed');
      const items = data.items || [];
      if (!items.length) {
        host.innerHTML = '<p class="cs-hint">No reassignments yet.</p>';
        return;
      }
      host.innerHTML = '<table class="cs-history"><thead><tr>' +
        '<th>When</th><th>Kind</th><th>Asset</th><th>From</th><th>To</th><th>Admin</th><th>Reason</th>' +
        '</tr></thead><tbody>' +
        items.map((r) =>
          `<tr><td>${esc(r.createdAt)}</td><td>${esc(r.assetKind)}</td><td>${esc(r.assetId)}</td>` +
          `<td>${esc(r.fromOwner || '')}</td><td>${esc(r.toOwner)}</td>` +
          `<td>${esc(r.adminUserId)}</td><td>${esc(r.reason || '')}</td></tr>`
        ).join('') +
        '</tbody></table>';
    } catch (e) {
      host.innerHTML = '<p class="cs-hint">' + esc(e.message || 'Could not load history') + '</p>';
    }
  }

  async function hydrateModelsFromServer() {
    try {
      const res = await fetch('/api/webcam-animations/models', {
        headers: apiHeaders(),
        credentials: 'include',
      });
      if (!res.ok) return;
      const data = await res.json();
      modelsDraft = (data.models || []).map((m) => ({ ...m }));
      modelsConcurrency = Math.max(1, Number(data.totalConcurrency) || 1);
      profilesDraft = (data.detectorProfiles || []).map((p) => ({ ...p }));
      const local = CS.getModelsConfig() || CS.DEFAULT_MODELS_CONFIG;
      draft.modelsConfig = {
        ...CS.DEFAULT_MODELS_CONFIG,
        ...local,
        totalConcurrency: modelsConcurrency,
        detectorProfileId: local.detectorProfileId || CS.DEFAULT_MODELS_CONFIG.detectorProfileId,
      };
    } catch (_) {
      /* ignore */
    }
  }

  function renderPanelContent() {
    const panel = document.getElementById('cs-panel');
    if (!panel) return;
    if (activeGroup !== 'models-config') {
      stopRecordingsPoll();
    }
    if (activeGroup === 'script-output') {
      panel.innerHTML = renderScriptOutputPanel();
      bindScriptOutputPanel();
    } else if (activeGroup === 'lemma-library') {
      panel.innerHTML = renderLemmaLibraryPanel();
      bindLemmaLibraryPanel();
    } else if (activeGroup === 'models-config') {
      panel.innerHTML = renderModelsConfigPanel();
      bindModelsConfigPanel();
    } else if (activeGroup === 'civil-lod') {
      panel.innerHTML = renderCivilLodPanel();
      bindCivilLodPanel();
    } else if (activeGroup === 'asset-owners') {
      panel.innerHTML = renderAssetOwnersPanel();
      bindAssetOwnersPanel();
    } else {
      panel.innerHTML = '<h1>Coming soon</h1><p class="cs-hint">This settings group is not available yet.</p>';
    }
  }

  function render() {
    renderGroupList();
    renderPanelContent();
  }

  window.addEventListener('hashchange', () => {
    const next = groupFromHash();
    if (activeGroup === 'models-config' && next !== 'models-config') {
      leaveModelsConfig();
    }
    activeGroup = next;
    render();
  });

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'settings', theme: 'light' });
  }

  activeGroup = groupFromHash();
  draft = CS.load();
  Promise.all([
    hydrateLemmaLibraryFromServer(),
    hydrateCivilLodFromServer(),
    hydrateModelsFromServer(),
  ]).finally(render);
})();
