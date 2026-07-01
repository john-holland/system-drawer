(function () {
  const api = (path, opts = {}) => {
    const userId = document.getElementById('user-id')?.value || 'anonymous';
    return fetch(path, {
      ...opts,
      headers: {
        'Content-Type': 'application/json',
        'X-User-ID': userId,
        ...(opts.headers || {}),
      },
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
  let view = 'browse';
  let registry = [];
  let loadout = [];
  let lemmaTargets = [];
  let episodeTargets = [];

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

  async function loadRegistry() {
    const data = await api('/api/mods/registry');
    registry = data.items || [];
  }

  async function loadTargets() {
    const ep = document.getElementById('episode-id')?.value || '';
    const [lemma, episode] = await Promise.all([
      api('/api/mods/moddable-targets?targetKind=lemma_prompt'),
      ep ? api(`/api/mods/moddable-targets?draftEpisodeId=${encodeURIComponent(ep)}&targetKind=episode_section`) : Promise.resolve({ items: [] }),
    ]);
    lemmaTargets = lemma.items || [];
    episodeTargets = episode.items || [];
  }

  async function renderBrowse() {
    await loadRegistry();
    app.innerHTML = `<section class="md-panel"><h2>Published mods</h2>
      ${registry.map((m) => `<div class="md-card"><h3>${m.displayName}</h3><p>${m.slug} · v${m.latestVersion || '—'}</p>
        <button data-enable="${m.id}">Enable latest</button></div>`).join('') || '<p>No published mods yet.</p>'}
      <h2>Your loadout</h2><pre id="loadout">${JSON.stringify(loadout, null, 2)}</pre>
      <button id="save-loadout">Save loadout</button></section>`;
    app.querySelector('#save-loadout')?.addEventListener('click', async () => {
      await api('/api/mods/enabled', { method: 'PUT', body: { packageIds: loadout } });
      alert('Loadout saved');
    });
  }

  async function renderUpload() {
    await loadTargets();
    app.innerHTML = `<section class="md-panel"><h2>Upload mod package</h2>
      <label>Display name<input id="mod-name" /></label>
      <div class="md-tabs"><span class="md-tab active">Lemma slots</span><span class="md-tab">Episode sections</span></div>
      <div class="md-target-list" id="lemma-list">${lemmaTargets.map(t => targetRow(t, 'lemma')).join('')}</div>
      <div class="md-target-list" id="episode-list">${episodeTargets.map(t => targetRow(t, 'episode')).join('')}</div>
      <button id="submit-mod">Create & publish</button></section>`;
    app.querySelector('#submit-mod')?.addEventListener('click', submitMod);
  }

  function targetRow(t, kind) {
    return `<label><input type="checkbox" data-target="${t.id}" data-kind="${kind}" />
      <strong>${t.slotKey}</strong> — ${t.label || t.targetKind} [${t.charStart}, ${t.charEnd})
      <textarea data-override="${t.id}" placeholder="Override text"></textarea></label>`;
  }

  async function submitMod() {
    const name = document.getElementById('mod-name')?.value?.trim();
    if (!name) { alert('Mod name required'); return; }
    const mod = await api('/api/mods', { method: 'POST', body: { displayName: name } });
    const lemmaOverrides = [];
    const episodeOverrides = [];
    app.querySelectorAll('input[data-target]:checked').forEach((cb) => {
      const id = cb.getAttribute('data-target');
      const text = app.querySelector(`textarea[data-override="${id}"]`)?.value || '';
      const row = { targetId: id, overrideText: text };
      if (cb.getAttribute('data-kind') === 'lemma') lemmaOverrides.push(row);
      else episodeOverrides.push(row);
    });
    const pkg = await api('/api/mods/packages', {
      method: 'POST',
      body: { modId: mod.id, publish: true, lemmaOverrides, episodeOverrides },
    });
    loadout.push(pkg.packageId);
    alert(`Published package ${pkg.packageId}`);
    setView('browse');
  }

  function render() {
    if (view === 'settings' && window.MayorDogModUploadSettings) {
      window.MayorDogModUploadSettings.render(app, api);
      return;
    }
    if (view === 'upload') renderUpload();
    else renderBrowse();
  }

  render();
  window.MayorDogModPortal = { api, setView };
})();
