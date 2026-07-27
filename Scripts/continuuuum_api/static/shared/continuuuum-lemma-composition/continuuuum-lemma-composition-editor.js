/* Composed lemma editor — inline mount and modal (shared across lemma library, script output) */
(function (global) {
  const Picker = global.ContinuuuumLemmaPicker;

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

  function buildSavePayload(children) {
    return {
      children: children.map((c, i) => ({
        entryId: c.entryId,
        sortOrder: i,
      })),
    };
  }

  /** Build sole-child subprompt: phrase → child lemma + {P:term} prompt. */
  async function prepareSubpromptComposition(callApi, parentEntryId, phrase, opts) {
    opts = opts || {};
    if (!Picker || !Picker.resolveOrCreateLemmaEntry) {
      throw new Error('Lemma picker unavailable');
    }
    // Do not exclude parent during lookup: Composition often seeds from the same
    // selection that is already bound to parentEntryId. Excluding caused POST 409
    // and left no recoverable entry. Self-composition is skipped below instead.
    const child = await Picker.resolveOrCreateLemmaEntry(callApi, phrase, [], opts);
    if (parentEntryId && child && child.id === parentEntryId) {
      return { lemmaPrompt: '', compositionChildren: [], skippedSelf: true };
    }
    const term = (child.term || phrase || '').trim();
    return {
      lemmaPrompt: `{P:${term}}`,
      compositionChildren: [{ entryId: child.id, term: child.term || term, sortOrder: 0 }],
    };
  }

  function mountCreateLemmaForm(host, callApi, opts) {
    opts = opts || {};
    host.innerHTML =
      '<div class="clp-create-form" style="margin:8px 0;padding:8px;border:1px solid #ddd;border-radius:4px">' +
      '<p style="margin:0 0 8px;font-size:13px"><strong>Create lemma</strong></p>' +
      '<label style="display:block;font-size:13px">Word / phrase' +
      '<input type="text" class="clp-create-word" style="width:100%;box-sizing:border-box;margin-top:4px"/></label>' +
      '<label style="display:block;font-size:13px;margin-top:6px">Part of speech' +
      '<select class="clp-create-pos" style="width:100%;margin-top:4px">' +
      '<option value="noun">noun</option><option value="verb">verb</option><option value="unknown">unknown</option>' +
      '</select></label>' +
      '<div style="margin-top:8px;display:flex;gap:8px;align-items:center">' +
      '<button type="button" class="clp-create-submit">Create &amp; add</button>' +
      '<button type="button" class="clp-create-cancel secondary">Cancel</button>' +
      '<span class="clp-create-msg" style="font-size:12px;color:#c62828"></span>' +
      '</div></div>';
    const wordInp = host.querySelector('.clp-create-word');
    if (wordInp && opts.initialWord) wordInp.value = opts.initialWord;
    host.querySelector('.clp-create-cancel').onclick = () => {
      host.innerHTML = '';
      host.style.display = 'none';
      if (opts.onCancel) opts.onCancel();
    };
    host.querySelector('.clp-create-submit').onclick = async () => {
      const msg = host.querySelector('.clp-create-msg');
      msg.textContent = '';
      try {
        const entry = await Picker.resolveOrCreateLemmaEntry(
          callApi,
          wordInp.value.trim(),
          opts.excludeIds || [],
          { partOfSpeech: host.querySelector('.clp-create-pos').value },
        );
        host.innerHTML = '';
        host.style.display = 'none';
        if (opts.onCreated) opts.onCreated(entry);
      } catch (e) {
        msg.textContent = e.message || 'Create failed';
      }
    };
    host.style.display = 'block';
  }

  function renderChildList(listEl, children, handlers) {
    listEl.innerHTML = '';
    if (!children.length) {
      listEl.innerHTML = '<li style="color:#666;font-size:13px">No child lemmas yet.</li>';
      return;
    }
    children.forEach((c, idx) => {
      const li = document.createElement('li');
      li.draggable = true;
      li.dataset.idx = String(idx);
      const labelWrap = document.createElement('span');
      labelWrap.style.flex = '1';
      if (global.ContinuuuumLemmaEntry) {
        labelWrap.appendChild(global.ContinuuuumLemmaEntry.createLink(c, { showId: true }));
      } else {
        labelWrap.innerHTML =
          `<strong>${escHtml(c.term || c.entryId)}</strong> ` +
          `<code style="font-size:11px">${escHtml(c.entryId)}</code>`;
      }
      li.innerHTML = `<span class="comp-handle" title="Drag to reorder">☰</span>`;
      li.appendChild(labelWrap);
      const removeBtn = document.createElement('button');
      removeBtn.type = 'button';
      removeBtn.className = 'comp-remove';
      removeBtn.dataset.idx = String(idx);
      removeBtn.textContent = 'Remove';
      li.appendChild(removeBtn);
      removeBtn.onclick = () => handlers.onRemove(idx);
      li.addEventListener('dragstart', (ev) => {
        ev.dataTransfer.setData('text/plain', String(idx));
        li.classList.add('dragging');
      });
      li.addEventListener('dragend', () => li.classList.remove('dragging'));
      li.addEventListener('dragover', (ev) => { ev.preventDefault(); });
      li.addEventListener('drop', (ev) => {
        ev.preventDefault();
        const from = parseInt(ev.dataTransfer.getData('text/plain'), 10);
        const to = idx;
        if (!Number.isNaN(from) && from !== to) handlers.onReorder(from, to);
      });
      listEl.appendChild(li);
    });
  }

  function mountEditorShell(container, opts) {
    const callApi = opts.callApi || defaultCallApi;
    let children = (opts.initialChildren || []).slice();

    container.innerHTML =
      '<div class="continuuuum-comp-editor">' +
      '<div class="clp-add-host"></div>' +
      '<div class="clp-add-row" style="margin:8px 0;display:flex;flex-wrap:wrap;align-items:center;gap:8px">' +
      '<button type="button" class="clp-add-btn">Add lemma</button>' +
      '<a href="#" class="clp-create-link" style="font-size:13px">Create lemma if not found</a>' +
      '</div>' +
      '<div class="clp-create-host" style="display:none"></div>' +
      '<ul class="continuuuum-comp-list"></ul>' +
      '<div class="continuuuum-comp-actions">' +
      '<button type="button" class="comp-save">Save composition</button>' +
      '<button type="button" class="comp-recomb secondary">Recombobulate spatial graph</button>' +
      '</div>' +
      '<div class="comp-msg" style="margin-top:8px;font-size:13px"></div>' +
      '</div>';

    const listEl = container.querySelector('.continuuuum-comp-list');
    const msgEl = container.querySelector('.comp-msg');
    const addHost = container.querySelector('.clp-add-host');
    const createHost = container.querySelector('.clp-create-host');
    const addBtn = container.querySelector('.clp-add-btn');
    const createLink = container.querySelector('.clp-create-link');
    let pickerVisible = false;
    let lastSearchQuery = '';
    let pickerApi = null;

    function excludeIds() {
      const ids = new Set(children.map((c) => c.entryId));
      if (opts.parentEntryId) ids.add(opts.parentEntryId);
      return Array.from(ids);
    }

    function refresh() {
      renderChildList(listEl, children, {
        onRemove: (idx) => { children.splice(idx, 1); refresh(); },
        onReorder: (from, to) => {
          const [item] = children.splice(from, 1);
          children.splice(to, 0, item);
          refresh();
        },
      });
    }

    function addChildEntry(entry) {
      if (!entry || !entry.id) return;
      if (children.some((c) => c.entryId === entry.id)) {
        msgEl.textContent = 'Lemma already in composition.';
        msgEl.style.color = '#c62828';
        return;
      }
      children.push({ entryId: entry.id, term: entry.term, sortOrder: children.length });
      msgEl.textContent = '';
      pickerVisible = false;
      addHost.style.display = 'none';
      addHost.innerHTML = '';
      pickerApi = null;
      createHost.style.display = 'none';
      createHost.innerHTML = '';
      refresh();
    }

    addBtn.onclick = async () => {
      if (pickerVisible && pickerApi) {
        const ok = await pickerApi.confirm();
        if (!ok) {
          msgEl.textContent = 'Pick a result, then press Enter or Add lemma again.';
          msgEl.style.color = '#666';
        }
        return;
      }
      pickerVisible = true;
      addHost.style.display = 'block';
      createHost.style.display = 'none';
      createHost.innerHTML = '';
      msgEl.textContent = '';
      if (Picker) {
        addHost.innerHTML = '';
        pickerApi = Picker.mountSearch(addHost, {
          callApi,
          excludeIds: excludeIds(),
          onSelect: addChildEntry,
        });
        const searchInp = addHost.querySelector('.clp-search-input');
        if (searchInp) {
          searchInp.addEventListener('input', () => { lastSearchQuery = searchInp.value; });
          searchInp.focus();
        }
      }
    };

    if (createLink) {
      createLink.onclick = (ev) => {
        ev.preventDefault();
        pickerVisible = false;
        pickerApi = null;
        addHost.style.display = 'none';
        addHost.innerHTML = '';
        mountCreateLemmaForm(createHost, callApi, {
          initialWord: lastSearchQuery,
          excludeIds: excludeIds(),
          onCreated: addChildEntry,
          onCancel: () => { createHost.style.display = 'none'; },
        });
      };
    }

    container.querySelector('.comp-save').onclick = async () => {
      if (!opts.parentEntryId) {
        msgEl.textContent = 'Save parent entry first, then save composition.';
        msgEl.style.color = '#c62828';
        return;
      }
      try {
        const data = await callApi(
          'PUT',
          `/api/thesaurus/entries/${encodeURIComponent(opts.parentEntryId)}/composition`,
          buildSavePayload(children),
        );
        children = (data.children || []).map((c) => ({
          entryId: c.entryId,
          term: c.term,
          sortOrder: c.sortOrder,
        }));
        refresh();
        msgEl.textContent = 'Composition saved.';
        msgEl.style.color = '#2e7d32';
        if (opts.onSaved) opts.onSaved(data);
      } catch (e) {
        msgEl.textContent = e.message || 'Save failed';
        msgEl.style.color = '#c62828';
      }
    };

    container.querySelector('.comp-recomb').onclick = () => {
      ContinuuuumLemmaCompositionEditor.openRecombobulateModal({
        parentEntryId: opts.parentEntryId,
        draftEpisodeId: opts.draftEpisodeId,
        scriptText: opts.scriptText || '',
        callApi,
        onDone: async () => {
          if (!opts.parentEntryId) return;
          try {
            const data = await callApi(
              'GET',
              `/api/thesaurus/entries/${encodeURIComponent(opts.parentEntryId)}/composition`,
            );
            children = (data.children || []).map((c) => ({
              entryId: c.entryId,
              term: c.term,
              sortOrder: c.sortOrder,
            }));
            refresh();
            if (opts.onSaved) opts.onSaved(data);
          } catch (_) { /* ignore */ }
        },
      });
    };

    refresh();
    return {
      getChildren: () => children.slice(),
      setChildren: (next) => { children = (next || []).slice(); refresh(); },
      setParentEntryId: (id) => { opts.parentEntryId = id; },
    };
  }

  function openRecombobulateModal(opts) {
    const callApi = opts.callApi || defaultCallApi;
    const overlay = document.createElement('div');
    overlay.className = 'continuuuum-comp-overlay';
    overlay.innerHTML =
      '<div class="continuuuum-comp-modal">' +
      '<h3>Recombobulate spatial graph</h3>' +
      '<p style="font-size:13px;color:#555">Audit script alignment and spatial containment. Check required items before applying fixes.</p>' +
      '<div class="recomb-issues"></div>' +
      '<div class="continuuuum-comp-actions">' +
      '<button type="button" class="recomb-apply" disabled>Apply acknowledged fixes</button>' +
      '<button type="button" class="recomb-close secondary">Close</button>' +
      '</div>' +
      '<div class="recomb-msg" style="margin-top:8px;font-size:13px"></div>' +
      '</div>';
    document.body.appendChild(overlay);

    const issuesEl = overlay.querySelector('.recomb-issues');
    const applyBtn = overlay.querySelector('.recomb-apply');
    const msgEl = overlay.querySelector('.recomb-msg');
    let issues = [];
    const checked = new Set();

    function updateApplyState() {
      const required = issues.filter((i) => i.requiresAck);
      const allChecked = required.every((i) => checked.has(i.id));
      applyBtn.disabled = required.length > 0 && !allChecked;
    }

    function renderIssues() {
      if (!issues.length) {
        issuesEl.innerHTML = '<p style="color:#2e7d32">No issues found.</p>';
        applyBtn.disabled = true;
        return;
      }
      issuesEl.innerHTML = issues.map((issue) => {
        const ack = issue.requiresAck
          ? `<label><input type="checkbox" class="recomb-ack" data-id="${escHtml(issue.id)}"/> Acknowledge fix</label>`
          : '';
        const diff = (issue.storedText != null || issue.currentText != null)
          ? `<div class="continuuuum-recomb-diff">Stored: "${escHtml(issue.storedText || '')}"<br>Current: "${escHtml(issue.currentText || '')}"</div>`
          : '';
        return `<div class="continuuuum-recomb-issue${issue.requiresAck ? ' requires-ack' : ''}">` +
          `<strong>${escHtml(issue.code)}</strong> — ${escHtml(issue.message)}` +
          diff + ack + '</div>';
      }).join('');
      issuesEl.querySelectorAll('.recomb-ack').forEach((cb) => {
        cb.onchange = () => {
          if (cb.checked) checked.add(cb.dataset.id);
          else checked.delete(cb.dataset.id);
          updateApplyState();
        };
      });
      updateApplyState();
    }

    async function loadAudit() {
      msgEl.textContent = 'Auditing…';
      try {
        const data = await callApi(
          'POST',
          `/api/thesaurus/entries/${encodeURIComponent(opts.parentEntryId)}/recombobulate-spatial`,
          {
            scriptText: opts.scriptText || '',
            draftEpisodeId: opts.draftEpisodeId || undefined,
          },
        );
        issues = data.issues || [];
        checked.clear();
        renderIssues();
        msgEl.textContent = '';
      } catch (e) {
        msgEl.textContent = e.message || 'Audit failed';
        msgEl.style.color = '#c62828';
      }
    }

    applyBtn.onclick = async () => {
      try {
        const data = await callApi(
          'POST',
          `/api/thesaurus/entries/${encodeURIComponent(opts.parentEntryId)}/recombobulate-spatial`,
          {
            scriptText: opts.scriptText || '',
            draftEpisodeId: opts.draftEpisodeId || undefined,
            apply: true,
            acknowledgedIssueIds: Array.from(checked),
          },
        );
        issues = data.issues || [];
        checked.clear();
        renderIssues();
        msgEl.textContent = `Applied ${(data.appliedIssueIds || []).length} fix(es).`;
        msgEl.style.color = '#2e7d32';
        if (opts.onDone) opts.onDone(data);
      } catch (e) {
        msgEl.textContent = e.message || 'Apply failed';
        msgEl.style.color = '#c62828';
      }
    };

    overlay.querySelector('.recomb-close').onclick = () => overlay.remove();
    overlay.addEventListener('click', (ev) => { if (ev.target === overlay) overlay.remove(); });

    loadAudit();
  }

  function openModal(opts) {
    if (global.ContinuuuumLemmaPromptEditor && global.ContinuuuumLemmaPromptEditor.openModal) {
      return global.ContinuuuumLemmaPromptEditor.openModal(opts);
    }
    opts = opts || {};
    const overlay = document.createElement('div');
    overlay.className = 'continuuuum-comp-overlay';
    const modal = document.createElement('div');
    modal.className = 'continuuuum-comp-modal';
    modal.innerHTML = '<h3>Edit lemma composition</h3>';
    const host = document.createElement('div');
    modal.appendChild(host);
    const closeRow = document.createElement('div');
    closeRow.className = 'continuuuum-comp-actions';
    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'secondary';
    closeBtn.textContent = 'Close';
    closeBtn.onclick = () => overlay.remove();
    closeRow.appendChild(closeBtn);
    modal.appendChild(closeRow);
    overlay.appendChild(modal);
    document.body.appendChild(overlay);
    overlay.addEventListener('click', (ev) => { if (ev.target === overlay) overlay.remove(); });

    mountEditorShell(host, {
      ...opts,
      onSaved: (data) => {
        if (opts.onSaved) opts.onSaved(data);
      },
    });
  }

  function mountInline(container, opts) {
    return mountEditorShell(container, opts || {});
  }

  /** Prefab | Composition tab strip for create forms */
  function mountCreateTabs(container, opts) {
    opts = opts || {};
    const prefabPanel = opts.prefabPanel;
    const compositionHost = opts.compositionHost;
    if (!container) return { getMode: () => 'prefab' };

    container.innerHTML =
      '<div class="continuuuum-comp-tabs">' +
      '<button type="button" data-mode="prefab" class="active">Prefab asset</button>' +
      '<button type="button" data-mode="composition">Lemma composition</button>' +
      '</div>';

    let mode = 'prefab';
    let inlineEditor = null;

    function setMode(next) {
      mode = next;
      container.querySelectorAll('button').forEach((b) => {
        b.classList.toggle('active', b.dataset.mode === mode);
      });
      if (prefabPanel) prefabPanel.style.display = mode === 'prefab' ? '' : 'none';
      if (compositionHost) {
        compositionHost.style.display = mode === 'composition' ? '' : 'none';
        if (mode === 'composition' && compositionHost && !inlineEditor) {
          inlineEditor = mountInline(compositionHost, {
            callApi: opts.callApi,
            parentEntryId: opts.parentEntryId || null,
            initialChildren: [],
          });
        }
      }
    }

    container.querySelectorAll('button').forEach((b) => {
      b.onclick = () => setMode(b.dataset.mode);
    });
    setMode('prefab');

    return {
      getMode: () => mode,
      getCompositionChildren: () => (inlineEditor ? inlineEditor.getChildren() : []),
      setParentEntryId: (id) => { if (inlineEditor) inlineEditor.setParentEntryId(id); },
      getInlineEditor: () => inlineEditor,
    };
  }

  const ContinuuuumLemmaCompositionEditor = {
    mountInline,
    openModal,
    openRecombobulateModal,
    mountCreateTabs,
    buildSavePayload,
    prepareSubpromptComposition,
    mountCreateLemmaForm,
  };

  global.ContinuuuumLemmaCompositionEditor = ContinuuuumLemmaCompositionEditor;

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = ContinuuuumLemmaCompositionEditor;
  }
})(typeof globalThis !== 'undefined' ? globalThis : typeof window !== 'undefined' ? window : global);
