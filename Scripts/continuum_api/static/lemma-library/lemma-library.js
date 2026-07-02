(function (global) {
  'use strict';

  const API = '';
  let browseItems = [];
  let locRows = [];
  let createModeTabs = null;
  let multisortBrowse = null;
  let multisortLoc = null;

  async function api(path, opts) {
    const headers = Object.assign(
      { 'Content-Type': 'application/json' },
      global.ContinuumUserSession ? global.ContinuumUserSession.getHeaders() : {},
      (opts && opts.headers) || {},
    );
    const r = await fetch(API + path, { credentials: 'include', ...opts, headers });
    const data = await r.json().catch(() => ({}));
    if (!r.ok) {
      const err = new Error(data.error || r.statusText);
      err.status = r.status;
      err.code = data.code;
      err.field = data.field;
      err.existingEntryId = data.existingEntryId;
      err.detail = data.detail;
      err.body = data;
      throw err;
    }
    return data;
  }

  const CREATE_FIELD_IDS = {
    word: 'f-word',
    term: 'f-word',
    prefabId: 'f-prefab',
    partOfSpeech: 'f-pos',
    language: 'f-lang',
    defaultProperties: 'f-props',
    description: 'f-desc',
    synonyms: 'f-syns',
  };

  function clearCreateFieldErrors() {
    document.querySelectorAll('#form-create label.field-error').forEach(l => {
      l.classList.remove('field-error');
      const hint = l.querySelector('.field-hint');
      if (hint) hint.remove();
    });
  }

  function highlightCreateField(field, message) {
    const inputId = CREATE_FIELD_IDS[field];
    if (!inputId) return;
    const input = document.getElementById(inputId);
    const label = input?.closest('label');
    if (!label) return;
    label.classList.add('field-error');
    if (message) {
      let hint = label.querySelector('.field-hint');
      if (!hint) {
        hint = document.createElement('span');
        hint.className = 'field-hint';
        label.appendChild(hint);
      }
      hint.textContent = message;
    }
    input?.focus();
  }

  function getRoute() {
    const raw = location.hash.slice(1) || 'browse';
    const [path, query] = raw.split('?');
    const params = new URLSearchParams(query || '');
    if (path.startsWith('entry/')) {
      return {
        page: 'entry',
        id: decodeURIComponent(path.slice(6)),
        propertyKey: params.get('propertyKey') || '',
        from: params.get('from') || sessionStorage.getItem('lemmaReturnPage') || '',
      };
    }
    return { page: path || 'browse', id: null, propertyKey: '', from: '' };
  }

  function resolveLemmaId(row) {
    return row?.lemmaId || row?.entryId || null;
  }

  function navigateToEntry(entryId, opts) {
    if (!entryId) return;
    opts = opts || {};
    const from = opts.from || (getRoute().page === 'localization' ? 'localization' : '');
    if (from) sessionStorage.setItem('lemmaReturnPage', from);
    let hash = 'entry/' + encodeURIComponent(entryId);
    const q = new URLSearchParams();
    if (opts.propertyKey) q.set('propertyKey', opts.propertyKey);
    if (from) q.set('from', from);
    const qs = q.toString();
    if (qs) hash += '?' + qs;
    location.hash = hash;
    route();
  }

  function setActiveNav(page) {
    document.querySelectorAll('#continuum-subnav [data-nav]').forEach(a => {
      a.classList.toggle('active', a.dataset.nav === page);
    });
  }

  function showView(page) {
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    const el = document.getElementById('view-' + page);
    if (el) el.classList.add('active');
    const layout = document.getElementById('main-layout');
    if (layout) {
      layout.classList.toggle('layout-single', page === 'create' || page === 'import' || page === 'translations' || page === 'entry');
    }
    const { from } = getRoute();
    setActiveNav(page === 'entry' ? (from || 'browse') : page);
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
    { id: 'builtInCat', label: 'Built-in category', field: 'builtInCategory', visible: false, asc: true },
    { id: 'spatialDims', label: 'Spatial generator dims', field: 'spatialGenDims', visible: false, asc: true },
    { id: 'spatial2d', label: '2D spatial generator', field: 'spatialGen2d', visible: false, asc: true },
    { id: 'spatial3d', label: '3D spatial generator', field: 'spatialGen3d', visible: false, asc: true },
    { id: 'spatial4d', label: '4D spatial generator', field: 'spatialGen4d', visible: false, asc: true },
    { id: 'components', label: 'Property keys', field: 'components', visible: false, asc: true },
    { id: 'compTypes', label: 'Component types', field: 'componentTypes', visible: false, asc: true },
  ];

  const LOC_DIMS = [
    { id: 'propKey', label: 'Property key', field: 'propertyKey', visible: true, asc: true },
    { id: 'lemma', label: 'Lemma term', field: 'lemmaTerm', visible: true, asc: true },
    { id: 'spec', label: 'Spec type', field: 'specType', visible: false, asc: true },
    { id: 'spatialSpec', label: 'Spatial generator spec', field: 'spatialSpecGroup', visible: false, asc: true },
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
        const cc = item.componentCreation || {};
        (cc.componentTypes || []).slice(0, 3).forEach(c => {
          const ch = document.createElement('span');
          ch.className = 'chip chip-component';
          ch.textContent = c;
          ch.title = 'Unity component type';
          chips.appendChild(ch);
        });
        if (cc.hasBlueprint) {
          const ch = document.createElement('span');
          ch.className = 'chip chip-blueprint';
          ch.textContent = 'Blueprint';
          chips.appendChild(ch);
        } else if (cc.hasRuntimeReports) {
          const ch = document.createElement('span');
          ch.className = 'chip chip-runtime';
          ch.textContent = 'Runtime';
          chips.appendChild(ch);
        }
        (item.components || []).slice(0, 2).forEach(c => {
          const ch = document.createElement('span');
          ch.className = 'chip chip-prop';
          ch.textContent = c;
          ch.title = 'Property key';
          chips.appendChild(ch);
        });
        (item.spatialGeneratorDefinitions || []).slice(0, 3).forEach(function (sg) {
          const ch = document.createElement('span');
          ch.className = 'chip chip-spatial';
          ch.textContent = (sg.dimension || '?').toUpperCase() + ': ' + (sg.label || sg.id || 'gen');
          ch.title = 'Spatial generator definition';
          chips.appendChild(ch);
        });
        if (item.clauseCount > 0) {
          const ch = document.createElement('span');
          ch.className = 'chip';
          ch.textContent = item.clauseCount + ' clauses';
          chips.appendChild(ch);
        }
        row.onclick = () => navigateToEntry(item.id, { from: 'browse' });
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
        chips[0].textContent = row.propertyKey || row.kind || row.bindingKind || '';
        chips[1].textContent = row.propertyValue || '';
        const entryId = resolveLemmaId(row);
        if (entryId) {
          el.classList.add('entry-row--linkable');
          el.title = 'Open lemma entry';
          el.onclick = () => navigateToEntry(entryId, {
            from: 'localization',
            propertyKey: row.propertyKey || '',
          });
        } else if (row.draftEpisodeId) {
          el.classList.add('entry-row--muted');
          el.title = `Draft ${row.draftEpisodeId} [${row.charStart}, ${row.charEnd}) — no linked lemma`;
        } else {
          el.classList.add('entry-row--muted');
          el.title = 'No linked lemma entry';
        }
        container.appendChild(el);
      });
    });
  }

  async function loadBrowse() {
    const q = document.getElementById('search-q')?.value || '';
    const language = document.getElementById('filter-lang')?.value || '';
    const source = document.getElementById('filter-source')?.value || 'all';
    const componentType = document.getElementById('filter-component-type')?.value?.trim() || '';
    const bucketId = document.getElementById('filter-bucket-id')?.value?.trim() || '';
    const hasMetadata = document.getElementById('filter-has-metadata')?.checked;
    const spatialDim = document.getElementById('filter-spatial-dim')?.value || '';
    const params = new URLSearchParams({ limit: '2000' });
    if (q) params.set('q', q);
    if (language) params.set('language', language);
    if (source) params.set('source', source);
    if (componentType) params.set('componentType', componentType);
    if (bucketId) params.set('bucketId', bucketId);
    if (hasMetadata) params.set('hasComponentMetadata', 'true');
    if (spatialDim) params.set('spatialDimension', spatialDim);
    const data = await api('/api/thesaurus/entries?' + params);
    browseItems = data.items || [];
    renderBrowseList();
  }

  async function loadLocalization() {
    const q = document.getElementById('loc-search-q')?.value || '';
    const propertyKey = document.getElementById('loc-filter-key')?.value || '';
    const draftId = document.getElementById('loc-filter-draft')?.value?.trim() || '';
    if (draftId) {
      const params = new URLSearchParams({ draftEpisodeId: draftId });
      if (propertyKey) params.set('bindingKind', 'localization');
      const data = await api('/api/thesaurus/clause-bindings?' + params);
      locRows = (data.items || []).map(b => ({
        lemmaTerm: b.selectionText,
        selectionText: b.selectionText,
        propertyKey: b.propertyKey || b.bindingKind,
        propertyValue: b.propertyValue,
        lemmaId: b.entryId,
        entryId: b.entryId,
        kind: b.bindingKind,
        bindingKind: b.bindingKind,
        draftEpisodeId: draftId,
        charStart: b.charStart,
        charEnd: b.charEnd,
      }));
      if (q) {
        const ql = q.toLowerCase();
        locRows = locRows.filter(r =>
          (r.selectionText || '').toLowerCase().includes(ql) ||
          (r.propertyKey || '').toLowerCase().includes(ql) ||
          (r.propertyValue || '').toLowerCase().includes(ql));
      }
      if (propertyKey) {
        locRows = locRows.filter(r => (r.propertyKey || '').includes(propertyKey));
      }
      renderLocList();
      return;
    }
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (propertyKey) params.set('propertyKey', propertyKey);
    const data = await api('/api/thesaurus/localization-view?' + params);
    locRows = (data.rows || []).map(function (row) {
      var pk = row.propertyKey || '';
      var spatialSpecGroup = '(other)';
      if (pk.indexOf('spatial-gen-') === 0 || pk === 'spatial-generator-definitions') {
        if (pk.indexOf('2d') >= 0 || pk === 'spatial-generator-definitions') spatialSpecGroup = '2D spatial generator';
        else if (pk.indexOf('3d') >= 0) spatialSpecGroup = '3D spatial generator';
        else if (pk.indexOf('4d') >= 0 || pk.indexOf('spatial-4d') >= 0 || pk.indexOf('spatial-t-') >= 0) {
          spatialSpecGroup = '4D spatial generator';
        } else spatialSpecGroup = 'Spatial generator';
      }
      return Object.assign({}, row, { spatialSpecGroup: spatialSpecGroup });
    });
    renderLocList();
  }

  async function loadEntry(id) {
    const routeInfo = getRoute();
    const highlightKey = routeInfo.propertyKey || '';
    const el = document.getElementById('entry-detail');
    if (!el) return;
    el.innerHTML = '<p class="muted">Loading lemma…</p>';
    let data;
    let meta = null;
    try {
      [data, meta] = await Promise.all([
        api('/api/thesaurus/entries?entryId=' + encodeURIComponent(id)),
        api('/api/thesaurus/entries/' + encodeURIComponent(id) + '/component-metadata').catch(() => null),
      ]);
    } catch (err) {
      el.innerHTML =
        '<h2>Lemma not found</h2>' +
        '<p class="msg error">' + esc(err.message || 'Could not load entry') + '</p>' +
        '<p><code>' + esc(id) + '</code></p>' +
        '<button type="button" class="secondary" id="back-browse">← Back to localization</button>';
      document.getElementById('back-browse').onclick = () => {
        location.hash = routeInfo.from || 'localization';
        route();
      };
      return;
    }
    const props = Object.entries(data.properties || {})
      .map(([k, v]) => {
        const hl = highlightKey && k === highlightKey ? ' class="highlight-prop"' : '';
        return `<tr${hl}><td>${esc(k)}</td><td>${esc(v)}</td></tr>`;
      })
      .join('');
    const syns = (data.synonyms || []).join(', ') || '—';
    const tags = (data.tags || []).join(', ') || '—';
    const libBase = window.ContinuumNav
      ? ContinuumNav.normalizeLibraryBase(localStorage.getItem('continuumLibraryBase') || '', location.origin)
      : (localStorage.getItem('continuumLibraryBase') || location.origin + '/library');
    const assetLink = (data.linkedAssetIds || [])[0];
    const cc = data.componentCreation || {};
    const compTypes = (cc.componentTypes || []).join(', ') || '—';
    let componentSection = '<h3>Component creation</h3>';
    if (!cc || (!cc.hasBlueprint && !cc.hasRuntimeReports)) {
      componentSection += '<p class="muted">No prefab blueprint or runtime reports yet. Scan in Unity (Lemma Properties → Scan prefab components).</p>';
    } else {
      componentSection += `<p><span class="chip chip-component">${cc.hasBlueprint ? 'Blueprint' : ''}</span> ` +
        `<span class="chip chip-runtime">${cc.hasRuntimeReports ? 'Runtime reports' : ''}</span></p>` +
        `<p><strong>Component types:</strong> ${esc(compTypes)}</p>`;
      if ((cc.bucketIds || []).length) {
        componentSection += '<p><strong>Bucket ids:</strong> ' +
          cc.bucketIds.map(b => {
            const href = libBase ? `${esc(libBase)}?highlight=${encodeURIComponent(b)}&view=spatial` : '#';
            return libBase ? `<a href="${href}" target="_blank">${esc(b)}</a>` : esc(b);
          }).join(', ') + '</p>';
      }
      if (meta?.blueprint?.payload?.nodes?.length) {
        componentSection += '<details open><summary>Farey object tree (blueprint)</summary><ul class="comp-tree">';
        meta.blueprint.payload.nodes.forEach(n => {
          const types = (n.components || []).map(c => c.typeName || c.type_name).filter(Boolean).join(', ');
          const f = n.farey || {};
          componentSection += `<li><code>${esc(n.path || n.gameObjectName || '')}</code> ` +
            (types ? `<small>${esc(types)}</small> ` : '') +
            (f.ln != null ? `<small>[${f.ln}/${f.ld}–${f.rn}/${f.rd}]</small>` : '') +
            '</li>';
        });
        componentSection += '</ul></details>';
      }
      if (meta?.reports?.length) {
        componentSection += '<details><summary>Recent runtime reports</summary><table class="preview"><thead><tr><th>Run</th><th>Captured</th><th>Buckets</th></tr></thead><tbody>';
        meta.reports.slice(0, 10).forEach(r => {
          const buckets = (r.payload?.spatialBuckets || []).map(b => b.bucketId || b.bucket_id).filter(Boolean).join(', ');
          componentSection += `<tr><td>${esc(r.runId || r.id)}</td><td>${esc(r.capturedAt || '')}</td><td>${esc(buckets || '—')}</td></tr>`;
        });
        componentSection += '</tbody></table></details>';
      }
    }
    const backTarget = routeInfo.from || 'browse';
    const backLabel = backTarget === 'localization' ? '← Back to localization' : '← Back to browse';
    const spatialDefs = data.spatialGeneratorDefinitions || [];
    let spatialSection = '<h3>Spatial generator definitions</h3>';
    if (!spatialDefs.length) {
      spatialSection += '<p class="muted">None — add via Properties as JSON key <code>spatial-generator-definitions</code> or per-dimension <code>spatial-gen-2d-label</code> keys.</p>';
    } else {
      spatialSection += '<table class="preview"><thead><tr><th>Dim</th><th>Label</th><th>Spec</th></tr></thead><tbody>';
      spatialDefs.forEach(function (sg) {
        var specBits = [];
        if (sg.gridResX != null) specBits.push('grid ' + sg.gridResX + (sg.gridResY ? '×' + sg.gridResY : '') + (sg.gridResZ ? '×' + sg.gridResZ : ''));
        if (sg.sliceCount != null) specBits.push('slices ' + sg.sliceCount);
        if (sg.spatial4dId) specBits.push('vol ' + sg.spatial4dId);
        if (sg.tMin != null || sg.tMax != null) specBits.push('t ' + (sg.tMin != null ? sg.tMin : '?') + '–' + (sg.tMax != null ? sg.tMax : '?'));
        spatialSection += '<tr><td>' + esc((sg.dimension || '').toUpperCase()) + '</td><td>' + esc(sg.label || sg.id || '—') + '</td><td>' + esc(specBits.join(' · ') || '—') + '</td></tr>';
      });
      spatialSection += '</tbody></table>';
    }
    el.innerHTML =
      '<h2>' + esc(data.term) + '</h2>' +
      (highlightKey ? '<p class="muted">Opened from localization · property <code>' + esc(highlightKey) + '</code></p>' : '') +
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
      '<dt>Composed</dt><dd>' + (data.isComposedLemma ? 'Yes' : 'No') + '</dd>' +
      '<dt>Prompt</dt><dd><code style="font-size:12px">' + esc((data.lemmaPrompt || '').slice(0, 120) || '—') + '</code></dd>' +
      '<dt>Timing</dt><dd>' + (data.defaultTiming
        ? esc(data.defaultTiming.tMin + 's – ' + data.defaultTiming.tMax + 's')
        : '—') + '</dd>' +
      '</dl>' +
      '<h3>Composed lemmas</h3>' +
      '<p id="entry-composed-lemmas"></p>' +
      '<button type="button" id="edit-lemma-entry">Edit lemma</button> ' +
      '<button type="button" id="edit-composition">Composition</button> ' +
      spatialSection +
      '<h3>Properties</h3><table class="preview"><tbody>' + (props || '<tr><td colspan=2>None</td></tr>') + '</tbody></table>' +
      componentSection +
      '<p style="margin-top:16px">' +
      (assetLink && libBase ? '<a href="' + esc(libBase) + '?highlight=' + encodeURIComponent(assetLink) + '&view=spatial" target="_blank">View USC asset on map</a> · ' : '') +
      '<a href="/api/deeplink?window=Continuum/Lemma+Properties&entryId=' + encodeURIComponent(id) + '" target="_blank">Open in Unity</a>' +
      '</p>' +
      '<button type="button" class="secondary" id="back-browse">' + backLabel + '</button>';
    const composedHost = document.getElementById('entry-composed-lemmas');
    if (composedHost && global.ContinuumLemmaEntry) {
      global.ContinuumLemmaEntry.renderInline(composedHost, data.compositionChildren || [], {
        emptyLabel: 'None',
      });
      composedHost.querySelector('.continuum-lemma-entry-empty')?.classList.add('muted');
    } else if (composedHost) {
      composedHost.innerHTML = (data.compositionChildren || []).length
        ? (data.compositionChildren || []).map((c) => `<span class="chip">${esc(c.term || c.entryId)}</span>`).join(' ')
        : '<span class="muted">None</span>';
    }
    document.getElementById('back-browse').onclick = () => {
      sessionStorage.removeItem('lemmaReturnPage');
      location.hash = backTarget;
      route();
    };
    document.getElementById('edit-lemma-entry')?.addEventListener('click', () => {
      if (global.ContinuumClauseSelector) {
        global.ContinuumClauseSelector.openLemmaEntryDialog({
          entryId: id,
          callApi: (method, path, body) => api(path, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: body != null ? JSON.stringify(body) : undefined,
          }),
          onSaved: () => loadEntry(id),
        });
        return;
      }
      window.open('/lemma-library#entry/' + encodeURIComponent(id), '_blank');
    });
    document.getElementById('edit-composition')?.addEventListener('click', () => {
      const editor = global.ContinuumLemmaPromptEditor || global.ContinuumLemmaCompositionEditor;
      if (!editor) return;
      editor.openModal({
        entryId: id,
        parentEntryId: id,
        seedPhrase: (data.compositionChildren || []).length ? undefined : (data.term || ''),
        initialChildren: data.compositionChildren || [],
        callApi: (method, path, body) => api(path, { method, headers: { 'Content-Type': 'application/json' }, body: body != null ? JSON.stringify(body) : undefined }),
        onSaved: () => loadEntry(id),
      });
    });
  }

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  async function loadPosTags(selected) {
    const sel = document.getElementById('f-pos');
    if (!sel) return;
    try {
      const data = await api('/api/thesaurus/pos-tags');
      const items = data.items || [];
      sel.innerHTML = items.map(row => {
        const seg = row.segment ? ` · ${row.segment}` : '';
        const cat = row.category ? ` (${row.category})` : '';
        const label = `${row.label || row.posTag}${seg}${cat}`;
        return `<option value="${esc(row.posTag)}">${esc(label)}</option>`;
      }).join('');
    } catch (_) {
      sel.innerHTML = '<option value="noun">Noun · noun (Subject)</option><option value="unknown">Unknown · unknown</option>';
    }
    const want = (selected || 'noun').trim().toLowerCase();
    const opt = Array.from(sel.options).find(o => (o.value || '').toLowerCase() === want);
    sel.value = opt ? opt.value : 'noun';
  }

  async function submitCreate(ev) {
    ev.preventDefault();
    const msg = document.getElementById('create-msg');
    msg.textContent = '';
    msg.className = 'msg';
    clearCreateFieldErrors();
    const body = {
      word: document.getElementById('f-word').value,
      description: document.getElementById('f-desc').value,
      language: document.getElementById('f-lang').value || 'en',
      partOfSpeech: (document.getElementById('f-pos').value || 'unknown').trim().toLowerCase(),
      prefabId: document.getElementById('f-prefab').value,
      defaultProperties: document.getElementById('f-props').value,
    };
    const mode = createModeTabs?.getMode?.() || 'prefab';
    if (mode === 'composition') {
      body.prefabId = '';
      const children = createModeTabs?.getCompositionChildren?.() || [];
      if (children.length) {
        body.composition = children.map((c, i) => ({ entryId: c.entryId, sortOrder: i }));
      }
    }
    const syns = document.getElementById('f-syns').value;
    if (syns) body.synonyms = syns.split(/[|,;]/).map(s => s.trim()).filter(Boolean);
    try {
      const data = await api('/api/thesaurus/entries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (data.entry?.id && mode === 'composition' && createModeTabs) {
        const pending = createModeTabs.getCompositionChildren?.() || [];
        if (pending.length && !body.composition) {
          await api('/api/thesaurus/entries/' + encodeURIComponent(data.entry.id) + '/composition', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              children: pending.map((c, i) => ({ entryId: c.entryId, sortOrder: i })),
            }),
          });
        }
      }
      const label = data.status === 'updated' ? 'Updated' : 'Created';
      msg.textContent = data.message || (label + ': ' + (data.entry?.term || body.word));
      msg.className = 'msg ok';
      if (data.entry?.id) {
        setTimeout(() => { location.hash = 'entry/' + encodeURIComponent(data.entry.id); route(); }, 600);
      }
    } catch (e) {
      if (e.field) highlightCreateField(e.field, e.message);
      msg.textContent = e.message;
      if (e.existingEntryId) {
        const link = document.createElement('a');
        link.href = '#entry/' + encodeURIComponent(e.existingEntryId);
        link.textContent = ' View existing entry';
        link.style.marginLeft = '8px';
        link.style.color = '#b0c8ff';
        link.addEventListener('click', ev => {
          ev.preventDefault();
          location.hash = 'entry/' + encodeURIComponent(e.existingEntryId);
          route();
        });
        msg.appendChild(link);
      }
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

  async function loadTranslationsView() {
    const srcSel = document.getElementById('xliff-source-lang');
    const tgtSel = document.getElementById('xliff-target-lang');
    if (!srcSel || !tgtSel) return;
    try {
      const data = await api('/api/thesaurus/languages');
      const items = data.items || [];
      const opts = items.map(l => `<option value="${esc(l.code)}">${esc(l.code)}</option>`).join('');
      srcSel.innerHTML = opts;
      tgtSel.innerHTML = opts;
      srcSel.value = 'en';
      if (items.some(l => l.code === 'fr')) tgtSel.value = 'fr';
      else if (items.length > 1) tgtSel.selectedIndex = 1;
    } catch (e) {
      srcSel.innerHTML = '<option value="en">en</option>';
      tgtSel.innerHTML = '<option value="fr">fr</option><option value="es">es</option>';
    }
  }

  async function exportXliff() {
    const msg = document.getElementById('xliff-msg');
    const src = document.getElementById('xliff-source-lang')?.value || 'en';
    const tgt = document.getElementById('xliff-target-lang')?.value || '';
    if (!tgt) { msg.textContent = 'Select a target language'; msg.className = 'msg error'; return; }
    msg.textContent = 'Exporting…';
    msg.className = 'msg';
    try {
      const r = await fetch(API + '/api/thesaurus/export-xliff?sourceLang=' + encodeURIComponent(src) + '&targetLang=' + encodeURIComponent(tgt), { credentials: 'include' });
      if (!r.ok) {
        const err = await r.json().catch(() => ({}));
        throw new Error(err.error || r.statusText);
      }
      const blob = await r.blob();
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = 'thesaurus-' + tgt + '.xliff';
      a.click();
      URL.revokeObjectURL(a.href);
      msg.textContent = 'Downloaded thesaurus-' + tgt + '.xliff';
      msg.className = 'msg ok';
    } catch (e) {
      msg.textContent = e.message;
      msg.className = 'msg error';
    }
  }

  async function importXliff() {
    const msg = document.getElementById('xliff-msg');
    const file = document.getElementById('xliff-import-file')?.files[0];
    if (!file) { msg.textContent = 'Choose an XLIFF file'; msg.className = 'msg error'; return; }
    const fd = new FormData();
    fd.append('file', file);
    msg.textContent = 'Importing…';
    msg.className = 'msg';
    try {
      const r = await fetch(API + '/api/thesaurus/import-xliff', { method: 'POST', body: fd, credentials: 'include' });
      const data = await r.json().catch(() => ({}));
      if (!r.ok) throw new Error(data.error || 'Import failed');
      msg.textContent = 'Updated ' + (data.updated ?? 0) + ', inserted ' + (data.inserted ?? 0);
      msg.className = 'msg ok';
    } catch (e) {
      msg.textContent = e.message;
      msg.className = 'msg error';
    }
  }

  async function runLanguageAudit() {
    const out = document.getElementById('xliff-audit-out');
    if (!out) return;
    out.textContent = 'Loading audit…';
    try {
      const data = await api('/api/thesaurus/language-audit');
      out.textContent = JSON.stringify(data, null, 2);
    } catch (e) {
      out.textContent = e.message;
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
    else if (page === 'translations') await loadTranslationsView();
    else if (page === 'entry' && id) await loadEntry(id);
  }

  function init() {
    initMultisort();
    loadPosTags('noun');
    if (global.ContinuumLemmaCompositionEditor) {
      createModeTabs = global.ContinuumLemmaCompositionEditor.mountCreateTabs(
        document.getElementById('create-mode-tabs'),
        {
          callApi: (method, path, body) => api(path, { method, headers: { 'Content-Type': 'application/json' }, body: body != null ? JSON.stringify(body) : undefined }),
          prefabPanel: document.getElementById('create-prefab-panel'),
          compositionHost: document.getElementById('create-composition-panel'),
        },
      );
    }
    document.getElementById('form-create')?.addEventListener('submit', submitCreate);
    document.getElementById('f-props')?.addEventListener('input', debounce(previewProps, 400));
    document.getElementById('btn-import')?.addEventListener('click', runImport);
    document.getElementById('btn-xliff-export')?.addEventListener('click', exportXliff);
    document.getElementById('btn-xliff-import')?.addEventListener('click', importXliff);
    document.getElementById('btn-xliff-audit')?.addEventListener('click', runLanguageAudit);
    document.getElementById('search-q')?.addEventListener('input', debounce(() => loadBrowse(), 300));
    document.getElementById('filter-lang')?.addEventListener('change', () => loadBrowse());
    document.getElementById('filter-source')?.addEventListener('change', () => loadBrowse());
    document.getElementById('filter-spatial-dim')?.addEventListener('change', () => loadBrowse());
    document.getElementById('loc-search-q')?.addEventListener('input', debounce(() => loadLocalization(), 300));
    document.getElementById('loc-filter-key')?.addEventListener('change', () => loadLocalization());
    document.getElementById('btn-refresh-browse')?.addEventListener('click', () => loadBrowse());
    window.addEventListener('hashchange', route);
    const params = new URLSearchParams(location.search);
    if (params.get('libraryBase')) {
      const normalized = window.ContinuumNav
        ? ContinuumNav.normalizeLibraryBase(params.get('libraryBase'), location.origin)
        : params.get('libraryBase').replace(/\/$/, '');
      localStorage.setItem('continuumLibraryBase', normalized);
    }
    const hashQuery = (location.hash.slice(1).split('?')[1] || '');
    const hashParams = new URLSearchParams(hashQuery);
    const prefillQ = params.get('q') || hashParams.get('q');
    if (prefillQ && document.getElementById('search-q')) {
      document.getElementById('search-q').value = prefillQ;
    }
    route();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})(typeof window !== 'undefined' ? window : globalThis);
