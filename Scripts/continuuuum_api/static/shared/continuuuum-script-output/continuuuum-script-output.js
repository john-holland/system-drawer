/** Script Output page controller */
(function (global) {
  const Permissions = global.ContinuuuumScriptPermissions;
  const Ack = global.ContinuuuumScriptAck;

  function escHtml(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function changeListSaveBody(data, rootEl) {
    const required = data?.required || [];
    const warnings = data?.warnings || [];
    const items = [...required, ...warnings]
      .filter((i) => i && i.id && !i._synthetic)
      .map((i) => {
        let ack = !!i.userAcknowledged;
        if (rootEl && i.severity === 'required') {
          const cb = rootEl.querySelector(`input[data-cl-id="${i.id}"]`);
          if (cb) ack = cb.checked;
        }
        return { id: i.id, userAcknowledged: ack };
      });
    return JSON.stringify({ items });
  }

  const ContinuuuumScriptOutput = {
    _state: null,

    init(options) {
      options = options || {};
      this._apiBase = options.apiBase || '/api';
      this._getUserId = options.getUserId || (() => 'anonymous');
      this._bindControls();
      this._readQueryParams();
      if (this._state && this._state.activeId) this.loadDraft();
    },

    _bindControls() {
      const loadBtn = document.getElementById('load-btn');
      const saveBtn = document.getElementById('save-btn');
      const suggestBtn = document.getElementById('suggest-btn');
      if (loadBtn) loadBtn.onclick = () => this.loadDraft();
      if (saveBtn) saveBtn.onclick = () => this.saveScript();
      if (suggestBtn) suggestBtn.onclick = () => this.submitSuggestion();
      const autoAddBtn = document.getElementById('auto-add-lemmas-btn');
      if (autoAddBtn) autoAddBtn.onclick = () => this.autoAddAllSingleLemmas();
      const userInp = document.getElementById('user-id');
      if (userInp) userInp.addEventListener('change', () => {
        if (this._state && this._state.activeId) this.loadDraft();
      });
    },

    _readQueryParams() {
      const params = new URLSearchParams(window.location.search);
      const draftId = params.get('draftId') || params.get('draft_id');
      const episodeId = params.get('episodeId') || params.get('episode_id');
      const draftInp = document.getElementById('draft-id');
      const epInp = document.getElementById('episode-id');
      this._state = {
        scriptSnapshot: '',
        editorInst: null,
        loadMode: 'draft',
        activeId: '',
        draftId: '',
        episodeId: episodeId || '',
        clauseBindings: [],
        permissions: null,
        draft: null,
        changeList: null,
        reviews: [],
        suggestions: [],
        bindingSuggestions: [],
        archivedSuggestions: [],
        comments: [],
        archivedComments: [],
        activeSuggestion: null,
        suggestionDiff: null,
        draftScriptId: null,
      };
      if (draftId && draftInp) {
        draftInp.value = draftId;
        this._state.loadMode = 'draft';
        this._state.activeId = draftId;
        this._state.draftId = draftId;
      }
      if (episodeId && epInp) {
        epInp.value = episodeId;
        if (!draftId) this._state.activeId = episodeId;
        this._state.episodeId = episodeId;
      }
    },

    async api(path, opts) {
      opts = opts || {};
      const sessionHeaders = global.ContinuuuumUserSession && global.ContinuuuumUserSession.getHeaders
        ? global.ContinuuuumUserSession.getHeaders()
        : { 'X-User-ID': this._getUserId() };
      const res = await fetch(this._apiBase + path, {
        ...opts,
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          ...sessionHeaders,
          ...(opts.headers || {}),
        },
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        const err = new Error(data.error || res.statusText || 'Request failed');
        err.code = data.code;
        throw err;
      }
      return data;
    },

    setStatus(msg, isError) {
      const el = document.getElementById('status');
      if (!el) return;
      el.textContent = msg || '';
      el.classList.toggle('is-error', !!isError);
    },

    async resolveDraftForEpisode(episodeId) {
      const drafts = await this.api(`/drafts/episodes?episodeId=${encodeURIComponent(episodeId)}`);
      const list = Array.isArray(drafts) ? drafts : (drafts.items || []);
      if (list.length > 0) return list[0].id;
      const created = await this.api('/drafts/episodes', {
        method: 'POST',
        body: JSON.stringify({ episodeId }),
      });
      return created.id;
    },

    async loadClauseBindings(draftId) {
      if (!draftId) {
        this._state.clauseBindings = [];
        return 0;
      }
      const data = await this.api(`/thesaurus/clause-bindings?draftEpisodeId=${encodeURIComponent(draftId)}`);
      this._state.clauseBindings = data.items || [];
      return this._state.clauseBindings.length;
    },

    renderHeader() {
      const el = document.getElementById('so-header');
      if (!el) return;
      const d = this._state.draft;
      const script = this._state.scriptMeta;
      if (!d) {
        el.innerHTML = '<h1>Script Output</h1><p class="so-panel-empty">Load a draft to begin.</p>';
        return;
      }
      const scriptUpdated = script && script.updatedAt ? script.updatedAt : '—';
      const review = (this._state.reviews || []).find((r) => r.id) || (this._state.reviews || [])[0];
      const reviewHref = this._state.draftId
        ? '/ui#review?draftId=' + encodeURIComponent(this._state.draftId) +
          (review && review.id ? '&reviewId=' + encodeURIComponent(review.id) : '')
        : '';
      el.innerHTML = `
        <h1>${escHtml(d.title || 'Untitled draft')}</h1>
        <div class="so-meta">
          <span><strong>Author</strong> ${escHtml(d.createdBy || 'anonymous')}</span>
          <span><strong>Created</strong> ${escHtml(d.createdAt || '—')}</span>
          <span><strong>Draft updated</strong> ${escHtml(d.updatedAt || '—')}</span>
          <span><strong>Script updated</strong> ${escHtml(scriptUpdated)}</span>
          ${this._state.changeList ? `<span><strong>Change list</strong> ${escHtml(this._state.changeList.workflowStatus || '—')}</span>` : ''}
          ${this._state.permissions ? `<span><strong>You</strong> ${escHtml(this._state.permissions.userId)}${this._state.permissions.isAuthor ? ' (author)' : ''}</span>` : ''}
          ${reviewHref ? `<span><a href="${escHtml(reviewHref)}" target="_blank" rel="noopener">Open review</a></span>` : ''}
        </div>`;
    },

    renderModeHint() {
      const el = document.getElementById('mode-hint');
      if (!el || !this._state.permissions) return;
      const p = this._state.permissions;
      el.className = 'so-mode-hint';
      if (p.editMode === 'suggest') {
        el.classList.add('so-suggest');
        el.textContent = 'Suggest-edit mode — changes are submitted as suggestions for the author to accept.';
      } else if (p.editMode === 'readonly') {
        el.classList.add('so-readonly');
        el.textContent = p.inReview
          ? 'Script is in review — read-only until withdrawn or approved.'
          : 'Read-only — you cannot edit this draft.';
      } else {
        el.textContent = 'Author edit mode — Save opens change list with Submit for review.';
      }
    },

    renderToolbarButtons() {
      const saveBtn = document.getElementById('save-btn');
      const suggestBtn = document.getElementById('suggest-btn');
      const tableReadBtn = document.getElementById('table-read-btn');
      const p = this._state.permissions || {};
      const hasDraft = !!(this._state.draft && this._state.activeId);
      if (saveBtn) {
        saveBtn.hidden = !hasDraft;
        saveBtn.disabled = !p.canSaveDirect;
        if (!p.canSaveDirect) {
          if (p.inReview) saveBtn.title = 'Script is in review — withdraw the change list to edit and save.';
          else if (p.committed) saveBtn.title = 'Draft is committed — read only.';
          else if (!p.isAuthor) saveBtn.title = 'Only the draft author can save — use Dev mode in the nav to switch user.';
          else saveBtn.title = 'Cannot save in the current mode.';
        } else {
          saveBtn.title = 'Save script edits (opens change list)';
        }
      }
      if (suggestBtn) suggestBtn.hidden = !p.canSuggestEdit;
      if (tableReadBtn) {
        const committed = !!(this._state.draft && this._state.draft.committedAt);
        tableReadBtn.hidden = !this._state.draftId || committed;
      }
      const autoAddBtn = document.getElementById('auto-add-lemmas-btn');
      const settingsLink = document.getElementById('auto-add-settings-link');
      const editable = !!(hasDraft && p.canSaveDirect);
      if (autoAddBtn) {
        autoAddBtn.hidden = !editable;
        autoAddBtn.disabled = !editable;
      }
      if (settingsLink) settingsLink.hidden = !hasDraft;
      this.renderSuggestionSubmitWrap();
      this.renderSuggestionReviewBar();
    },

    renderSuggestionSubmitWrap() {
      const wrap = document.getElementById('so-suggest-submit-wrap');
      if (!wrap) return;
      const p = this._state.permissions || {};
      wrap.hidden = !p.canSuggestEdit;
    },

    _mergedSuggestions() {
      const script = (this._state.suggestions || []).map((s) => ({ ...s, suggestionType: s.suggestionType || 'script' }));
      const binding = (this._state.bindingSuggestions || []).map((s) => ({ ...s, suggestionType: s.suggestionType || 'binding' }));
      return [...binding, ...script];
    },

    _suggestionSnippet(s) {
      if (!s) return '';
      if (s.suggestionType === 'binding') {
        const p = s.proposed || {};
        const sel = (p.selectionText || '').slice(0, 40).replace(/\n/g, ' ');
        return `Binding [${p.charStart ?? '?'}, ${p.charEnd ?? '?'}) "${sel}"`;
      }
      return (s.suggestedScriptText || '').slice(0, 60).replace(/\n/g, ' ');
    },

    async loadBindingSuggestions(draftId) {
      const res = await this.api(`/drafts/episodes/${draftId}/binding-suggestions?status=pending`).catch(() => ({ items: [] }));
      this._state.bindingSuggestions = res.items || [];
    },

    renderSuggestionReviewBar() {
      const el = document.getElementById('so-suggestion-review-bar');
      if (!el) return;
      const p = this._state.permissions || {};
      const items = this._mergedSuggestions();
      const sug = this._state.activeSuggestion;
      if (!p.canAcceptSuggestion || !items.length) {
        el.hidden = true;
        el.innerHTML = '';
        return;
      }
      el.hidden = false;
      const diff = this._state.suggestionDiff;
      const required = diff ? (diff.required || []).filter((i) => i.severity !== 'warning') : [];
      const warnings = diff
        ? (diff.warnings || []).concat((diff.required || []).filter((i) => i.severity === 'warning'))
        : [];
      const snippet = sug ? this._suggestionSnippet(sug) : '';
      const diffNote = sug && !diff && sug.suggestionType !== 'binding'
        ? '<p class="so-panel-empty">Diff could not be loaded — you can still accept or reject the suggested script text.</p>'
        : '';
      const selectHint = !sug
        ? '<p class="so-panel-empty">Select a suggestion above to review it.</p>'
        : `<p class="so-panel-empty" style="font-style:normal;color:#444">Reviewing ${sug.suggestionType === 'binding' ? 'binding edit' : 'suggestion'} from <strong>${escHtml(sug.suggestedBy || '')}</strong>: "${escHtml(snippet)}"</p>`;
      el.innerHTML = `
        <h3>Accept or reject suggestion</h3>
        ${selectHint}
        ${diffNote}
        ${sug ? `
        <ul class="so-ack-list" id="so-suggestion-review-acks">
          ${required.map((i, idx) =>
            `<li><label><input type="checkbox" data-idx="${idx}" data-kind="required"/> ${escHtml(i.description)}</label></li>`,
          ).join('') || '<li><em>No required acknowledgments</em></li>'}
        </ul>
        ${warnings.length ? `<details><summary>Warnings (${warnings.length})</summary><ul>${warnings.map((w) => `<li>${escHtml(w.description)}</li>`).join('')}</ul></details>` : ''}
        <div class="so-review-actions">
          <button type="button" id="so-review-accept-btn"${sug ? '' : ' disabled'}>Accept suggestion</button>
          <button type="button" id="so-review-reject-btn"${sug ? '' : ' disabled'}>Reject</button>
        </div>` : ''}`;
      if (sug) {
        this._state._suggestionAckState = required.map((i) => ({ ...i }));
        const acceptBtn = el.querySelector('#so-review-accept-btn');
        const rejectBtn = el.querySelector('#so-review-reject-btn');
        if (acceptBtn) acceptBtn.onclick = () => this.acceptSuggestion();
        if (rejectBtn) rejectBtn.onclick = () => this.rejectSuggestion();
        el.querySelectorAll('#so-suggestion-review-acks input').forEach((inp) => {
          inp.onchange = () => {
            const idx = +inp.dataset.idx;
            if (this._state._suggestionAckState[idx]) {
              this._state._suggestionAckState[idx].userAcknowledged = inp.checked;
            }
          };
        });
      }
    },

    renderSuggestionsList() {
      const el = document.getElementById('so-suggestions');
      const section = document.getElementById('so-suggestions-section');
      if (!el) return;
      const items = this._mergedSuggestions();
      const p = this._state.permissions || {};
      if (!items.length) {
        if (section) section.hidden = true;
        el.innerHTML = '';
        return;
      }
      if (section) section.hidden = false;
      el.innerHTML = items.map((s) => {
        const snippet = this._suggestionSnippet(s);
        const active = this._state.activeSuggestion && this._state.activeSuggestion.id === s.id;
        const kindLabel = s.suggestionType === 'binding' ? '<span class="so-meta-line">Binding edit · </span>' : '';
        const actions = p.canAcceptSuggestion
          ? `<div class="so-suggestion-actions">
              <button type="button" class="so-accept-inline" data-id="${escHtml(s.id)}">Accept</button>
              <button type="button" class="so-reject-inline" data-id="${escHtml(s.id)}">Reject</button>
            </div>`
          : (p.canSuggestEdit
            ? '<p class="so-panel-empty" style="margin:4px 0 0">Awaiting author review.</p>'
            : '');
        return `<div class="so-suggestion-card${active ? ' is-active' : ''}" data-id="${escHtml(s.id)}" role="button" tabindex="0">
          ${kindLabel}<span class="so-snippet">"${escHtml(snippet)}"</span>
          <span class="so-meta-line">${escHtml(s.suggestedBy)} · ${escHtml(s.createdAt || '')}</span>
          ${actions}
        </div>`;
      }).join('');
      el.querySelectorAll('.so-suggestion-card').forEach((card) => {
        const open = () => this.selectSuggestion(card.dataset.id);
        card.onclick = (ev) => {
          if (ev.target.closest('.so-suggestion-actions')) return;
          open();
        };
        card.onkeydown = (ev) => { if (ev.key === 'Enter') open(); };
      });
      el.querySelectorAll('.so-accept-inline').forEach((btn) => {
        btn.onclick = async (ev) => {
          ev.stopPropagation();
          await this.selectSuggestion(btn.dataset.id);
          if (this._state.suggestionDiff && !this._checkSuggestionAcks()) return;
          await this.acceptSuggestion();
        };
      });
      el.querySelectorAll('.so-reject-inline').forEach((btn) => {
        btn.onclick = async (ev) => {
          ev.stopPropagation();
          await this.selectSuggestion(btn.dataset.id);
          await this.rejectSuggestion();
        };
      });
    },

    async selectSuggestion(id) {
      const s = this._mergedSuggestions().find((x) => x.id === id);
      if (!s) return;
      this._state.activeSuggestion = s;
      this.renderSuggestionsList();
      try {
        if (s.suggestionType === 'binding') {
          const preview = (s.proposed && s.proposed._preview) || {};
          const diff = {
            required: preview.required || [],
            warnings: preview.warnings || [],
          };
          this._state.suggestionDiff = Ack
            ? Ack.mergeAckIntoChangeListData(diff, this._state.changeList)
            : diff;
        } else {
          const diff = await this.api(
            `/drafts/episodes/${this._state.activeId}/script-suggestions/${id}/diff`,
          );
          this._state.suggestionDiff = Ack
            ? Ack.mergeAckIntoChangeListData(diff, this._state.changeList)
            : diff;
        }
      } catch (e) {
        this._state.suggestionDiff = null;
        this.setStatus(e.message, true);
      }
      this.renderDiffPanel();
      this.renderAcceptBar();
      this.renderSuggestionReviewBar();
      this.renderSuggestionComments();
      const acceptSection = document.getElementById('so-accept-section');
      if (acceptSection && this._state.permissions?.canAcceptSuggestion) {
        acceptSection.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }
    },

    renderDiffPanel() {
      const el = document.getElementById('so-diffs');
      const section = document.getElementById('so-diffs-section');
      if (!el) return;
      const diff = this._state.suggestionDiff;
      if (!diff || !this._state.activeSuggestion) {
        if (section) section.hidden = true;
        el.innerHTML = '';
        return;
      }
      if (section) section.hidden = false;
      const items = [...(diff.required || []), ...(diff.warnings || [])];
      if (!items.length) {
        el.innerHTML = '<p class="so-panel-empty">No binding impacts from this suggestion.</p>';
        return;
      }
      el.innerHTML = items.map((i) =>
        `<div class="so-diff-card ${i.severity === 'required' ? 'so-diff-required' : 'so-diff-warning'}">
          <strong>${escHtml(i.severity)}</strong>
          <span>${escHtml(i.description || i.itemType)}</span>
        </div>`,
      ).join('');
    },

    renderAcceptBar() {
      const el = document.getElementById('so-accept-bar');
      const section = document.getElementById('so-accept-section');
      if (!el) return;
      const p = this._state.permissions || {};
      const diff = this._state.suggestionDiff;
      const sug = this._state.activeSuggestion;
      if (!p.canAcceptSuggestion || !sug) {
        if (section) section.hidden = true;
        el.innerHTML = '';
        return;
      }
      if (section) section.hidden = false;
      const required = diff ? (diff.required || []).filter((i) => i.severity !== 'warning') : [];
      const warnings = diff
        ? (diff.warnings || []).concat((diff.required || []).filter((i) => i.severity === 'warning'))
        : [];
      const diffNote = diff
        ? ''
        : '<p class="so-panel-empty">Diff could not be loaded — you can still accept or reject the suggested script text.</p>';
      el.innerHTML = `
        ${diffNote}
        <ul class="so-ack-list" id="so-suggestion-acks">
          ${required.map((i, idx) =>
            `<li><label><input type="checkbox" data-idx="${idx}" data-kind="required"/> ${escHtml(i.description)}</label></li>`,
          ).join('') || '<li><em>No required acknowledgments</em></li>'}
        </ul>
        <div>
          ${warnings.length ? `<details><summary>Warnings (${warnings.length})</summary><ul>${warnings.map((w) => `<li>${escHtml(w.description)}</li>`).join('')}</ul></details>` : ''}
          <button type="button" id="so-accept-btn">Accept suggestion</button>
          <button type="button" id="so-reject-btn">Reject</button>
        </div>`;
      this._state._suggestionAckState = required.map((i) => ({ ...i }));
      document.getElementById('so-accept-btn').onclick = () => this.acceptSuggestion();
      document.getElementById('so-reject-btn').onclick = () => this.rejectSuggestion();
      el.querySelectorAll('#so-suggestion-acks input').forEach((inp) => {
        inp.onchange = () => {
          const idx = +inp.dataset.idx;
          if (this._state._suggestionAckState[idx]) {
            this._state._suggestionAckState[idx].userAcknowledged = inp.checked;
          }
        };
      });
    },

    _checkSuggestionAcks() {
      const unchecked = (this._state._suggestionAckState || []).filter(
        (i) => i.severity !== 'warning' && !i.userAcknowledged,
      );
      if (unchecked.length) {
        alert('Acknowledge all required items before continuing');
        return false;
      }
      return true;
    },

    async acceptSuggestion() {
      if (!this._checkSuggestionAcks()) return;
      const sug = this._state.activeSuggestion;
      if (!sug) return;
      const id = sug.id;
      const isBinding = sug.suggestionType === 'binding';
      const path = isBinding
        ? `/drafts/episodes/${this._state.activeId}/binding-suggestions/${id}`
        : `/drafts/episodes/${this._state.activeId}/script-suggestions/${id}`;
      try {
        const result = await this.api(path, {
          method: 'PATCH',
          body: JSON.stringify({ action: 'accept' }),
        });
        this.setStatus(isBinding ? 'Binding suggestion accepted' : 'Suggestion accepted');
        if (isBinding && result && (result.changeListId || (result.required && result.required.length))) {
          await this.loadDraft();
          this.openChangeListModal(result);
          return;
        }
        await this.loadDraft();
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async rejectSuggestion() {
      const sug = this._state.activeSuggestion;
      if (!sug) return;
      const id = sug.id;
      const isBinding = sug.suggestionType === 'binding';
      const path = isBinding
        ? `/drafts/episodes/${this._state.activeId}/binding-suggestions/${id}`
        : `/drafts/episodes/${this._state.activeId}/script-suggestions/${id}`;
      try {
        await this.api(path, {
          method: 'PATCH',
          body: JSON.stringify({ action: 'reject' }),
        });
        this.setStatus(isBinding ? 'Binding suggestion rejected' : 'Suggestion rejected');
        this._state.activeSuggestion = null;
        this._state.suggestionDiff = null;
        await this.loadDraft();
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    renderChangeListInline() {
      const el = document.getElementById('so-change-list');
      if (!el) return;
      const cl = this._state.changeList;
      if (!cl || !cl.id) {
        el.innerHTML = '<p class="so-panel-empty">No active change list.</p>';
        return;
      }
      const items = cl.items || [];
      const required = items.filter((i) => i.severity === 'required');
      const warnings = items.filter((i) => i.severity !== 'required');
      const p = this._state.permissions || {};
      const blocked = p.inReview;
      const canEdit = !blocked && p.canSaveDirect;
      el.innerHTML = `
        <div class="so-cl-toolbar">
          <h3 style="margin:0">Change list <small>(${escHtml(cl.workflowStatus || 'in_progress')}, rev ${cl.revision || 0})</small></h3>
          ${canEdit ? '<button type="button" id="so-cl-revert" class="so-cl-revert-btn">Revert</button>' : ''}
        </div>
        <p style="font-size:12px;color:#666">${escHtml(cl.id)}</p>
        <h4>Required</h4>
        <ul class="so-cl-required">${required.map((i) =>
          `<li><label><input type="checkbox" data-cl-id="${escHtml(i.id)}" ${i.userAcknowledged ? 'checked' : ''} ${blocked ? 'disabled' : ''}/> ${escHtml(i.description || i.itemType)}</label></li>`,
        ).join('') || '<li><em>None</em></li>'}</ul>
        <details><summary>Warnings (${warnings.length})</summary><ul>${warnings.map((i) => `<li>${escHtml(i.description || '')}</li>`).join('') || '<li>None</li>'}</ul></details>
        <div class="so-cl-actions">
          ${canEdit ? '<button type="button" id="so-cl-save">Save change list</button>' : ''}
          ${canEdit && p.canSubmitChangeList ? '<button type="button" id="so-cl-submit">Submit for review</button>' : ''}
          ${!blocked && cl.workflowStatus === 'in_review' ? '<button type="button" id="so-cl-withdraw">Withdraw</button>' : ''}
        </div>
        ${blocked ? '<p style="font-size:13px;color:#666;margin-top:8px">In review — withdraw from Review page to edit.</p>' : ''}`;

      const saveBtn = el.querySelector('#so-cl-save');
      if (saveBtn) saveBtn.onclick = () => this.saveChangeListInline();
      const submitBtn = el.querySelector('#so-cl-submit');
      if (submitBtn) submitBtn.onclick = () => this.submitChangeListInline();
      const revertBtn = el.querySelector('#so-cl-revert');
      if (revertBtn) revertBtn.onclick = () => this.revertToSnapshot();
      const withdrawBtn = el.querySelector('#so-cl-withdraw');
      if (withdrawBtn) {
        withdrawBtn.onclick = async () => {
          await this.api(`/localization/change-lists/${cl.id}/withdraw`, { method: 'POST', body: '{}' });
          await this.loadDraft();
        };
      }
    },

    async reloadChangeList() {
      const draftId = this._state.activeId;
      if (!draftId) return;
      const clRes = await this.api(`/localization/change-lists?draftEpisodeId=${encodeURIComponent(draftId)}`).catch(() => null);
      this._state.changeList = clRes && clRes.id ? clRes : (clRes && clRes.item ? clRes.item : null);
      this.renderChangeListInline();
      this.renderHeader();
    },

    async saveChangeListInline() {
      const cl = this._state.changeList;
      const el = document.getElementById('so-change-list');
      if (!cl || !cl.id || !el) return;
      const required = (cl.items || []).filter((i) => i.severity === 'required' && !i._synthetic);
      const unchecked = required.filter((i) => {
        const cb = el.querySelector(`input[data-cl-id="${CSS.escape(i.id)}"]`);
        return cb ? !cb.checked : !i.userAcknowledged;
      });
      if (unchecked.length) {
        this.setStatus('Acknowledge all required items before save', true);
        return;
      }
      try {
        await this.api(`/localization/change-lists/${cl.id}/save`, {
          method: 'POST',
          body: changeListSaveBody({ required: cl.items.filter((i) => i.severity === 'required'), warnings: cl.items.filter((i) => i.severity !== 'required') }, el),
        });
        await this.reloadChangeList();
        this.setStatus('Change list saved');
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async submitChangeListInline() {
      const cl = this._state.changeList;
      const el = document.getElementById('so-change-list');
      if (!cl || !cl.id || !el) return;
      const required = (cl.items || []).filter((i) => i.severity === 'required' && !i._synthetic);
      const unchecked = required.filter((i) => {
        const cb = el.querySelector(`input[data-cl-id="${CSS.escape(i.id)}"]`);
        return cb ? !cb.checked : !i.userAcknowledged;
      });
      if (unchecked.length) {
        this.setStatus('Acknowledge all required items before submit', true);
        return;
      }
      try {
        await this.api(`/localization/change-lists/${cl.id}/save`, {
          method: 'POST',
          body: changeListSaveBody({ required: cl.items.filter((i) => i.severity === 'required'), warnings: cl.items.filter((i) => i.severity !== 'required') }, el),
        });
        await this.api(`/localization/change-lists/${cl.id}/submit-for-review`, { method: 'POST', body: '{}' });
        this.setStatus('Submitted for review');
        await this.loadDraft();
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async revertToSnapshot() {
      const draftId = this._state.activeId;
      const snapshot = this._state.scriptSnapshot;
      if (!draftId || snapshot == null) return;
      const current = global.ContinuuuumScriptEditor.getValue(this._state.editorInst);
      if (current === snapshot && !(this._state.changeList && (this._state.changeList.items || []).length)) {
        this.setStatus('Nothing to revert');
        return;
      }
      if (!global.confirm('Revert script and pending change list to the last saved version?')) return;
      try {
        await this.api(`/drafts/episodes/${draftId}/script`, {
          method: 'PUT',
          body: JSON.stringify({ scriptText: snapshot }),
        });
        if (this._state.editorInst) {
          global.ContinuuuumScriptEditor.setScriptText(this._state.editorInst, snapshot);
          this._state.editorInst.overlaySnapshotText = snapshot;
          this._state.editorInst._overlaySpanSig = null;
        }
        await this.loadClauseBindings(draftId);
        this._refreshEditorBindings();
        await this.loadDraft();
        this.setStatus('Reverted to last saved script');
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    renderComments() {
      const el = document.getElementById('so-comments');
      if (!el) return;
      const general = (this._state.comments || []).filter((c) => c.commentType !== 'suggestion');
      el.innerHTML = general.map((c) => this._commentHtml(c)).join('') || '<p class="so-panel-empty">No comments this cycle.</p>';
      const form = document.getElementById('so-comment-form');
      if (form) form.hidden = !(this._state.permissions && this._state.permissions.canComment);
    },

    renderSuggestionComments() {
      const el = document.getElementById('so-suggestion-comments');
      if (!el || !this._state.activeSuggestion) return;
      const linked = (this._state.comments || []).filter(
        (c) => c.scriptSuggestionId === this._state.activeSuggestion.id,
      );
      el.innerHTML = linked.length
        ? linked.map((c) => this._commentHtml(c)).join('')
        : '<p class="so-panel-empty">No comments on this suggestion.</p>';
    },

    _commentHtml(c) {
      const chips = [];
      if (c.sourcePage && c.sourcePage !== 'script_output') {
        chips.push(`<span class="so-chip">${escHtml(c.sourcePage)}</span>`);
      }
      if (c.linkedCommentId) {
        chips.push(`<span class="so-chip">linked</span>`);
      }
      return `<div class="so-comment">${chips.join('')} ${escHtml(c.commentText)}
        <small style="color:#888"> — ${escHtml(c.authorUserId || '')} ${escHtml(c.createdAt || '')}</small></div>`;
    },

    renderArchivedComments() {
      const el = document.getElementById('so-old-comments');
      if (!el) return;
      el.innerHTML = (this._state.archivedComments || []).map((c) =>
        `<li>${escHtml(c.commentText)} <small>(${escHtml(c.archivedAt || '')})</small></li>`,
      ).join('') || '<li><em>None</em></li>';
    },

    renderArchivedSuggestions() {
      const el = document.getElementById('so-old-suggestions');
      if (!el) return;
      el.innerHTML = (this._state.archivedSuggestions || []).map((s) =>
        `<li>${escHtml(s.status)} by ${escHtml(s.suggestedBy)} — "${escHtml((s.suggestedScriptText || '').slice(0, 40))}" <small>(${escHtml(s.archivedAt || s.resolvedAt || '')})</small></li>`,
      ).join('') || '<li><em>None</em></li>';
    },

    mountEditor(text) {
      const host = document.getElementById('editor-host');
      if (!host) return;
      host.innerHTML = '';
      const p = this._state.permissions || {};
      const readOnly = p.editMode === 'readonly';
      this._state.editorInst = global.ContinuuuumScriptEditor.mount(host, {
        overlaySnapshotText: this._state.scriptSnapshot,
        scriptText: text,
        draftId: this._state.activeId,
        draftEpisodeId: this._state.activeId,
        draftScriptId: this._state.draftScriptId || undefined,
        mode: readOnly ? 'review' : 'edit',
        readOnly,
        permissions: p,
        clausePanelHost: '#so-lemma-panel',
        clauseBindings: this._state.clauseBindings,
        onBindingsChanged: async () => {
          await this.loadClauseBindings(this._state.activeId);
          this._refreshEditorBindings();
        },
        onBindingEdited: async (editRes) => {
          if (editRes && editRes.suggestion) {
            this.setStatus('Binding edit submitted as a suggestion for the author to accept.');
            await this.loadBindingSuggestions(this._state.activeId);
            this.renderSuggestionsList();
            return;
          }
          await this.loadClauseBindings(this._state.activeId);
          this._refreshEditorBindings();
          this.openChangeListModal(editRes);
        },
      });
    },

    _refreshEditorBindings() {
      const inst = this._state.editorInst;
      if (!inst) return;
      inst.options.clauseBindings = this._state.clauseBindings;
      inst._overlaySpanSig = null;
      const SE = global.ContinuuuumScriptEditor;
      SE.renderOverlays(inst, inst.options);
      SE.renderClausePanel(inst);
      if (SE.renderClauseSuggestions) SE.renderClauseSuggestions(inst);
      if (SE.updateToolbarActions) SE.updateToolbarActions(inst);
      this.renderHeader();
    },

    async autoAddAllSingleLemmas() {
      const LemmaAuto = global.ContinuuuumScriptLemmaAuto;
      if (!LemmaAuto) {
        this.setStatus('Auto-add module not loaded', true);
        return;
      }
      try {
        await LemmaAuto.autoAddAllSingleLemmas({
          draftId: this._state.activeId,
          activeId: this._state.activeId,
          api: (path, opts) => this.api(path, opts),
          getScriptText: () => global.ContinuuuumScriptEditor.getValue(this._state.editorInst),
          loadClauseBindings: (id) => this.loadClauseBindings(id),
          refreshEditor: async () => this._refreshEditorBindings(),
          onComplete: async () => {
            await this.reloadChangeList();
          },
          setStatus: (msg, isError) => this.setStatus(msg, isError),
        });
      } catch (e) {
        this.setStatus(e.message || 'Auto-add failed', true);
      }
    },

    openChangeListModal(editRes, afterRefresh) {
      const cl = this._state.changeList;
      const payload = Ack ? Ack.mergeAckIntoChangeListData(editRes, cl) : editRes;
      if (Ack) payload._changeListForAck = cl;
      if (!payload.changeListId && !(payload.required && payload.required.length) && !(payload.warnings && payload.warnings.length)) {
        if (afterRefresh) afterRefresh();
        return;
      }
      global.ContinuuuumChangeListModal.open(payload.changeListId, payload, {
        onSave: async (clId, clData) => {
          if (clId) {
            await this.api(`/localization/change-lists/${clId}/save`, {
              method: 'POST',
              body: changeListSaveBody(clData),
            });
          }
          if (afterRefresh) await afterRefresh();
          await this.reloadChangeList();
          this.setStatus('Change list saved');
        },
        onSubmit: async (clId, clData) => {
          if (clId) {
            await this.api(`/localization/change-lists/${clId}/save`, {
              method: 'POST',
              body: changeListSaveBody(clData),
            });
            await this.api(`/localization/change-lists/${clId}/submit-for-review`, { method: 'POST', body: '{}' });
          }
          this.setStatus('Submitted for review');
          await this.loadDraft();
        },
        onWithdraw: async (clId) => {
          await this.api(`/localization/change-lists/${clId}/withdraw`, { method: 'POST', body: '{}' });
          await this.loadDraft();
        },
        onRevert: async () => {
          await this.revertToSnapshot();
        },
      });
    },

    async loadDraft() {
      const draftInp = document.getElementById('draft-id');
      const epInp = document.getElementById('episode-id');
      let draftId = draftInp && draftInp.value.trim();
      const episodeId = epInp && epInp.value.trim();
      this.setStatus('Loading…');
      try {
        if (!draftId && episodeId) {
          draftId = await this.resolveDraftForEpisode(episodeId);
          if (draftInp) draftInp.value = draftId;
        }
        if (!draftId) {
          this.setStatus('Enter a Draft ID or Episode ID', true);
          return;
        }
        this._state.loadMode = 'draft';
        this._state.activeId = draftId;
        this._state.draftId = draftId;

        const [draft, scriptRes, clRes, reviewsRes, sugRes, bindSugRes, archSugRes, commRes] = await Promise.all([
          this.api(`/drafts/episodes/${draftId}`),
          this.api(`/drafts/episodes/${draftId}/script`).catch(() => ({ scriptText: '', language: 'en' })),
          this.api(`/localization/change-lists?draftEpisodeId=${encodeURIComponent(draftId)}`).catch(() => null),
          this.api(`/drafts/episodes/${draftId}/reviews`).catch(() => ({ items: [] })),
          this.api(`/drafts/episodes/${draftId}/script-suggestions?status=pending`).catch(() => ({ items: [] })),
          this.api(`/drafts/episodes/${draftId}/binding-suggestions?status=pending`).catch(() => ({ items: [] })),
          this.api(`/drafts/episodes/${draftId}/script-suggestions?status=archived`).catch(() => ({ items: [] })),
          this.api(`/drafts/episodes/${draftId}/comments?sourcePage=script_output&includeArchived=true`).catch(() => ({ items: [], archived: [] })),
        ]);

        this._state.draft = draft;
        const scripts = draft.scripts || [];
        this._state.scriptMeta = scripts[0] || scriptRes;
        this._state.draftScriptId = scriptRes.id || (scripts[0] && scripts[0].id);
        this._state.scriptSnapshot = scriptRes.scriptText || '';
        this._state.changeList = clRes && clRes.id ? clRes : (clRes && clRes.item ? clRes.item : null);
        this._state.reviews = reviewsRes.items || [];
        this._state.suggestions = sugRes.items || [];
        this._state.bindingSuggestions = bindSugRes.items || [];
        this._state.archivedSuggestions = archSugRes.items || [];
        this._state.comments = commRes.items || [];
        this._state.archivedComments = commRes.archived || [];
        this._state.activeSuggestion = null;
        this._state.suggestionDiff = null;

        this._state.permissions = Permissions
          ? Permissions.resolveScriptPermissions({
            draft,
            changeList: this._state.changeList,
            userId: this._getUserId(),
            review: (this._state.reviews || [])[0],
          })
          : { canSaveDirect: true, canSuggestEdit: false, editMode: 'author', inReview: false, canComment: true, isAuthor: true };

        await this.loadClauseBindings(draftId);
        this.renderHeader();
        this.renderModeHint();
        this.renderToolbarButtons();
        this.renderSuggestionsList();
        if (this._state.permissions.canAcceptSuggestion && this._mergedSuggestions().length && !this._state.activeSuggestion) {
          await this.selectSuggestion(this._mergedSuggestions()[0].id);
        } else {
          this.renderSuggestionReviewBar();
        }
        this.mountEditor(this._state.scriptSnapshot);
        this.renderDiffPanel();
        this.renderAcceptBar();
        this.renderChangeListInline();
        this.renderComments();
        this.renderArchivedComments();
        this.renderArchivedSuggestions();
        this.renderSuggestionComments();

        const n = this._state.clauseBindings.length;
        this.setStatus(`Loaded — ${n === 1 ? '1 clause binding' : `${n} clause bindings`}.`);
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async saveScript() {
      if (!this._state.activeId || !this._state.permissions || !this._state.permissions.canSaveDirect) {
        this.setStatus('Cannot save — no permission', true);
        return;
      }
      const newText = global.ContinuuuumScriptEditor.getValue(this._state.editorInst);
      try {
        const editRes = await this.api(`/scripts/${this._state.activeId}/apply-edit`, {
          method: 'POST',
          body: JSON.stringify({ oldText: this._state.scriptSnapshot, newText }),
        });
        const cl = this._state.changeList;
        const payload = Ack ? Ack.mergeAckIntoChangeListData(editRes, cl) : editRes;
        if (Ack) payload._changeListForAck = cl;
        global.ContinuuuumChangeListModal.open(payload.changeListId, payload, {
          onSave: async (clId, clData) => {
            await this.api(`/drafts/episodes/${this._state.activeId}/script`, {
              method: 'PUT',
              body: JSON.stringify({ scriptText: newText }),
            });
            if (clId) {
              await this.api(`/localization/change-lists/${clId}/save`, {
                method: 'POST',
                body: changeListSaveBody(clData),
              });
            }
            this._state.scriptSnapshot = newText;
            if (this._state.editorInst) this._state.editorInst.overlaySnapshotText = newText;
            await this.loadClauseBindings(this._state.activeId);
            this._refreshEditorBindings();
            this.setStatus('Draft saved');
            await this.reloadChangeList();
          },
          onSubmit: async (clId, clData) => {
            if (clId) {
              await this.api(`/localization/change-lists/${clId}/save`, {
                method: 'POST',
                body: changeListSaveBody(clData),
              });
              await this.api(`/localization/change-lists/${clId}/submit-for-review`, { method: 'POST', body: '{}' });
            }
            this.setStatus('Submitted for review');
            await this.loadDraft();
          },
          onRevert: async () => {
            await this.revertToSnapshot();
          },
        });
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async submitSuggestion() {
      if (!this._state.activeId || !this._state.permissions || !this._state.permissions.canSuggestEdit) {
        this.setStatus('Cannot submit suggestion', true);
        return;
      }
      const newText = global.ContinuuuumScriptEditor.getValue(this._state.editorInst);
      const commentInp = document.getElementById('suggest-comment');
      const commentText = commentInp ? commentInp.value.trim() : '';
      try {
        await this.api(`/drafts/episodes/${this._state.activeId}/script-suggestions`, {
          method: 'POST',
          body: JSON.stringify({
            suggestedScriptText: newText,
            commentText: commentText || undefined,
          }),
        });
        if (commentInp) commentInp.value = '';
        this.setStatus('Suggestion submitted');
        await this.loadDraft();
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },

    async addComment() {
      const ta = document.getElementById('so-new-comment');
      const text = ta && ta.value.trim();
      if (!text) return;
      try {
        await this.api(`/drafts/episodes/${this._state.activeId}/comments`, {
          method: 'POST',
          body: JSON.stringify({
            commentText: text,
            sourcePage: 'script_output',
            commentType: 'general',
          }),
        });
        if (ta) ta.value = '';
        const commRes = await this.api(
          `/drafts/episodes/${this._state.activeId}/comments?sourcePage=script_output&includeArchived=true`,
        );
        this._state.comments = commRes.items || [];
        this._state.archivedComments = commRes.archived || [];
        this.renderComments();
        this.renderArchivedComments();
      } catch (e) {
        this.setStatus(e.message, true);
      }
    },
  };

  global.ContinuuuumScriptOutput = ContinuuuumScriptOutput;
})(typeof window !== 'undefined' ? window : globalThis);
