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
        editor.setReadOnly(options.mode === 'review' && options.committed);
      } else {
        const ta = document.createElement('textarea');
        ta.className = 'script-viewer';
        ta.style.width = '100%';
        ta.style.minHeight = '200px';
        ta.value = options.scriptText || '';
        ta.readOnly = options.mode === 'review' && options.committed;
        editorEl.appendChild(ta);
        editor = { _ta: ta, getValue: () => ta.value, setValue: v => { ta.value = v; }, getSession: () => null, selection: { getRange: () => null } };
      }

      const inst = { el, editor, options, aceLoaded };
      this._instance = inst;
      this.renderOverlays(inst, options);
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
          charStart: b.charStart, charEnd: b.charEnd, kind: 'clause', text: b.selectionText || b.propertyKey,
        })),
        ...(options.reviewComments || []).map(c => ({
          charStart: c.textSelectionStart, charEnd: c.textSelectionEnd, kind: 'comment', text: c.commentText,
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
      box.style.cssText = 'background:#fff;padding:20px;max-width:560px;max-height:80vh;overflow:auto;border-radius:6px';
      const required = (data && data.required) || [];
      const warnings = (data && data.warnings) || [];
      box.innerHTML = `<h3>Change list ${changeListId || ''} (rev ${data?.revision ?? 0})</h3>
        <h4>Required</h4><ul id="cl-required">${required.map((i, idx) => `<li><label><input type="checkbox" data-idx="${idx}" ${i.userAcknowledged ? 'checked' : ''}/> ${i.description}</label></li>`).join('') || '<li>None</li>'}</ul>
        <details><summary>Warnings (${warnings.length})</summary><ul>${warnings.map(i => `<li>${i.description}</li>`).join('') || '<li>None</li>'}</ul></details>
        <div style="margin-top:12px"><button id="cl-save">Save</button> <button id="cl-submit">Submit for review</button> <button id="cl-cancel">Cancel</button></div>`;
      overlay.appendChild(box);
      document.body.appendChild(overlay);
      box.querySelector('#cl-cancel').onclick = () => overlay.remove();
      box.querySelector('#cl-save').onclick = async () => {
        if (callbacks.onSave) await callbacks.onSave(changeListId, data);
        overlay.remove();
      };
      box.querySelector('#cl-submit').onclick = async () => {
        const unchecked = box.querySelectorAll('#cl-required input:not(:checked)');
        if (unchecked.length) { alert('Acknowledge all required items before submit'); return; }
        if (callbacks.onSubmit) await callbacks.onSubmit(changeListId);
        overlay.remove();
      };
    },
  };

  global.ContinuumScriptEditor = ContinuumScriptEditor;
  global.ContinuumChangeListModal = ContinuumChangeListModal;
})(typeof window !== 'undefined' ? window : globalThis);
