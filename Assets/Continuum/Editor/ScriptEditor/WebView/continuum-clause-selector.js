/* Shared Continuum clause selector — unified attach for property / lemma / localization */
(function (global) {
  const BINDING_KINDS = ['property', 'lemma', 'localization', 'prompt_placeholder'];

  function gcd(a, b) {
    while (b) { const t = b; b = a % b; a = t; }
    return a || 1;
  }

  const ContinuumClauseSelector = {
    fromEditorSelection(editorInst) {
      const sel = global.ContinuumScriptEditor
        ? global.ContinuumScriptEditor.getSelection(editorInst)
        : { charStart: 0, charEnd: 0, text: '' };
      const text = sel.text || '';
      const n = Math.max((global.ContinuumScriptEditor?.getValue(editorInst) || '').length, 1);
      const cs = Math.max(0, Math.min(sel.charStart, n));
      const ce = Math.max(cs, Math.min(sel.charEnd, n));
      const g1 = gcd(cs, n);
      const g2 = gcd(ce, n);
      return {
        charStart: cs,
        charEnd: ce,
        selectionText: text,
        fareyLeftNum: cs / g1,
        fareyLeftDen: n / g1,
        fareyRightNum: ce / g2,
        fareyRightDen: n / g2,
        draftScriptId: editorInst?.options?.draftScriptId,
        draftEpisodeId: editorInst?.options?.draftId,
      };
    },

    async callApi(method, path, body) {
      if (global.ContinuumScriptEditor?.callApi) {
        return global.ContinuumScriptEditor.callApi(method, path, body);
      }
      const res = await fetch(path, {
        method: method || 'GET',
        headers: { 'Content-Type': 'application/json' },
        body: body && method !== 'GET' ? JSON.stringify(body) : undefined,
      });
      const text = await res.text();
      if (!res.ok) throw new Error(text || res.statusText);
      try { return JSON.parse(text); } catch (_) { return text; }
    },

    openAttachDialog(clauseRef, options) {
      options = options || {};
      const overlay = document.createElement('div');
      overlay.className = 'continuum-clause-overlay';
      overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.45);z-index:110;display:flex;align-items:center;justify-content:center';
      const box = document.createElement('div');
      box.style.cssText = 'background:#fff;padding:16px;max-width:520px;width:90%;border-radius:6px;color:#222';
      box.innerHTML = `
        <h3 style="margin:0 0 8px">Attach to clause</h3>
        <p style="font-size:13px;color:#555">"${(clauseRef.selectionText || '').slice(0, 60)}" [${clauseRef.charStart}, ${clauseRef.charEnd})</p>
        <div role="tablist" style="display:flex;gap:8px;margin-bottom:12px">
          <button type="button" data-tab="property">Property</button>
          <button type="button" data-tab="lemma">Lemma</button>
          <button type="button" data-tab="localization">Localization</button>
        </div>
        <div id="clause-tab-property">
          <label>Property key <select id="clause-prop-key"><option value="">Loading…</option></select></label>
          <label style="display:block;margin-top:8px">Value <input id="clause-prop-val" style="width:100%"/></label>
        </div>
        <div id="clause-tab-lemma" hidden>
          <label>Entry ID <input id="clause-entry-id" style="width:100%"/></label>
          <label style="display:block;margin-top:8px">Property key <input id="clause-lemma-key" value="entry-id" style="width:100%"/></label>
        </div>
        <div id="clause-tab-localization" hidden>
          <label>Language code <input id="clause-lang" placeholder="es" style="width:100%"/></label>
          <label style="display:block;margin-top:8px">Translation <input id="clause-loc-val" style="width:100%"/></label>
        </div>
        <div style="margin-top:14px;display:flex;gap:8px">
          <button type="button" id="clause-attach-save">Attach</button>
          <button type="button" id="clause-attach-cancel">Cancel</button>
        </div>`;
      overlay.appendChild(box);
      document.body.appendChild(overlay);

      let activeTab = options.mode || 'property';
      const showTab = (name) => {
        activeTab = name;
        ['property', 'lemma', 'localization'].forEach(t => {
          const el = box.querySelector('#clause-tab-' + t);
          if (el) el.hidden = t !== name;
        });
      };
      box.querySelectorAll('[data-tab]').forEach(btn => {
        btn.onclick = () => showTab(btn.dataset.tab);
      });
      showTab(activeTab);

      ContinuumClauseSelector.callApi('GET', '/api/thesaurus/property-specs').then(data => {
        const sel = box.querySelector('#clause-prop-key');
        sel.innerHTML = (data.items || []).map(s => `<option value="${s.key}">${s.key}</option>`).join('') || '<option value="non-ik-animation">non-ik-animation</option>';
      }).catch(() => {});

      box.querySelector('#clause-attach-cancel').onclick = () => overlay.remove();
      box.querySelector('#clause-attach-save').onclick = async () => {
        try {
          let body = {
            ...clauseRef,
            draftScriptId: clauseRef.draftScriptId || options.draftScriptId,
            scriptText: options.scriptText || '',
          };
          if (activeTab === 'property') {
            body.bindingKind = 'property';
            body.propertyKey = box.querySelector('#clause-prop-key').value;
            body.propertyValue = box.querySelector('#clause-prop-val').value;
          } else if (activeTab === 'lemma') {
            body.bindingKind = 'lemma';
            body.entryId = box.querySelector('#clause-entry-id').value;
            body.propertyKey = box.querySelector('#clause-lemma-key').value || 'entry-id';
            body.propertyValue = body.entryId;
          } else {
            body.bindingKind = 'localization';
            const lang = box.querySelector('#clause-lang').value.trim();
            body.propertyKey = 'lang:' + lang;
            body.propertyValue = box.querySelector('#clause-loc-val').value;
          }
          await ContinuumClauseSelector.callApi('POST', '/api/thesaurus/clause-bindings', body);
          overlay.remove();
          if (options.onAttached) await options.onAttached();
        } catch (e) {
          alert(e.message || String(e));
        }
      };
    },
  };

  global.ContinuumClauseSelector = ContinuumClauseSelector;
  global.ContinuumClauseSelectorBindingKinds = BINDING_KINDS;
})(typeof window !== 'undefined' ? window : globalThis);
