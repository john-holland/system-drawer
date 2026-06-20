(function () {
  'use strict';

  const API = '';
  let browseItems = [];
  let locRows = [];
  let multisortBrowse = null;
  let multisortLoc = null;

  async function api(path, opts) {
    const r = await fetch(API + path, { credentials: 'include', ...opts });
    const data = await r.json().catch(() => ({}));
    if (!r.ok) throw new Error(data.error || r.statusText);
    return data;
  }

  function getRoute() {
    const h = (location.hash.slice(1) || 'browse').split('?')[0];
    if (h.startsWith('entry/')) return { page: 'entry', id: decodeURIComponent(h.slice(6)) };
    return { page: h || 'browse' };
  }

  function setActiveNav(page) {
    document.querySelectorAll('[data-nav]').forEach(a => {
      a.classList.toggle('active', a.dataset.nav === page);
    });
  }

  function showView(page) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    const el = document.getElementById('view-' + page);
    if (el) el.classList.add('active');
    const layout = document.getElementById('main-layout');
    if (layout) {
      layout.classList.toggle('layout-single', page === 'create' || page === 'import' || page === 'entry');
    }
    setActiveNav(page === 'entry' ? 'browse' : page);
  }

  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  const BROWSE_DIMS = [
    { id: 'alpha', label: 'Alphabetical', field: 'alpha', visible: true, asc: true },
    { id: 'pos', label: 'Part of speech', field: 'posTag', visible: true, asc: true },
    { id: 'source', label: 'Built-in vs custom', field: 'isBuiltIn', visible: true, asc: true },
    { id: 'components', label: 'Components', field: 'components', visible: false, asc: true },
  ];

  const LOC_DIMS = [
    { id: 'propKey', label: 'Property key', field: 'propertyKey', visible: true, asc: true },
    { id: 'lemma', label: 'Lemma term', field: 'lemmaTerm', visible: true, asc: true },
    { id: 'spec', label: 'Spec type', field: 'specType', visible: false, asc: true },
    { id: 'component', label: 'Component', field: 'component', visible: true, asc: true },
    { id: 'source', label: 'Built-in vs custom', field: 'isBuiltIn', visible: false, asc: true },
  ];

  function renderBrowseList() {
    const container = document.getElementById('browse-results');
    const countEl = document.getElementById('results-count');
    if (!container || !multisortBrowse) return;
    const groups = multisortBrowse.groupItems(browseItems);
    container.innerHTML = '';
    let total = 0;
    groups.forEach(g => {
      const hdr = document.createElement('div');
      hdr.className = 'section-header';
      hdr.textContent = g.key;
      container.appendChild(hdr);
      g.items.forEach(item => {
        total++;
        const row = document.createElement('div');
        row.className = 'entry-row';
        row.innerHTML =
          '<span class="entry-term"></span>' +
          '<span class="badge"></span>' +
          '<span class="chip-pos chip"></span>' +
          '<span class="chips"></span>';
        row.querySelector('.entry-term').textContent = item.term;
        const badge = row.querySelector('.badge');
        badge.textContent = item.isBuiltIn ? 'Built-in' : 'Custom';
        badge.classList.add(item.isBuiltIn ? 'builtin' : 'custom');
        row.querySelector('.chip-pos').textContent = item.posTag || '';
        const chips = row.querySelector('.chips');
        (item.components || []).slice(0, 3).forEach(c => {
          const ch = document.createElement('span');
          ch.className = 'chip';
          ch.textContent = c;
          chips.appendChild(ch);
        });
        if (item.clauseCount > 0) {
          const ch = document.createElement('span');
          ch.className = 'chip';
          ch.textContent = item.clauseCount + ' clauses';
          chips.appendChild(ch);
        }
        row.onclick = () => { location.hash = 'entry/' + encodeURIComponent(item.id); route(); };
        container.appendChild(row);
      });
    });
    if (countEl) countEl.textContent = total + ' entries';
  }

  function renderLocList() {
    const container = document.getElementById('loc-results');
    if (!container || !multisortLoc) return;
    const groups = multisortLoc.groupItems(locRows);
    container.innerHTML = '';
    groups.forEach(g => {
      const hdr = document.createElement('div');
      hdr.className = 'section-header';
      hdr.textContent = g.key;
      container.appendChild(hdr);
      g.items.forEach(row => {
        const el = document.createElement('div');
        el.className = 'entry-row';
        const term = row.lemmaTerm || row.selectionText || '—';
        el.innerHTML =
          '<span class="entry-term"></span>' +
          '<span class="chip"></span>' +
          '<span class="chip"></span>';
        el.querySelector('.entry-term').textContent = term;
        const chips = el.querySelectorAll('.chip');
        chips[0].textContent = row.propertyKey || row.kind || '';
        chips[1].textContent = row.propertyValue || '';
        if (row.lemmaId) {
          el.onclick = () => { location.hash = 'entry/' + encodeURIComponent(row.lemmaId); route(); };
        }
        container.appendChild(el);
      });
    });
  }

  async function loadBrowse() {
    const q = document.getElementById('search-q')?.value || '';
    const language = document.getElementById('filter-lang')?.value || '';
    const source = document.getElementById('filter-source')?.value || 'all';
    const params = new URLSearchParams({ limit: '2000' });
    if (q) params.set('q', q);
    if (language) params.set('language', language);
    if (source) params.set('source', source);
    const data = await api('/api/thesaurus/entries?' + params);
    browseItems = data.items || [];
    renderBrowseList();
  }

  async function loadLocalization() {
    const q = document.getElementById('loc-search-q')?.value || '';
    const propertyKey = document.getElementById('loc-filter-key')?.value || '';
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (propertyKey) params.set('propertyKey', propertyKey);
    const data = await api('/api/thesaurus/localization-view?' + params);
    locRows = data.rows || [];
    renderLocList();
  }

  async function loadEntry(id) {
    const data = await api('/api/thesaurus/entries?entryId=' + encodeURIComponent(id));
    const el = document.getElementById('entry-detail');
    if (!el) return;
    const props = Object.entries(data.properties || {})
      .map(([k, v]) => `<tr><td>${esc(k)}</td><td>${esc(v)}</td></tr>`)
      .join('');
    const syns = (data.synonyms || []).join(', ') || '—';
    const tags = (data.tags || []).join(', ') || '—';
    const libBase = localStorage.getItem('continuumLibraryBase') || '';
    const assetLink = (data.linkedAssetIds || [])[0];
    el.innerHTML =
      '<h2>' + esc(data.term) + '</h2>' +
      '<dl class="detail-grid">' +
      '<dt>ID</dt><dd><code>' + esc(data.id) + '</code></dd>' +
      '<dt>Part of speech</dt><dd>' + esc(data.posTag) + '</dd>' +
      '<dt>Language</dt><dd>' + esc(data.languageCode) + '</dd>' +
      '<dt>Source</dt><dd>' + (data.isBuiltIn ? 'Built-in' : 'Custom') +
      (data.builtInCategory ? ' (' + esc(data.builtInCategory) + ')' : '') + '</dd>' +
      '<dt>Definition</dt><dd>' + esc(data.definition || '—') + '</dd>' +
      '<dt>Synonyms</dt><dd>' + esc(syns) + '</dd>' +
      '<dt>Tags</dt><dd>' + esc(tags) + '</dd>' +
      '<dt>Clauses</dt><dd>' + (data.clauseCount || 0) + '</dd>' +
      '</dl>' +
      '<h3>Properties</h3><table class="preview"><tbody>' + (props || '<tr><td colspan=2>None</td></tr>') + '</tbody></table>' +
      '<p style="margin-top:16px">' +
      (assetLink && libBase ? '<a href="' + esc(libBase) + '?highlight=' + encodeURIComponent(assetLink) + '" target="_blank">View USC asset on map</a> · ' : '') +
      '<a href="/api/deeplink?window=Continuum/Lemma+Properties&entryId=' + encodeURIComponent(id) + '" target="_blank">Open in Unity</a>' +
      '</p>' +
      '<button type="button" class="secondary" id="back-browse">← Back to browse</button>';
    document.getElementById('back-browse').onclick = () => { location.hash = 'browse'; route(); };
  }

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  async function submitCreate(ev) {
    ev.preventDefault();
    const msg = document.getElementById('create-msg');
    msg.textContent = '';
    msg.className = 'msg';
    const body = {
      word: document.getElementById('f-word').value,
      description: document.getElementById('f-desc').value,
      language: document.getElementById('f-lang').value || 'en',
      partOfSpeech: document.getElementById('f-pos').value || 'unknown',
      prefabId: document.getElementById('f-prefab').value,
      defaultProperties: document.getElementById('f-props').value,
    };
    const syns = document.getElementById('f-syns').value;
    if (syns) body.synonyms = syns.split(/[|,;]/).map(s => s.trim()).filter(Boolean);
    try {
      const data = await api('/api/thesaurus/entries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      msg.textContent = 'Created: ' + (data.entry?.term || body.word);
      msg.className = 'msg ok';
      if (data.entry?.id) setTimeout(() => { location.hash = 'entry/' + encodeURIComponent(data.entry.id); route(); }, 600);
    } catch (e) {
      msg.textContent = e.message;
      msg.className = 'msg error';
    }
  }

  async function previewProps() {
    const raw = document.getElementById('f-props').value;
    if (!raw.trim()) { document.getElementById('props-preview').textContent = ''; return; }
    try {
      const data = await api('/api/thesaurus/parse-properties', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ defaultProperties: raw }),
      });
      document.getElementById('props-preview').textContent = JSON.stringify(data.properties, null, 2);
    } catch (e) {
      document.getElementById('props-preview').textContent = e.message;
    }
  }

  async function runImport() {
    const msg = document.getElementById('import-msg');
    const file = document.getElementById('import-file').files[0];
    if (!file) { msg.textContent = 'Choose a file'; msg.className = 'msg error'; return; }
    const fd = new FormData();
    fd.append('file', file);
    fd.append('format', document.getElementById('import-format').value);
    msg.textContent = 'Importing…';
    try {
      const r = await fetch(API + '/api/thesaurus/entries/import', { method: 'POST', body: fd, credentials: 'include' });
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'Import failed');
      msg.textContent = 'Created ' + data.created + ', updated ' + data.updated + ', skipped ' + data.skipped;
      msg.className = 'msg ok';
      if (data.errors?.length) {
        msg.textContent += '. Errors: ' + data.errors.slice(0, 5).map(e => 'row ' + e.row + ': ' + e.error).join('; ');
      }
    } catch (e) {
      msg.textContent = e.message;
      msg.className = 'msg error';
    }
  }

  function initMultisort() {
    const browsePanel = document.getElementById('multisort-browse');
    const locPanel = document.getElementById('multisort-loc');
    if (browsePanel && window.ContinuumMultisort) {
      multisortBrowse = ContinuumMultisort.mount(browsePanel, {
        storageKey: 'lemma-browse',
        dimensions: BROWSE_DIMS,
        title: 'Browse sort',
        onChange: () => renderBrowseList(),
      });
    }
    if (locPanel && window.ContinuumMultisort) {
      multisortLoc = ContinuumMultisort.mount(locPanel, {
        storageKey: 'lemma-localization',
        dimensions: LOC_DIMS,
        title: 'Localization sort',
        onChange: () => renderLocList(),
      });
    }
  }

  async function route() {
    const { page, id } = getRoute();
    showView(page === 'entry' ? 'entry' : page);
    if (page === 'browse') await loadBrowse();
    else if (page === 'localization') await loadLocalization();
    else if (page === 'entry' && id) await loadEntry(id);
  }

  function init() {
    initMultisort();
    document.getElementById('form-create')?.addEventListener('submit', submitCreate);
    document.getElementById('f-props')?.addEventListener('input', debounce(previewProps, 400));
    document.getElementById('btn-import')?.addEventListener('click', runImport);
    document.getElementById('search-q')?.addEventListener('input', debounce(() => loadBrowse(), 300));
    document.getElementById('filter-lang')?.addEventListener('change', () => loadBrowse());
    document.getElementById('filter-source')?.addEventListener('change', () => loadBrowse());
    document.getElementById('loc-search-q')?.addEventListener('input', debounce(() => loadLocalization(), 300));
    document.getElementById('loc-filter-key')?.addEventListener('change', () => loadLocalization());
    document.getElementById('btn-refresh-browse')?.addEventListener('click', () => loadBrowse());
    window.addEventListener('hashchange', route);
    const params = new URLSearchParams(location.search);
    if (params.get('libraryBase')) localStorage.setItem('continuumLibraryBase', params.get('libraryBase'));
    route();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
