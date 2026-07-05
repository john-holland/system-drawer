/**
 * Continuuuum Cave shell: login, presence, RobotCopy / caveRoute delegation, session bridge.
 */
(function (global) {
  'use strict';

  var presenceTimer = null;
  var manifestMessages = null;

  function traceId() {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) return crypto.randomUUID();
    return 'trace-' + Date.now() + '-' + Math.random().toString(16).slice(2);
  }

  function api(path, opts) {
    opts = opts || {};
    var headers = Object.assign(
      { 'Content-Type': 'application/json' },
      global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getHeaders() : {},
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

  function loadManifestMessages() {
    if (manifestMessages) return Promise.resolve(manifestMessages);
    return api('/api/config/overview').then(function (data) {
      manifestMessages = (data.manifest && data.manifest.messages) || {};
      return manifestMessages;
    }).catch(function () {
      manifestMessages = {};
      return manifestMessages;
    });
  }

  function caveRoute(route, payload, opts) {
    opts = opts || {};
    var body = {
      schema_version: '2.0',
      route: route,
      payload: payload || {},
      trace_id: opts.traceId || traceId(),
      reply_mode: 'sync_http',
    };
    if (opts.tenant) body.tenant = opts.tenant;
    return api('/cave/route', { method: 'POST', body: JSON.stringify(body) });
  }

  function caveMessage(message, payload, opts) {
    return loadManifestMessages().then(function (messages) {
      var structural = messages[message];
      if (!structural) {
        return Promise.reject(new Error('Unknown cave message: ' + message));
      }
      return caveRoute('continuuuum:' + structural, payload, opts);
    });
  }

  function RobotCopy(config) {
    this.apiBase = (config && config.apiBase) || '';
    this.tomeId = (config && config.tomeId) || 'continuuuum';
    this._tomeCache = null;
  }

  RobotCopy.prototype._loadTome = function () {
    var self = this;
    if (self._tomeCache) return Promise.resolve(self._tomeCache);
    return api('/api/tomes/' + encodeURIComponent(self.tomeId)).then(function (tome) {
      self._tomeCache = tome;
      return tome;
    });
  };

  RobotCopy.prototype._resolveRoute = function (tomeId, machineId, event) {
    var self = this;
    return self._loadTome().then(function (tome) {
      var machines = tome.machines || {};
      var machine = machines[machineId] || {};
      var events = machine.events || {};
      var messageName = events[event] || events[String(event).toUpperCase()] || events[String(event).toLowerCase()];
      if (!messageName) {
        var flows = (tome.robotCopy && tome.robotCopy.flows) || {};
        var flow = flows[event] || flows[String(event).toLowerCase()];
        if (flow && flow.message) messageName = flow.message;
      }
      if (!messageName) {
        return Promise.reject(new Error('No route for ' + tomeId + '/' + machineId + ' event ' + event));
      }
      return loadManifestMessages().then(function (messages) {
        var structural = messages[messageName];
        if (!structural) {
          return Promise.reject(new Error('Unknown message alias: ' + messageName));
        }
        return 'continuuuum:' + structural;
      });
    });
  };

  RobotCopy.prototype.sendMessage = function (machineRoute, payload) {
    var parts = String(machineRoute).split('/');
    var tomeId = parts[0] || this.tomeId;
    var machineId = parts[1] || parts[0];
    if (parts.length === 1) machineId = parts[0];
    var event = (payload && payload.event) || 'MESSAGE';
    var data = (payload && payload.data) || payload || {};
    var self = this;
    return self._resolveRoute(tomeId, machineId, event).then(function (route) {
      return caveRoute(route, data);
    }).then(function (resp) {
      return { ok: true, result: resp, tomeId: tomeId, machineId: machineId, event: event };
    });
  };

  function checkPreorderGate() {
    return caveMessage('legal_preordering_gate').then(function (data) {
      var gate = data.gate || {};
      if (gate.status === 'blocked' || gate.status === 'investigating') {
        global.dispatchEvent(new CustomEvent('continuuuum-preorder-blocked', { detail: data }));
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
          user: global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getUserId() : 'anonymous',
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
      if (global.ContinuuuumUserSession && data.user) {
        global.ContinuuuumUserSession.setUserId(data.user);
      }
      return data;
    });
  }

  function init(opts) {
    opts = opts || {};
    var tomeId = opts.tomeId || 'continuuuum';
    var robotCopy = new RobotCopy({ tomeId: tomeId, apiBase: opts.apiBase || '' });
    if (opts.presence !== false) startPresence(tomeId);
    checkPreorderGate();
    return {
      tomeId: tomeId,
      robotCopy: robotCopy,
      api: api,
      caveRoute: caveRoute,
      caveMessage: caveMessage,
      login: login,
      stopPresence: stopPresence,
    };
  }

  global.ContinuuuumCaveShell = {
    init: init,
    RobotCopy: RobotCopy,
    api: api,
    caveRoute: caveRoute,
    caveMessage: caveMessage,
    login: login,
    checkPreorderGate: checkPreorderGate,
  };

  global.ContinuuuumCaveClient = global.ContinuuuumCaveShell;
})(typeof window !== 'undefined' ? window : globalThis);
