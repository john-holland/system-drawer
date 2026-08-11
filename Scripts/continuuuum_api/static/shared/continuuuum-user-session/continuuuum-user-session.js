(function (global) {
  'use strict';

  var USER_KEY = 'continuuuumUserId';
  var DEV_KEY = 'continuuuumDevMode';
  var ADMIN_KEY = 'continuuuumAdminMode';
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
    var detail = { userId: getUserId(), devMode: isDevMode(), adminMode: isAdmin(), kind: kind || 'change' };
    listeners.forEach(function (fn) {
      try { fn(detail); } catch (_) { /* ignore */ }
    });
    try {
      global.dispatchEvent(new CustomEvent('continuuuum-user-changed', { detail: detail }));
    } catch (_) { /* ignore */ }
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

  function getHeaders(extra) {
    var h = { 'X-User-ID': getUserId() };
    if (isAdmin()) h['X-Admin'] = '1';
    if (extra) {
      Object.keys(extra).forEach(function (k) { h[k] = extra[k]; });
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
    getHeaders: getHeaders,
    onChange: onChange,
    isPresent: isPresent,
    applyPreset: applyPreset,
    ensurePresent: ensurePresent,
    presetButtonsHtml: presetButtonsHtml,
    wirePresetButtons: wirePresetButtons,
  };
})(typeof window !== 'undefined' ? window : globalThis);
