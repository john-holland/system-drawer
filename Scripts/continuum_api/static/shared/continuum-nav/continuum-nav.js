(function (global) {
  'use strict';

  var APPS = [
    { id: 'library', label: 'Library' },
    { id: 'lemma', label: 'Lemma Library' },
    { id: 'hub', label: 'Episodic hub' },
    { id: 'network', label: 'Network', path: '/network-definitions' },
    { id: 'camera', label: 'Camera', path: '/camera-pathing' },
    { id: 'table-read', label: 'Table Read', path: '/table-read' },
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

  function persistFromQuery() {
    var params = new URLSearchParams(location.search);
    var lemma = params.get('lemmaApiBase');
    var library = params.get('libraryBase');
    if (lemma) localStorage.setItem('lemmaApiBase', lemma.replace(/\/$/, ''));
    if (library) localStorage.setItem('continuumLibraryBase', library.replace(/\/$/, ''));
  }

  function resolveAppUrls() {
    persistFromQuery();
    var lemmaBase = (localStorage.getItem('lemmaApiBase') || '').replace(/\/$/, '');
    var libraryBase = (localStorage.getItem('continuumLibraryBase') || '').replace(/\/$/, '');
    var origin = location.origin;
    var path = location.pathname || '';

    if (!lemmaBase) {
      if (path.indexOf('/lemma-library') >= 0 || path.indexOf('/ui') >= 0) {
        lemmaBase = origin;
      } else {
        lemmaBase = swapPort(origin, 5050);
      }
    }
    if (!libraryBase) {
      if (path.indexOf('/library') >= 0 || (path === '/' && path.indexOf('/lemma-library') < 0 && path.indexOf('/ui') < 0)) {
        libraryBase = origin + (path.indexOf('/library') >= 0 ? path.replace(/\/library.*$/, '/library') : '/library');
      } else {
        libraryBase = swapPort(origin, 5051) + '/library';
      }
    }
    if (libraryBase.indexOf('/library') < 0) libraryBase += '/library';

    return {
      library: libraryBase,
      lemma: lemmaBase + '/lemma-library',
      hub: lemmaBase + '/ui',
      network: lemmaBase + '/network-definitions',
      camera: lemmaBase + '/camera-pathing',
      'table-read': lemmaBase + '/table-read',
      lemmaApiBase: lemmaBase,
    };
  }

  function detectApp(fallback) {
    var path = location.pathname || '';
    if (path.indexOf('/lemma-library') >= 0) return 'lemma';
    if (path.indexOf('/network-definitions') >= 0) return 'network';
    if (path.indexOf('/camera-pathing') >= 0 || path.indexOf('/camera-scenes') >= 0) return 'camera';
    if (path.indexOf('/table-read') >= 0) return 'table-read';
    if (path.indexOf('/ui') >= 0) return 'hub';
    if (path.indexOf('/library') >= 0 || path === '/') return 'library';
    return fallback || 'library';
  }

  function apiBase() {
    var urls = resolveAppUrls();
    return urls.lemmaApiBase;
  }

  function mountDevUserSwitcher(host, opts) {
    if (!host || opts.devUserSwitcher === false) return;
    var Session = global.ContinuumUserSession;
    if (!Session) return;

    var wrap = document.createElement('div');
    wrap.className = 'continuum-dev-user';
    wrap.innerHTML =
      '<label class="continuum-dev-toggle" title="Developer mode — switch user identity">' +
        '<input type="checkbox" id="continuum-dev-mode" /> Dev' +
      '</label>' +
      '<span class="continuum-user-label" id="continuum-user-label"></span>' +
      '<span class="continuum-dev-panel" id="continuum-dev-panel" hidden>' +
        '<input type="text" id="continuum-user-input" placeholder="user id" />' +
        '<select id="continuum-user-pick"><option value="">— users —</option></select>' +
        '<button type="button" id="continuum-user-apply">Apply</button>' +
      '</span>';

    host.insertBefore(wrap, host.firstChild);

    var devCb = wrap.querySelector('#continuum-dev-mode');
    var label = wrap.querySelector('#continuum-user-label');
    var panel = wrap.querySelector('#continuum-dev-panel');
    var input = wrap.querySelector('#continuum-user-input');
    var pick = wrap.querySelector('#continuum-user-pick');
    var applyBtn = wrap.querySelector('#continuum-user-apply');

    function refreshLabel() {
      var uid = Session.getUserId();
      label.textContent = uid;
      label.title = 'Current user: ' + uid;
      if (Session.isDevMode()) {
        input.value = uid;
        label.classList.add('continuum-user-label--dev');
      } else {
        label.classList.remove('continuum-user-label--dev');
      }
    }

    function refreshPanel() {
      var on = Session.isDevMode();
      devCb.checked = on;
      panel.hidden = !on;
      wrap.classList.toggle('continuum-dev-user--active', on);
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

    Session.onChange(refreshPanel);
    refreshPanel();
    if (Session.isDevMode()) loadUsers();
  }

  function mount(opts) {
    opts = opts || {};
    var root = opts.root;
    if (typeof root === 'string') root = document.querySelector(root);
    if (!root) root = document.getElementById('continuum-nav-root');
    if (!root) return null;

    var app = opts.app || detectApp();
    var theme = opts.theme || (app === 'hub' ? 'light' : 'dark');
    var urls = resolveAppUrls();

    root.className = 'continuum-nav-root';
    root.dataset.theme = theme;

    var appsHtml = APPS.map(function (item) {
      var href = urls[item.id];
      var active = item.id === app ? ' class="active" aria-current="page"' : '';
      return '<a href="' + esc(href) + '" data-continuum-app="' + esc(item.id) + '"' + active + '>' + esc(item.label) + '</a>';
    }).join('');

    var subnavHtml = '';
    if (opts.subnav && opts.subnav.length) {
      subnavHtml = '<nav class="continuum-subnav" id="continuum-subnav">' + opts.subnav.map(function (item) {
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
      '<header class="continuum-header continuum-header--' + esc(theme) + '">' +
        '<div class="continuum-header-brand"><a href="' + esc(urls.library) + '">Continuum</a></div>' +
        '<nav class="continuum-header-apps">' + appsHtml + '</nav>' +
        '<div class="continuum-header-extra"></div>' +
      '</header>' +
      subnavHtml;

    var extraHost = root.querySelector('.continuum-header-extra');
    mountDevUserSwitcher(extraHost, opts);

    if (opts.extraEl) {
      var extra = typeof opts.extraEl === 'string' ? document.querySelector(opts.extraEl) : opts.extraEl;
      if (extra && extraHost) {
        extraHost.appendChild(extra);
      }
    } else if (opts.extraHtml && extraHost) {
      var appExtra = document.createElement('div');
      appExtra.className = 'continuum-header-app-extra';
      appExtra.innerHTML = opts.extraHtml;
      extraHost.appendChild(appExtra);
    }

    root.querySelectorAll('[data-continuum-app]').forEach(function (link) {
      link.addEventListener('click', function () {
        localStorage.setItem('lemmaApiBase', urls.lemmaApiBase);
        localStorage.setItem('continuumLibraryBase', urls.library);
      });
    });

    return {
      getUrls: function () { return urls; },
      setSubnavActive: function (matchFn) {
        var sub = root.querySelector('#continuum-subnav');
        if (!sub) return;
        sub.querySelectorAll('a').forEach(function (a) {
          a.classList.toggle('active', !!matchFn(a));
        });
      },
    };
  }

  global.ContinuumNav = {
    mount: mount,
    resolveAppUrls: resolveAppUrls,
    detectApp: detectApp,
  };
})(typeof window !== 'undefined' ? window : globalThis);
