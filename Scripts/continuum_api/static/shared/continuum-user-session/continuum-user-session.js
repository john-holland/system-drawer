(function (global) {
  'use strict';

  var USER_KEY = 'continuumUserId';
  var DEV_KEY = 'continuumDevMode';
  var ADMIN_KEY = 'continuumAdminMode';
  var listeners = [];

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
      global.dispatchEvent(new CustomEvent('continuum-user-changed', { detail: detail }));
    } catch (_) { /* ignore */ }
  }

  function getUserId() {
    return normUser(localStorage.getItem(USER_KEY));
  }

  function setUserId(id) {
    var next = normUser(id);
    if (next === getUserId()) return;
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

  bootstrapFromQuery();

  global.ContinuumUserSession = {
    getUserId: getUserId,
    setUserId: setUserId,
    isDevMode: isDevMode,
    setDevMode: setDevMode,
    isAdmin: isAdmin,
    setAdmin: setAdmin,
    getHeaders: getHeaders,
    onChange: onChange,
  };
})(typeof window !== 'undefined' ? window : globalThis);
