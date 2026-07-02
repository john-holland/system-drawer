/* Table read room controller */
(function (global) {
  'use strict';

  var API = '/api';
  var caveShell = global.ContinuumCaveShell
    ? global.ContinuumCaveShell.init({ tomeId: 'table-read-tome', presence: false })
    : null;

  function caveMsg(message, payload) {
    if (!caveShell) return Promise.reject(new Error('ContinuumCaveShell not loaded'));
    return caveShell.caveMessage(message, payload || {});
  }

  function headers(extra) {
    return global.ContinuumUserSession
      ? global.ContinuumUserSession.getHeaders(Object.assign({ 'Content-Type': 'application/json' }, extra || {}))
      : Object.assign({ 'Content-Type': 'application/json', 'X-User-ID': 'anonymous' }, extra || {});
  }

  var socket = null;
  var state = {
    sessionId: null,
    draftId: null,
    snapshot: null,
    editorInst: null,
    suggestions: [],
    recording: null,
    chatPanel: null,
    users: [],
  };

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
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

  function loadUsers() {
    return fetch('/api/users', { headers: headers() })
      .then(function (r) { return r.ok ? r.json() : { items: [] }; })
      .then(function (data) { state.users = data.items || []; })
      .catch(function () { state.users = []; });
  }

  function mountChatPanel() {
    if (!global.ContinuumChatPanel || !state.snapshot) return;
    var roomId = state.snapshot.session && state.snapshot.session.chatRoomId;
    if (!roomId) return;
    localStorage.setItem('continuumChatTableReadRoom', roomId);
    var host = document.getElementById('tr-chat-panel');
    if (!host) return;
    if (state.chatPanel) {
      state.chatPanel.setChatRoomId(roomId);
      return;
    }
    state.chatPanel = global.ContinuumChatPanel.mount(host, {
      chatRoomId: roomId,
      useTome: true,
    });
  }

  function renderSessionBar() {
    var el = document.getElementById('tr-session-bar');
    if (!el || !state.snapshot) return;
    var s = state.snapshot.session;
    var link = s.shareUrl || (location.origin + '/table-read?session=' + encodeURIComponent(s.id) + '&draft=' + encodeURIComponent(s.draftEpisodeId));
    var userOpts = state.users.map(function (u) {
      return '<option value="' + esc(u.userId) + '">' + esc(u.userId) + '</option>';
    }).join('');
    el.innerHTML =
      '<div><strong>Session</strong> ' + esc(s.id.slice(0, 8)) + '… · ' + esc(s.status) + '</div>' +
      '<div class="tr-share"><label>Share <input readonly value="' + esc(link) + '" id="tr-share-link" /></label>' +
      '<button type="button" id="tr-copy-link">Copy</button></div>' +
      (isHost() && s.status === 'active'
        ? '<div class="tr-invite"><select id="tr-invite-user"><option value="">— invite user —</option>' + userOpts + '</select>' +
          '<button type="button" id="tr-invite-btn">Invite</button></div>' +
          '<button type="button" id="tr-end-session" class="tr-danger">End session</button>'
        : '');
    var copyBtn = document.getElementById('tr-copy-link');
    if (copyBtn) copyBtn.onclick = function () {
      var inp = document.getElementById('tr-share-link');
      if (inp) { inp.select(); document.execCommand('copy'); setStatus('Link copied'); }
    };
    var inviteBtn = document.getElementById('tr-invite-btn');
    if (inviteBtn) inviteBtn.onclick = function () {
      var sel = document.getElementById('tr-invite-user');
      var uid = sel && sel.value;
      if (!uid) { setStatus('Select a user to invite', true); return; }
      fetch('/api/tomes/table-read-tome/machines/inviteMachine/message', {
        method: 'POST',
        headers: headers(),
        body: JSON.stringify({
          event: 'INVITE_USER',
          data: { sessionId: s.id, userId: uid },
        }),
      })
        .then(function (r) { return r.json(); })
        .then(function (d) {
          var res = d.result || d;
          if (res.error) throw new Error(res.error);
          setStatus('Invited ' + uid);
          if (state.chatPanel) state.chatPanel.refresh();
        })
        .catch(function (e) { setStatus(e.message, true); });
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
    mountChatPanel();
  }

  function loadSuggestions(draftId) {
    return api('/drafts/episodes/' + encodeURIComponent(draftId) + '/script-suggestions?status=pending')
      .then(function (data) { state.suggestions = data.items || []; })
      .catch(function () { state.suggestions = []; });
  }

  function openSessionViaTome(sessionId) {
    var displayName = global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous';
    return fetch('/api/tomes/table-read-tome/machines/sessionMachine/message', {
      method: 'POST',
      headers: headers(),
      body: JSON.stringify({
        event: 'SESSION_OPEN',
        data: { sessionId: sessionId, displayName: displayName },
      }),
    })
      .then(function (r) {
        return r.json().then(function (data) {
          if (!r.ok) throw new Error((data.result && data.result.error) || data.error || r.statusText);
          return data.result || data;
        });
      })
      .then(function (snap) {
        if (snap && snap.session) applySnapshot(snap);
        else return joinSession(sessionId);
      });
  }

  function joinSession(sessionId) {
    return caveMsg('table_read_session_open', {
      sessionId: sessionId,
      displayName: global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous',
    }).then(applySnapshot);
  }

  function initDialogueMode(setId) {
    setStatus('Opening dialogue session…');
    caveMsg('dialogue_session_open', { setId: setId, traceId: 'web-' + Date.now() })
      .then(function (snap) {
        state.dialogueSession = snap;
        renderDialoguePanel(snap);
        setStatus('Dialogue: ' + setId);
      })
      .catch(function (e) { setStatus(e.message || 'Dialogue open failed', true); });
  }

  function renderDialoguePanel(snap) {
    var host = document.getElementById('tr-dialogue-panel') || document.createElement('div');
    host.id = 'tr-dialogue-panel';
    host.className = 'tr-dialogue-panel';
    var node = snap.currentNode || {};
    var choices = snap.choices || [];
    host.innerHTML =
      '<div class="tr-dialogue-line">' + esc(node.text || '') + '</div>' +
      '<div class="tr-dialogue-choices">' +
      choices.map(function (c) {
        return '<button type="button" class="tr-dialogue-choice" data-answer="' + esc(c.answerId) + '">' +
          esc(c.text || c.answerId) + '</button>';
      }).join('') +
      '</div>';
    document.body.prepend(host);
    host.querySelectorAll('.tr-dialogue-choice').forEach(function (btn) {
      btn.onclick = function () {
        var answerId = btn.getAttribute('data-answer');
        caveMsg('dialogue_choose', {
          sessionId: (state.dialogueSession && state.dialogueSession.sessionId) || snap.sessionId,
          answerId: answerId,
        })
          .then(function (next) {
            state.dialogueSession = next;
            renderDialoguePanel(next);
            if (next.currentNode && next.currentNode.audioRef) {
              var audio = new Audio(next.currentNode.audioRef);
              audio.play().catch(function () {});
            }
          });
      };
    });
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function init() {
    var params = new URLSearchParams(location.search);
    state.sessionId = params.get('session');
    state.draftId = params.get('draft');
    state.dialogueSetId = params.get('dialogueSet');
    if (state.dialogueSetId) {
      initDialogueMode(state.dialogueSetId);
      return;
    }
    if (!state.sessionId) {
      setStatus('Open with ?session=…&draft=… or start from Script Output', true);
      return;
    }
    setStatus('Joining…');
    var chain = Promise.resolve();
    chain = chain.then(function () { return loadUsers(); });
    if (state.draftId) chain = chain.then(function () { return loadSuggestions(state.draftId); });
    chain
      .then(function () { return openSessionViaTome(state.sessionId); })
      .then(function () {
        connectSocket();
        setStatus('Connected');
      })
      .catch(function (e) { setStatus(e.message, true); });

    if (global.ContinuumUserSession) {
      global.ContinuumUserSession.onChange(function () {
        openSessionViaTome(state.sessionId).then(connectSocket);
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
