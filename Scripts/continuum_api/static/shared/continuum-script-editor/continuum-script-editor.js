/* Shared Continuum script editor — Ace + dashed clause overlays */
(function (global) {
  const Spans = global.ContinuumScriptSpans;

  function bindingSummary(b) {
    const kind = b.bindingKind || b.binding_kind || 'property';
    if (kind === 'lemma') {
      return `entry: ${b.entryId || b.propertyValue || b.property_value || '—'}`;
    }
    if (kind === 'localization') {
      return `${b.propertyKey || b.property_key || 'lang'} → ${b.propertyValue || b.property_value || ''}`;
    }
    return `${b.propertyKey || b.property_key || 'property'}=${b.propertyValue || b.property_value || ''}`;
  }

  function renderLemmaBindingMeta(metaEl, binding, snippet) {
    const entryId = binding.entryId || binding.entry_id || binding.propertyValue || binding.property_value;
    if (!entryId) {
      metaEl.textContent = bindingSummary(binding);
      return;
    }
    const term = (binding._term || (snippet || '').trim() || entryId).trim();
    const LE = global.ContinuumLemmaEntry;
    if (!LE) {
      metaEl.textContent = `entry: ${entryId}`;
      return;
    }
    if (term && term !== entryId) {
      metaEl.appendChild(document.createTextNode(`${term} · `));
    }
    metaEl.appendChild(LE.createLink({ entryId, term }, {
      label: entryId,
      title: `Open lemma: ${term}`,
    }));
  }

  function hasNonEmptySelection(inst) {
    const sel = ContinuumScriptEditor.getSelection(inst);
    return sel.charEnd > sel.charStart && (sel.text || '').length > 0;
  }

  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

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

  function finishAceEditorLayout(editor, scriptText) {
    if (!editor || !editor.setValue) return;
    editor.setValue(scriptText || '', -1);
    editor.clearSelection();
    try {
      editor.setTheme('ace/theme/textmate');
      editor.session.setMode('ace/mode/text');
    } catch (_) { /* theme/mode optional */ }
    editor.setOptions({
      useWorker: false,
      fontSize: '13px',
      showPrintMargin: false,
      wrap: true,
    });
    editor.setReadOnly(!!editor._continuumReadOnly);
    const resize = () => {
      try {
        editor.resize(true);
        editor.renderer.updateFull(true);
      } catch (_) { /* ace not ready */ }
    };
    if (typeof requestAnimationFrame !== 'undefined') {
      requestAnimationFrame(resize);
    } else {
      resize();
    }
  }

  const ContinuumScriptEditor = {
    _instance: null,
    _bridge: null,

    mountWithBridge(el, bridge, options) {
      this._bridge = bridge;
      if (typeof window !== 'undefined') {
        window.unityBridge = {
          callApi: (method, path, body) => bridge.callApi(method, path, body),
          postMessage: (msg) => bridge.postMessage && bridge.postMessage(msg),
        };
      }
      return this.mount(el, options);
    },

    async callApi(method, path, body) {
      if (this._bridge && this._bridge.callApi) {
        return this._bridge.callApi(method, path, body);
      }
      const headers = { 'Content-Type': 'application/json' };
      if (global.ContinuumUserSession && global.ContinuumUserSession.getHeaders) {
        Object.assign(headers, global.ContinuumUserSession.getHeaders());
      }
      const res = await fetch(path, {
        method: method || 'GET',
        headers,
        credentials: 'include',
        body: body && method !== 'GET' ? (typeof body === 'string' ? body : JSON.stringify(body)) : undefined,
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

    mount(el, options) {
      if (!el) return null;
      options = options || {};
      el.innerHTML = '';
      el.classList.add('continuum-script-editor-host');
      const toolbar = document.createElement('div');
      toolbar.className = 'continuum-script-toolbar';
      const attachBtn = document.createElement('button');
      attachBtn.type = 'button';
      attachBtn.textContent = 'Attach clause';
      attachBtn.setAttribute('aria-label', 'Attach property, lemma, or localization to selection');
      toolbar.appendChild(attachBtn);
      const modSlotBtn = document.createElement('button');
      modSlotBtn.type = 'button';
      modSlotBtn.textContent = 'Mark Mayor Dog mod slot';
      modSlotBtn.setAttribute('aria-label', 'Mark selection as Mayor Dog Mod slot for player overrides');
      toolbar.appendChild(modSlotBtn);
      const suggestionsEl = document.createElement('div');
      suggestionsEl.className = 'continuum-clause-suggestions';
      suggestionsEl.setAttribute('aria-label', 'Reuse lemma or property configs for selected text');
      suggestionsEl.hidden = true;
      toolbar.appendChild(suggestionsEl);
      el.appendChild(toolbar);

      const editorEl = document.createElement('div');
      editorEl.id = 'continuum-ace-editor';
      editorEl.style.height = options.height || '240px';
      editorEl.style.width = '100%';
      el.appendChild(editorEl);

      const clausePanel = document.createElement('div');
      clausePanel.id = 'continuum-clause-panel';
      clausePanel.className = 'continuum-clause-panel';
      clausePanel.setAttribute('aria-label', 'Clauses at cursor');
      const clauseHost = options.clausePanelHost
        ? (typeof options.clausePanelHost === 'string'
          ? document.querySelector(options.clausePanelHost)
          : options.clausePanelHost)
        : null;
      if (clauseHost) {
        clauseHost.innerHTML = '';
        clauseHost.appendChild(clausePanel);
      } else {
        el.appendChild(clausePanel);
      }

      const readOnly = options.readOnly || (options.mode === 'review' && options.committed);
      const aceLoaded = typeof ace !== 'undefined';
      let editor;
      if (aceLoaded) {
        editor = ace.edit(editorEl);
        editor._continuumReadOnly = readOnly;
        finishAceEditorLayout(editor, options.scriptText || '');
      } else {
        const ta = document.createElement('textarea');
        ta.className = 'script-viewer';
        ta.style.width = '100%';
        ta.style.minHeight = '200px';
        ta.value = options.scriptText || '';
        ta.readOnly = readOnly;
        editorEl.appendChild(ta);
        editor = { _ta: ta, getValue: () => ta.value, setValue: v => { ta.value = v; }, getSession: () => null, selection: { getRange: () => null } };
      }

      const inst = { el, editor, options, aceLoaded, toolbar, attachBtn, modSlotBtn, suggestionsEl, clausePanel, readOnly };
      inst.overlaySnapshotText = options.overlaySnapshotText ?? options.scriptText ?? '';
      inst._suggestionSeq = 0;
      this._instance = inst;
      this.renderOverlays(inst, options);
      this.renderClausePanel(inst);
      this.renderClauseSuggestions(inst);

      const scheduleRefresh = () => {
        if (inst._overlayUpdating) return;
        if (typeof document !== 'undefined' && document.querySelector('.continuum-clause-overlay')) return;
        if (inst._overlayRaf) cancelAnimationFrame(inst._overlayRaf);
        inst._overlayRaf = requestAnimationFrame(() => {
          inst._overlayRaf = null;
          ContinuumScriptEditor.renderOverlays(inst, inst.options);
          ContinuumScriptEditor.renderClausePanel(inst);
          ContinuumScriptEditor.renderClauseSuggestions(inst);
        });
      };

      if (aceLoaded) {
        editor.session.on('change', scheduleRefresh);
        editor.session.on('changeScrollTop', scheduleRefresh);
        editor.session.on('changeScrollLeft', scheduleRefresh);
        editor.selection.on('changeCursor', scheduleRefresh);
        editor.selection.on('changeSelection', scheduleRefresh);
      } else {
        const ta = editor._ta;
        if (ta) {
          ta.addEventListener('input', scheduleRefresh);
          ta.addEventListener('click', scheduleRefresh);
          ta.addEventListener('keyup', scheduleRefresh);
        }
      }

      attachBtn.onclick = () => {
        if (!global.ContinuumClauseSelector) {
          alert('ContinuumClauseSelector not loaded');
          return;
        }
        const clauseRef = global.ContinuumClauseSelector.fromEditorSelection(inst);
        global.ContinuumClauseSelector.openAttachDialog(clauseRef, {
          draftScriptId: options.draftScriptId,
          draftEpisodeId: options.draftEpisodeId || options.draftId,
          scriptText: ContinuumScriptEditor.getValue(inst),
          onAttached: async () => {
            if (options.onBindingsChanged) await options.onBindingsChanged();
            ContinuumScriptEditor.renderOverlays(inst, inst.options);
            ContinuumScriptEditor.renderClausePanel(inst);
            ContinuumScriptEditor.renderClauseSuggestions(inst);
          },
        });
      };

      modSlotBtn.disabled = !!readOnly;
      modSlotBtn.onclick = () => this.markMayorDogModSlot(inst);

      return inst;
    },

    async fetchClauseSuggestions(selectionText, inst) {
      const CS = global.ContinuumClauseSelector;
      const callApi = (method, path, body) => ContinuumScriptEditor.callApi(method, path, body);
      const text = String(selectionText ?? '').trim();
      if (!text) return [];

      const [bindingsRes, entriesRes] = await Promise.all([
        callApi('GET', `/api/thesaurus/clause-bindings?selectionText=${encodeURIComponent(text)}`).catch(() => ({ items: [] })),
        callApi('GET', `/api/thesaurus/entries?q=${encodeURIComponent(text)}&limit=10`).catch(() => ({ items: [] })),
      ]);

      const options = inst?.options || {};
      const scriptText = ContinuumScriptEditor.getValue(inst);
      const snapshot = inst?.overlaySnapshotText ?? options.overlaySnapshotText ?? options.scriptText ?? scriptText;
      const sel = ContinuumScriptEditor.getSelection(inst);
      const atSelection = Spans
        ? Spans.bindingsAtRange(snapshot, scriptText, options.clauseBindings || [], sel.charStart, sel.charEnd)
        : [];
      const applied = new Set(
        (CS && CS.bindingTemplateKey
          ? atSelection.map(b => CS.bindingTemplateKey(b))
          : atSelection.map(b => `${b.bindingKind}:${b.propertyKey}:${b.propertyValue}`)),
      );

      const currentScriptId = options.draftScriptId;
      const templates = [];
      const seen = new Set();
      const sourceGroups = new Map();

      (bindingsRes.items || []).forEach((b) => {
        const sameSpan = currentScriptId
          && (b.draftScriptId || b.draft_script_id) === currentScriptId
          && (b.charStart ?? b.char_start) === sel.charStart
          && (b.charEnd ?? b.char_end) === sel.charEnd;
        if (sameSpan) return;

        const key = CS ? CS.bindingTemplateKey(b) : `${b.bindingKind}:${b.propertyKey}:${b.propertyValue}`;
        if (applied.has(key) || seen.has(key)) return;
        seen.add(key);
        templates.push(b);

        const sk = CS ? CS.bindingSourceKey(b) : `${b.draftScriptId}:${b.charStart}:${b.charEnd}`;
        if (!sourceGroups.has(sk)) sourceGroups.set(sk, []);
        sourceGroups.get(sk).push(b);
      });

      (entriesRes.items || []).forEach((entry) => {
        if (!entry?.id) return;
        const tpl = CS ? CS.lemmaEntryTemplate(entry) : {
          bindingKind: 'lemma',
          propertyKey: 'entry-id',
          propertyValue: entry.id,
          entryId: entry.id,
        };
        const key = CS ? CS.bindingTemplateKey(tpl) : `lemma:${entry.id}`;
        if (applied.has(key) || seen.has(key)) return;
        if (String(entry.term || '').toLowerCase() !== text.toLowerCase()) return;
        seen.add(key);
        templates.push(tpl);
      });

      const bundles = [];
      sourceGroups.forEach((group, sk) => {
        if (group.length < 2) return;
        const bundleKeys = group.map(b => (CS ? CS.bindingTemplateKey(b) : ''));
        if (bundleKeys.some(k => applied.has(k))) return;
        bundles.push({
          _bundle: true,
          _sourceKey: sk,
          _templates: group,
          _label: `Apply all (${group.length})`,
          _tooltip: group.map((b) => (CS ? CS.suggestionTooltip(b) : bindingSummary(b))).join('\n\n'),
        });
      });

      return [...bundles, ...templates];
    },

    renderClauseSuggestions(inst) {
      inst = inst || this._instance;
      if (!inst || !inst.suggestionsEl) return;

      const el = inst.suggestionsEl;
      if (inst.readOnly || !hasNonEmptySelection(inst)) {
        el.hidden = true;
        el.innerHTML = '';
        return;
      }

      const sel = this.getSelection(inst);
      const selectionText = (sel.text || '').trim();
      if (!selectionText) {
        el.hidden = true;
        el.innerHTML = '';
        return;
      }

      el.hidden = false;
      if (!inst._debouncedSuggestions) {
        inst._debouncedSuggestions = debounce((text) => {
          ContinuumScriptEditor._loadClauseSuggestions(inst, text);
        }, 200);
      }
      if (inst._lastSuggestionText !== selectionText) {
        el.innerHTML = '<span class="continuum-clause-suggestions-hint">Loading…</span>';
        inst._debouncedSuggestions(selectionText);
      }
    },

    async _loadClauseSuggestions(inst, selectionText) {
      const seq = ++inst._suggestionSeq;
      inst._lastSuggestionText = selectionText;
      try {
        const items = await ContinuumScriptEditor.fetchClauseSuggestions(selectionText, inst);
        if (seq !== inst._suggestionSeq) return;
        const el = inst.suggestionsEl;
        if (!el || inst.readOnly || !hasNonEmptySelection(inst)) return;

        el.innerHTML = '';
        if (!items.length) {
          el.innerHTML = '<span class="continuum-clause-suggestions-hint">No matching configs</span>';
          return;
        }

        const CS = global.ContinuumClauseSelector;
        items.forEach((tpl) => {
          const btn = document.createElement('button');
          btn.type = 'button';
          const kind = tpl._bundle ? 'bundle' : (tpl.bindingKind || tpl.binding_kind || 'property');
          btn.className = `continuum-clause-suggestion-btn continuum-clause-suggestion-${kind}`;
          btn.textContent = 'Apply: ' + (tpl._label || (CS ? CS.suggestionLabel(tpl) : bindingSummary(tpl)));
          btn.title = tpl._tooltip || (CS ? CS.suggestionTooltip(tpl) : bindingSummary(tpl));
          btn.onclick = async () => {
            if (!CS) {
              alert('ContinuumClauseSelector not loaded');
              return;
            }
            try {
              const clauseRef = CS.fromEditorSelection(inst);
              const toClone = tpl._bundle ? tpl._templates : [tpl];
              await CS.cloneBindingTemplates(toClone, clauseRef, {
                draftScriptId: inst.options.draftScriptId,
                draftEpisodeId: inst.options.draftEpisodeId || inst.options.draftId,
                scriptText: ContinuumScriptEditor.getValue(inst),
              });
              if (inst.options.onBindingsChanged) await inst.options.onBindingsChanged();
              ContinuumScriptEditor.renderOverlays(inst, inst.options);
              ContinuumScriptEditor.renderClausePanel(inst);
              ContinuumScriptEditor.renderClauseSuggestions(inst);
            } catch (e) {
              alert(e.message || String(e));
            }
          };
          el.appendChild(btn);
        });
      } catch (_) {
        if (seq !== inst._suggestionSeq) return;
        inst.suggestionsEl.innerHTML = '<span class="continuum-clause-suggestions-hint">Could not load suggestions</span>';
      }
    },

    getCursorRange(inst) {
      inst = inst || this._instance;
      if (!inst) return { charStart: 0, charEnd: 1 };
      const sel = this.getSelection(inst);
      let charStart = sel.charStart;
      let charEnd = sel.charEnd;
      if (charEnd <= charStart) charEnd = charStart + 1;
      return { charStart, charEnd };
    },

    getSelection(inst) {
      inst = inst || this._instance;
      if (!inst) return { charStart: 0, charEnd: 0, text: '' };
      if (inst.aceLoaded && inst.editor.selection) {
        const range = inst.editor.getSelectionRange();
        const text = inst.editor.session.getTextRange(range) || '';
        return {
          charStart: inst.editor.session.doc.positionToIndex(range.start),
          charEnd: inst.editor.session.doc.positionToIndex(range.end),
          text,
        };
      }
      const ta = inst.editor._ta;
      if (ta) {
        return {
          charStart: ta.selectionStart,
          charEnd: ta.selectionEnd,
          text: ta.value.substring(ta.selectionStart, ta.selectionEnd),
        };
      }
      return { charStart: 0, charEnd: 0, text: '' };
    },

    renderClausePanel(inst) {
      inst = inst || this._instance;
      if (!inst || !inst.clausePanel) return;
      const panel = inst.clausePanel;
      const options = inst.options || {};
      const text = this.getValue(inst);
      const snapshot = inst.overlaySnapshotText ?? options.overlaySnapshotText ?? options.scriptText ?? text;
      const range = this.getCursorRange(inst);
      const bindings = Spans
        ? Spans.bindingsAtRange(snapshot, text, options.clauseBindings || [], range.charStart, range.charEnd)
        : [];

      panel.innerHTML = '';
      const heading = document.createElement('div');
      heading.className = 'continuum-clause-panel-heading';
      heading.textContent = bindings.length
        ? `Clauses at selection (${range.charStart}, ${range.charEnd})`
        : 'No clauses at selection';
      panel.appendChild(heading);

      const list = document.createElement('div');
      list.className = 'continuum-clause-panel-list';
      bindings.forEach((b) => {
        const card = document.createElement('div');
        card.className = 'continuum-clause-card';
        const kind = b.bindingKind || b.binding_kind || 'property';
        const liveSlice = text.substring(b.charStart, b.charEnd);
        const snippet = liveSlice || b.selectionText || b.selection_text || '—';
        card.innerHTML =
          `<span class="continuum-clause-kind continuum-clause-kind-${kind}">${kind}</span>` +
          `<span class="continuum-clause-snippet">"${snippet.slice(0, 40)}" [${b.charStart}, ${b.charEnd})</span>`;
        const meta = document.createElement('span');
        meta.className = 'continuum-clause-meta';
        if (kind === 'lemma') {
          renderLemmaBindingMeta(meta, b, snippet);
        } else {
          meta.textContent = bindingSummary(b);
        }
        card.appendChild(meta);
        if (!inst.readOnly && global.ContinuumClauseSelector) {
          if (kind === 'lemma') {
            const entryId = b.entryId || b.entry_id || b.propertyValue || b.property_value;
            if (entryId && global.ContinuumClauseSelector.openLemmaEntryDialog) {
              const editBtn = document.createElement('button');
              editBtn.type = 'button';
              editBtn.textContent = 'Edit lemma';
              editBtn.className = 'continuum-clause-edit-btn';
              editBtn.onclick = () => {
                global.ContinuumClauseSelector.openLemmaEntryDialog({
                  entryId,
                  selectionText: (snippet || '').trim(),
                  prefabOnly: true,
                  binding: b,
                  draftEpisodeId: options.draftEpisodeId || options.draftId,
                  scriptText: text,
                  onEdited: async (result) => {
                    if (options.onBindingsChanged) await options.onBindingsChanged();
                    if (options.onBindingEdited) options.onBindingEdited(result);
                    ContinuumScriptEditor.renderOverlays(inst, inst.options);
                    ContinuumScriptEditor.renderClausePanel(inst);
                  },
                });
              };
              card.appendChild(editBtn);
            }
          } else {
            const editBtn = document.createElement('button');
            editBtn.type = 'button';
            editBtn.textContent = 'Edit';
            editBtn.className = 'continuum-clause-edit-btn';
            editBtn.onclick = () => {
              global.ContinuumClauseSelector.openEditDialog(b, {
                draftEpisodeId: options.draftEpisodeId || options.draftId,
                draftScriptId: options.draftScriptId,
                scriptText: text,
                onEdited: async (result) => {
                  if (options.onBindingsChanged) await options.onBindingsChanged();
                  if (options.onBindingEdited) options.onBindingEdited(result);
                  ContinuumScriptEditor.renderOverlays(inst, inst.options);
                  ContinuumScriptEditor.renderClausePanel(inst);
                },
              });
            };
            card.appendChild(editBtn);
          }
        }
        if (kind === 'lemma' && global.ContinuumLemmaPromptEditor) {
          const entryId = b.entryId || b.entry_id || b.propertyValue || b.property_value;
          if (entryId) {
            const snippet = (b.selectionText || b.selection_text || text.substring(
              b.charStart ?? b.char_start ?? 0,
              b.charEnd ?? b.char_end ?? 0,
            ) || '').trim();
            const compBtn = document.createElement('button');
            compBtn.type = 'button';
            compBtn.textContent = 'Composition';
            compBtn.className = 'continuum-clause-edit-btn';
            compBtn.style.marginLeft = '4px';
            compBtn.onclick = () => {
              global.ContinuumLemmaPromptEditor.openModal({
                entryId,
                parentEntryId: entryId,
                draftEpisodeId: options.draftEpisodeId || options.draftId,
                scriptText: text,
                seedPhrase: snippet,
                onSaved: () => {
                  if (options.onBindingsChanged) options.onBindingsChanged();
                },
              });
            };
            card.appendChild(compBtn);
          }
        }
        list.appendChild(card);
      });
      panel.appendChild(list);
    },

    async markMayorDogModSlot(inst) {
      inst = inst || this._instance;
      if (!inst) return;
      if (inst.readOnly) {
        alert('Script is read-only — withdraw from review or switch to edit mode first.');
        return;
      }
      const sel = this.getSelection(inst);
      if (sel.charEnd <= sel.charStart || !(sel.text || '').trim()) {
        alert('Select script text to mark as a Mayor Dog Mod slot.');
        return;
      }
      const options = inst.options || {};
      const draftId = options.draftEpisodeId || options.draftId;
      if (!draftId) {
        alert('Draft episode ID is required to mark episode mod slots.');
        return;
      }
      const label = (sel.text || '').trim().slice(0, 48);
      const slotKey = label.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'mod-slot';
      const body = {
        targetKind: 'episode_section',
        draftEpisodeId: draftId,
        charStart: sel.charStart,
        charEnd: sel.charEnd,
        slotKey: `${slotKey}-${Date.now().toString(36).slice(-4)}`,
        label,
        sourceText: this.getValue(inst),
      };
      try {
        const resp = await this.callApi('POST', '/api/mods/moddable-targets', body);
        const item = resp.item || resp;
        const token = `{M:${item.slotKey || body.slotKey}}`;
        if (inst.aceLoaded && inst.editor) {
          const Range = ace.require('ace/range').Range;
          const session = inst.editor.getSession();
          const start = session.doc.indexToPosition(sel.charEnd);
          session.insert(start, token);
        } else if (inst.editor && inst.editor._ta) {
          const ta = inst.editor._ta;
          const v = ta.value;
          ta.value = v.slice(0, sel.charEnd) + token + v.slice(sel.charEnd);
        }
        inst._overlaySpanSig = null;
        if (inst.options.onScriptChanged) inst.options.onScriptChanged(this.getValue(inst));
        this.renderOverlays(inst, inst.options);
        alert(`Mayor Dog Mod slot created: ${item.slotKey || body.slotKey}`);
      } catch (err) {
        alert(err.message || 'Failed to create mod slot');
      }
    },

    renderOverlays(inst, options) {
      inst = inst || this._instance;
      options = options || inst?.options || {};
      if (!inst || !inst.aceLoaded) return;
      const session = inst.editor.getSession();
      const text = inst.editor.getValue();
      const docLen = text.length;
      const snapshot = inst.overlaySnapshotText ?? options.overlaySnapshotText ?? options.scriptText ?? text;
      const spans = Spans
        ? Spans.buildOverlaySpans(text, snapshot, options.clauseBindings, options.reviewComments)
        : [];
      const sig = `${docLen}|` + spans.map((s) => `${s.kind}:${s.charStart}:${s.charEnd}`).join('|');
      if (inst._overlaySpanSig === sig && inst._markers && inst._markers.length) return;
      inst._overlaySpanSig = sig;

      inst._overlayUpdating = true;
      try {
        inst._markers = inst._markers || [];
        inst._markers.forEach(id => session.removeMarker(id));
        inst._markers = [];
        if (!docLen) return;
        spans.forEach(span => {
          let cs = Math.max(0, Math.min(span.charStart, docLen));
          let ce = Math.max(cs, Math.min(span.charEnd, docLen));
          if (ce <= cs) return;
          const Range = ace.require('ace/range').Range;
          const start = session.doc.indexToPosition(cs);
          const end = session.doc.indexToPosition(ce);
          if (start.row === end.row && start.column === end.column) return;
          const cls = span.kind === 'prompt'
            ? 'ace-prompt-placeholder'
            : span.kind === 'mayorDogModSlot'
              ? 'ace-mayor-dog-mod-slot'
              : span.kind === 'clause'
                ? 'ace-loc-clause'
                : 'ace-review-comment';
          const id = session.addMarker(new Range(start.row, start.column, end.row, end.column), cls, 'text', false);
          inst._markers.push(id);
        });
      } finally {
        inst._overlayUpdating = false;
      }
    },

    resize(inst) {
      inst = inst || this._instance;
      if (!inst || !inst.aceLoaded || !inst.editor) return;
      try {
        inst.editor.resize(true);
        inst.editor.renderer.updateFull(true);
      } catch (_) { /* ignore */ }
    },

    setScriptText(inst, scriptText) {
      inst = inst || this._instance;
      if (!inst) return;
      const text = scriptText || '';
      inst.options.scriptText = text;
      inst.overlaySnapshotText = text;
      if (inst.aceLoaded) {
        finishAceEditorLayout(inst.editor, text);
      } else if (inst.editor && inst.editor._ta) {
        inst.editor._ta.value = text;
      }
      inst._overlaySpanSig = null;
      this.renderOverlays(inst, inst.options);
      this.renderClausePanel(inst);
    },

    getValue(inst) {
      inst = inst || this._instance;
      return inst ? inst.editor.getValue() : '';
    },
  };

  const ContinuumChangeListModal = {
    open(changeListId, data, callbacks) {
      callbacks = callbacks || {};
      if (!changeListId && !(data?.required?.length) && !(data?.warnings?.length)) return;
      const overlay = document.createElement('div');
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.4);z-index:100;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:20px;max-width:560px;max-height:80vh;overflow:auto;border-radius:6px;color:#222';
      const required = (data && data.required) || [];
      const warnings = (data && data.warnings) || [];
      const ackExtra = (global.ContinuumScriptAck && data && data._changeListForAck)
        ? global.ContinuumScriptAck.buildChangeListAckItems(data._changeListForAck)
        : [];
      const state = {
        required: [...ackExtra.map((i) => ({ ...i })), ...required.map((i) => ({ ...i }))],
        warnings: warnings.map(i => ({ ...i })),
      };

      function unacknowledgedRequired(items) {
        if (global.ContinuumScriptAck && global.ContinuumScriptAck.unacknowledgedRequired) {
          return global.ContinuumScriptAck.unacknowledgedRequired(items);
        }
        return (items || []).filter(i => !i.userAcknowledged && i.severity !== 'warning');
      }

      function render() {
        box.innerHTML = `<h3>Change list ${changeListId || ''} (rev ${data?.revision ?? 0})</h3>
          <p style="font-size:12px;color:#666">Status: ${data?.workflowStatus || 'in_progress'}</p>
          <h4>Required</h4>
          <ul id="cl-required">${state.required.map((i, idx) => `<li><label><input type="checkbox" data-idx="${idx}" ${i.userAcknowledged ? 'checked' : ''}/> ${i.description}</label></li>`).join('') || '<li>None</li>'}</ul>
          <details><summary>Warnings (${state.warnings.length})</summary><ul>${state.warnings.map(i => `<li>${i.description}</li>`).join('') || '<li>None</li>'}</ul></details>
          <div style="margin-top:12px"><button id="cl-save">Save</button> <button id="cl-submit">Submit for review</button> <button id="cl-withdraw">Withdraw</button> <button id="cl-cancel">Cancel</button></div>`;
        box.querySelector('#cl-cancel').onclick = () => overlay.remove();
        box.querySelectorAll('#cl-required input').forEach(inp => {
          inp.onchange = () => { state.required[+inp.dataset.idx].userAcknowledged = inp.checked; };
        });
        box.querySelector('#cl-save').onclick = async () => {
          const unchecked = unacknowledgedRequired(state.required);
          if (unchecked.length) { alert('Acknowledge all required items before save'); return; }
          if (callbacks.onSave) await callbacks.onSave(changeListId, { ...data, required: state.required, warnings: state.warnings });
          overlay.remove();
        };
        box.querySelector('#cl-submit').onclick = async () => {
          const unchecked = unacknowledgedRequired(state.required);
          if (unchecked.length) { alert('Acknowledge all required items before submit'); return; }
          if (callbacks.onSubmit) await callbacks.onSubmit(changeListId, { ...data, required: state.required });
          overlay.remove();
        };
        const withdrawBtn = box.querySelector('#cl-withdraw');
        if (withdrawBtn) {
          withdrawBtn.onclick = async () => {
            if (callbacks.onWithdraw) await callbacks.onWithdraw(changeListId);
            overlay.remove();
          };
          if (data?.workflowStatus !== 'in_review') withdrawBtn.style.display = 'none';
        }
      }
      render();
      overlay.appendChild(box);
      document.body.appendChild(overlay);
    },
  };

  global.ContinuumScriptEditor = ContinuumScriptEditor;
  global.ContinuumChangeListModal = ContinuumChangeListModal;
})(typeof window !== 'undefined' ? window : globalThis);
