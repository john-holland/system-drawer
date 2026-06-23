/* Table read room controller */
(function (global) {
  'use strict';

  var API = '/api';
  var socket = null;
  var state = {
    sessionId: null,
    draftId: null,
    snapshot: null,
    editorInst: null,
    suggestions: [],
    recording: null,
  };

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  function headers(extra) {
    return global.ContinuumUserSession
      ? global.ContinuumUserSession.getHeaders(Object.assign({ 'Content-Type': 'application/json' }, extra || {}))
      : Object.assign({ 'Content-Type': 'application/json', 'X-User-ID': 'anonymous' }, extra || {});
  }

  function api(path, opts) {
    opts = opts || {};
    return fetch(API + path, Object.assign({}, opts, { headers: headers(opts.headers) }))
      .then(function (r) {
        return r.json().then(function (data) {
          if (!r.ok) throw new Error(data.error || r.statusText);
          return data;
        });
      });
  }

  function setStatus(msg, isError) {
    var el = document.getElementById('tr-status');
    if (!el) return;
    el.textContent = msg || '';
    el.className = isError ? 'tr-status tr-error' : 'tr-status';
  }

  function isHost() {
    var uid = global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous';
    return state.snapshot && state.snapshot.session && state.snapshot.session.hostUserId === uid;
  }

  function connectSocket() {
    if (!state.sessionId || typeof io === 'undefined') return;
    if (socket) socket.disconnect();
    socket = io('/table-read', { path: '/socket.io' });
    socket.on('connect', function () {
      socket.emit('join_session', { sessionId: state.sessionId });
    });
    socket.on('session_state', applySnapshot);
    socket.on('turn_changed', function () { refreshSession(); });
    socket.on('participant_joined', function () { refreshSession(); });
    socket.on('participant_left', function () { refreshSession(); });
    socket.on('mode_changed', function () { refreshSession(); });
    socket.on('session_ended', function () { refreshSession(); });
    socket.on('recording_status', function () { refreshSession(); });
  }

  function applySnapshot(snap) {
    state.snapshot = snap;
    renderAll();
  }

  function refreshSession() {
    if (!state.sessionId) return;
    return api('/table-read/sessions/' + encodeURIComponent(state.sessionId))
      .then(applySnapshot)
      .catch(function (e) { setStatus(e.message, true); });
  }

  function renderSessionBar() {
    var el = document.getElementById('tr-session-bar');
    if (!el || !state.snapshot) return;
    var s = state.snapshot.session;
    var link = location.origin + '/table-read?session=' + encodeURIComponent(s.id) + '&draft=' + encodeURIComponent(s.draftEpisodeId);
    el.innerHTML =
      '<div><strong>Session</strong> ' + esc(s.id.slice(0, 8)) + '… · ' + esc(s.status) + '</div>' +
      '<div class="tr-share"><label>Share <input readonly value="' + esc(link) + '" id="tr-share-link" /></label>' +
      '<button type="button" id="tr-copy-link">Copy</button></div>' +
      (isHost() && s.status === 'active'
        ? '<button type="button" id="tr-end-session" class="tr-danger">End session</button>'
        : '');
    var copyBtn = document.getElementById('tr-copy-link');
    if (copyBtn) copyBtn.onclick = function () {
      var inp = document.getElementById('tr-share-link');
      if (inp) { inp.select(); document.execCommand('copy'); setStatus('Link copied'); }
    };
    var endBtn = document.getElementById('tr-end-session');
    if (endBtn) endBtn.onclick = function () {
      api('/table-read/sessions/' + s.id + '/end', { method: 'POST' })
        .then(function () { setStatus('Session ended'); refreshSession(); });
    };
  }

  function renderModeControls() {
    var el = document.getElementById('tr-mode-controls');
    if (!el || !state.snapshot) return;
    var s = state.snapshot.session;
    if (!isHost() || s.status !== 'active') {
      el.innerHTML = '<p class="tr-muted">Mode: ' + esc(s.segmentMode) + ' · ' + esc(s.contentSource) +
        (s.suggestionId ? ' · suggestion ' + esc(s.suggestionId.slice(0, 8)) : '') + '</p>';
      return;
    }
    var sugOpts = state.suggestions.map(function (sg) {
      return '<option value="' + esc(sg.id) + '"' + (s.suggestionId === sg.id ? ' selected' : '') + '>' +
        esc((sg.suggestedBy || 'user') + ' — ' + (sg.createdAt || '').slice(0, 10)) + '</option>';
    }).join('');
    el.innerHTML =
      '<label>Content <select id="tr-content-source">' +
        '<option value="draft"' + (s.contentSource === 'draft' ? ' selected' : '') + '>Draft script</option>' +
        '<option value="suggestion"' + (s.contentSource === 'suggestion' ? ' selected' : '') + '>Suggestion</option>' +
      '</select></label>' +
      '<label>Suggestion <select id="tr-suggestion-id"><option value="">—</option>' + sugOpts + '</select></label>' +
      '<label>Segment <select id="tr-segment-mode">' +
        '<option value="script"' + (s.segmentMode === 'script' ? ' selected' : '') + '>Script blocks</option>' +
        '<option value="comments"' + (s.segmentMode === 'comments' ? ' selected' : '') + '>Comments</option>' +
      '</select></label>' +
      '<label>Comments <select id="tr-comment-mode">' +
        '<option value="all"' + (s.commentMode === 'all' ? ' selected' : '') + '>Read all (host advances)</option>' +
        '<option value="round_robin"' + (s.commentMode === 'round_robin' ? ' selected' : '') + '>Round robin</option>' +
      '</select></label>' +
      '<button type="button" id="tr-apply-mode">Apply</button>' +
      '<button type="button" id="tr-rebuild-queue">Rebuild queue</button>';
    document.getElementById('tr-apply-mode').onclick = function () {
      var body = {
        contentSource: document.getElementById('tr-content-source').value,
        suggestionId: document.getElementById('tr-suggestion-id').value || null,
        segmentMode: document.getElementById('tr-segment-mode').value,
        commentMode: document.getElementById('tr-comment-mode').value,
      };
      if (body.contentSource === 'draft') body.suggestionId = null;
      api('/table-read/sessions/' + s.id, { method: 'PATCH', body: JSON.stringify(body) })
        .then(applySnapshot)
        .catch(function (e) { setStatus(e.message, true); });
    };
    document.getElementById('tr-rebuild-queue').onclick = function () {
      api('/table-read/sessions/' + s.id + '/rebuild-queue', { method: 'POST' })
        .then(applySnapshot);
    };
  }

  function renderParticipants() {
    var el = document.getElementById('tr-participants');
    if (!el || !state.snapshot) return;
    var cur = state.snapshot.currentTurn;
    var curUser = cur && cur.assignedUserId;
    el.innerHTML = (state.snapshot.participants || []).map(function (p) {
      var active = p.userId === curUser && !p.leftAt ? ' tr-participant--active' : '';
      var rec = (state.snapshot.recordings || []).some(function (r) {
        return r.userId === p.userId && r.status === 'recording';
      });
      return '<div class="tr-participant' + active + '">' +
        '<span class="tr-participant-name">' + esc(p.displayName || p.userId) + '</span>' +
        '<span class="tr-participant-role">' + esc(p.role) + '</span>' +
        (rec ? '<span class="tr-rec-dot" title="Recording">●</span>' : '') +
        (p.leftAt ? '<span class="tr-muted">left</span>' : '') +
      '</div>';
    }).join('') || '<p class="tr-muted">No participants</p>';
  }

  function renderTurnControls() {
    var el = document.getElementById('tr-turn-controls');
    if (!el || !state.snapshot) return;
    var snap = state.snapshot;
    var turn = snap.currentTurn;
    var canAdvance = snap.session.status === 'active' && turn &&
      (isHost() || snap.yourTurn);
    el.innerHTML =
      (snap.yourTurn ? '<span class="tr-your-turn">Your turn</span>' : '') +
      '<button type="button" id="tr-advance"' + (canAdvance ? '' : ' disabled') + '>Advance</button>' +
      (isHost() ? '<button type="button" id="tr-skip">Skip</button>' : '');
    var adv = document.getElementById('tr-advance');
    if (adv) adv.onclick = function () {
      api('/table-read/sessions/' + snap.session.id + '/advance', { method: 'POST' })
        .then(applySnapshot);
    };
    var skip = document.getElementById('tr-skip');
    if (skip) skip.onclick = function () {
      api('/table-read/sessions/' + snap.session.id + '/skip', { method: 'POST' })
        .then(applySnapshot);
    };
  }

  function renderReadingPane() {
    var commentEl = document.getElementById('tr-comment-card');
    var host = document.getElementById('tr-editor-host');
    var turnBanner = document.getElementById('tr-turn-banner');
    if (!state.snapshot) return;
    var snap = state.snapshot;
    var turn = snap.currentTurn;
    if (!turnBanner) {
      turnBanner = document.createElement('div');
      turnBanner.id = 'tr-turn-banner';
      turnBanner.className = 'tr-panel';
      var editorSection = host && host.parentElement;
      if (editorSection) editorSection.insertBefore(turnBanner, host);
    }
    if (turn) {
      turnBanner.innerHTML = '<strong>Turn ' + (turn.turnIndex + 1) + '</strong> · ' +
        esc(turn.assignedUserId || '—') + '<pre class="tr-turn-text">' + esc(turn.textSnapshot) + '</pre>';
      turnBanner.hidden = false;
    } else {
      turnBanner.hidden = true;
    }
    if (snap.session.segmentMode === 'comments' && turn) {
      if (commentEl) {
        commentEl.hidden = false;
        commentEl.innerHTML = '<h3>Comment</h3><div class="tr-comment-text">' + esc(turn.textSnapshot) + '</div>' +
          '<p class="tr-muted">Reader: ' + esc(turn.assignedUserId || '—') + '</p>';
      }
      if (host) host.parentElement.hidden = true;
      return;
    }
    if (commentEl) commentEl.hidden = true;
    if (host) host.parentElement.hidden = false;
  }

  function mountEditor() {
    var host = document.getElementById('tr-editor-host');
    if (!host || !global.ContinuumScriptEditor || !state.snapshot) return;
    var text = state.snapshot.scriptText || '';
    if (state.editorInst && state.editorInst.editor) {
      state.editorInst.editor.setValue(text, -1);
      return;
    }
    state.editorInst = global.ContinuumScriptEditor.mount(host, {
      scriptText: text,
      mode: 'review',
      readOnly: true,
      height: '360px',
    });
  }

  function renderRecordingPanel() {
    var el = document.getElementById('tr-recording-panel');
    if (!el || !global.ContinuumTableReadRecorder) return;
    global.ContinuumTableReadRecorder.renderPanel(el, {
      sessionId: state.sessionId,
      snapshot: state.snapshot,
      api: api,
      headers: headers,
      onStatus: setStatus,
    });
  }

  function renderAll() {
    renderSessionBar();
    renderModeControls();
    renderParticipants();
    renderTurnControls();
    renderReadingPane();
    mountEditor();
    renderRecordingPanel();
  }

  function loadSuggestions(draftId) {
    return api('/drafts/episodes/' + encodeURIComponent(draftId) + '/script-suggestions?status=pending')
      .then(function (data) { state.suggestions = data.items || []; })
      .catch(function () { state.suggestions = []; });
  }

  function joinSession(sessionId) {
    return api('/table-read/sessions/' + encodeURIComponent(sessionId) + '/join', {
      method: 'POST',
      body: JSON.stringify({ displayName: global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous' }),
    }).then(applySnapshot);
  }

  function init() {
    var params = new URLSearchParams(location.search);
    state.sessionId = params.get('session');
    state.draftId = params.get('draft');
    if (!state.sessionId) {
      setStatus('Open with ?session=…&draft=… or start from Script Output', true);
      return;
    }
    setStatus('Joining…');
    var chain = Promise.resolve();
    if (state.draftId) chain = loadSuggestions(state.draftId);
    chain
      .then(function () { return joinSession(state.sessionId); })
      .then(function () {
        connectSocket();
        setStatus('Connected');
      })
      .catch(function (e) { setStatus(e.message, true); });

    if (global.ContinuumUserSession) {
      global.ContinuumUserSession.onChange(function () {
        joinSession(state.sessionId).then(connectSocket);
      });
    }
    window.addEventListener('beforeunload', function () {
      if (global.ContinuumTableReadRecorder) global.ContinuumTableReadRecorder.flushOnLeave();
      if (state.sessionId) {
        navigator.sendBeacon(
          API + '/table-read/sessions/' + encodeURIComponent(state.sessionId) + '/leave',
          new Blob([JSON.stringify({})], { type: 'application/json' })
        );
      }
    });
  }

  global.ContinuumTableRead = { init: init, refreshSession: refreshSession, getState: function () { return state; } };
})(typeof window !== 'undefined' ? window : globalThis);
