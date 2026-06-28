/**
 * Continuum Cave shell: login, presence, RobotCopy message delegation, session bridge.
 */
(function (global) {
  'use strict';

  var presenceTimer = null;

  function api(path, opts) {
    opts = opts || {};
    var headers = Object.assign(
      { 'Content-Type': 'application/json' },
      global.ContinuumUserSession ? global.ContinuumUserSession.getHeaders() : {},
      opts.headers || {}
    );
    return fetch(path, Object.assign({ credentials: 'include', headers: headers }, opts)).then(function (r) {
      return r.json().catch(function () { return {}; }).then(function (data) {
        if (!r.ok) {
          var err = new Error(data.error || r.statusText);
          err.status = r.status;
          err.body = data;
          throw err;
        }
        return data;
      });
    });
  }

  function RobotCopy(config) {
    this.apiBase = (config && config.apiBase) || '';
    this.tomeId = (config && config.tomeId) || 'continuum';
  }

  RobotCopy.prototype.sendMessage = function (machineRoute, payload) {
    var parts = String(machineRoute).split('/');
    var tomeId = parts[0] || this.tomeId;
    var machineId = parts[1] || parts[0];
    if (parts.length === 1) machineId = parts[0];
    return api('/api/tomes/' + encodeURIComponent(tomeId) + '/machines/' + encodeURIComponent(machineId) + '/message', {
      method: 'POST',
      body: JSON.stringify({
        event: (payload && payload.event) || 'MESSAGE',
        data: (payload && payload.data) || payload || {},
      }),
    });
  };

  function checkPreorderGate() {
    return api('/api/legal/platform-features/preordering').then(function (data) {
      var gate = data.gate || {};
      if (gate.status === 'blocked' || gate.status === 'investigating') {
        global.dispatchEvent(new CustomEvent('continuum-preorder-blocked', { detail: data }));
      }
      return data;
    }).catch(function () { return null; });
  }

  function startPresence(tomeId) {
    stopPresence();
    function tick() {
      api('/api/editor/presence?caveOrTomeId=' + encodeURIComponent(tomeId)).catch(function () {});
      api('/api/editor/presence', {
        method: 'POST',
        body: JSON.stringify({
          caveOrTomeId: tomeId,
          user: global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous',
          location: location.pathname,
        }),
      }).catch(function () {});
    }
    tick();
    presenceTimer = setInterval(tick, 15000);
  }

  function stopPresence() {
    if (presenceTimer) {
      clearInterval(presenceTimer);
      presenceTimer = null;
    }
  }

  function login(username, password) {
    return api('/api/login', {
      method: 'POST',
      body: JSON.stringify({ username: username, password: password || '' }),
    }).then(function (data) {
      if (global.ContinuumUserSession && data.user) {
        global.ContinuumUserSession.setUserId(data.user);
      }
      return data;
    });
  }

  function init(opts) {
    opts = opts || {};
    var tomeId = opts.tomeId || 'continuum';
    var robotCopy = new RobotCopy({ tomeId: tomeId, apiBase: opts.apiBase || '' });
    if (opts.presence !== false) startPresence(tomeId);
    checkPreorderGate();
    return {
      tomeId: tomeId,
      robotCopy: robotCopy,
      api: api,
      login: login,
      stopPresence: stopPresence,
    };
  }

  global.ContinuumCaveShell = {
    init: init,
    RobotCopy: RobotCopy,
    api: api,
    login: login,
    checkPreorderGate: checkPreorderGate,
  };
})(typeof window !== 'undefined' ? window : globalThis);
