/* Shared Continuum script editor — Ace + dotted overlays */
(function (global) {
  const P_RE = /\{\{?P:[^}]+\}?\}?|\{P:[^}]+\}/g;

  function parsePromptSpans(text) {
    const spans = [];
    if (!text) return spans;
    let m;
    while ((m = P_RE.exec(text)) !== null) {
      spans.push({ charStart: m.index, charEnd: m.index + m[0].length, text: m[0], kind: 'prompt' });
    }
    return spans;
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
      const res = await fetch(path, {
        method: method || 'GET',
        headers: { 'Content-Type': 'application/json' },
        body: body && method !== 'GET' ? (typeof body === 'string' ? body : JSON.stringify(body)) : undefined,
      });
      const text = await res.text();
      if (!res.ok) throw new Error(text || res.statusText);
      try { return JSON.parse(text); } catch (_) { return text; }
    },

    mount(el, options) {
      if (!el) return null;
      options = options || {};
      el.innerHTML = '';
      const toolbar = document.createElement('div');
      toolbar.style.cssText = 'display:flex;gap:8px;margin-bottom:6px;flex-wrap:wrap';
      const attachBtn = document.createElement('button');
      attachBtn.type = 'button';
      attachBtn.textContent = 'Attach clause';
      attachBtn.setAttribute('aria-label', 'Attach property, lemma, or localization to selection');
      toolbar.appendChild(attachBtn);
      el.appendChild(toolbar);

      const editorEl = document.createElement('div');
      editorEl.id = 'continuum-ace-editor';
      editorEl.style.height = options.height || '240px';
      editorEl.style.width = '100%';
      el.appendChild(editorEl);

      const aceLoaded = typeof ace !== 'undefined';
      let editor;
      if (aceLoaded) {
        editor = ace.edit(editorEl);
        editor.setTheme('ace/theme/textmate');
        editor.session.setMode('ace/mode/plain_text');
        editor.setValue(options.scriptText || '', -1);
        editor.setReadOnly(options.readOnly || (options.mode === 'review' && options.committed));
        editor.session.on('change', () => {
          const inst = { el, editor, options, aceLoaded };
          ContinuumScriptEditor.renderOverlays(inst, { ...options, clauseBindings: options.clauseBindings, reviewComments: options.reviewComments });
        });
      } else {
        const ta = document.createElement('textarea');
        ta.className = 'script-viewer';
        ta.style.width = '100%';
        ta.style.minHeight = '200px';
        ta.value = options.scriptText || '';
        ta.readOnly = options.readOnly || (options.mode === 'review' && options.committed);
        editorEl.appendChild(ta);
        editor = { _ta: ta, getValue: () => ta.value, setValue: v => { ta.value = v; }, getSession: () => null, selection: { getRange: () => null } };
      }

      const inst = { el, editor, options, aceLoaded, toolbar, attachBtn };
      this._instance = inst;
      this.renderOverlays(inst, options);

      attachBtn.onclick = () => {
        if (!global.ContinuumClauseSelector) {
          alert('ContinuumClauseSelector not loaded');
          return;
        }
        const clauseRef = global.ContinuumClauseSelector.fromEditorSelection(inst);
        global.ContinuumClauseSelector.openAttachDialog(clauseRef, {
          draftScriptId: options.draftScriptId,
          scriptText: ContinuumScriptEditor.getValue(inst),
          onAttached: async () => {
            if (options.onBindingsChanged) await options.onBindingsChanged();
            ContinuumScriptEditor.renderOverlays(inst, inst.options);
          },
        });
      };

      return inst;
    },

    getSelection(inst) {
      inst = inst || this._instance;
      if (!inst) return { charStart: 0, charEnd: 0, text: '' };
      if (inst.aceLoaded && inst.editor.selection) {
        const range = inst.editor.getSelectionRange();
        const text = inst.editor.session.getTextRange(range);
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

    renderOverlays(inst, options) {
      inst = inst || this._instance;
      options = options || inst?.options || {};
      if (!inst || !inst.aceLoaded) return;
      const session = inst.editor.getSession();
      const text = inst.editor.getValue();
      const spans = [
        ...parsePromptSpans(text),
        ...(options.clauseBindings || []).map(b => ({
          charStart: b.charStart,
          charEnd: b.charEnd,
          kind: 'clause',
          text: (b.bindingKind || 'property') + ': ' + (b.selectionText || b.propertyKey || ''),
          bindingKind: b.bindingKind,
        })),
        ...(options.reviewComments || []).map(c => ({
          charStart: c.textSelectionStart,
          charEnd: c.textSelectionEnd,
          kind: 'comment',
          text: c.commentText,
        })),
      ];
      inst._markers = inst._markers || [];
      inst._markers.forEach(id => session.removeMarker(id));
      inst._markers = [];
      spans.forEach(span => {
        if (span.charEnd <= span.charStart) return;
        const Range = ace.require('ace/range').Range;
        const start = session.doc.indexToPosition(span.charStart);
        const end = session.doc.indexToPosition(span.charEnd);
        const cls = span.kind === 'prompt' ? 'ace-prompt-placeholder' : span.kind === 'clause' ? 'ace-loc-clause' : 'ace-review-comment';
        const id = session.addMarker(new Range(start.row, start.column, end.row, end.column), cls, 'text', false);
        inst._markers.push(id);
      });
    },

    getValue(inst) {
      inst = inst || this._instance;
      return inst ? inst.editor.getValue() : '';
    },
  };

  const ContinuumChangeListModal = {
    open(changeListId, data, callbacks) {
      callbacks = callbacks || {};
      const overlay = document.createElement('div');
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.4);z-index:100;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:20px;max-width:560px;max-height:80vh;overflow:auto;border-radius:6px;color:#222';
      const required = (data && data.required) || [];
      const warnings = (data && data.warnings) || [];
      const state = { required: required.map(i => ({ ...i })), warnings: warnings.map(i => ({ ...i })) };

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
          if (callbacks.onSave) await callbacks.onSave(changeListId, { ...data, required: state.required, warnings: state.warnings });
          overlay.remove();
        };
        box.querySelector('#cl-submit').onclick = async () => {
          const unchecked = state.required.filter(i => !i.userAcknowledged && i.severity !== 'warning');
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
