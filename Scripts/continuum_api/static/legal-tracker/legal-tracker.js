(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount({ root: '#continuum-nav-root', app: 'legal-tracker' });
  }

  var caveShell = window.ContinuumCaveShell
    ? window.ContinuumCaveShell.init({ tomeId: 'legal-tracker-tome', presence: false })
    : null;

  function caveMsg(message, payload) {
    if (!caveShell) return Promise.reject(new Error('ContinuumCaveShell not loaded'));
    return caveShell.caveMessage(message, payload || {});
  }

  if (window.ContinuumUserSession) {
    var stored = localStorage.getItem('continuumUserId');
    if (stored) ContinuumUserSession.setUserId(stored);
  }

  function userId() {
    if (window.ContinuumUserSession) return ContinuumUserSession.getUserId();
    return localStorage.getItem('continuumUserId') || 'editor';
  }
  var modalBody = document.getElementById('case-modal-body');
  var modalTitle = document.getElementById('case-modal-title');
  var currentCaseId = null;
  var currentCaseData = null;

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  var modalOverlay = document.getElementById('case-modal-overlay');

  function caseUrl(caseId) {
    return '/legal-tracker/?case=' + encodeURIComponent(caseId);
  }

  function setCaseQuery(caseId, replace) {
    var url = new URL(location.href);
    if (caseId) {
      url.searchParams.set('case', caseId);
    } else {
      url.searchParams.delete('case');
    }
    var method = replace ? 'replaceState' : 'pushState';
    history[method]({ caseId: caseId || null }, '', url.pathname + url.search);
  }

  function openModal() {
    modalOverlay.classList.add('open');
    modalOverlay.setAttribute('aria-hidden', 'false');
  }

  function closeModal() {
    modalOverlay.classList.remove('open');
    modalOverlay.setAttribute('aria-hidden', 'true');
    modalBody.innerHTML = '';
    currentCaseId = null;
    currentCaseData = null;
    setCaseQuery(null, true);
  }

  modalOverlay.addEventListener('click', function (ev) {
    if (ev.target === modalOverlay) closeModal();
  });
  document.getElementById('case-modal-close').onclick = closeModal;
  document.addEventListener('keydown', function (ev) {
    if (ev.key === 'Escape' && modalOverlay.classList.contains('open')) closeModal();
  });

  function renderEditForm(caseData) {
    return (
      '<section class="case-edit"><h3>Edit case</h3>' +
      '<label>Title<input id="edit-title" value="' + esc(caseData.title || '') + '" /></label>' +
      '<label>Status<select id="edit-status">' +
      ['open', 'in_review', 'resolved', 'closed'].map(function (s) {
        return '<option value="' + s + '"' + ((caseData.status || '') === s ? ' selected' : '') + '>' + s + '</option>';
      }).join('') +
      '</select></label>' +
      '<label>Severity<select id="edit-severity">' +
      ['critical', 'high', 'medium', 'low'].map(function (s) {
        return '<option value="' + s + '"' + ((caseData.severity || '').toLowerCase() === s ? ' selected' : '') + '>' + s + '</option>';
      }).join('') +
      '</select></label>' +
      '<label>Category<input id="edit-category" value="' + esc(caseData.category || '') + '" /></label>' +
      '<label>Assigned to<input id="edit-assigned" value="' + esc(caseData.assignedTo || caseData.assigned_to || '') + '" /></label>' +
      '<label>Description<textarea id="edit-description" rows="3">' + esc(caseData.description || '') + '</textarea></label>' +
      '<button type="button" id="btn-save-case">Save changes</button></section>'
    );
  }

  function renderReviewSection(caseData, codeLines) {
    var resolutions = caseData.resolutions || [];
    return (
      '<section class="case-review"><h3>Review</h3>' +
      '<p class="hint">Add resolutions and code refs below. Document review UI integrates at the bottom of this panel.</p>' +
      (resolutions.length
        ? resolutions.map(function (r) {
          return '<div class="resolution"><strong>' + esc(r.resolution_type || r.resolutionType) + '</strong>' +
            ' · ' + esc(r.resolved_at || r.resolvedAt || '') +
            '<div>' + esc(r.summary) + '</div></div>';
        }).join('')
        : '<p><em>No resolutions yet</em></p>') +
      (codeLines.length
        ? '<h4 style="font-size:0.9rem">Code line refs</h4>' +
          codeLines.map(function (l) {
            return '<div class="code-ref">' + esc(l.repo) + ':' + esc(l.file_path) + ':' +
              esc(l.start_line) + '-' + esc(l.end_line) +
              (l.note ? ' — ' + esc(l.note) : '') + '</div>';
          }).join('')
        : '') +
      '<h4 style="font-size:0.9rem;margin-top:0.75rem">Add resolution</h4>' +
      '<label>Summary<textarea id="resolution-summary" rows="2" placeholder="Resolution summary"></textarea></label>' +
      '<label>Type<select id="resolution-type"><option value="fix">fix</option><option value="waiver">waiver</option><option value="defer">defer</option></select></label>' +
      '<button type="button" id="btn-add-resolution">Add resolution</button>' +
      '<p style="margin-top:0.75rem"><a href="/continuum_editor/#legal" target="_blank" rel="noopener">Open full document review (Continuum editor)</a></p>' +
      '</section>'
    );
  }

  function renderCaseModal(caseData, codeLines) {
    currentCaseData = caseData;
    modalTitle.textContent = caseData.title || caseData.id;
    var sev = (caseData.severity || '').toLowerCase();
    var patents = caseData.patentRefs || caseData.patent_refs || [];

    modalBody.innerHTML =
      '<p class="case-meta"><span class="sev-' + esc(sev) + '">' + esc(caseData.severity) + '</span>' +
      ' · ' + esc(caseData.status) + ' · ' + esc(caseData.category) + '</p>' +
      '<dl class="case-meta">' +
      '<dt>Case ID</dt><dd>' + esc(caseData.id) + '</dd>' +
      (caseData.slug ? '<dt>Slug</dt><dd>' + esc(caseData.slug) + '</dd>' : '') +
      (caseData.featureKey || caseData.feature_key
        ? '<dt>Feature key</dt><dd><code>' + esc(caseData.featureKey || caseData.feature_key) + '</code></dd>'
        : '') +
      (caseData.saurceProductId || caseData.saurce_product_id
        ? '<dt>Saurce product</dt><dd>' + esc(caseData.saurceProductId || caseData.saurce_product_id) + '</dd>'
        : '') +
      (caseData.opened_at ? '<dt>Opened</dt><dd>' + esc(caseData.opened_at) + '</dd>' : '') +
      (patents.length ? '<dt>Patent refs</dt><dd>' + esc(patents.join(', ')) + '</dd>' : '') +
      '</dl>' +
      renderEditForm(caseData) +
      renderReviewSection(caseData, codeLines || []);

    document.getElementById('btn-save-case').onclick = saveCaseEdits;
    document.getElementById('btn-add-resolution').onclick = addResolution;
  }

  async function saveCaseEdits() {
    if (!currentCaseId) return;
    var body = {
      title: document.getElementById('edit-title').value,
      status: document.getElementById('edit-status').value,
      severity: document.getElementById('edit-severity').value,
      category: document.getElementById('edit-category').value,
      assignedTo: document.getElementById('edit-assigned').value,
      description: document.getElementById('edit-description').value,
    };
    try {
      await caveMsg('patch_legal_case', Object.assign({ case_id: currentCaseId }, body));
      await openCaseModal(currentCaseId, true);
      load().catch(console.error);
    } catch (e) {
      alert('Save failed: ' + e.message);
    }
  }

  async function addResolution() {
    if (!currentCaseId) return;
    var summary = document.getElementById('resolution-summary').value.trim();
    if (!summary) return;
    try {
      await caveMsg('legal_case_resolution', {
        case_id: currentCaseId,
        summary: summary,
        resolutionType: document.getElementById('resolution-type').value,
        resolvedBy: userId(),
      });
      document.getElementById('resolution-summary').value = '';
      await openCaseModal(currentCaseId, true);
    } catch (e) {
      alert('Resolution failed: ' + e.message);
    }
  }

  async function openCaseModal(caseId, replaceHistory) {
    if (!caseId) return;
    currentCaseId = caseId;
    modalBody.innerHTML = '<p>Loading…</p>';
    modalTitle.textContent = 'Legal case';
    openModal();
    setCaseQuery(caseId, !!replaceHistory);

    try {
      var detail = await caveMsg('get_legal_case', { case_id: caseId });
      var codeRes = await caveMsg('legal_case_code_lines', { case_id: caseId }).catch(function () {
        return { items: [] };
      });
      renderCaseModal(detail, codeRes.items || []);
    } catch (e) {
      modalBody.innerHTML = '<p class="errors">Failed to load case: ' + esc(e.message) + '</p>';
    }
  }

  function bindCaseLinks() {
    document.querySelectorAll('a.case-title-link, a.gate-case-link').forEach(function (a) {
      a.onclick = function (ev) {
        if (ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.button === 1) return;
        ev.preventDefault();
        openCaseModal(a.getAttribute('data-case-id') || '', false);
      };
    });
  }

  async function load() {
    var tbody = document.getElementById('cases');
    var gatesEl = document.getElementById('gates');
    var params = new URLSearchParams(location.search);
    var caseFilter = params.get('case');

    try {
      var casesRes = await caveMsg('list_legal_cases', { status: 'open' });
      var items = casesRes.items || casesRes.cases || [];
      tbody.innerHTML = items.map(function (c) {
        var sev = (c.severity || '').toLowerCase();
        return '<tr class="sev-' + esc(sev) + '"><td><a class="case-title-link" href="' +
          caseUrl(c.id) + '" data-case-id="' + esc(c.id) + '">' +
          esc(c.title) + '</a></td><td>' + esc(c.status) + '</td><td>' + esc(c.severity) +
          '</td><td>' + esc(c.category) + '</td><td>' + esc(c.saurceProductId || c.saurce_product_id || '—') + '</td></tr>';
      }).join('') || '<tr><td colspan="5"><em>No open cases</em></td></tr>';
      bindCaseLinks();
    } catch (e) {
      tbody.innerHTML = '<tr><td colspan="5" class="errors">Failed to load cases: ' + esc(e.message) + '</td></tr>';
    }

    try {
      var gatesRes = await caveMsg('legal_feature_gates');
      var gates = gatesRes.items || [];
      gatesEl.innerHTML = gates.length
        ? gates.map(function (g) {
          var cid = g.legal_case_id || g.legalCaseId;
          var caseLink = cid
            ? '<a class="gate-case-link" href="' + caseUrl(cid) + '" data-case-id="' + esc(cid) + '">' +
              esc(cid) + '</a>'
            : esc(g.legal_case_id);
          return '<div class="gate"><strong>' + esc(g.feature_key) + '</strong> — ' +
            esc(g.status) + ' (case ' + caseLink + ')</div>';
        }).join('')
        : '<em>No feature gates configured</em>';
      bindCaseLinks();
    } catch (e) {
      gatesEl.innerHTML = '<em class="errors">Feature gates unavailable: ' + esc(e.message) + '</em>';
    }

    if (caseFilter && caseFilter !== currentCaseId) {
      openCaseModal(caseFilter, true);
    }
  }

  window.addEventListener('popstate', function () {
    var caseId = new URLSearchParams(location.search).get('case');
    if (caseId) {
      openCaseModal(caseId, true);
    } else if (modalOverlay.classList.contains('open')) {
      modalOverlay.classList.remove('open');
      modalOverlay.setAttribute('aria-hidden', 'true');
      modalBody.innerHTML = '';
      currentCaseId = null;
    }
  });

  document.getElementById('btn-refresh').onclick = load;
  load().catch(console.error);
})();
