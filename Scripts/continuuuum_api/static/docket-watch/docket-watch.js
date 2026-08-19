(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'docket-watch' });
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function headers() {
    return window.ContinuuuumUserSession ? ContinuuuumUserSession.getHeaders() : {};
  }

  function load() {
    fetch('/api/legal/cases?caseKind=external_litigation', { headers: headers() })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var rows = (data.items || []).map(function (c) {
          var meta = c.externalMetadata || {};
          return '<tr data-id="' + esc(c.id) + '"><td>' + esc(c.title) + '</td><td>' +
            esc(c.status) + '</td><td>' + esc(meta.court || '') + '</td><td>' +
            esc(meta.mdlNumber || meta.agency || c.category) + '</td></tr>';
        });
        document.getElementById('cases').innerHTML = rows.join('') || '<tr><td colspan="4">None</td></tr>';
        document.getElementById('cases').onclick = function (ev) {
          var tr = ev.target.closest('tr[data-id]');
          if (tr) loadDocket(tr.getAttribute('data-id'), tr.cells[0].textContent);
        };
        (data.items || []).forEach(function (c) {
          loadDocket(c.id, c.title, true);
        });
      });

    fetch('/api/legal/watchlist', { headers: headers() })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var rows = (data.items || []).map(function (w) {
          return '<tr><td>' + esc(w.title) + '</td><td>' + esc(w.jurisdiction) + '</td><td>' +
            esc(w.agency) + '</td><td>' + esc(w.status) + '</td></tr>';
        });
        document.getElementById('watch').innerHTML = rows.join('') || '<tr><td colspan="4">None</td></tr>';
      });
  }

  var docketAccum = [];
  function loadDocket(caseId, title, append) {
    fetch('/api/legal/cases/' + encodeURIComponent(caseId) + '/docket-entries', { headers: headers() })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var extra = (data.items || []).map(function (e) {
          return '<tr><td>' + esc(title || e.case_id) + '</td><td>' + esc(e.filed_at) + '</td><td>' +
            esc(e.title) + '</td><td>' + esc(e.entry_kind) + '</td></tr>';
        });
        if (append) {
          docketAccum = docketAccum.concat(extra);
        } else {
          docketAccum = extra;
        }
        document.getElementById('docket').innerHTML = docketAccum.join('') || '<tr><td colspan="4">None</td></tr>';
      });
  }

  document.getElementById('btn-refresh').onclick = function () {
    docketAccum = [];
    load();
  };
  load();
})();
