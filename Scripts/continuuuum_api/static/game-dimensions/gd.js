(function () {
  'use strict';

  var Session = window.ContinuuuumUserSession;
  var games = [];
  var dims = [];
  var matrixRows = [];
  var undoStack = [];
  var redoStack = [];
  var serverSnapshot = null;
  var activeClId = null;
  var LS_KEY = 'continuuuumGdMatrix';

  function api(path, opts) {
    opts = opts || {};
    var headers = Object.assign({ 'Content-Type': 'application/json' }, Session.getHeaders());
    return fetch(path, Object.assign({}, opts, { headers: headers })).then(function (r) {
      return r.json().then(function (j) {
        if (!r.ok) throw Object.assign(new Error(j.error || r.statusText), { status: r.status, body: j });
        return j;
      });
    });
  }

  function showTab(name) {
    document.querySelectorAll('.gd-tabs button').forEach(function (b) {
      b.classList.toggle('active', b.getAttribute('data-tab') === name);
    });
    ['view', 'create', 'visibility', 'reviews'].forEach(function (t) {
      var el = document.getElementById('gd-panel-' + t);
      if (el) el.hidden = t !== name;
    });
    if (name === 'view') loadView();
    if (name === 'visibility') loadVisibility();
    if (name === 'reviews') loadChangeLists();
  }

  function loadCatalogs() {
    return Promise.all([
      api('/api/gd/games'),
      api('/api/gd/dimensions'),
    ]).then(function (pair) {
      games = pair[0] || [];
      dims = pair[1] || [];
    });
  }

  function loadView() {
    api('/api/gd/associations').then(function (rows) {
      var byEnt = {};
      (rows || []).forEach(function (r) {
        var k = r.tableName + '|' + r.entityId;
        if (!byEnt[k]) byEnt[k] = { tableName: r.tableName, entityId: r.entityId, games: {}, dims: {} };
        byEnt[k].games[r.gameId] = true;
        byEnt[k].dims[r.dimensionId] = true;
      });
      var gameById = {};
      games.forEach(function (g) { gameById[g.id] = g.slug; });
      var dimById = {};
      dims.forEach(function (d) { dimById[d.id] = String(d.dimIndex); });
      var body = document.getElementById('gd-view-body');
      body.innerHTML = Object.keys(byEnt).map(function (k) {
        var e = byEnt[k];
        var gs = Object.keys(e.games).map(function (id) { return gameById[id] || id; }).join(', ');
        var ds = Object.keys(e.dims).map(function (id) { return dimById[id] || id; }).join(', ');
        return '<tr><td>' + e.tableName + '</td><td>' + e.entityId + '</td><td>' + gs + '</td><td>' + ds + '</td></tr>';
      }).join('') || '<tr><td colspan="4">No associations</td></tr>';
    }).catch(function (err) {
      document.getElementById('gd-view-body').innerHTML =
        '<tr><td colspan="4">' + (err.message || 'error') + '</td></tr>';
    });
  }

  function pushUndo() {
    undoStack.push(JSON.stringify(matrixRows));
    if (undoStack.length > 50) undoStack.shift();
    redoStack = [];
  }

  function renderMatrix() {
    var head = document.getElementById('gd-matrix-head');
    var body = document.getElementById('gd-matrix-body');
    head.innerHTML = '<tr><th>Table</th><th>Id</th><th>Games</th><th>Dimensions</th></tr>';
    body.innerHTML = matrixRows.map(function (row, idx) {
      var gChecks = games.map(function (g) {
        var on = (row.gameIds || []).indexOf(g.id) >= 0;
        return '<label><input type="checkbox" data-row="' + idx + '" data-kind="game" data-id="' + g.id + '"' +
          (on ? ' checked' : '') + '/> ' + (g.slug || g.displayName) + '</label>';
      }).join(' ');
      var dChecks = dims.map(function (d) {
        var on = (row.dimensionIds || []).indexOf(d.id) >= 0;
        return '<label><input type="checkbox" data-row="' + idx + '" data-kind="dim" data-id="' + d.id + '"' +
          (on ? ' checked' : '') + '/> ' + d.dimIndex + '</label>';
      }).join(' ');
      return '<tr><td>' + row.tableName + '</td><td>' + row.entityId + '</td><td>' + gChecks + '</td><td>' + dChecks + '</td></tr>';
    }).join('') || '<tr><td colspan="4">Load entities to edit</td></tr>';

    body.querySelectorAll('input[type=checkbox]').forEach(function (cb) {
      cb.addEventListener('change', function () {
        pushUndo();
        var i = parseInt(cb.getAttribute('data-row'), 10);
        var kind = cb.getAttribute('data-kind');
        var id = cb.getAttribute('data-id');
        var key = kind === 'game' ? 'gameIds' : 'dimensionIds';
        var list = (matrixRows[i][key] || []).slice();
        var at = list.indexOf(id);
        if (cb.checked && at < 0) list.push(id);
        if (!cb.checked && at >= 0) list.splice(at, 1);
        matrixRows[i][key] = list;
        persistLocal();
      });
    });
  }

  function persistLocal() {
    try {
      localStorage.setItem(LS_KEY + ':' + Session.getUserId(), JSON.stringify({
        updatedAt: Date.now(),
        rows: matrixRows,
      }));
    } catch (_) { /* ignore */ }
  }

  function loadAssociable() {
    var table = document.getElementById('gd-create-table').value;
    Promise.all([
      api('/api/gd/associable?table=' + encodeURIComponent(table)),
      api('/api/gd/associations?table=' + encodeURIComponent(table)),
    ]).then(function (pair) {
      var items = (pair[0].items || []);
      var assocs = pair[1] || [];
      var byEnt = {};
      assocs.forEach(function (r) {
        if (!byEnt[r.entityId]) byEnt[r.entityId] = { gameIds: {}, dimensionIds: {} };
        byEnt[r.entityId].gameIds[r.gameId] = true;
        byEnt[r.entityId].dimensionIds[r.dimensionId] = true;
      });
      var serverRows = items.map(function (it) {
        var a = byEnt[it.id] || { gameIds: {}, dimensionIds: {} };
        return {
          tableName: table,
          entityId: it.id,
          gameIds: Object.keys(a.gameIds),
          dimensionIds: Object.keys(a.dimensionIds),
        };
      });
      serverSnapshot = JSON.stringify(serverRows);
      var localRaw = null;
      try { localRaw = localStorage.getItem(LS_KEY + ':' + Session.getUserId()); } catch (_) {}
      if (localRaw) {
        var local = JSON.parse(localRaw);
        if (local && local.rows && local.rows.length && JSON.stringify(local.rows) !== serverSnapshot) {
          var choice = window.confirm(
            'Local changes differ from server. OK = merge local over server, Cancel = discard local.'
          );
          if (choice) matrixRows = local.rows;
          else matrixRows = serverRows;
        } else {
          matrixRows = serverRows;
        }
      } else {
        matrixRows = serverRows;
      }
      undoStack = [];
      redoStack = [];
      renderMatrix();
      document.getElementById('gd-create-status').textContent = items.length + ' entities loaded';
    }).catch(function (err) {
      document.getElementById('gd-create-status').textContent = err.message || 'load failed';
    });
  }

  function saveMatrix() {
    api('/api/gd/associations', {
      method: 'PUT',
      body: JSON.stringify({ matrix: matrixRows }),
    }).then(function () {
      serverSnapshot = JSON.stringify(matrixRows);
      persistLocal();
      document.getElementById('gd-create-status').textContent = 'Saved';
    }).catch(function (err) {
      document.getElementById('gd-create-status').textContent = err.message || 'save failed';
    });
  }

  function submitReview() {
    var items = [];
    matrixRows.forEach(function (row) {
      (row.gameIds || []).forEach(function (gid) {
        (row.dimensionIds || []).forEach(function (did) {
          items.push({
            op: 'add',
            tableName: row.tableName,
            entityId: row.entityId,
            gameId: gid,
            dimensionId: did,
          });
        });
      });
    });
    api('/api/gd/change-lists', {
      method: 'POST',
      body: JSON.stringify({ title: 'Association matrix', items: items }),
    }).then(function (cl) {
      return api('/api/gd/change-lists/' + cl.id + '/submit-for-review', { method: 'POST', body: '{}' });
    }).then(function () {
      document.getElementById('gd-create-status').textContent = 'Submitted for review';
      showTab('reviews');
    }).catch(function (err) {
      document.getElementById('gd-create-status').textContent = err.message || 'submit failed';
    });
  }

  function loadVisibility() {
    if (!Session.isAdmin()) return;
    api('/api/gd/visibility').then(function (matrix) {
      var host = document.getElementById('gd-visibility-list');
      var blocks = [];
      (matrix.games || []).forEach(function (g) {
        blocks.push(visRowHtml('game', g.id, g.displayName || g.slug, g.isPublic, g.grantedUserIds || []));
      });
      (matrix.dimensions || []).forEach(function (d) {
        blocks.push(visRowHtml('dimension', d.id, d.displayName || ('Dim ' + d.dimIndex), d.isPublic, d.grantedUserIds || []));
      });
      host.innerHTML = blocks.join('') || '<p>Empty catalog.</p>';
      host.querySelectorAll('[data-save-vis]').forEach(function (btn) {
        btn.addEventListener('click', function () {
          var row = btn.closest('.gd-vis-row');
          var kind = row.getAttribute('data-kind');
          var sid = row.getAttribute('data-id');
          var pub = row.querySelector('[data-public]').checked;
          if (pub && kind === 'game' && !window.confirm('Make this game public?')) return;
          var users = (row.querySelector('[data-users]').value || '')
            .split(',').map(function (s) { return s.trim(); }).filter(Boolean);
          api('/api/gd/visibility', {
            method: 'PUT',
            body: JSON.stringify({
              subjectKind: kind,
              subjectId: sid,
              isPublic: pub,
              grantUserIds: users,
            }),
          }).then(function () { loadVisibility(); });
        });
      });
    });
  }

  function visRowHtml(kind, id, label, isPublic, users) {
    return '<div class="gd-vis-row" data-kind="' + kind + '" data-id="' + id + '">' +
      '<strong>' + kind + '</strong> <span>' + label + '</span>' +
      '<label><input type="checkbox" data-public' + (isPublic ? ' checked' : '') + '/> Public</label>' +
      '<label>Users <input type="text" data-users value="' + (users || []).join(', ') + '" placeholder="user1, user2"/></label>' +
      '<button type="button" data-save-vis>Save</button></div>';
  }

  function loadChangeLists() {
    api('/api/gd/change-lists').then(function (list) {
      var ul = document.getElementById('gd-cl-list');
      ul.innerHTML = (list || []).map(function (cl) {
        return '<li data-id="' + cl.id + '">' + (cl.title || cl.id) + ' — ' + cl.status + '</li>';
      }).join('') || '<li>No change lists</li>';
      ul.querySelectorAll('li[data-id]').forEach(function (li) {
        li.addEventListener('click', function () { openCl(li.getAttribute('data-id')); });
      });
    });
  }

  function openCl(id) {
    activeClId = id;
    api('/api/gd/change-lists/' + id).then(function (cl) {
      document.getElementById('gd-cl-detail').hidden = false;
      document.getElementById('gd-cl-title').textContent = cl.title || cl.id;
      document.getElementById('gd-cl-status').textContent = cl.status + ' · owner ' + cl.ownerUserId;
      document.getElementById('gd-cl-items').innerHTML = (cl.items || []).map(function (it) {
        return '<li>' + it.op + ' ' + it.tableName + ' ' + it.entityId + '</li>';
      }).join('');
      document.getElementById('gd-cl-comments').innerHTML = (cl.comments || []).map(function (c) {
        return '<li>' + c.authorUserId + ': ' + c.body + '</li>';
      }).join('');
    });
  }

  function boot() {
    if (window.ContinuuuumNav) {
      window.ContinuuuumNav.mount({
        root: document.getElementById('continuuuum-nav-root'),
        app: 'game-dimensions',
      });
    }
    document.getElementById('gd-tab-visibility').hidden = !Session.isAdmin();
    Session.onChange(function () {
      document.getElementById('gd-tab-visibility').hidden = !Session.isAdmin();
    });

    document.querySelectorAll('.gd-tabs button').forEach(function (b) {
      b.addEventListener('click', function () { showTab(b.getAttribute('data-tab')); });
    });
    document.getElementById('gd-load-associable').addEventListener('click', loadAssociable);
    document.getElementById('gd-save').addEventListener('click', saveMatrix);
    document.getElementById('gd-revert').addEventListener('click', function () {
      if (!serverSnapshot) return;
      matrixRows = JSON.parse(serverSnapshot);
      renderMatrix();
    });
    document.getElementById('gd-undo').addEventListener('click', function () {
      if (!undoStack.length) return;
      redoStack.push(JSON.stringify(matrixRows));
      matrixRows = JSON.parse(undoStack.pop());
      renderMatrix();
    });
    document.getElementById('gd-redo').addEventListener('click', function () {
      if (!redoStack.length) return;
      undoStack.push(JSON.stringify(matrixRows));
      matrixRows = JSON.parse(redoStack.pop());
      renderMatrix();
    });
    document.getElementById('gd-submit-review').addEventListener('click', submitReview);
    document.getElementById('gd-refresh-cls').addEventListener('click', loadChangeLists);
    document.getElementById('gd-cl-comment-btn').addEventListener('click', function () {
      if (!activeClId) return;
      var body = document.getElementById('gd-cl-comment').value;
      api('/api/gd/change-lists/' + activeClId + '/comments', {
        method: 'POST',
        body: JSON.stringify({ body: body }),
      }).then(function () { openCl(activeClId); });
    });
    document.getElementById('gd-cl-approve').addEventListener('click', function () {
      if (!activeClId) return;
      api('/api/gd/change-lists/' + activeClId + '/reviewers', {
        method: 'POST',
        body: JSON.stringify({ reviewerUserId: Session.getUserId() }),
      }).then(function () {
        return api('/api/gd/change-lists/' + activeClId + '/reviewers/' + encodeURIComponent(Session.getUserId()), {
          method: 'PATCH',
          body: JSON.stringify({ status: 'approved' }),
        });
      }).then(function () { openCl(activeClId); });
    });
    document.getElementById('gd-cl-request').addEventListener('click', function () {
      if (!activeClId) return;
      api('/api/gd/change-lists/' + activeClId + '/reviewers/' + encodeURIComponent(Session.getUserId()), {
        method: 'PATCH',
        body: JSON.stringify({ status: 'request_changes' }),
      }).then(function () { openCl(activeClId); });
    });
    document.getElementById('gd-cl-commit').addEventListener('click', function () {
      if (!activeClId) return;
      api('/api/gd/change-lists/' + activeClId + '/commit', { method: 'POST', body: '{}' })
        .then(function () { openCl(activeClId); loadView(); });
    });

    Session.ensurePresent().then(function () {
      return loadCatalogs();
    }).then(function () {
      showTab('view');
    });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
