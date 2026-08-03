(function (global) {
  'use strict';

  var APPS = [
    { id: 'library', label: 'Library' },
    { id: 'import', label: 'USC Import' },
    { id: 'lemma', label: 'Lemma Library' },
    { id: 'hub', label: 'Episodic hub' },
    { id: 'story-board', label: 'Stories', path: '/story-board' },
    { id: 'project-calendar', label: 'Calendar', path: '/project-calendar' },
    { id: 'budget-dashboard', label: 'Budget', path: '/budget-dashboard' },
    { id: 'legal-tracker', label: 'Legal', path: '/legal-tracker' },
    { id: 'network', label: 'Network', path: '/network-definitions' },
    { id: 'cities', label: 'Cities', path: '/city-config' },
    { id: 'society', label: 'Society', path: '/society-dashboard' },
    { id: 'restaurants', label: 'Restaurants', path: '/restaurants' },
    { id: 'stations', label: 'Stations', path: '/stations' },
    { id: 'keycards', label: 'Keycards', path: '/keycards' },
    { id: 'camera', label: 'Camera', path: '/camera-pathing' },
    { id: 'table-read', label: 'Table Read', path: '/table-read' },
    { id: 'sql-viewer', label: 'SQL Viewer', path: '/sql-viewer' },
    { id: 'credits', label: 'Credits', path: '/credits' },
    { id: 'inventory-loadouts', label: 'Inventory Loadouts', path: '/inventory-loadouts' },
    { id: 'lemma-build', label: 'Lemma Build', path: '/lemma-build' },
    { id: 'lemma-completion', label: 'Lemma Completion', path: '/lemma-completion' },
    { id: 'settings', label: 'Settings', path: '/settings' },
  ];

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function swapPort(origin, port) {
    try {
      var u = new URL(origin);
      u.port = String(port);
      return u.origin;
    } catch (_) {
      return origin;
    }
  }

  function sameOriginLibraryBase(origin) {
    return String(origin || location.origin).replace(/\/$/, '') + '/library';
  }

  /** Migrate deprecated dual-server base (library on :5051) to same-origin /library. */
  function normalizeLibraryBase(stored, origin) {
    var fallback = sameOriginLibraryBase(origin);
    if (!stored) return fallback;
    stored = String(stored).replace(/\/$/, '');
    try {
      var absolute = stored.indexOf('://') >= 0 ? stored : sameOriginLibraryBase(origin);
      var u = new URL(absolute);
      var page = new URL(origin || location.origin);
      if (u.port === '5051' && page.port !== '5051') {
        localStorage.setItem('continuuuumLibraryBase', fallback);
        return fallback;
      }
      if (u.origin !== page.origin && page.pathname.indexOf('/library') >= 0) {
        localStorage.setItem('continuuuumLibraryBase', fallback);
        return fallback;
      }
      if (absolute.indexOf('/library') < 0) {
        absolute = u.origin + '/library';
      }
      return absolute;
    } catch (_) {
      return fallback;
    }
  }

  function persistFromQuery() {
    var params = new URLSearchParams(location.search);
    var lemma = params.get('lemmaApiBase');
    var library = params.get('libraryBase');
    if (lemma) localStorage.setItem('lemmaApiBase', lemma.replace(/\/$/, ''));
    if (library) {
      localStorage.setItem(
        'continuuuumLibraryBase',
        normalizeLibraryBase(library.replace(/\/$/, ''), location.origin)
      );
    }
  }

  function isViteDevOrigin(origin) {
    try {
      var port = new URL(origin).port;
      return port === '5174' || port === '5175' || port === '5173';
    } catch (_) {
      return false;
    }
  }

  function resolveAppUrls() {
    persistFromQuery();
    var lemmaBase = (localStorage.getItem('lemmaApiBase') || '').replace(/\/$/, '');
    var libraryBase = normalizeLibraryBase(localStorage.getItem('continuuuumLibraryBase') || '', origin);
    var origin = location.origin;
    var path = location.pathname || '';

    // Vite dev servers proxy /api — ignore stale lemmaApiBase pointing at :5050.
    if (isViteDevOrigin(origin)) {
      lemmaBase = origin;
    }

    if (!lemmaBase) {
      if (path.indexOf('/lemma-library') >= 0 || path.indexOf('/ui') >= 0 || isViteDevOrigin(origin)) {
        lemmaBase = origin;
      } else {
        lemmaBase = swapPort(origin, 5050);
      }
    }
    if (!libraryBase) {
      libraryBase = sameOriginLibraryBase(origin);
    }
    if (libraryBase.indexOf('/library') < 0) libraryBase += '/library';

    return {
      library: libraryBase,
      import: libraryBase + '?panel=upload',
      lemma: lemmaBase + '/lemma-library',
      hub: lemmaBase + '/ui',
      'story-board': lemmaBase + '/story-board',
      'project-calendar': lemmaBase + '/project-calendar',
      'budget-dashboard': lemmaBase + '/budget-dashboard',
      'legal-tracker': lemmaBase + '/legal-tracker',
      network: lemmaBase + '/network-definitions',
      cities: lemmaBase + '/city-config',
      society: lemmaBase + '/society-dashboard',
      restaurants: lemmaBase + '/restaurants',
      stations: lemmaBase + '/stations',
      keycards: lemmaBase + '/keycards',
      camera: lemmaBase + '/camera-pathing',
      'table-read': lemmaBase + '/table-read',
      'sql-viewer': lemmaBase + '/sql-viewer',
      credits: lemmaBase + '/credits',
      'inventory-loadouts': lemmaBase + '/inventory-loadouts',
      'lemma-build': lemmaBase + '/lemma-build',
      'lemma-completion': lemmaBase + '/lemma-completion',
      settings: lemmaBase + '/settings',
      lemmaApiBase: lemmaBase,
    };
  }

  function isImportPanelUrl(path, search) {
    if (path.indexOf('/continuuuum_editor') >= 0) return true;
    if (path.indexOf('/library') < 0 && path !== '/') return false;
    return search.get('panel') === 'upload' || search.get('upload') === '1';
  }

  function detectApp(fallback) {
    var path = location.pathname || '';
    var search = new URLSearchParams(location.search || '');
    if (isImportPanelUrl(path, search)) return 'import';
    if (path.indexOf('/lemma-library') >= 0) return 'lemma';
    if (path.indexOf('/network-definitions') >= 0) return 'network';
    if (path.indexOf('/city-config') >= 0) return 'cities';
    if (path.indexOf('/society-dashboard') >= 0) return 'society';
    if (path.indexOf('/restaurants') >= 0) return 'restaurants';
    if (path.indexOf('/stations') >= 0) return 'stations';
    if (path.indexOf('/keycards') >= 0) return 'keycards';
    if (path.indexOf('/camera-pathing') >= 0 || path.indexOf('/camera-scenes') >= 0) return 'camera';
    if (path.indexOf('/table-read') >= 0) return 'table-read';
    if (path.indexOf('/sql-viewer') >= 0) return 'sql-viewer';
    if (path.indexOf('/credits') >= 0) return 'credits';
    if (path.indexOf('/inventory-loadouts') >= 0) return 'inventory-loadouts';
    if (path.indexOf('/lemma-build') >= 0) return 'lemma-build';
    if (path.indexOf('/lemma-completion') >= 0) return 'lemma-completion';
    if (path.indexOf('/story-board') >= 0) return 'story-board';
    if (path.indexOf('/project-calendar') >= 0) return 'project-calendar';
    if (path.indexOf('/budget-dashboard') >= 0) return 'budget-dashboard';
    if (path.indexOf('/legal-tracker') >= 0) return 'legal-tracker';
    if (path.indexOf('/settings') >= 0) return 'settings';
    if (path.indexOf('/ui') >= 0) return 'hub';
    if (path.indexOf('/library') >= 0 || path === '/') return 'library';
    return fallback || 'library';
  }

  function apiBase() {
    var urls = resolveAppUrls();
    return urls.lemmaApiBase;
  }

  function mountPreorderBanner(root) {
    if (!global.ContinuuuumCaveShell || !global.ContinuuuumCaveShell.checkPreorderGate) return;
    global.ContinuuuumCaveShell.checkPreorderGate().then(function (data) {
      if (!data || !data.gate) return;
      if (data.gate.status !== 'blocked' && data.gate.status !== 'investigating') return;
      var banner = document.createElement('div');
      banner.className = 'continuuuum-preorder-banner';
      banner.style.cssText = 'background:#5a3d00;color:#ffe9b0;padding:8px 16px;font-size:13px;text-align:center';
      banner.textContent = 'Preorder feature blocked pending patent clearance (resaurce legal).';
      root.insertBefore(banner, root.firstChild);
    }).catch(function () {});
  }

  function mountDevUserSwitcher(host, opts) {
    if (!host || opts.devUserSwitcher === false) return;
    var Session = global.ContinuuuumUserSession;
    if (!Session) return;

    var wrap = document.createElement('div');
    wrap.className = 'continuuuum-dev-user';
    wrap.innerHTML =
      '<label class="continuuuum-dev-toggle" title="Developer mode — switch user identity">' +
        '<input type="checkbox" id="continuuuum-dev-mode" /> Dev' +
      '</label>' +
      '<span class="continuuuum-user-label" id="continuuuum-user-label"></span>' +
      '<span class="continuuuum-dev-panel" id="continuuuum-dev-panel" hidden>' +
        '<input type="text" id="continuuuum-user-input" placeholder="user id" />' +
        '<select id="continuuuum-user-pick"><option value="">— users —</option></select>' +
        '<button type="button" id="continuuuum-user-apply">Apply</button>' +
        '<label class="continuuuum-admin-toggle" title="Send X-Admin header for admin API routes">' +
          '<input type="checkbox" id="continuuuum-admin-mode" /> Admin' +
        '</label>' +
      '</span>';

    host.insertBefore(wrap, host.firstChild);

    var devCb = wrap.querySelector('#continuuuum-dev-mode');
    var label = wrap.querySelector('#continuuuum-user-label');
    var panel = wrap.querySelector('#continuuuum-dev-panel');
    var input = wrap.querySelector('#continuuuum-user-input');
    var pick = wrap.querySelector('#continuuuum-user-pick');
    var applyBtn = wrap.querySelector('#continuuuum-user-apply');
    var adminCb = wrap.querySelector('#continuuuum-admin-mode');

    function refreshLabel() {
      var uid = Session.getUserId();
      label.textContent = uid;
      label.title = 'Current user: ' + uid;
      if (Session.isDevMode()) {
        input.value = uid;
        label.classList.add('continuuuum-user-label--dev');
      } else {
        label.classList.remove('continuuuum-user-label--dev');
      }
    }

    function refreshPanel() {
      var on = Session.isDevMode();
      devCb.checked = on;
      panel.hidden = !on;
      wrap.classList.toggle('continuuuum-dev-user--active', on);
      if (adminCb) {
        adminCb.checked = Session.isAdmin();
        adminCb.disabled = !on;
      }
      refreshLabel();
    }

    function loadUsers() {
      var base = apiBase();
      fetch(base + '/api/users', { headers: Session.getHeaders() })
        .then(function (r) { return r.ok ? r.json() : { items: [] }; })
        .then(function (data) {
          var items = data.items || [];
          pick.innerHTML = '<option value="">— users —</option>' +
            items.map(function (u) {
              return '<option value="' + esc(u.userId) + '">' + esc(u.userId) + '</option>';
            }).join('');
        })
        .catch(function () { /* ignore */ });
    }

    function applyUser() {
      Session.setUserId(input.value);
      refreshLabel();
    }

    devCb.addEventListener('change', function () {
      Session.setDevMode(devCb.checked);
      refreshPanel();
      if (devCb.checked) loadUsers();
    });

    applyBtn.addEventListener('click', applyUser);
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Enter') applyUser();
    });
    pick.addEventListener('change', function () {
      if (pick.value) {
        input.value = pick.value;
        applyUser();
      }
    });

    if (adminCb) {
      adminCb.addEventListener('change', function () {
        Session.setAdmin(adminCb.checked);
        refreshPanel();
      });
    }

    Session.onChange(refreshPanel);
    refreshPanel();
    if (Session.isDevMode()) loadUsers();
  }

  function renderChatContent(text) {
    if (global.ContinuuuumChatPanel && global.ContinuuuumChatPanel.renderContent) {
      return global.ContinuuuumChatPanel.renderContent(text);
    }
    return esc(text);
  }

  function applyInventoryChatTheme(panel, enabled) {
    panel.classList.toggle('continuuuum-chat-panel--inventory', !!enabled);
    if (enabled) {
      panel.style.background = 'rgba(0, 0, 0, 0.8)';
      panel.style.borderLeft = '2px solid #2e7d32';
      panel.style.color = '#4caf50';
    } else {
      panel.style.background = '#161b22';
      panel.style.borderLeft = '1px solid #30363d';
      panel.style.color = '';
    }
  }

  function pollTableReadInvites(panel, loadChatMessages) {
    var hdrs = global.ContinuuuumUserSession
      ? global.ContinuuuumUserSession.getHeaders()
      : { 'X-User-ID': 'anonymous' };
    fetch('/api/notifications?limit=10', { headers: hdrs })
      .then(function (r) { return r.ok ? r.json() : { items: [] }; })
      .then(function (data) {
        var items = data.items || [];
        items.forEach(function (n) {
          if (n.type !== 'table_read_chat_invite' || n.read_at) return;
          var payload = {};
          try { payload = JSON.parse(n.message || '{}'); } catch (_) { /* ignore */ }
          if (payload.chatRoomId) {
            localStorage.setItem('continuuuumChatTableReadRoom', payload.chatRoomId);
            localStorage.setItem('continuuuumChatOpen', '1');
            var roomInput = panel.querySelector('#continuuuum-chat-room');
            if (roomInput) roomInput.value = payload.chatRoomId;
            applyInventoryChatTheme(panel, true);
            panel.style.display = 'flex';
            loadChatMessages(payload.chatRoomId);
          }
          if (n.id) {
            fetch('/api/notifications/' + encodeURIComponent(n.id) + '/read', {
              method: 'POST',
              headers: hdrs,
            }).catch(function () { /* ignore */ });
          }
        });
      })
      .catch(function () { /* ignore */ });
  }

  function mountChatPanel(root) {
    var open = localStorage.getItem('continuuuumChatOpen') === '1';
    var panel = document.getElementById('continuuuum-chat-panel');
    if (!panel) {
      panel = document.createElement('aside');
      panel.id = 'continuuuum-chat-panel';
      panel.className = 'continuuuum-chat-panel';
      panel.style.cssText = 'position:fixed;top:48px;right:0;width:320px;max-width:90vw;height:calc(100vh - 48px);z-index:9000;display:none;flex-direction:column;padding:0.5rem;box-sizing:border-box';
      panel.innerHTML =
        '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:0.5rem">' +
          '<strong>Chat</strong><button type="button" id="continuuuum-chat-close">×</button></div>' +
        '<div id="continuuuum-chat-room-row" style="margin-bottom:0.25rem">' +
          '<span id="continuuuum-chat-room-label" style="font-size:0.8rem;color:#8b949e;display:none"></span>' +
          '<input id="continuuuum-chat-room" placeholder="Chat room ID" style="width:100%" />' +
        '</div>' +
        '<div id="continuuuum-chat-messages" style="flex:1;overflow:auto;font-size:0.85rem;padding:0.25rem;margin-bottom:0.25rem"></div>' +
        '<textarea id="continuuuum-chat-input" rows="2" placeholder="Message" style="width:100%"></textarea>' +
        '<button type="button" id="continuuuum-chat-send" style="margin-top:0.25rem">Send</button>';
      document.body.appendChild(panel);
      panel.querySelector('#continuuuum-chat-close').onclick = function () {
        localStorage.setItem('continuuuumChatOpen', '0');
        panel.style.display = 'none';
      };
      panel.querySelector('#continuuuum-chat-send').onclick = function () {
        var room = panel.querySelector('#continuuuum-chat-room').value.trim();
        if (!room) {
          room = localStorage.getItem('continuuuumChatTableReadRoom') || localStorage.getItem('continuuuumChatStoryRoom') || '';
        }
        var text = panel.querySelector('#continuuuum-chat-input').value.trim();
        if (!room || !text) return;
        fetch('/api/chat/messages', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-User-ID': (window.ContinuuuumUserSession && ContinuuuumUserSession.getUserId()) || 'user' },
          body: JSON.stringify({ chatRoomId: room, content: text, sender: (window.ContinuuuumUserSession && ContinuuuumUserSession.getUserId()) || 'user' }),
        }).then(function (r) {
          if (!r.ok) {
            return r.json().catch(function () { return {}; }).then(function (data) {
              var box = panel.querySelector('#continuuuum-chat-messages');
              box.innerHTML = '<div class="continuuuum-chat-msg continuuuum-chat-msg--system">' +
                esc(data.detail || data.error || ('Send failed (' + r.status + ')')) + '</div>';
            });
          }
          panel.querySelector('#continuuuum-chat-input').value = '';
          loadChatMessages(room);
        });
      };
    }
    function bindRoomUi(activeRoom, roomKind) {
      var roomInput = panel.querySelector('#continuuuum-chat-room');
      var roomLabel = panel.querySelector('#continuuuum-chat-room-label');
      if (!roomInput) return;
      if (activeRoom && roomKind) {
        roomInput.value = activeRoom;
        roomInput.style.display = 'none';
        if (roomLabel) {
          roomLabel.style.display = 'block';
          roomLabel.textContent = (roomKind === 'table_read' ? 'Table read' : 'Story') + ' chat · ' + activeRoom.slice(0, 12) + '…';
        }
      } else {
        roomInput.style.display = 'block';
        if (roomLabel) roomLabel.style.display = 'none';
      }
    }
    function loadChatMessages(roomId) {
      if (!roomId) return;
      fetch('/api/chat/messages?chatRoomId=' + encodeURIComponent(roomId))
        .then(function (r) {
          return r.json().catch(function () { return {}; }).then(function (data) {
            return { ok: r.ok, status: r.status, data: data };
          });
        })
        .then(function (result) {
          var box = panel.querySelector('#continuuuum-chat-messages');
          if (!result.ok) {
            box.innerHTML = '<div class="continuuuum-chat-msg continuuuum-chat-msg--system">' +
              esc(result.data.detail || result.data.error || ('Chat unavailable (' + result.status + ')')) + '</div>';
            return;
          }
          box.innerHTML = (result.data.messages || []).map(function (m) {
            var cls = m.type === 'system' ? ' continuuuum-chat-msg--system' : '';
            return '<div class="continuuuum-chat-msg' + cls + '"><b>' + esc(m.sender) + '</b>: ' + renderChatContent(m.content) + '</div>';
          }).join('');
          box.scrollTop = box.scrollHeight;
        });
    }
    function refreshChatState() {
      var tableReadRoom = localStorage.getItem('continuuuumChatTableReadRoom');
      var storyRoom = localStorage.getItem('continuuuumChatStoryRoom');
      var activeRoom = tableReadRoom || storyRoom;
      applyInventoryChatTheme(panel, !!tableReadRoom);
      bindRoomUi(activeRoom, tableReadRoom ? 'table_read' : (storyRoom ? 'story' : null));
      if (activeRoom) loadChatMessages(activeRoom);
      return activeRoom;
    }
    panel._continuuuumRefreshChat = refreshChatState;
    panel._continuuuumLoadChatMessages = loadChatMessages;
    panel.style.display = open ? 'flex' : 'none';
    refreshChatState();
    var pollMs = parseInt(localStorage.getItem('continuuuumChatPollMs') || '8000', 10) || 8000;
    if (panel._continuuuumChatPoll) clearInterval(panel._continuuuumChatPoll);
    panel._continuuuumChatPoll = setInterval(function () {
      if (panel.style.display === 'none') return;
      var room = (panel.querySelector('#continuuuum-chat-room') || {}).value;
      if (!room) {
        room = localStorage.getItem('continuuuumChatTableReadRoom') || localStorage.getItem('continuuuumChatStoryRoom') || '';
      }
      if (room) loadChatMessages(room);
      pollTableReadInvites(panel, loadChatMessages);
    }, pollMs);
    return panel;
  }

  function openChat(opts) {
    opts = opts || {};
    if (opts.kind === 'story') {
      localStorage.removeItem('continuuuumChatTableReadRoom');
      if (opts.roomId) localStorage.setItem('continuuuumChatStoryRoom', opts.roomId);
    } else if (opts.kind === 'table_read') {
      localStorage.removeItem('continuuuumChatStoryRoom');
      if (opts.roomId) localStorage.setItem('continuuuumChatTableReadRoom', opts.roomId);
    }
    localStorage.setItem('continuuuumChatOpen', '1');
    var panel = mountChatPanel(document.body);
    panel.style.display = 'flex';
    if (panel._continuuuumRefreshChat) panel._continuuuumRefreshChat();
    return panel;
  }

  function mountChatToggle(extraHost) {
    if (!extraHost) return;
    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'continuuuum-chat-toggle';
    btn.textContent = 'Chat';
    btn.title = 'Toggle resaurce chat panel';
    btn.onclick = function () {
      var panel = mountChatPanel(document.body);
      var open = panel.style.display !== 'none';
      localStorage.setItem('continuuuumChatOpen', open ? '0' : '1');
      panel.style.display = open ? 'none' : 'flex';
      if (!open) {
        pollTableReadInvites(panel, function (roomId) {
          fetch('/api/chat/messages?chatRoomId=' + encodeURIComponent(roomId))
            .then(function (r) { return r.json(); })
            .then(function (d) {
              var box = panel.querySelector('#continuuuum-chat-messages');
              box.innerHTML = (d.messages || []).map(function (m) {
                return '<div><b>' + esc(m.sender) + '</b>: ' + renderChatContent(m.content) + '</div>';
              }).join('');
            });
        });
        var room = panel.querySelector('#continuuuum-chat-room').value.trim();
        if (room) {
          fetch('/api/chat/messages?chatRoomId=' + encodeURIComponent(room))
            .then(function (r) { return r.json(); })
            .then(function (d) {
              var box = panel.querySelector('#continuuuum-chat-messages');
              box.innerHTML = (d.messages || []).map(function (m) {
                return '<div><b>' + esc(m.sender) + '</b>: ' + renderChatContent(m.content) + '</div>';
              }).join('');
            });
        }
      }
    };
    extraHost.appendChild(btn);
  }

  function mount(optsOrRoot, maybeOpts) {
    var opts = {};
    if (
      optsOrRoot &&
      typeof optsOrRoot === 'object' &&
      optsOrRoot.nodeType !== 1 &&
      !(optsOrRoot instanceof HTMLElement)
    ) {
      opts = optsOrRoot;
    } else {
      opts = maybeOpts || {};
      if (typeof optsOrRoot === 'string') opts.root = optsOrRoot;
      else if (optsOrRoot && optsOrRoot.nodeType === 1) opts.root = optsOrRoot;
    }
    var root = opts.root;
    if (typeof root === 'string') root = document.querySelector(root);
    if (!root) root = document.getElementById('continuuuum-nav-root');
    if (!root) return null;

    var app = opts.app || detectApp();
    var theme = opts.theme || (app === 'hub' ? 'light' : 'dark');
    var urls = resolveAppUrls();

    root.className = 'continuuuum-nav-root';
    root.dataset.theme = theme;

    var appsHtml = APPS.map(function (item) {
      var href = item.path ? (urls.lemmaApiBase + item.path) : urls[item.id];
      var active = item.id === app ? ' class="active" aria-current="page"' : '';
      return '<a href="' + esc(href) + '" data-continuuuum-app="' + esc(item.id) + '"' + active + '>' + esc(item.label) + '</a>';
    }).join('');

    var subnavHtml = '';
    if (opts.subnav && opts.subnav.length) {
      subnavHtml = '<nav class="continuuuum-subnav" id="continuuuum-subnav">' + opts.subnav.map(function (item) {
        var cls = item.active ? ' class="active"' : '';
        var attrs = '';
        if (item.attrs) {
          Object.keys(item.attrs).forEach(function (key) {
            attrs += ' ' + key + '="' + esc(item.attrs[key]) + '"';
          });
        }
        return '<a href="' + esc(item.href || '#') + '"' + cls + attrs + '>' + esc(item.label) + '</a>';
      }).join('') + '</nav>';
    }

    root.innerHTML =
      '<header class="continuuuum-header continuuuum-header--' + esc(theme) + '">' +
        '<div class="continuuuum-header-brand"><a href="' + esc(urls.library) + '">Continuuuum</a></div>' +
        '<nav class="continuuuum-header-apps">' + appsHtml + '</nav>' +
        '<div class="continuuuum-header-extra"></div>' +
      '</header>' +
      subnavHtml;

    var extraHost = root.querySelector('.continuuuum-header-extra');
    mountDevUserSwitcher(extraHost, opts);
    mountChatToggle(extraHost);

    if (opts.extraEl) {
      var extra = typeof opts.extraEl === 'string' ? document.querySelector(opts.extraEl) : opts.extraEl;
      if (extra && extraHost) {
        extraHost.appendChild(extra);
      }
    } else if (opts.extraHtml && extraHost) {
      var appExtra = document.createElement('div');
      appExtra.className = 'continuuuum-header-app-extra';
      appExtra.innerHTML = opts.extraHtml;
      extraHost.appendChild(appExtra);
    }

    root.querySelectorAll('[data-continuuuum-app]').forEach(function (link) {
      link.addEventListener('click', function () {
        localStorage.setItem('lemmaApiBase', urls.lemmaApiBase);
        localStorage.setItem('continuuuumLibraryBase', urls.library);
      });
    });

    mountPreorderBanner(root);
    mountChatPanel(document.body);

    return {
      getUrls: function () { return urls; },
      setSubnavActive: function (matchFn) {
        var sub = root.querySelector('#continuuuum-subnav');
        if (!sub) return;
        sub.querySelectorAll('a').forEach(function (a) {
          a.classList.toggle('active', !!matchFn(a));
        });
      },
    };
  }

  global.ContinuuuumNav = {
    mount: mount,
    openChat: openChat,
    resolveAppUrls: resolveAppUrls,
    detectApp: detectApp,
    normalizeLibraryBase: normalizeLibraryBase,
    sameOriginLibraryBase: sameOriginLibraryBase,
  };
})(typeof window !== 'undefined' ? window : globalThis);
