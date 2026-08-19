/* Table read room controller */
(function (global) {
  'use strict';

  var API = '/api';
  var caveShell = global.ContinuuuumCaveShell
    ? global.ContinuuuumCaveShell.init({ tomeId: 'table-read-tome', presence: false })
    : null;

  function caveMsg(message, payload) {
    if (!caveShell) return Promise.reject(new Error('ContinuuuumCaveShell not loaded'));
    return caveShell.caveMessage(message, payload || {});
  }

  function headers(extra) {
    return global.ContinuuuumUserSession
      ? global.ContinuuuumUserSession.getHeaders(Object.assign({ 'Content-Type': 'application/json' }, extra || {}))
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
    processingView: false,
    processing: null,
    previewAudio: null,
    animModalSegId: null,
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
    var uid = global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getUserId() : 'anonymous';
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
    socket.on('quotes_updated', function () { refreshSession(); loadProcessing(); });
    socket.on('processing_updated', function (payload) {
      if (payload && payload.segments) state.processing = payload;
      renderQuoteMap();
      renderProcessing();
    });
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
    if (!global.ContinuuuumChatPanel || !state.snapshot) return;
    var roomId = state.snapshot.session && state.snapshot.session.chatRoomId;
    if (!roomId) return;
    localStorage.setItem('continuuuumChatTableReadRoom', roomId);
    var host = document.getElementById('tr-chat-panel');
    if (!host) return;
    if (state.chatPanel) {
      state.chatPanel.setChatRoomId(roomId);
      return;
    }
    state.chatPanel = global.ContinuuuumChatPanel.mount(host, {
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
      '<a class="tr-proc-link" href="/table-read/' + encodeURIComponent(s.id) + '/processing">Processing</a>' +
      (isHost() && s.status === 'active'
        ? '<div class="tr-invite"><select id="tr-invite-user"><option value="">— invite user —</option>' + userOpts + '</select>' +
          '<button type="button" id="tr-invite-btn">Invite</button></div>' +
          '<button type="button" id="tr-end-session" class="tr-danger">End session</button>'
        : '') +
      (isHost()
        ? '<button type="button" id="tr-restart-session">Restart / rerecord</button>'
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
    var restartBtn = document.getElementById('tr-restart-session');
    if (restartBtn) restartBtn.onclick = function () {
      api('/table-read/sessions/' + s.id + '/restart', { method: 'POST' })
        .then(applySnapshot);
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
    var canPrevious = canAdvance && turn && turn.turnIndex > 0;
    el.innerHTML =
      (snap.yourTurn ? '<span class="tr-your-turn">Your turn</span>' : '') +
      '<button type="button" id="tr-previous"' + (canPrevious ? '' : ' disabled') + '>Previous</button>' +
      '<button type="button" id="tr-advance"' + (canAdvance ? '' : ' disabled') + '>Advance</button>' +
      (isHost() ? '<button type="button" id="tr-skip">Skip</button>' : '');
    var prev = document.getElementById('tr-previous');
    if (prev) prev.onclick = function () {
      api('/table-read/sessions/' + snap.session.id + '/retreat', { method: 'POST' })
        .then(applySnapshot);
    };
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

  function quoteCharacters() {
    var map = (state.snapshot && state.snapshot.quoteMap) || (state.processing && state.processing.quoteMap) || [];
    return map.map(function (c) {
      return { characterName: c.characterName, dialogActorId: c.dialogActorId };
    });
  }

  function mountEditor() {
    var host = document.getElementById('tr-editor-host');
    if (!host || !global.ContinuuuumScriptEditor || !state.snapshot) return;
    var text = state.snapshot.scriptText || '';
    var chars = quoteCharacters();
    var hostUser = isHost();
    var quoteOpts = {
      tableReadCharacters: chars,
      onTableReadQuote: hostUser ? function (payload) {
        var start = Number(payload.charStart);
        var end = Number(payload.charEnd);
        var located = end > start && !(start === 0 && end === 1);
        if (!located) {
          setStatus('Highlight a quote before clicking a character name (not the empty [0, 1] cursor).', true);
          return;
        }
        api('/table-read/sessions/' + state.sessionId + '/quotes', {
          method: 'POST',
          body: JSON.stringify({
            characterName: payload.characterName,
            dialogActorId: payload.dialogActorId || '',
            charStart: start,
            charEnd: end,
          }),
        }).then(function (data) {
          if (state.snapshot) {
            state.snapshot.quotes = data.quotes;
            state.snapshot.quoteMap = data.quoteMap;
            state.snapshot.characters = data.characters;
          }
          renderQuoteMap();
          if (state.editorInst) {
            state.editorInst.options.tableReadCharacters = quoteCharacters();
          }
        }).catch(function (e) { setStatus(e.message, true); });
      } : null,
    };
    if (state.editorInst && state.editorInst.editor) {
      state.editorInst.editor.setValue(text, -1);
      state.editorInst.readOnly = !hostUser;
      state.editorInst.options.tableReadCharacters = chars;
      state.editorInst.options.onTableReadQuote = quoteOpts.onTableReadQuote;
      if (state.editorInst.editor.setReadOnly) state.editorInst.editor.setReadOnly(!hostUser);
      return;
    }
    state.editorInst = global.ContinuuuumScriptEditor.mount(host, Object.assign({
      scriptText: text,
      mode: hostUser ? 'table-read' : 'review',
      readOnly: !hostUser,
      height: '360px',
    }, quoteOpts));
  }

  function renderRecordingPanel() {
    var el = document.getElementById('tr-recording-panel');
    if (!el || !global.ContinuuuumTableReadRecorder) return;
    global.ContinuuuumTableReadRecorder.renderPanel(el, {
      sessionId: state.sessionId,
      snapshot: state.snapshot,
      api: api,
      headers: headers,
      onStatus: setStatus,
    });
  }

  function renderQuoteMap() {
    var el = document.getElementById('tr-quote-map');
    if (!el || !state.snapshot) return;
    var map = (state.snapshot.quoteMap || (state.processing && state.processing.quoteMap) || []);
    el.hidden = false;
    var hostUser = isHost();
    var rows = map.map(function (c) {
      var quotes = (c.quotes || []).filter(function (q) {
        return Number(q.end) > Number(q.start) && !(Number(q.start) === 0 && Number(q.end) === 1);
      }).map(function (q) {
        return '<label class="tr-quote-span">start <input type="number" data-qid="' + esc(q.id) + '" data-field="start" value="' + esc(q.start) + '"' + (hostUser ? '' : ' disabled') + '>' +
          ' end <input type="number" data-qid="' + esc(q.id) + '" data-field="end" value="' + esc(q.end) + '"' + (hostUser ? '' : ' disabled') + '></label>';
      }).join('');
      return '<div class="tr-char-row">' +
        '<strong>' + esc(c.characterName) + '</strong>' +
        ' → actor <input type="text" data-char="' + esc(c.characterName) + '" class="tr-actor-id" value="' + esc(c.dialogActorId || '') + '"' + (hostUser ? '' : ' disabled') + '>' +
        (quotes || '<span class="tr-muted">No quotes yet</span>') + '</div>';
    }).join('') || '<p class="tr-muted">No characters yet. Add a name, or highlight a line and click a character button.</p>';
    el.innerHTML = '<h2>Quote map</h2>' + rows +
      (hostUser
        ? '<div class="tr-add-char"><input id="tr-new-char" placeholder="Character name">' +
          '<input id="tr-new-actor" placeholder="dialogActorId">' +
          '<button type="button" id="tr-add-char">Add character</button></div>'
        : '');
    if (!hostUser) return;
    var addBtn = document.getElementById('tr-add-char');
    if (addBtn) addBtn.onclick = function () {
      var name = (document.getElementById('tr-new-char') || {}).value;
      var actor = (document.getElementById('tr-new-actor') || {}).value;
      if (!name) return;
      api('/table-read/sessions/' + state.sessionId + '/quotes', {
        method: 'POST',
        body: JSON.stringify({
          characterName: name,
          dialogActorId: actor,
        }),
      }).then(function (data) {
        state.snapshot.quotes = data.quotes;
        state.snapshot.quoteMap = data.quoteMap;
        state.snapshot.characters = data.characters;
        renderQuoteMap();
        if (state.editorInst) state.editorInst.options.tableReadCharacters = quoteCharacters();
      }).catch(function (e) { setStatus(e.message, true); });
    };
    el.querySelectorAll('input[data-qid]').forEach(function (inp) {
      inp.onchange = function () {
        var body = {};
        if (inp.getAttribute('data-field') === 'start') body.charStart = Number(inp.value);
        else body.charEnd = Number(inp.value);
        api('/table-read/sessions/' + state.sessionId + '/quotes/' + inp.getAttribute('data-qid'), {
          method: 'PATCH',
          body: JSON.stringify(body),
        }).then(function (data) {
          state.snapshot.quotes = data.quotes;
          state.snapshot.quoteMap = data.quoteMap;
        }).catch(function (e) { setStatus(e.message, true); });
      };
    });
    el.querySelectorAll('.tr-actor-id').forEach(function (inp) {
      inp.onchange = function () {
        var name = inp.getAttribute('data-char');
        var quotes = ((state.snapshot.quoteMap || []).find(function (c) { return c.characterName === name; }) || {}).quotes || [];
        quotes.forEach(function (q) {
          api('/table-read/sessions/' + state.sessionId + '/quotes/' + q.id, {
            method: 'PATCH',
            body: JSON.stringify({ dialogActorId: inp.value }),
          });
        });
        if (!quotes.length) {
          api('/table-read/sessions/' + state.sessionId + '/quotes', {
            method: 'POST',
            body: JSON.stringify({ characterName: name, dialogActorId: inp.value }),
          }).then(function (data) {
            state.snapshot.quotes = data.quotes;
            state.snapshot.quoteMap = data.quoteMap;
            state.snapshot.characters = data.characters;
          });
        }
      };
    });
  }

  function loadProcessing() {
    if (!state.sessionId) return Promise.resolve();
    return api('/table-read/sessions/' + encodeURIComponent(state.sessionId) + '/processing')
      .then(function (data) {
        state.processing = data;
        if (state.snapshot) {
          state.snapshot.quoteMap = data.quoteMap || state.snapshot.quoteMap;
          state.snapshot.quotes = data.quotes || state.snapshot.quotes;
          state.snapshot.savedAt = data.savedAt;
        }
        renderQuoteMap();
        renderProcessing();
        return data;
      })
      .catch(function () { return null; });
  }

  function patchSegment(id, body) {
    return api('/table-read/sessions/' + state.sessionId + '/processing/segments/' + id, {
      method: 'PATCH',
      body: JSON.stringify(body),
    }).then(function (data) {
      state.processing = data;
      renderProcessing();
    }).catch(function (e) { setStatus(e.message, true); });
  }

  function stopPreview() {
    if (state.previewAudio) {
      state.previewAudio.pause();
      state.previewAudio = null;
    }
    if (state.previewCtx) {
      try { state.previewCtx.close(); } catch (_) {}
      state.previewCtx = null;
    }
  }

  function playCompositionPreview() {
    var proc = state.processing;
    if (!proc) return;
    stopPreview();
    var items = proc.composition || [];
    var audio = new Audio();
    state.previewAudio = audio;
    var clips = items.filter(function (i) { return i.kind === 'clip' && i.audioUrl; });
    if (!clips.length) {
      setStatus('No included clips to preview');
      return;
    }
    var idx = 0;
    var silences = items;
    function playNext() {
      while (idx < silences.length && silences[idx].kind === 'silence') {
        var ms = (silences[idx].seconds || 0) * 1000;
        idx += 1;
        setTimeout(playNext, ms);
        return;
      }
      if (idx >= silences.length) return;
      var clip = silences[idx];
      idx += 1;
      audio.src = clip.audioUrl;
      audio.play().then(function () {
        audio.onended = playNext;
      }).catch(playNext);
    }
    playNext();
  }

  function derivedProfileSpec(profiles, profileId) {
    var list = profiles || [];
    var p = list.find(function (x) { return x.id === profileId; }) || list.find(function (x) { return x.enabled !== false; });
    if (!p) return '';
    return p.poseEngine === 'mocapanything' ? (p.mocapSpec || '') : (p.mediapipeSpec || '');
  }

  function profileEngine(profiles, profileId) {
    var list = profiles || [];
    var p = list.find(function (x) { return x.id === profileId; });
    return p ? p.poseEngine : 'mediapipe';
  }

  function renderProcessedAssets(proc) {
    var rows = proc.processedAssets || [];
    if (!rows.length) return '<p class="tr-muted">No processed assets yet. Save to catalog IDs.</p>';
    return '<h3>Processed assets</h3><table class="tr-assets"><thead><tr>' +
      '<th>kind</th><th>id</th><th>USC id</th><th>download</th></tr></thead><tbody>' +
      rows.map(function (a) {
        var lib = a.libraryDocId || '';
        var href = lib ? '/api/library/documents/' + encodeURIComponent(lib) + '/download' : '';
        return '<tr><td>' + esc(a.kind) + '</td><td>' + esc(a.id || '') + '</td><td>' + esc(lib) + '</td><td>' +
          (href ? '<a href="' + esc(href) + '" target="_blank" rel="noopener">download</a>' : '') +
          '</td></tr>';
      }).join('') +
      '</tbody></table>';
  }

  function renderAnimModal(proc, hostUser) {
    var sid = state.animModalSegId;
    if (!sid) return '';
    var seg = (proc.segments || []).find(function (s) { return s.id === sid; });
    if (!seg) return '';
    var profiles = proc.detectorProfiles || [];
    var props = seg.animProps || {};
    var profileId = seg.detectorProfileId || (profiles[0] && profiles[0].id) || '';
    var spec = derivedProfileSpec(profiles, profileId);
    var engine = profileEngine(profiles, profileId);
    var kinds = ['ambulatory', 'vehicle', 'dance', 'misc'];
    var gran = ['decimillisecond', 'millisecond', 'centisecond', 'decisecond', 'second', 'decasecond', 'minute'];
    var parts = proc.videoParts || [];
    var dis = hostUser ? '' : ' disabled';
    return '<div class="tr-modal-backdrop" id="tr-anim-modal">' +
      '<div class="tr-modal" role="dialog" aria-labelledby="tr-anim-title">' +
      '<h3 id="tr-anim-title">Animation properties</h3>' +
      '<p class="tr-muted">' + esc(seg.characterName || '') + ' · ' + esc(seg.quoteText || '') + '</p>' +
      '<label>Detector profile <select id="tr-anim-profile"' + dis + '>' +
      profiles.filter(function (p) { return p.enabled !== false; }).map(function (p) {
        return '<option value="' + esc(p.id) + '"' + (p.id === profileId ? ' selected' : '') + '>' + esc(p.label || p.id) + '</option>';
      }).join('') +
      '</select></label>' +
      '<label>Pinned model spec <input id="tr-anim-spec" value="' + esc(spec) + '" readonly></label>' +
      '<label>Kind <select id="tr-anim-kind"' + dis + '>' +
      kinds.map(function (k) {
        return '<option value="' + k + '"' + ((props.webcamAnimKind || 'ambulatory') === k ? ' selected' : '') + '>' + k + '</option>';
      }).join('') +
      '</select></label>' +
      '<label>Subsection <input id="tr-anim-subsection" value="' + esc(props.subsection || '') + '"' + dis + '></label>' +
      '<label>Start ms <input type="number" id="tr-anim-start" value="' + esc(props.timelineStartMs || 0) + '"' + dis + '></label>' +
      '<label>End ms <input type="number" id="tr-anim-end" value="' + esc(props.timelineEndMs || 0) + '"' + dis + '></label>' +
      '<label>Granularity <select id="tr-anim-gran"' + dis + '>' +
      gran.map(function (g) {
        return '<option value="' + g + '"' + ((props.granularity || 'millisecond') === g ? ' selected' : '') + '>' + g + '</option>';
      }).join('') +
      '</select></label>' +
      '<label>Species (MoCapAnything) <input id="tr-anim-species" value="' + esc(props.species || '') + '"' + dis +
      (engine === 'mocapanything' ? ' required' : '') + '></label>' +
      '<label>Session video part <select id="tr-anim-part"' + dis + '>' +
      '<option value="">— file upload —</option>' +
      parts.map(function (p) {
        var pick = String(p.libraryDocId) === String(seg.videoLibraryDocId) ? ' selected' : '';
        return '<option value="' + esc(p.libraryDocId) + '"' + pick + '>part ' + esc(p.partIndex) + ' · ' + esc(p.libraryDocId) + '</option>';
      }).join('') +
      '</select></label>' +
      (hostUser ? '<label>Video file <input type="file" id="tr-anim-file" accept="video/*"></label>' : '') +
      (seg.videoLibraryDocId ? '<p class="tr-muted">USC video id ' + esc(seg.videoLibraryDocId) + ' · ' + esc(seg.animStatus || 'idle') + '</p>' : '') +
      '<div class="tr-proc-actions">' +
      (hostUser ? '<button type="button" id="tr-anim-save">Save properties</button><button type="button" id="tr-anim-now">Process now</button>' : '') +
      '<button type="button" id="tr-anim-close">Close</button>' +
      '</div></div></div>';
  }

  function collectAnimModalBody() {
    var profileId = (document.getElementById('tr-anim-profile') || {}).value || '';
    var part = (document.getElementById('tr-anim-part') || {}).value || '';
    return {
      processVideoAnimation: true,
      detectorProfileId: profileId,
      videoLibraryDocId: part || undefined,
      animProps: {
        webcamAnimKind: (document.getElementById('tr-anim-kind') || {}).value || 'ambulatory',
        subsection: (document.getElementById('tr-anim-subsection') || {}).value || '',
        timelineStartMs: Number((document.getElementById('tr-anim-start') || {}).value || 0),
        timelineEndMs: Number((document.getElementById('tr-anim-end') || {}).value || 0),
        granularity: (document.getElementById('tr-anim-gran') || {}).value || 'millisecond',
        species: (document.getElementById('tr-anim-species') || {}).value || '',
        modelSpec: (document.getElementById('tr-anim-spec') || {}).value || '',
      },
    };
  }

  function bindAnimModal(proc) {
    var close = document.getElementById('tr-anim-close');
    if (close) close.onclick = function () { state.animModalSegId = null; renderProcessing(); };
    var backdrop = document.getElementById('tr-anim-modal');
    if (backdrop) backdrop.onclick = function (ev) {
      if (ev.target === backdrop) { state.animModalSegId = null; renderProcessing(); }
    };
    var profile = document.getElementById('tr-anim-profile');
    var spec = document.getElementById('tr-anim-spec');
    if (profile && spec) {
      profile.onchange = function () {
        spec.value = derivedProfileSpec(proc.detectorProfiles || [], profile.value);
      };
    }
    var save = document.getElementById('tr-anim-save');
    if (save) save.onclick = function () {
      var sid = state.animModalSegId;
      var body = collectAnimModalBody();
      var fileEl = document.getElementById('tr-anim-file');
      var file = fileEl && fileEl.files && fileEl.files[0];
      var go = function () { return patchSegment(sid, body); };
      if (file) {
        var fd = new FormData();
        fd.append('file', file);
        fd.append('document_type', 'video');
        var h = headers();
        delete h['Content-Type'];
        fetch(API + '/table-read/usc-upload', { method: 'POST', headers: h, body: fd })
          .then(function (r) { return r.json().then(function (d) { if (!r.ok) throw new Error(d.error || r.statusText); return d; }); })
          .then(function (d) {
            body.videoLibraryDocId = String(d.id);
            return go();
          })
          .catch(function (e) { setStatus(e.message, true); });
      } else {
        go();
      }
    };
    var now = document.getElementById('tr-anim-now');
    if (now) now.onclick = function () {
      var sid = state.animModalSegId;
      var body = collectAnimModalBody();
      var fileEl = document.getElementById('tr-anim-file');
      var file = fileEl && fileEl.files && fileEl.files[0];
      function processNow(payload) {
        return api('/table-read/sessions/' + state.sessionId + '/processing/segments/' + sid + '/process-anim', {
          method: 'POST',
          body: JSON.stringify(payload),
        }).then(function (data) {
          state.processing = data;
          renderProcessing();
        });
      }
      if (file) {
        var fd = new FormData();
        fd.append('file', file);
        fd.append('document_type', 'video');
        var h = headers();
        delete h['Content-Type'];
        fetch(API + '/table-read/usc-upload', { method: 'POST', headers: h, body: fd })
          .then(function (r) { return r.json().then(function (d) { if (!r.ok) throw new Error(d.error || r.statusText); return d; }); })
          .then(function (d) {
            body.videoLibraryDocId = String(d.id);
            return processNow(body);
          })
          .catch(function (e) { setStatus(e.message, true); });
      } else {
        processNow(body).catch(function (e) { setStatus(e.message, true); });
      }
    };
  }

  function renderProcessing() {
    var el = document.getElementById('tr-processing');
    if (!el) return;
    var show = state.processingView || (state.snapshot && state.snapshot.session && state.snapshot.session.status === 'ended');
    if (!show && !state.processing) {
      el.hidden = true;
      return;
    }
    el.hidden = false;
    var proc = state.processing;
    if (!proc) {
      el.innerHTML = '<h2>Processing</h2><p class="tr-muted">Loading…</p>';
      return;
    }
    var hostUser = isHost();
    var segs = (proc.segments || []).map(function (s) {
      var dis = hostUser ? '' : ' disabled';
      return '<div class="tr-seg-box" data-seg="' + esc(s.id) + '">' +
        '<div class="tr-seg-head"><strong>' + esc(s.characterName || '') + '</strong> ' +
        '<span class="tr-muted">' + esc(s.quoteText || '') + '</span> · ' + esc(s.status) +
        (s.matchOk ? ' · match' : ' · no match') + '</div>' +
        (s.audioUrl ? '<audio controls src="' + esc(s.audioUrl) + '"></audio>' : '<p class="tr-muted">No take yet</p>') +
        '<label><input type="checkbox" data-field="include"' + (s.include ? ' checked' : '') + dis + '> Include</label>' +
        '<label><input type="checkbox" data-field="pauseBefore"' + (s.pauseBefore ? ' checked' : '') + dis + '> Add pause before</label>' +
        '<input type="number" step="0.1" min="0" data-field="pauseBeforeSec" value="' + esc(s.pauseBeforeSec) + '"' + dis + '>' +
        '<label><input type="checkbox" data-field="pauseAfter"' + (s.pauseAfter ? ' checked' : '') + dis + '> Add pause after</label>' +
        '<input type="number" step="0.1" min="0" data-field="pauseAfterSec" value="' + esc(s.pauseAfterSec) + '"' + dis + '>' +
        '<label><input type="checkbox" data-field="insertPause"' + (s.insertPause ? ' checked' : '') + dis + '> Insert pause</label>' +
        '<input type="range" min="0" max="1" step="0.01" data-field="insertPausePos" value="' + esc(s.insertPausePos) + '"' + dis + '>' +
        '<input type="number" step="0.1" min="0" data-field="insertPauseSec" value="' + esc(s.insertPauseSec) + '"' + dis + '>' +
        '<label><input type="checkbox" data-field="processVideoAnimation"' + (s.processVideoAnimation ? ' checked' : '') + dis + '> Process video animation</label>' +
        (hostUser ? '<button type="button" data-anim-props="' + esc(s.id) + '"' + (s.processVideoAnimation ? '' : ' disabled') + '>Animation properties…</button>' : '') +
        (s.animStatus && s.animStatus !== 'idle' ? '<span class="tr-muted">anim ' + esc(s.animStatus) + '</span>' : '') +
        (hostUser ? '<input type="file" accept="audio/*" data-upload="' + esc(s.id) + '">' : '') +
        '</div>';
    }).join('') || '<p class="tr-muted">No processing segments. Add quotes first.</p>';
    el.innerHTML = '<h2>Processing</h2>' +
      '<div class="tr-proc-actions">' +
      '<button type="button" id="tr-preview-play">Preview play</button>' +
      '<button type="button" id="tr-preview-pause">Preview pause</button>' +
      (hostUser
        ? '<button type="button" id="tr-save-sync">' + esc(proc.saveLabel || 'Save') + '</button>' +
          '<button type="button" id="tr-update-script">Update script</button>'
        : '') +
      (proc.savedAt ? '<span class="tr-muted">saved ' + esc(proc.savedAt) + '</span>' : '') +
      '</div>' + segs +
      renderProcessedAssets(proc) +
      renderAnimModal(proc, hostUser) +
      '<p class="tr-muted">Comments stay on the session comment card. No suggestion / change-request on processing.</p>';
    var play = document.getElementById('tr-preview-play');
    if (play) play.onclick = playCompositionPreview;
    var pause = document.getElementById('tr-preview-pause');
    if (pause) pause.onclick = function () {
      if (state.previewAudio) state.previewAudio.pause();
    };
    var saveBtn = document.getElementById('tr-save-sync');
    if (saveBtn) saveBtn.onclick = function () {
      var path = (proc.savedAt ? '/sync' : '/save');
      api('/table-read/sessions/' + state.sessionId + path, { method: 'POST' })
        .then(function () { return loadProcessing(); })
        .then(function () { setStatus(proc.savedAt ? 'Synced' : 'Saved'); })
        .catch(function (e) { setStatus(e.message, true); });
    };
    var upd = document.getElementById('tr-update-script');
    if (upd) upd.onclick = function () {
      api('/table-read/sessions/' + state.sessionId + '/update-script', { method: 'POST' })
        .then(function () { return loadProcessing(); })
        .catch(function (e) { setStatus(e.message, true); });
    };
    el.querySelectorAll('.tr-seg-box').forEach(function (box) {
      var sid = box.getAttribute('data-seg');
      box.querySelectorAll('[data-field]').forEach(function (inp) {
        inp.onchange = function () {
          var body = {};
          var field = inp.getAttribute('data-field');
          body[field] = inp.type === 'checkbox' ? inp.checked : Number(inp.value);
          patchSegment(sid, body);
        };
      });
      var up = box.querySelector('[data-upload]');
      if (up) up.onchange = function () {
        if (!up.files || !up.files[0]) return;
        var fd = new FormData();
        fd.append('file', up.files[0]);
        var h = headers();
        delete h['Content-Type'];
        fetch(API + '/table-read/sessions/' + state.sessionId + '/processing/segments/' + sid + '/upload', {
          method: 'POST',
          headers: h,
          body: fd,
        }).then(function (r) { return r.json().then(function (d) { if (!r.ok) throw new Error(d.error || r.statusText); return d; }); })
          .then(function (data) { state.processing = data; renderProcessing(); })
          .catch(function (e) { setStatus(e.message, true); });
      };
      var animBtn = box.querySelector('[data-anim-props]');
      if (animBtn) animBtn.onclick = function () {
        state.animModalSegId = sid;
        renderProcessing();
      };
    });
    bindAnimModal(proc);
  }

  function renderAll() {
    renderSessionBar();
    renderModeControls();
    renderParticipants();
    renderTurnControls();
    renderReadingPane();
    mountEditor();
    renderRecordingPanel();
    renderQuoteMap();
    renderProcessing();
    mountChatPanel();
  }

  function loadSuggestions(draftId) {
    return api('/drafts/episodes/' + encodeURIComponent(draftId) + '/script-suggestions?status=pending')
      .then(function (data) { state.suggestions = data.items || []; })
      .catch(function () { state.suggestions = []; });
  }

  function openSessionViaTome(sessionId) {
    var displayName = global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getUserId() : 'anonymous';
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
      displayName: global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getUserId() : 'anonymous',
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
    var parts = location.pathname.replace(/\/+$/, '').split('/');
    if (parts.length >= 4 && parts[parts.length - 1] === 'processing') {
      state.processingView = true;
      state.sessionId = decodeURIComponent(parts[parts.length - 2]);
    } else {
      state.sessionId = params.get('session');
    }
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
        return loadProcessing();
      })
      .then(function () { setStatus('Connected'); })
      .catch(function (e) { setStatus(e.message, true); });

    if (global.ContinuuuumUserSession) {
      global.ContinuuuumUserSession.onChange(function () {
        openSessionViaTome(state.sessionId).then(connectSocket);
      });
    }
    window.addEventListener('beforeunload', function () {
      if (global.ContinuuuumTableReadRecorder) global.ContinuuuumTableReadRecorder.flushOnLeave();
      if (state.sessionId) {
        navigator.sendBeacon(
          API + '/table-read/sessions/' + encodeURIComponent(state.sessionId) + '/leave',
          new Blob([JSON.stringify({})], { type: 'application/json' })
        );
      }
    });
  }

  global.ContinuuuumTableRead = { init: init, refreshSession: refreshSession, getState: function () { return state; } };
})(typeof window !== 'undefined' ? window : globalThis);
