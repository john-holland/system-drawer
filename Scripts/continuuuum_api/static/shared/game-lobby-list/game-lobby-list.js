(function (global) {
  'use strict';

  function el(id) { return document.getElementById(id); }

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  async function jget(url) {
    var r = await fetch(url);
    return r.json();
  }

  async function jsend(url, method, body) {
    var r = await fetch(url, {
      method: method,
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : '{}'
    });
    var data = await r.json().catch(function () { return {}; });
    if (!r.ok) throw new Error(data.error || r.statusText);
    return data;
  }

  function nestSessions(sessions) {
    var byParent = {};
    (sessions || []).forEach(function (s) {
      var pid = s.parentId || '';
      if (!byParent[pid]) byParent[pid] = [];
      byParent[pid].push(s);
    });
    Object.keys(byParent).forEach(function (k) {
      byParent[k].sort(function (a, b) { return (a.peckingOrder || 0) - (b.peckingOrder || 0); });
    });
    return byParent;
  }

  function voteDebugHtml(session) {
    var runs = session.runs || [];
    if (!runs.length) return '';
    var bits = runs.map(function (run) {
      var players = (run.votesPerPlayer || []).map(function (p) {
        return esc(p.playerId || '(unknown)') + ': ' + JSON.stringify(p.votes);
      }).join('; ');
      var demo = (run.votesPerDemographic || []).map(function (d) {
        return esc(d.demographicSliceId) + ' ' + d.percent + '% ' + JSON.stringify(d.options);
      }).join('; ');
      var actors = (run.actorVotes || []).map(function (a) {
        return esc(a.actorId) + '→' + esc(a.optionId) + (a.demographicSliceId ? ' [' + esc(a.demographicSliceId) + ']' : '');
      }).join(', ');
      return 'run ' + esc(run.runId) + ' ballot ' + esc(run.ballotId || '') +
        '\nvotes/player: ' + (players || '—') +
        '\ndemographic %: ' + (demo || '—') +
        '\nactor votes: ' + (actors || '—');
    });
    return '<div class="gll-debug">' + bits.join('\n\n') + '</div>';
  }

  function playersHtml(session) {
    var players = session.players || [];
    var rows = players.map(function (p) {
      return '<div>' + esc(p.displayName || p.playerId) +
        ' <button type="button" data-download-local="' + esc(session.id) + '" data-player="' + esc(p.playerId) + '">Download local client data</button></div>';
    }).join('');
    return '<div class="gll-players">' +
      (rows || '<div class="gll-muted">No players yet</div>') +
      '</div>';
  }

  function renderSessionTree(byParent, parentId, depth, opts) {
    var kids = byParent[parentId || ''] || [];
    var wrap = document.createElement('div');
    kids.forEach(function (s) {
      var sc = document.createElement('div');
      sc.className = 'gll-session';
      sc.style.marginLeft = (depth * 12) + 'px';
      var ps = s.privateServer || {};
      var runtime = ps.runtimeKind || '';
      sc.innerHTML =
        '<strong>' + esc(s.displayName || s.id) + '</strong> pecking ' + (s.peckingOrder || 0) +
        (s.live ? ' live' : '') +
        (s.active ? ' (active)' : '') +
        ' <a href="/votes?gameSessionId=' + encodeURIComponent(s.id) + '">Votes</a> ' +
        '<button type="button" data-close="' + esc(s.id) + '">Close (adopt)</button>' +
        '<button type="button" data-umbrella="' + esc(s.id) + '">Umbrella close</button>' +
        '<button type="button" data-manage-players="' + esc(s.id) + '" data-lobby="' + esc(s.lobbySessionName || '') + '">Manage players</button>' +
        playersHtml(s) +
        (opts && opts.showVotes ? voteDebugHtml(s) : '') +
        (ps.id ? (
          '<div class="gll-host">' +
            esc(runtime || 'minecraft') +
            (ps.advertiseAddress ? ' @ ' + esc(ps.advertiseAddress) : '') +
            (ps.gamePort ? ':' + ps.gamePort : '') +
            (ps.motd ? ' · ' + esc(ps.motd) : '') +
            (ps.worldName ? ' · world ' + esc(ps.worldName) : '') +
            (ps.onlineMode ? ' · online-mode' : '') +
            '<div>' +
              '<button type="button" data-flip="' + esc(ps.id) + '" data-runtime="minecraft">Minecraft</button>' +
              '<button type="button" data-flip="' + esc(ps.id) + '" data-runtime="proton_unity">Flip Proton Unity</button>' +
              '<button type="button" data-flip="' + esc(ps.id) + '" data-runtime="unreal">Flip Unreal</button>' +
            '</div></div>'
        ) : '');
      wrap.appendChild(sc);
      var nested = renderSessionTree(byParent, s.id, depth + 1, opts);
      if (nested.childNodes.length) wrap.appendChild(nested);
    });
    return wrap;
  }

  function render(root, data, opts) {
    opts = opts || {};
    var configs = data.configs || [];
    var lobbies = data.lobbies || [];
    var filtered = data.filteredSessionIds;
    var allow = null;
    if (Array.isArray(filtered)) {
      allow = {};
      filtered.forEach(function (id) { allow[id] = true; });
    }
    var byConfig = {};
    lobbies.forEach(function (lb) {
      var cid = lb.configId || '';
      if (!byConfig[cid]) byConfig[cid] = [];
      byConfig[cid].push(lb);
    });
    root.innerHTML = '';
    configs.forEach(function (cfg) {
      var group = document.createElement('div');
      group.className = 'gll-config';
      group.innerHTML =
        '<h2>' + esc(cfg.name || cfg.id) + '</h2>' +
        '<div class="gll-muted">type ' + esc(cfg.lobbyTypeId || '') + ' / ' + esc(cfg.contentKind || '') + ' ' + esc(cfg.contentId || '') +
        ' · size ' + (cfg.gameSize || 8) + ' · ' + esc(cfg.mode || '') + '</div>' +
        '<button type="button" data-create-lobby="' + esc(cfg.id) + '">Create lobby</button>' +
        '<button type="button" data-edit-config="' + esc(cfg.id) + '">Edit</button>';
      (byConfig[cfg.id] || []).forEach(function (lb) {
        var sessions = (lb.sessions || []).filter(function (s) {
          return !allow || allow[s.id];
        });
        var box = document.createElement('div');
        box.className = 'gll-lobby';
        box.innerHTML =
          '<h3>' + esc(lb.displayName || lb.name) + (lb.active ? ' (active)' : '') +
          (lb.playerCount != null ? ' · ' + lb.playerCount + ' players' : '') +
          (lb.runtimeKind ? ' · ' + esc(lb.runtimeKind) : '') + '</h3>' +
          '<div class="gll-muted">' + esc(lb.name) + '</div>' +
          '<button type="button" data-edit-lobby="' + esc(lb.name) + '">Edit lobby</button>' +
          '<button type="button" data-create-session="' + esc(lb.name) + '">Create session</button>' +
          '<button type="button" data-close-lobby="' + esc(lb.name) + '">Close lobby</button>' +
          '<a href="/voting-places?lobbyId=' + encodeURIComponent(lb.name) + '">Voting places</a>';
        box.appendChild(renderSessionTree(nestSessions(sessions), '', 0, opts));
        group.appendChild(box);
      });
      root.appendChild(group);
    });
    var orphans = byConfig[''] || [];
    if (orphans.length) {
      var og = document.createElement('div');
      og.className = 'gll-config';
      og.innerHTML = '<h2>Unconfigured instances</h2>';
      orphans.forEach(function (lb) {
        var box = document.createElement('div');
        box.className = 'gll-lobby';
        box.innerHTML = '<h3>' + esc(lb.displayName || lb.name) + '</h3>' +
          '<button type="button" data-edit-lobby="' + esc(lb.name) + '">Edit lobby</button>' +
          '<button type="button" data-create-session="' + esc(lb.name) + '">Create session</button>' +
          '<button type="button" data-close-lobby="' + esc(lb.name) + '">Close lobby</button>';
        box.appendChild(renderSessionTree(nestSessions(lb.sessions || []), '', 0, opts));
        og.appendChild(box);
      });
      root.appendChild(og);
    }
    root._configs = configs;
    root._lobbies = lobbies;
  }

  function ensureModals() {
    if (el('gll-session-modal')) return;
    var wrap = document.createElement('div');
    wrap.innerHTML =
      '<div class="gll-modal-backdrop" id="gll-session-modal" role="dialog" aria-modal="true" hidden>' +
        '<div class="gll-modal">' +
          '<h2>Create session</h2>' +
          '<p class="gll-muted" id="gll-session-sub"></p>' +
          '<label>Display name <input id="gll-sess-name" value="Session"></label>' +
          '<label>Parent <select id="gll-sess-parent"><option value="">Root (no parent)</option></select></label>' +
          '<label>Pecking order <input id="gll-sess-pecking" type="number" placeholder="after last sibling"></label>' +
          '<div class="gll-modal-actions">' +
            '<button type="button" id="gll-session-cancel">Cancel</button>' +
            '<button type="button" id="gll-session-save">Create</button>' +
          '</div>' +
        '</div>' +
      '</div>' +
      '<div class="gll-modal-backdrop" id="gll-edit-modal" role="dialog" aria-modal="true" hidden>' +
        '<div class="gll-modal">' +
          '<h2 id="gll-edit-title">Edit lobby</h2>' +
          '<p class="gll-muted" id="gll-edit-sub"></p>' +
          '<p class="gll-err" id="gll-edit-err" hidden></p>' +
          '<input type="hidden" id="gll-edit-kind">' +
          '<input type="hidden" id="gll-edit-id">' +
          '<label>Name <input id="gll-edit-name"></label>' +
          '<label>Lobby type id <input id="gll-edit-type"></label>' +
          '<label>Content kind <select id="gll-edit-ckind">' +
            '<option value="game_mode">Game mode</option>' +
            '<option value="expansion">Expansion</option>' +
            '<option value="mod">Mod</option>' +
          '</select></label>' +
          '<label>Content id <input id="gll-edit-content"></label>' +
          '<label>Game size <input id="gll-edit-size" type="number"></label>' +
          '<label>Mode <select id="gll-edit-mode">' +
            '<option value="SinglePlayer">SinglePlayer</option>' +
            '<option value="AuthoritativePeerToPeer">AuthoritativePeerToPeer</option>' +
            '<option value="ClassicLockstep">ClassicLockstep</option>' +
          '</select></label>' +
          '<label>Min players to start <input id="gll-edit-min" type="number"></label>' +
          '<label>Max spectators <input id="gll-edit-spec" type="number"></label>' +
          '<label><input id="gll-edit-password" type="checkbox"> Require password</label>' +
          '<label><input id="gll-edit-spectators" type="checkbox"> Allow spectators</label>' +
          '<label>Runtime <select id="gll-edit-runtime">' +
            '<option value="minecraft">Minecraft</option>' +
            '<option value="proton_unity">Proton Unity</option>' +
            '<option value="unreal">Unreal</option>' +
          '</select></label>' +
          '<label>Advertise address <input id="gll-edit-addr"></label>' +
          '<label>Game port <input id="gll-edit-gport" type="number"></label>' +
          '<label>Lobby port <input id="gll-edit-lport" type="number"></label>' +
          '<label>Properties JSON <textarea id="gll-edit-json">{}</textarea></label>' +
          '<div class="gll-modal-actions">' +
            '<button type="button" id="gll-edit-cancel">Cancel</button>' +
            '<button type="button" id="gll-edit-save">Save</button>' +
          '</div>' +
        '</div>' +
      '</div>';
    while (wrap.firstChild) document.body.appendChild(wrap.firstChild);
  }

  function fillEdit(kind, row) {
    el('gll-edit-kind').value = kind;
    el('gll-edit-id').value = row.id || row.name || '';
    el('gll-edit-name').value = row.name || '';
    el('gll-edit-type').value = row.lobbyTypeId || '';
    el('gll-edit-ckind').value = row.contentKind || 'game_mode';
    el('gll-edit-content').value = row.contentId || '';
    el('gll-edit-size').value = row.gameSize || 8;
    el('gll-edit-mode').value = row.mode || 'SinglePlayer';
    el('gll-edit-min').value = row.minPlayersToStart || 1;
    el('gll-edit-spec').value = row.maxSpectators || 4;
    el('gll-edit-password').checked = !!row.requirePassword;
    el('gll-edit-spectators').checked = row.allowSpectators !== false;
    el('gll-edit-runtime').value = row.runtimeKind || 'minecraft';
    el('gll-edit-addr').value = row.advertiseAddress || '';
    el('gll-edit-gport').value = row.gamePort || '';
    el('gll-edit-lport').value = row.lobbyPort || '';
    var props = row.propertiesJson;
    el('gll-edit-json').value = typeof props === 'string' ? props : JSON.stringify(props || {}, null, 2);
    el('gll-edit-title').textContent = kind === 'config' ? 'Edit config' : 'Edit lobby';
    el('gll-edit-sub').textContent = kind === 'config' ? (row.id || '') : (row.name || '');
    el('gll-edit-err').hidden = true;
    el('gll-edit-modal').hidden = false;
  }

  function readEdit() {
    var props = {};
    try { props = JSON.parse(el('gll-edit-json').value || '{}'); } catch (err) { throw new Error('propertiesJson must be object JSON'); }
    return {
      name: el('gll-edit-name').value.trim(),
      lobbyTypeId: el('gll-edit-type').value.trim(),
      contentKind: el('gll-edit-ckind').value,
      contentId: el('gll-edit-content').value.trim(),
      gameSize: Number(el('gll-edit-size').value),
      mode: el('gll-edit-mode').value,
      minPlayersToStart: Number(el('gll-edit-min').value),
      maxSpectators: Number(el('gll-edit-spec').value),
      requirePassword: el('gll-edit-password').checked,
      allowSpectators: el('gll-edit-spectators').checked,
      runtimeKind: el('gll-edit-runtime').value,
      advertiseAddress: el('gll-edit-addr').value.trim(),
      gamePort: el('gll-edit-gport').value ? Number(el('gll-edit-gport').value) : undefined,
      lobbyPort: el('gll-edit-lport').value ? Number(el('gll-edit-lport').value) : undefined,
      propertiesJson: props
    };
  }

  function downloadJson(filename, data) {
    var blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    var a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  function bind(opts) {
    opts = opts || {};
    ensureModals();
    var sessionLobby = '';
    var sessionList = [];
    var refresh = opts.onRefresh || function () {};

    async function openSessionModal(lobbyName) {
      sessionLobby = lobbyName;
      var lb = await jget('/api/game-lobbies/' + encodeURIComponent(lobbyName));
      sessionList = lb.sessions || [];
      el('gll-session-sub').textContent = lobbyName;
      el('gll-sess-name').value = 'Session';
      var sel = el('gll-sess-parent');
      sel.innerHTML = '<option value="">Root (no parent)</option>';
      sessionList.forEach(function (s) {
        var o = document.createElement('option');
        o.value = s.id;
        o.textContent = (s.displayName || s.id) + ' [' + (s.peckingOrder || 0) + ']';
        sel.appendChild(o);
      });
      var parentId = sel.value;
      var siblings = sessionList.filter(function (s) { return (s.parentId || '') === parentId; });
      var maxP = -1;
      siblings.forEach(function (s) { if ((s.peckingOrder || 0) > maxP) maxP = s.peckingOrder || 0; });
      el('gll-sess-pecking').value = String(maxP + 1);
      el('gll-session-modal').hidden = false;
    }

    function closeSessionModal() {
      el('gll-session-modal').hidden = true;
      sessionLobby = '';
    }

    function closeEditModal() { el('gll-edit-modal').hidden = true; }

    document.addEventListener('click', async function (e) {
      var t = e.target;
      if (!t || !t.getAttribute) return;
      try {
        var editLobby = t.getAttribute('data-edit-lobby');
        if (editLobby) {
          var lb = await jget('/api/game-lobbies/' + encodeURIComponent(editLobby));
          fillEdit('lobby', lb);
          return;
        }
        var editCfg = t.getAttribute('data-edit-config');
        if (editCfg) {
          var cfg = await jget('/api/game-lobby-configs/' + encodeURIComponent(editCfg));
          fillEdit('config', cfg);
          return;
        }
        var spawn = t.getAttribute('data-create-lobby');
        if (spawn) { await jsend('/api/game-lobbies', 'POST', { configId: spawn }); await refresh(); return; }
        var cs = t.getAttribute('data-create-session');
        if (cs) { await openSessionModal(cs); return; }
        var cl = t.getAttribute('data-close');
        if (cl) { await jsend('/api/game-sessions/' + cl + '/close', 'POST', { mode: 'adopt' }); await refresh(); return; }
        var um = t.getAttribute('data-umbrella');
        if (um) { await jsend('/api/game-sessions/' + um + '/close', 'POST', { mode: 'umbrella' }); await refresh(); return; }
        var closeLb = t.getAttribute('data-close-lobby');
        if (closeLb) { await jsend('/api/game-lobbies/' + encodeURIComponent(closeLb) + '/close', 'POST'); await refresh(); return; }
        var manage = t.getAttribute('data-manage-players');
        if (manage) {
          var lobby = t.getAttribute('data-lobby') || '';
          location.href = '/players?sessionId=' + encodeURIComponent(manage) + '&lobby=' + encodeURIComponent(lobby);
          return;
        }
        var flip = t.getAttribute('data-flip');
        if (flip) {
          var runtime = t.getAttribute('data-runtime');
          await jsend('/api/private-servers/' + encodeURIComponent(flip) + '/flip-runtime', 'POST', { runtimeKind: runtime });
          await refresh();
          return;
        }
        var dl = t.getAttribute('data-download-local');
        if (dl) {
          var pid = t.getAttribute('data-player');
          var payload = await jget('/api/game-sessions/' + encodeURIComponent(dl) + '/players/' + encodeURIComponent(pid) + '/local-client');
          downloadJson(dl + '-' + pid + '-local-client.json', payload);
          return;
        }
      } catch (err) {
        alert(err.message || String(err));
      }
    });

    el('gll-session-cancel').addEventListener('click', closeSessionModal);
    el('gll-session-modal').addEventListener('click', function (e) {
      if (e.target === el('gll-session-modal')) closeSessionModal();
    });
    el('gll-sess-parent').addEventListener('change', function () {
      var parentId = el('gll-sess-parent').value;
      var siblings = sessionList.filter(function (s) { return (s.parentId || '') === parentId; });
      var maxP = -1;
      siblings.forEach(function (s) { if ((s.peckingOrder || 0) > maxP) maxP = s.peckingOrder || 0; });
      el('gll-sess-pecking').value = String(maxP + 1);
    });
    el('gll-session-save').addEventListener('click', async function () {
      var body = {
        lobbySessionName: sessionLobby,
        displayName: el('gll-sess-name').value.trim() || 'Session'
      };
      if (el('gll-sess-parent').value) body.parentId = el('gll-sess-parent').value;
      var peck = el('gll-sess-pecking').value;
      if (peck !== '') body.peckingOrder = Number(peck);
      await jsend('/api/game-sessions', 'POST', body);
      closeSessionModal();
      await refresh();
    });
    el('gll-edit-cancel').addEventListener('click', closeEditModal);
    el('gll-edit-modal').addEventListener('click', function (e) {
      if (e.target === el('gll-edit-modal')) closeEditModal();
    });
    el('gll-edit-save').addEventListener('click', async function () {
      try {
        var body = readEdit();
        var kind = el('gll-edit-kind').value;
        var id = el('gll-edit-id').value;
        if (kind === 'config') {
          body.id = id;
          await jsend('/api/game-lobby-configs/' + encodeURIComponent(id), 'PUT', body);
        } else {
          await jsend('/api/game-lobbies/' + encodeURIComponent(id) + '/prefab', 'PUT', body);
        }
        closeEditModal();
        await refresh();
      } catch (err) {
        el('gll-edit-err').hidden = false;
        el('gll-edit-err').textContent = err.message || String(err);
      }
    });
  }

  async function fetchAll() {
    var configs = await jget('/api/game-lobby-configs');
    var lobbies = await jget('/api/game-lobbies');
    return { configs: configs || [], lobbies: lobbies || [] };
  }

  global.GameLobbyList = {
    fetchAll: fetchAll,
    render: render,
    bind: bind,
    jget: jget,
    jsend: jsend,
    nestSessions: nestSessions
  };
})(typeof window !== 'undefined' ? window : globalThis);
