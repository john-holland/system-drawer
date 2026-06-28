/* Inventory-styled resaurce chat panel */
(function (global) {
  'use strict';

  var pollTimers = {};

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function renderContent(text) {
    var safe = esc(text);
    safe = safe.replace(/\[([^\]]+)\]\(([^)]+)\)/g, function (_, label, url) {
      return '<a href="' + esc(url) + '" class="continuum-chat-link" target="_blank" rel="noopener">' + esc(label) + '</a>';
    });
    safe = safe.replace(/(https?:\/\/[^\s<]+)/g, function (url) {
      return '<a href="' + esc(url) + '" class="continuum-chat-link" target="_blank" rel="noopener">' + esc(url) + '</a>';
    });
    return safe;
  }

  function headers(extra) {
    if (global.ContinuumUserSession) {
      return global.ContinuumUserSession.getHeaders(Object.assign({ 'Content-Type': 'application/json' }, extra || {}));
    }
    return Object.assign({ 'Content-Type': 'application/json', 'X-User-ID': 'anonymous' }, extra || {});
  }

  function listMessages(chatRoomId, useTome) {
    if (useTome) {
      return fetch('/api/tomes/table-read-tome/machines/messagesMachine/message', {
        method: 'POST',
        headers: headers(),
        body: JSON.stringify({
          event: 'LIST_MESSAGES',
          data: { chatRoomId: chatRoomId },
        }),
      }).then(function (r) { return r.json(); }).then(function (d) { return d.result || d; });
    }
    return fetch('/api/chat/messages?chatRoomId=' + encodeURIComponent(chatRoomId), { headers: headers() })
      .then(function (r) { return r.json(); });
  }

  function sendMessage(chatRoomId, content, sender, useTome) {
    if (useTome) {
      return fetch('/api/tomes/table-read-tome/machines/messagesMachine/message', {
        method: 'POST',
        headers: headers(),
        body: JSON.stringify({
          event: 'SEND_MESSAGE',
          data: { chatRoomId: chatRoomId, content: content, sender: sender },
        }),
      });
    }
    return fetch('/api/chat/messages', {
      method: 'POST',
      headers: headers(),
      body: JSON.stringify({ chatRoomId: chatRoomId, content: content, sender: sender }),
    });
  }

  function renderMessages(box, messages) {
    if (!box) return;
    var items = messages || [];
    box.innerHTML = items.map(function (m) {
      var cls = m.type === 'system' ? 'continuum-chat-msg continuum-chat-msg--system' : 'continuum-chat-msg';
      return '<div class="' + cls + '"><span class="continuum-chat-sender">' + esc(m.sender) + '</span> ' +
        renderContent(m.content) + '</div>';
    }).join('');
    box.scrollTop = box.scrollHeight;
  }

  function mount(el, options) {
    options = options || {};
    if (!el) return null;
    var chatRoomId = options.chatRoomId || '';
    var sender = options.sender || (global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'user');
    var pollMs = options.pollMs || 4000;
    var useTome = options.useTome !== false;
    var mountId = 'chat-' + Math.random().toString(36).slice(2);

    el.className = (el.className ? el.className + ' ' : '') + 'continuum-chat-panel continuum-chat-panel--inventory';
    el.innerHTML =
      '<div class="continuum-chat-panel-header"><strong>Chat</strong></div>' +
      '<div class="continuum-chat-messages" id="' + mountId + '-msgs"></div>' +
      '<div class="continuum-chat-compose">' +
        '<textarea class="continuum-chat-input" rows="2" placeholder="Message"></textarea>' +
        '<button type="button" class="continuum-chat-send">Send</button>' +
      '</div>';

    var msgBox = el.querySelector('#' + mountId + '-msgs');
    var input = el.querySelector('.continuum-chat-input');
    var sendBtn = el.querySelector('.continuum-chat-send');

    function refresh() {
      if (!chatRoomId) return;
      listMessages(chatRoomId, useTome).then(function (data) {
        renderMessages(msgBox, data.messages || []);
      }).catch(function () { /* ignore */ });
    }

    sendBtn.onclick = function () {
      var text = (input.value || '').trim();
      if (!text || !chatRoomId) return;
      sendMessage(chatRoomId, text, sender, useTome).then(function () {
        input.value = '';
        refresh();
      });
    };

    input.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendBtn.click();
      }
    });

    if (pollTimers[mountId]) clearInterval(pollTimers[mountId]);
    pollTimers[mountId] = setInterval(refresh, pollMs);
    refresh();

    return {
      setChatRoomId: function (id) {
        chatRoomId = id || '';
        refresh();
      },
      refresh: refresh,
      destroy: function () {
        if (pollTimers[mountId]) {
          clearInterval(pollTimers[mountId]);
          delete pollTimers[mountId];
        }
      },
    };
  }

  global.ContinuumChatPanel = { mount: mount, renderContent: renderContent };
})(typeof window !== 'undefined' ? window : globalThis);
