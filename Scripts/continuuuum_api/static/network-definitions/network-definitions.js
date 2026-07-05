(function (global) {
  'use strict';

  var API = (localStorage.getItem('lemmaApiBase') || location.origin).replace(/\/$/, '');
  var TABS = [
    { id: 'networks', label: 'Networks' },
    { id: 'devices', label: 'Devices' },
    { id: 'routes', label: 'Routes' },
    { id: 'pam', label: 'PAM' },
    { id: 'playbooks', label: 'Playbooks' },
    { id: 'frame', label: 'Frame processor' },
    { id: 'export', label: 'USC export' },
  ];
  var active = 'networks';

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  function fetchJson(path, opts) {
    return fetch(API + path, opts || {}).then(function (r) {
      if (!r.ok) throw new Error(r.status + ' ' + path);
      return r.json();
    });
  }

  function renderTabs() {
    var el = document.getElementById('nd-tabs');
    el.innerHTML = TABS.map(function (t) {
      return '<button type="button" data-tab="' + t.id + '"' + (t.id === active ? ' class="active"' : '') + '>' + esc(t.label) + '</button>';
    }).join('');
    el.querySelectorAll('button').forEach(function (btn) {
      btn.addEventListener('click', function () {
        active = btn.getAttribute('data-tab');
        renderTabs();
        renderPanel();
      });
    });
  }

  function table(headers, rows) {
    var h = headers.map(function (x) { return '<th>' + esc(x) + '</th>'; }).join('');
    var body = rows.map(function (row) {
      return '<tr>' + row.map(function (c) { return '<td>' + c + '</td>'; }).join('') + '</tr>';
    }).join('');
    return '<table class="nd-table"><thead><tr>' + h + '</tr></thead><tbody>' + body + '</tbody></table>';
  }

  function renderNetworks(panel) {
    fetchJson('/api/telecom/networks').then(function (data) {
      var rows = (data.items || []).map(function (n) {
        return [
          esc(n.id),
          esc(n.name),
          n.virtual ? 'yes' : 'no',
          n.discoveryCrossRoute ? 'yes' : 'no',
          esc(n.playbookPath || ''),
          esc(n.createdAt || ''),
        ];
      });
      panel.innerHTML = '<h2>Networks</h2>' + (rows.length
        ? table(['ID', 'Name', 'Virtual', 'Cross-route', 'Playbook', 'Created'], rows)
        : '<p>No networks registered. POST to <code>/api/telecom/networks</code> or sync a playbook.</p>');
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderDevices(panel) {
    fetchJson('/api/telecom/devices').then(function (data) {
      var rows = (data.items || []).map(function (d) {
        return [esc(d.id), esc(d.displayName), esc(d.phoneE164 || ''), esc(d.ipv6Full || ''), esc(d.networkId)];
      });
      panel.innerHTML = '<h2>Devices</h2>' + table(['ID', 'Name', 'Phone', 'IPv6', 'Network'], rows);
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderRoutes(panel) {
    fetchJson('/api/telecom/routes').then(function (data) {
      var rows = (data.items || []).map(function (r) {
        return [esc(r.networkId), esc(r.prefix), esc(r.nextHop || ''), String(r.metric)];
      });
      panel.innerHTML = '<h2>Routes</h2>' + (rows.length
        ? table(['Network', 'Prefix', 'Next hop', 'Metric'], rows)
        : '<p>No routes yet. Add via <code>POST /api/telecom/routes</code> or import from a playbook.</p>');
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderPam(panel) {
    fetchJson('/api/telecom/pam/users').then(function (data) {
      var rows = (data.items || []).map(function (u) {
        return [esc(u.name), esc((u.permissions || []).join(', '))];
      });
      panel.innerHTML = '<h2>PAM Users</h2>' + table(['Name', 'Permissions'], rows);
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderPlaybooks(panel) {
    fetchJson('/api/telecom/playbooks?sync=1').then(function (data) {
      var rows = (data.items || []).map(function (p) {
        return [esc(p.path), esc(p.name)];
      });
      panel.innerHTML = '<h2>Playbooks</h2>' + table(['Path', 'Name'], rows);
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderFrame(panel) {
    fetchJson('/api/telecom/frame-processor/status').then(function (s) {
      panel.innerHTML = '<h2>Frame processor</h2><div class="nd-status"><pre>' + esc(JSON.stringify(s, null, 2)) + '</pre></div>';
    }).catch(function (e) { panel.textContent = 'Error: ' + e.message; });
  }

  function renderExport(panel) {
    panel.innerHTML =
      '<h2>USC export</h2>' +
      '<div class="nd-form">' +
      '<label>Episode ID <input id="nd-ep-id" placeholder="episode-1"></label>' +
      '<label>USC JSON <textarea id="nd-usc-json" rows="6" placeholder=\'[{"uscAssetId":"a1","displayName":"Terminal"}]\'></textarea></label>' +
      '<button type="button" id="nd-export-btn">Export</button>' +
      '<div id="nd-export-result" class="nd-status"></div></div>';
    document.getElementById('nd-export-btn').addEventListener('click', function () {
      var ep = document.getElementById('nd-ep-id').value.trim();
      var raw = document.getElementById('nd-usc-json').value.trim();
      var selection = [];
      try { selection = JSON.parse(raw || '[]'); } catch (_) { alert('Invalid JSON'); return; }
      fetchJson('/api/telecom/export/usc', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ episodeId: ep || null, uscSelection: selection }),
      }).then(function (r) {
        document.getElementById('nd-export-result').textContent = JSON.stringify(r, null, 2);
      }).catch(function (e) {
        document.getElementById('nd-export-result').textContent = 'Error: ' + e.message;
      });
    });
  }

  function renderPanel() {
    var panel = document.getElementById('nd-panel');
    panel.innerHTML = 'Loading…';
    if (active === 'networks') return renderNetworks(panel);
    if (active === 'devices') return renderDevices(panel);
    if (active === 'routes') return renderRoutes(panel);
    if (active === 'pam') return renderPam(panel);
    if (active === 'playbooks') return renderPlaybooks(panel);
    if (active === 'frame') return renderFrame(panel);
    if (active === 'export') return renderExport(panel);
  }

  if (global.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'network' });
    if (window.ContinuuuumTomeBootstrap) ContinuuuumTomeBootstrap.mountPage({ tomeId: 'network-tome' });
  }
  renderTabs();
  renderPanel();
})(typeof window !== 'undefined' ? window : globalThis);
