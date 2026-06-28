(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount(document.getElementById('continuum-nav-root'), { app: 'legal-tracker' });
  }

  var modalOverlay = document.getElementById('case-modal-overlay');
  var modalBody = document.getElementById('case-modal-body');
  var modalTitle = document.getElementById('case-modal-title');

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  async function fetchJson(path) {
    var r = await fetch(path);
    if (!r.ok) {
      var err = new Error('HTTP ' + r.status + ' for ' + path);
      err.status = r.status;
      throw err;
    }
    return r.json();
  }

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
    setCaseQuery(null, true);
  }

  modalOverlay.addEventListener('click', function (ev) {
    if (ev.target === modalOverlay) closeModal();
  });
  document.getElementById('case-modal-close').onclick = closeModal;
  document.addEventListener('keydown', function (ev) {
    if (ev.key === 'Escape' && modalOverlay.classList.contains('open')) closeModal();
  });

  function renderCaseModal(caseData, codeLines) {
    modalTitle.textContent = caseData.title || caseData.id;
    var sev = (caseData.severity || '').toLowerCase();
    var resolutions = caseData.resolutions || [];
    var patents = caseData.patentRefs || caseData.patent_refs || [];
    var lines = codeLines || [];

    modalBody.innerHTML =
      '<p class="case-meta"><span class="sev-' + esc(sev) + '">' + esc(caseData.severity) + '</span>' +
      ' · ' + esc(caseData.status) + ' · ' + esc(caseData.category) + '</p>' +
      (caseData.description
        ? '<p>' + esc(caseData.description) + '</p>'
        : '<p><em>No description</em></p>') +
      '<dl class="case-meta">' +
      '<dt>Case ID</dt><dd>' + esc(caseData.id) + '</dd>' +
      (caseData.slug ? '<dt>Slug</dt><dd>' + esc(caseData.slug) + '</dd>' : '') +
      (caseData.featureKey || caseData.feature_key
        ? '<dt>Feature key</dt><dd><code>' + esc(caseData.featureKey || caseData.feature_key) + '</code></dd>'
        : '') +
      (caseData.saurceProductId || caseData.saurce_product_id
        ? '<dt>Saurce product</dt><dd>' + esc(caseData.saurceProductId || caseData.saurce_product_id) + '</dd>'
        : '') +
      (caseData.assigned_to || caseData.assignedTo
        ? '<dt>Assigned to</dt><dd>' + esc(caseData.assigned_to || caseData.assignedTo) + '</dd>'
        : '') +
      (caseData.opened_at ? '<dt>Opened</dt><dd>' + esc(caseData.opened_at) + '</dd>' : '') +
      (caseData.closed_at ? '<dt>Closed</dt><dd>' + esc(caseData.closed_at) + '</dd>' : '') +
      (patents.length ? '<dt>Patent refs</dt><dd>' + esc(patents.join(', ')) + '</dd>' : '') +
      '</dl>' +
      (resolutions.length
        ? '<h3 style="font-size:0.95rem;margin-top:1rem">Resolutions</h3>' +
          resolutions.map(function (r) {
            return '<div class="resolution"><strong>' + esc(r.resolution_type || r.resolutionType) + '</strong>' +
              ' · ' + esc(r.resolved_at || r.resolvedAt || '') +
              '<div>' + esc(r.summary) + '</div></div>';
          }).join('')
        : '') +
      (lines.length
        ? '<h3 style="font-size:0.95rem;margin-top:1rem">Code line refs</h3>' +
          lines.map(function (l) {
            return '<div class="code-ref">' + esc(l.repo) + ':' + esc(l.file_path) + ':' +
              esc(l.start_line) + '-' + esc(l.end_line) +
              (l.note ? ' — ' + esc(l.note) : '') + '</div>';
          }).join('')
        : '');
  }

  async function openCaseModal(caseId, replaceHistory) {
    if (!caseId) return;
    modalBody.innerHTML = '<p>Loading…</p>';
    modalTitle.textContent = 'Legal case';
    openModal();
    setCaseQuery(caseId, !!replaceHistory);

    try {
      var detail = await fetchJson('/api/legal/cases/' + encodeURIComponent(caseId));
      var codeRes = await fetchJson('/api/legal/cases/' + encodeURIComponent(caseId) + '/code-lines').catch(function () {
        return { items: [] };
      });
      renderCaseModal(detail, codeRes.items || []);
    } catch (e) {
      modalBody.innerHTML = '<p class="errors">Failed to load case: ' + esc(e.message) + '</p>';
    }
  }

  function bindCaseLinks() {
    document.querySelectorAll('a.case-title-link').forEach(function (a) {
      a.onclick = function (ev) {
        ev.preventDefault();
        openCaseModal(a.getAttribute('data-case-id') || '', false);
      };
    });
    document.querySelectorAll('a.gate-case-link').forEach(function (a) {
      a.onclick = function (ev) {
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
      var casesRes = await fetchJson('/api/legal/cases?status=open');
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
      var gatesRes = await fetchJson('/api/legal/feature-gates');
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

    if (caseFilter) {
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
    }
  });

  document.getElementById('btn-refresh').onclick = load;
  load().catch(console.error);
})();
