(function (global) {
  'use strict';

  var USER_KEY = 'continuuuumUserId';
  var DEV_KEY = 'continuuuumDevMode';
  var ADMIN_KEY = 'continuuuumAdminMode';
  var GAME_KEY = 'continuuuumGame';
  var DIMENSION_KEY = 'continuuuumDimension';
  var listeners = [];
  var PRESETS = ['developer', 'admin', 'user1', 'user2', 'user3', 'user4', 'user5', 'user6'];
  var gateEl = null;
  var styleInjected = false;

  function normUser(id) {
    var s = String(id == null ? '' : id).trim();
    return s || 'anonymous';
  }

  function bootstrapFromQuery() {
    try {
      var params = new URLSearchParams(location.search);
      var uid = params.get('userId');
      var dev = params.get('dev');
      if (uid) localStorage.setItem(USER_KEY, normUser(uid));
      if (dev === '1' || dev === 'true') localStorage.setItem(DEV_KEY, '1');
    } catch (_) { /* ignore */ }
  }

  function notify(kind) {
    var detail = {
      userId: getUserId(),
      devMode: isDevMode(),
      adminMode: isAdmin(),
      game: getGame(),
      dimension: getDimension(),
      kind: kind || 'change',
    };
    listeners.forEach(function (fn) {
      try { fn(detail); } catch (_) { /* ignore */ }
    });
    try {
      global.dispatchEvent(new CustomEvent('continuuuum-user-changed', { detail: detail }));
    } catch (_) { /* ignore */ }
  }

  function getGame() {
    var g = localStorage.getItem(GAME_KEY);
    return g == null || g === '' ? 'main' : String(g);
  }

  function getDimension() {
    var d = localStorage.getItem(DIMENSION_KEY);
    if (d == null || d === '') return '0';
    return String(d);
  }

  function apiBase() {
    try {
      return (localStorage.getItem('lemmaApiBase') || location.origin || '').replace(/\/$/, '');
    } catch (_) {
      return '';
    }
  }

  function patchUserContext(body) {
    var base = apiBase();
    if (!base || typeof fetch !== 'function') return Promise.resolve(null);
    return fetch(base + '/api/gd/user-context', {
      method: 'PATCH',
      headers: Object.assign({ 'Content-Type': 'application/json' }, getHeaders()),
      body: JSON.stringify(body || {}),
    }).then(function (r) { return r.ok ? r.json() : null; }).catch(function () { return null; });
  }

  function setGame(slug) {
    var next = String(slug == null ? '' : slug).trim() || 'main';
    var prev = getGame();
    localStorage.setItem(GAME_KEY, next);
    if (prev !== next) {
      notify('game');
      return patchUserContext({ game: next }).then(function () { return next; });
    }
    return Promise.resolve(next);
  }

  function setDimension(dim) {
    var next = String(dim == null ? '0' : dim).trim() || '0';
    var prev = getDimension();
    localStorage.setItem(DIMENSION_KEY, next);
    if (prev !== next) {
      notify('dimension');
      var base = apiBase();
      if (base && typeof fetch === 'function') {
        return fetch(base + '/api/gd/dimension-switch', {
          method: 'POST',
          headers: Object.assign({ 'Content-Type': 'application/json' }, getHeaders()),
          body: JSON.stringify({ game: getGame(), dimension: next }),
        }).then(function (r) { return r.ok ? r.json() : null; })
          .then(function () { return next; })
          .catch(function () { return next; });
      }
      return patchUserContext({ dimension: next }).then(function () { return next; });
    }
    return Promise.resolve(next);
  }

  function getQuery(extra) {
    var q = {};
    var policy = getGdPolicy();
    if (policy.game) q.game = getGame();
    if (policy.dimension) q.dimension = getDimension();
    if (extra) {
      Object.keys(extra).forEach(function (k) { q[k] = extra[k]; });
    }
    return q;
  }

  function appendGameDimensionQuery(url) {
    var policy = getGdPolicy();
    if (!policy.game && !policy.dimension) return url;
    var u;
    try {
      u = new URL(url, location.origin);
    } catch (_) {
      return url;
    }
    if (policy.game) u.searchParams.set('game', getGame());
    if (policy.dimension) u.searchParams.set('dimension', getDimension());
    return u.pathname + u.search + (u.hash || '');
  }

  /**
   * Before creating a lemma/property off dim 0.
   * Returns: 'abort' | 'switched' | 'forceLanding'
   */
  function confirmCreateDimensionGate() {
    var dim = parseInt(getDimension(), 10) || 0;
    if (dim === 0) return 'ok';
    var ans = global.confirm(
      'Switch to dimension 0 to create?\n\nOK = switch to dim 0 and try again\nCancel = create at current dimension'
    );
    if (ans) {
      setDimension(0);
      try {
        global.alert('Dimension switched to 0 — try again.');
      } catch (_) { /* ignore */ }
      return 'switched';
    }
    // second confirm for cancel-vs-force: browsers only have OK/Cancel once
    var force = global.confirm('Create at landing dimension ' + dim + ' instead?');
    return force ? 'forceLanding' : 'abort';
  }

  function getUserId() {
    return normUser(localStorage.getItem(USER_KEY));
  }

  function setUserId(id) {
    var next = normUser(id);
    if (next === getUserId()) {
      notify('user');
      return;
    }
    localStorage.setItem(USER_KEY, next);
    notify('user');
  }

  function isDevMode() {
    return localStorage.getItem(DEV_KEY) === '1';
  }

  function setDevMode(on) {
    var next = !!on;
    var prev = isDevMode();
    if (next) localStorage.setItem(DEV_KEY, '1');
    else {
      localStorage.removeItem(DEV_KEY);
      setAdmin(false);
    }
    if (prev !== next) notify('dev');
  }

  function isAdmin() {
    return localStorage.getItem(ADMIN_KEY) === '1';
  }

  function setAdmin(on) {
    var next = !!on;
    var prev = isAdmin();
    if (next) localStorage.setItem(ADMIN_KEY, '1');
    else localStorage.removeItem(ADMIN_KEY);
    if (prev !== next) notify('admin');
  }

  /**
   * Path → nav app id (mirrors ContinuuuumNav.detectApp markers) for when nav is not loaded.
   */
  var PATH_APP_MARKERS = [
    ['/lemma-library', 'lemma'],
    ['/network-definitions', 'network'],
    ['/city-config', 'cities'],
    ['/society-dashboard', 'society'],
    ['/restaurants', 'restaurants'],
    ['/stations', 'stations'],
    ['/keycards', 'keycards'],
    ['/vehicle-inventory', 'vehicle-inventory'],
    ['/phone-wires', 'phone-wires'],
    ['/camera-pathing', 'camera'],
    ['/camera-scenes', 'camera'],
    ['/table-read', 'table-read'],
    ['/sql-viewer', 'sql-viewer'],
    ['/credits', 'credits'],
    ['/garbage-bags', 'garbage-bags'],
    ['/airplanes', 'airplanes'],
    ['/transit', 'transit'],
    ['/train-seats', 'train-seats'],
    ['/staff-hours', 'staff-hours'],
    ['/mayor-dog-mods', 'mayor-dog-mods'],
    ['/inventory-loadouts', 'inventory-loadouts'],
    ['/lemma-build', 'lemma-build'],
    ['/lemma-completion', 'lemma-completion'],
    ['/story-board', 'story-board'],
    ['/project-calendar', 'project-calendar'],
    ['/budget-dashboard', 'budget-dashboard'],
    ['/payroll', 'payroll'],
    ['/game-dimensions', 'game-dimensions'],
    ['/webcam-animations', 'webcam-animations'],
    ['/legal-tracker', 'legal-tracker'],
    ['/docket-watch', 'docket-watch'],
    ['/chat-entitlements', 'chat-entitlements'],
    ['/chat-lexicon', 'chat-lexicon'],
    ['/chat-tos', 'chat-entitlements'],
    ['/settings', 'settings'],
    ['/ui', 'hub'],
    ['/library', 'library'],
  ];

  function detectAppIdFromPath(pathname) {
    var path = String(pathname == null ? (typeof location !== 'undefined' ? location.pathname : '') : pathname);
    var search = '';
    try {
      search = typeof location !== 'undefined' ? (location.search || '') : '';
    } catch (_) { /* ignore */ }
    var params = new URLSearchParams(search);
    if (params.get('panel') === 'upload' || params.get('upload') === '1') return 'import';
    for (var i = 0; i < PATH_APP_MARKERS.length; i++) {
      if (path.indexOf(PATH_APP_MARKERS[i][0]) >= 0) return PATH_APP_MARKERS[i][1];
    }
    if (path === '/' || path === '') return 'library';
    return null;
  }

  /**
   * Per-app Game/Dimension send policy. Prefer ContinuuuumNav matrix when loaded.
   */
  function getGdPolicy(pathname) {
    var Nav = global.ContinuuuumNav;
    if (Nav && typeof Nav.gdPolicyForApp === 'function') {
      var appId = null;
      if (pathname != null && pathname !== '') {
        appId = detectAppIdFromPath(pathname);
      } else if (typeof Nav.detectApp === 'function') {
        appId = Nav.detectApp();
      } else {
        appId = detectAppIdFromPath();
      }
      return Nav.gdPolicyForApp(appId);
    }
    var fallbackId = detectAppIdFromPath(pathname);
    // Fallback matrix when nav script is absent (same locked table).
    var FALLBACK = {
      library: { game: true, dimension: true },
      import: { game: true, dimension: true },
      lemma: { game: true, dimension: true },
      hub: { game: true, dimension: false },
      'story-board': { game: true, dimension: false },
      'project-calendar': { game: true, dimension: false },
      'budget-dashboard': { game: true, dimension: false },
      payroll: { game: false, dimension: false },
      'game-dimensions': { game: false, dimension: false },
      'webcam-animations': { game: true, dimension: true },
      'legal-tracker': { game: false, dimension: false },
      'docket-watch': { game: false, dimension: false },
      'chat-entitlements': { game: false, dimension: false },
      'chat-lexicon': { game: false, dimension: false },
      network: { game: true, dimension: true },
      cities: { game: true, dimension: true },
      society: { game: true, dimension: true },
      restaurants: { game: true, dimension: true },
      stations: { game: true, dimension: true },
      keycards: { game: true, dimension: true },
      'vehicle-inventory': { game: true, dimension: true },
      'phone-wires': { game: true, dimension: true },
      camera: { game: true, dimension: true },
      'table-read': { game: true, dimension: false },
      'sql-viewer': { game: false, dimension: false },
      credits: { game: true, dimension: false },
      'garbage-bags': { game: true, dimension: true },
      airplanes: { game: true, dimension: true },
      transit: { game: true, dimension: true },
      'train-seats': { game: true, dimension: true },
      'staff-hours': { game: true, dimension: true },
      'mayor-dog-mods': { game: true, dimension: true },
      'inventory-loadouts': { game: true, dimension: true },
      'lemma-build': { game: true, dimension: true },
      'lemma-completion': { game: true, dimension: true },
      settings: { game: false, dimension: false },
    };
    return FALLBACK[fallbackId] || { game: false, dimension: false };
  }

  /** @deprecated Prefer getGdPolicy().game / .dimension — kept for callers that only knew finance strip. */
  function isFinanceProvince(pathname) {
    var p = getGdPolicy(pathname);
    return !p.game && !p.dimension;
  }

  /** True when either Game or Dimension context should be sent. */
  function usesGameDimensionContext(pathname) {
    var p = getGdPolicy(pathname);
    return !!(p.game || p.dimension);
  }

  function getHeaders(extra) {
    var h = { 'X-User-ID': getUserId() };
    var policy = getGdPolicy();
    if (policy.game) h['X-Game'] = getGame();
    if (policy.dimension) h['X-Dimension'] = getDimension();
    if (isAdmin()) h['X-Admin'] = '1';
    if (extra) {
      Object.keys(extra).forEach(function (k) {
        if (k === 'X-Game' && !policy.game) return;
        if (k === 'X-Dimension' && !policy.dimension) return;
        h[k] = extra[k];
      });
    }
    return h;
  }

  function onChange(fn) {
    if (typeof fn === 'function') listeners.push(fn);
    return function () {
      var i = listeners.indexOf(fn);
      if (i >= 0) listeners.splice(i, 1);
    };
  }

  function isPresent() {
    var uid = getUserId();
    return !!uid && uid.toLowerCase() !== 'anonymous';
  }

  /** Apply a developer quick-pick identity. */
  function applyPreset(id) {
    var key = String(id || '').trim();
    if (!key) return;
    setDevMode(true);
    if (key === 'admin') {
      setAdmin(true);
      setUserId('admin');
    } else {
      setAdmin(false);
      setUserId(key);
    }
  }

  function presetButtonsHtml(className) {
    var cls = className || 'continuuuum-preset-btn';
    return PRESETS.map(function (p) {
      return '<button type="button" class="' + cls + '" data-preset="' + p + '">' + p + '</button>';
    }).join('');
  }

  function wirePresetButtons(root, onPicked) {
    if (!root) return;
    root.querySelectorAll('[data-preset]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        applyPreset(btn.getAttribute('data-preset'));
        if (typeof onPicked === 'function') onPicked(btn.getAttribute('data-preset'));
      });
    });
  }

  function injectGateStyles() {
    if (styleInjected || typeof document === 'undefined') return;
    styleInjected = true;
    var style = document.createElement('style');
    style.id = 'continuuuum-login-gate-style';
    style.textContent =
      '#continuuuum-login-gate{position:fixed;inset:0;z-index:10000;background:rgba(12,16,22,.92);' +
      'display:flex;align-items:center;justify-content:center;padding:24px;}' +
      '#continuuuum-login-gate[hidden]{display:none!important;}' +
      '.continuuuum-login-card{background:#161b22;color:#e6edf3;border:1px solid #30363d;border-radius:10px;' +
      'max-width:420px;width:100%;padding:22px 24px;box-shadow:0 12px 40px rgba(0,0,0,.45);}' +
      '.continuuuum-login-card h2{margin:0 0 8px;font-size:1.25rem;}' +
      '.continuuuum-login-card p{margin:0 0 14px;color:#8b949e;font-size:.95rem;line-height:1.4;}' +
      '.continuuuum-login-card label{display:block;font-size:.85rem;margin-bottom:4px;}' +
      '.continuuuum-login-card input[type=text]{width:100%;box-sizing:border-box;padding:8px 10px;' +
      'border-radius:6px;border:1px solid #30363d;background:#0d1117;color:#e6edf3;margin-bottom:10px;}' +
      '.continuuuum-login-actions{display:flex;gap:8px;margin-bottom:14px;}' +
      '.continuuuum-login-actions button,.continuuuum-login-presets button{cursor:pointer;}' +
      '.continuuuum-login-continue{background:#238636;color:#fff;border:0;border-radius:6px;padding:8px 14px;}' +
      '.continuuuum-login-presets{display:flex;flex-wrap:wrap;gap:6px;}' +
      '.continuuuum-login-presets button{background:#21262d;color:#e6edf3;border:1px solid #30363d;' +
      'border-radius:999px;padding:4px 10px;font-size:12px;}' +
      '.continuuuum-login-presets button:hover{border-color:#58a6ff;color:#58a6ff;}' +
      '.continuuuum-login-dev-label{font-size:12px;color:#8b949e;margin:0 0 6px;}';
    document.head.appendChild(style);
  }

  function hideGate() {
    if (gateEl) {
      gateEl.hidden = true;
    }
  }

  /**
   * Promise resolves when a non-anonymous user is set.
   * Shows a login overlay with free-text + developer presets when needed.
   */
  function ensurePresent(opts) {
    opts = opts || {};
    if (isPresent()) {
      return Promise.resolve({ userId: getUserId() });
    }
    injectGateStyles();
    return new Promise(function (resolve) {
      if (!gateEl) {
        gateEl = document.createElement('div');
        gateEl.id = 'continuuuum-login-gate';
        document.body.appendChild(gateEl);
      }
      gateEl.hidden = false;
      gateEl.innerHTML =
        '<div class="continuuuum-login-card" role="dialog" aria-modal="true" aria-labelledby="continuuuum-login-title">' +
          '<h2 id="continuuuum-login-title">' + (opts.title || 'Sign in required') + '</h2>' +
          '<p>This page needs a user identity (not anonymous). Pick a developer account or enter a user id.</p>' +
          '<label for="continuuuum-login-input">User ID</label>' +
          '<input id="continuuuum-login-input" type="text" placeholder="your-user-id" autocomplete="username" />' +
          '<div class="continuuuum-login-actions">' +
            '<button type="button" class="continuuuum-login-continue" id="continuuuum-login-continue">Continue</button>' +
          '</div>' +
          '<p class="continuuuum-login-dev-label">Developer mode quick pick</p>' +
          '<div class="continuuuum-login-presets">' + presetButtonsHtml() + '</div>' +
        '</div>';

      function finish() {
        if (!isPresent()) return;
        hideGate();
        resolve({ userId: getUserId() });
      }

      var input = gateEl.querySelector('#continuuuum-login-input');
      gateEl.querySelector('#continuuuum-login-continue').addEventListener('click', function () {
        var v = (input && input.value) || '';
        if (!String(v).trim() || String(v).trim().toLowerCase() === 'anonymous') {
          input && input.focus();
          return;
        }
        setDevMode(true);
        setAdmin(String(v).trim() === 'admin');
        setUserId(v);
        finish();
      });
      if (input) {
        input.addEventListener('keydown', function (e) {
          if (e.key === 'Enter') {
            gateEl.querySelector('#continuuuum-login-continue').click();
          }
        });
        setTimeout(function () { input.focus(); }, 0);
      }
      wirePresetButtons(gateEl, function () { finish(); });
    });
  }

  bootstrapFromQuery();

  global.ContinuuuumUserSession = {
    PRESETS: PRESETS,
    getUserId: getUserId,
    setUserId: setUserId,
    isDevMode: isDevMode,
    setDevMode: setDevMode,
    isAdmin: isAdmin,
    setAdmin: setAdmin,
    getGame: getGame,
    setGame: setGame,
    getDimension: getDimension,
    setDimension: setDimension,
    getQuery: getQuery,
    appendGameDimensionQuery: appendGameDimensionQuery,
    confirmCreateDimensionGate: confirmCreateDimensionGate,
    getGdPolicy: getGdPolicy,
    isFinanceProvince: isFinanceProvince,
    usesGameDimensionContext: usesGameDimensionContext,
    getHeaders: getHeaders,
    onChange: onChange,
    isPresent: isPresent,
    applyPreset: applyPreset,
    ensurePresent: ensurePresent,
    presetButtonsHtml: presetButtonsHtml,
    wirePresetButtons: wirePresetButtons,
  };
})(typeof window !== 'undefined' ? window : globalThis);
