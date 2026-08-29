(function () {
  'use strict';

  var GLL = window.GameLobbyList;
  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'votes' });
  }

  function el(id) { return document.getElementById(id); }

  function card(html) {
    var d = document.createElement('div');
    d.className = 'votes-card';
    d.innerHTML = html;
    return d;
  }

  function kindLabel(kind) {
    if (kind === 'Measure') return 'measures';
    if (kind === 'Candidate') return 'candidates';
    return 'questions';
  }

  function defaultOptions(kind) {
    if (kind === 'Candidate') return [{ optionId: '', displayName: '' }];
    return [
      { optionId: 'yes', displayName: 'Yes' },
      { optionId: 'no', displayName: 'No' }
    ];
  }

  function renderOptions(options, kind) {
    var root = el('votes-options');
    root.innerHTML = '';
    el('votes-list-label').textContent = kindLabel(kind);
    (options && options.length ? options : defaultOptions(kind)).forEach(function (opt) {
      var row = document.createElement('div');
      row.className = 'votes-option';
      row.innerHTML =
        '<input data-opt-id placeholder="id" value="' + (opt.optionId || '') + '">' +
        '<input data-opt-name placeholder="display" value="' + (opt.displayName || '') + '">' +
        '<button type="button" data-remove-opt>Remove</button>';
      root.appendChild(row);
    });
  }

  function renderDemo(demo) {
    var root = el('votes-demo');
    root.innerHTML = '';
    var slices = (demo && demo.slices) || [];
    if (!slices.length) {
      slices = [
        { sliceId: 'dem', groupProperty: 'party', groupValue: 'democrat', share01: 0.5, yesTilt01: 0.62 },
        { sliceId: 'rep', groupProperty: 'party', groupValue: 'republican', share01: 0.5, yesTilt01: 0.38 }
      ];
    }
    slices.forEach(function (s) {
      var row = document.createElement('div');
      row.className = 'votes-slice';
      row.innerHTML =
        '<input data-sl-id placeholder="slice id" value="' + (s.sliceId || '') + '">' +
        '<input data-sl-prop placeholder="property" value="' + (s.groupProperty || 'party') + '">' +
        '<input data-sl-val placeholder="value" value="' + (s.groupValue || '') + '">' +
        '<input data-sl-share type="number" step="0.05" min="0" max="1" value="' + (s.share01 != null ? s.share01 : 0.5) + '" title="share">' +
        '<input data-sl-tilt type="number" step="0.05" min="0" max="1" value="' + (s.yesTilt01 != null ? s.yesTilt01 : 0.5) + '" title="yes tilt">' +
        '<button type="button" data-remove-slice>Remove</button>';
      root.appendChild(row);
    });
  }

  function readOptions() {
    return Array.prototype.map.call(el('votes-options').querySelectorAll('.votes-option'), function (row) {
      return {
        optionId: row.querySelector('[data-opt-id]').value.trim(),
        displayName: row.querySelector('[data-opt-name]').value.trim()
      };
    }).filter(function (o) { return o.optionId; });
  }

  function readDemo() {
    var slices = Array.prototype.map.call(el('votes-demo').querySelectorAll('.votes-slice'), function (row) {
      return {
        sliceId: row.querySelector('[data-sl-id]').value.trim(),
        groupProperty: row.querySelector('[data-sl-prop]').value.trim(),
        groupValue: row.querySelector('[data-sl-val]').value.trim(),
        share01: Number(row.querySelector('[data-sl-share]').value),
        yesTilt01: Number(row.querySelector('[data-sl-tilt]').value)
      };
    }).filter(function (s) { return s.sliceId || s.groupValue; });
    return { slices: slices };
  }

  function formatShare(n) {
    return (Math.round(Number(n) * 100) / 100).toFixed(2);
  }

  function shareInputs() {
    return Array.prototype.slice.call(el('votes-demo').querySelectorAll('[data-sl-share]'));
  }

  function reconcileDemographicShares(shares, changedIndex) {
    var n = (shares || []).length;
    if (!n) return [];
    if (n === 1) return [1];
    function units(v) {
      var u = Math.round(Number(v) * 100);
      if (!isFinite(u)) u = 0;
      return Math.max(0, Math.min(100, u));
    }
    if (changedIndex == null || changedIndex < 0 || changedIndex >= n) {
      var all = Math.floor(100 / n);
      var allExtra = 100 - all * n;
      return shares.map(function (_, i) { return (all + (i === n - 1 ? allExtra : 0)) / 100; });
    }
    var changed = units(shares[changedIndex]);
    var remainder = 100 - changed;
    var others = n - 1;
    var even = Math.floor(remainder / others);
    var extra = remainder - even * others;
    var lastOther = changedIndex === n - 1 ? n - 2 : n - 1;
    return shares.map(function (_, i) {
      if (i === changedIndex) return changed / 100;
      return (even + (i === lastOther ? extra : 0)) / 100;
    });
  }

  function applyShareReconcile(changedInput, rewriteChanged) {
    var inputs = shareInputs();
    if (!inputs.length) return;
    var idx = changedInput ? inputs.indexOf(changedInput) : -1;
    var shares = inputs.map(function (inp) { return Number(inp.value); });
    if (idx >= 0) {
      if (shares[idx] > 1) { changedInput.value = '1'; shares[idx] = 1; }
      if (shares[idx] < 0) { changedInput.value = '0'; shares[idx] = 0; }
    }
    var next = reconcileDemographicShares(shares, idx >= 0 ? idx : null);
    inputs.forEach(function (inp, i) {
      if (i === idx && !rewriteChanged) return;
      inp.value = formatShare(next[i]);
    });
  }

  function readGov() {
    return {
      republic01: Number(el('gov-republic').value),
      parliamentary01: Number(el('gov-parl').value),
      theocracy01: Number(el('gov-theo').value),
      monarchyCeremonial01: Number(el('gov-cer').value),
      monarchyReal01: Number(el('gov-real').value),
      junta01: Number(el('gov-junta').value),
      parliamentarySenateEnablesTheocracy: el('gov-senate-theo').checked
    };
  }

  function fillGov(mix) {
    mix = mix || {};
    el('gov-republic').value = mix.republic01 != null ? mix.republic01 : 0.7;
    el('gov-parl').value = mix.parliamentary01 || 0;
    el('gov-theo').value = mix.theocracy01 || 0;
    el('gov-cer').value = mix.monarchyCeremonial01 || 0;
    el('gov-real').value = mix.monarchyReal01 || 0;
    el('gov-junta').value = mix.junta01 || 0;
    el('gov-senate-theo').checked = !!mix.parliamentarySenateEnablesTheocracy;
  }

  function syncMethodRow() {
    var kind = el('votes-ballot-kind').value;
    var method = el('votes-tally-method').value;
    el('votes-method-row').style.display = kind === 'Candidate' ? '' : 'none';
    el('votes-seats-wrap').style.display = method === 'stv' ? '' : 'none';
  }

  function fillBallot(b) {
    el('votes-ballot-name').value = (b && b.name) || '';
    el('votes-ballot-kind').value = (b && b.kind) || 'Question';
    el('votes-ballot-title').value = (b && b.title) || 'Ballot';
    el('votes-ballot-prompt').value = (b && b.prompt) || '';
    el('votes-tally-method').value = (b && b.tallyMethod) || (b && b.spec && b.spec.tallyMethod) || 'plurality';
    el('votes-seats').value = (b && b.seats) || (b && b.spec && b.spec.seats) || 2;
    renderOptions((b && b.options) || (b && b.spec && b.spec.options), el('votes-ballot-kind').value);
    renderDemo((b && b.demographics) || (b && b.spec && b.spec.demographics));
    fillGov((b && b.govMix) || {});
    syncMethodRow();
    var err = el('votes-ballot-err');
    if (b && b.errors && b.errors.length) {
      err.hidden = false;
      err.textContent = b.errors.join(' · ');
    } else {
      err.hidden = true;
    }
  }

  function sessionItems(data) {
    if (Array.isArray(data)) return data;
    return (data && data.items) || [];
  }

  function includesQ(text, q) {
    return String(text == null ? '' : text).toLowerCase().indexOf(q) !== -1;
  }

  function propertyValues(props) {
    if (typeof props === 'string') {
      try { props = JSON.parse(props); } catch (err) { return props ? [props] : []; }
    }
    if (props == null) return [];
    if (typeof props !== 'object') return [String(props)];
    var out = [];
    (function walk(v) {
      if (v == null) return;
      if (typeof v === 'object') {
        if (Array.isArray(v)) v.forEach(walk);
        else Object.keys(v).forEach(function (k) { walk(v[k]); });
        return;
      }
      out.push(String(v));
    })(props);
    return out;
  }

  function matchesPropertyValue(props, needle) {
    return propertyValues(props).some(function (v) { return includesQ(v, needle); });
  }

  function ballotMatches(b, q) {
    if (!b || !q) return false;
    return includesQ(b.kind, q) || includesQ(b.listLabel, q) || includesQ(b.role, q) ||
      includesQ(b.name, q) || includesQ(b.title, q) || includesQ(b.prompt, q);
  }

  function sessionSelfMatch(s, q, matchingBallotIds) {
    if (!s) return false;
    if (includesQ(s.displayName, q) || includesQ(s.id, q) || includesQ(s.lobbySessionName, q)) return true;
    var vc = s.voteConfig || {};
    if (includesQ(vc.lastKind, q) || includesQ(vc.lastBallotId, q)) return true;
    return (s.runs || []).some(function (run) {
      return includesQ(run.ballotId, q) || includesQ(run.ballotKind, q) || !!(matchingBallotIds && matchingBallotIds[run.ballotId]);
    });
  }

  function lobbySelfMatch(lb, cfg, q) {
    if (!lb) return false;
    if (includesQ(lb.name, q) || includesQ(lb.displayName, q) || includesQ(lb.lobbyTypeId, q) ||
      includesQ(lb.contentKind, q) || includesQ(lb.contentId, q) || includesQ(lb.mode, q)) return true;
    if (matchesPropertyValue(lb.propertiesJson, q)) return true;
    if (!cfg) return false;
    return includesQ(cfg.name, q) || includesQ(cfg.id, q) || includesQ(cfg.lobbyTypeId, q) ||
      includesQ(cfg.contentId, q) || matchesPropertyValue(cfg.propertiesJson, q);
  }

  function matchingBallotIds(ballots, q) {
    var ids = {};
    (ballots || []).forEach(function (b) {
      if (ballotMatches(b, q) && b.name) ids[b.name] = true;
    });
    return ids;
  }

  function filterLobbyData(data, query, ballots) {
    var q = (query || '').trim().toLowerCase();
    var configs = (data && data.configs) || [];
    var lobbies = (data && data.lobbies) || [];
    if (!q) return { configs: configs, lobbies: lobbies };
    var byId = {};
    configs.forEach(function (c) { byId[c.id] = c; });
    var keepAllUnder = {};
    configs.forEach(function (c) {
      if (includesQ(c.name, q) || includesQ(c.id, q) || matchesPropertyValue(c.propertiesJson, q)) keepAllUnder[c.id] = true;
    });
    var ballotIds = matchingBallotIds(ballots, q);
    var sessionIds = [];
    var kept = [];
    var keepCfg = {};
    Object.keys(keepAllUnder).forEach(function (id) { keepCfg[id] = true; });
    lobbies.forEach(function (lb) {
      var cfg = byId[lb.configId];
      var self = !!(keepAllUnder[lb.configId] || lobbySelfMatch(lb, cfg, q));
      var hitSessions = (lb.sessions || []).filter(function (s) { return sessionSelfMatch(s, q, ballotIds); });
      if (!self && !hitSessions.length) return;
      kept.push(lb);
      if (lb.configId) keepCfg[lb.configId] = true;
      (self ? (lb.sessions || []) : hitSessions).forEach(function (s) {
        if (s && s.id) sessionIds.push(s.id);
      });
    });
    return {
      configs: configs.filter(function (c) { return keepCfg[c.id]; }),
      lobbies: kept,
      filteredSessionIds: sessionIds
    };
  }

  function runMatches(run, q, ballotIds, ballotsByName) {
    if (!run) return false;
    if (includesQ(run.ballotId, q) || includesQ(run.ballotKind, q) || includesQ(run.runId, q) ||
      includesQ(run.gameSessionId, q)) return true;
    if (ballotIds && ballotIds[run.ballotId]) return true;
    return ballotMatches(ballotsByName && ballotsByName[run.ballotId], q);
  }

  var pageData = { configs: [], lobbies: [], ballots: [], runs: [], results: [] };

  function ballotsByName() {
    var map = {};
    (pageData.ballots || []).forEach(function (b) { if (b && b.name) map[b.name] = b; });
    return map;
  }

  function paintBallots(list) {
    var broot = el('votes-ballots');
    broot.innerHTML = '';
    (list || []).forEach(function (b) {
      var names = (b.options || []).map(function (o) { return o.displayName || o.optionId; }).join(', ');
      var errs = (b.errors || []).join(' · ');
      broot.appendChild(card(
        '<strong>Ballot</strong> ' + (b.title || b.name) +
        ' <span class="votes-kind">' + (b.kind || '') + ' · ' + (b.tallyMethod || 'plurality') +
        (b.tallyMethod === 'stv' ? ' · ' + (b.seats || 2) + ' seats' : '') +
        ' · ' + (b.listLabel || kindLabel(b.kind)) + '</span>' +
        '<div>' + (names || '—') + '</div>' +
        (errs ? '<div class="votes-err">' + errs + '</div>' : '') +
        '<button type="button" data-pick-ballot="' + b.name + '">Edit</button>' +
        '<button type="button" data-build-named="' + b.name + '">Build</button>' +
        '<button type="button" data-remove-ballot="' + b.name + '">Remove</button>'
      ));
    });
  }

  function paintRuns(list) {
    var rroot = el('votes-runs');
    rroot.innerHTML = '';
    (list || []).forEach(function (r) {
      rroot.appendChild(card(
        '<div>run ' + r.runId + ' session ' + (r.gameSessionId || '') + '</div>' +
        '<div>ballot ' + (r.ballotId || '') + ' certified ' + r.certified +
        (r.tally && r.tally.method ? ' · ' + r.tally.method : '') +
        (r.tally && r.tally.winners ? ' winners ' + (r.tally.winners || []).join(', ') : '') + '</div>' +
        '<div>votes/player ' + JSON.stringify(r.votesPerPlayer || []) + '</div>' +
        '<div>demographic % ' + JSON.stringify(r.votesPerDemographic || []) + '</div>' +
        '<div>actors ' + JSON.stringify(r.actorVotes || []) + '</div>' +
        '<button data-recount="' + r.runId + '">Recount</button>' +
        '<button data-certify="' + r.runId + '">Certify</button>'
      ));
    });
  }

  function paintResults(list) {
    var res = el('votes-results');
    res.innerHTML = '';
    (list || []).forEach(function (r) {
      res.appendChild(card(
        '<div>' + r.runId + ' hash ' + r.tallyHash + '</div>' +
        '<pre>' + JSON.stringify({ tally: r.tally, votesPerPlayer: r.votesPerPlayer, votesPerDemographic: r.votesPerDemographic, actorVotes: r.actorVotes }, null, 2) + '</pre>'
      ));
    });
  }

  function applyFilter() {
    var q = el('votes-lobby-filter') ? el('votes-lobby-filter').value : '';
    var needle = (q || '').trim().toLowerCase();
    var lobbyView = filterLobbyData(pageData, needle, pageData.ballots);
    GLL.render(el('votes-lobbies'), lobbyView, { showVotes: true });
    if (!needle) {
      paintBallots(pageData.ballots);
      paintRuns(pageData.runs);
      paintResults(pageData.results);
      return;
    }
    var ids = matchingBallotIds(pageData.ballots, needle);
    var byName = ballotsByName();
    paintBallots((pageData.ballots || []).filter(function (b) { return ballotMatches(b, needle); }));
    var runs = (pageData.runs || []).filter(function (r) { return runMatches(r, needle, ids, byName); });
    var runIds = {};
    runs.forEach(function (r) { if (r.runId) runIds[r.runId] = true; });
    paintRuns(runs);
    paintResults((pageData.results || []).filter(function (r) {
      return runIds[r.runId] || runMatches(r, needle, ids, byName);
    }));
  }

  async function refresh() {
    var data = await GLL.fetchAll();
    pageData.configs = data.configs || [];
    pageData.lobbies = data.lobbies || [];
    pageData.ballots = await GLL.jget('/api/votes/ballots') || [];
    pageData.runs = await GLL.jget('/api/votes/runs') || [];
    pageData.results = await GLL.jget('/api/votes/results') || [];
    applyFilter();
  }

  async function activeSession() {
    var list = sessionItems(await GLL.jget('/api/game-sessions'));
    var q = new URLSearchParams(location.search).get('gameSessionId');
    return list.find(function (s) { return s.id === q; }) || list.find(function (s) { return s.active; }) || list[0];
  }

  async function saveBallot() {
    var kind = el('votes-ballot-kind').value;
    var body = {
      name: el('votes-ballot-name').value.trim(),
      kind: kind,
      title: el('votes-ballot-title').value.trim() || 'Ballot',
      prompt: el('votes-ballot-prompt').value,
      options: readOptions(),
      demographics: readDemo(),
      govMix: readGov(),
      tallyMethod: kind === 'Candidate' ? el('votes-tally-method').value : 'plurality',
      seats: Number(el('votes-seats').value) || 2
    };
    if (!body.name) throw new Error('name required');
    var saved = await GLL.jsend('/api/votes/ballots', 'POST', body);
    fillBallot(saved);
    return saved;
  }

  el('votes-ballot-kind').addEventListener('change', function () {
    renderOptions(readOptions(), el('votes-ballot-kind').value);
    syncMethodRow();
  });
  el('votes-tally-method').addEventListener('change', syncMethodRow);
  el('votes-add-option').addEventListener('click', function () {
    var opts = readOptions();
    opts.push({ optionId: '', displayName: '' });
    renderOptions(opts, el('votes-ballot-kind').value);
  });
  el('votes-add-slice').addEventListener('click', function () {
    var demo = readDemo();
    demo.slices.push({ sliceId: '', groupProperty: 'party', groupValue: '', share01: 0.1, yesTilt01: 0.5 });
    renderDemo(demo);
    applyShareReconcile(shareInputs()[shareInputs().length - 1], true);
  });
  el('votes-options').addEventListener('click', function (e) {
    if (e.target.getAttribute('data-remove-opt') == null) return;
    e.target.closest('.votes-option').remove();
  });
  el('votes-demo').addEventListener('click', function (e) {
    if (e.target.getAttribute('data-remove-slice') == null) return;
    e.target.closest('.votes-slice').remove();
    applyShareReconcile(null, true);
  });
  el('votes-demo').addEventListener('input', function (e) {
    if (e.target.getAttribute('data-sl-share') == null) return;
    applyShareReconcile(e.target, false);
  });
  el('votes-demo').addEventListener('change', function (e) {
    if (e.target.getAttribute('data-sl-share') == null) return;
    applyShareReconcile(e.target, true);
  });
  el('votes-ballot-form').addEventListener('submit', async function (e) {
    e.preventDefault();
    try { await saveBallot(); await refresh(); } catch (err) { alert(err.message); }
  });
  el('votes-build-ballot').addEventListener('click', async function () {
    try {
      var saved = await saveBallot();
      var gs = await activeSession();
      if (!gs) throw new Error('no session');
      var built = await GLL.jsend('/api/votes/ballots/' + encodeURIComponent(saved.name) + '/build', 'POST', { gameSessionId: gs.id });
      var err = el('votes-ballot-err');
      if (built.errors && built.errors.length) {
        err.hidden = false;
        err.textContent = built.errors.join(' · ');
      } else err.hidden = true;
      await refresh();
    } catch (err) { alert(err.message); }
  });
  el('votes-lobby-filter').addEventListener('input', applyFilter);
  el('votes-remove-ballot').addEventListener('click', async function () {
    try {
      var name = el('votes-ballot-name').value.trim();
      if (!name) throw new Error('name required');
      await GLL.jsend('/api/votes/ballots/' + encodeURIComponent(name), 'DELETE');
      fillBallot(null);
      await refresh();
    } catch (err) { alert(err.message); }
  });
  el('votes-create-run').addEventListener('click', async function () {
    var gs = await activeSession();
    await GLL.jsend('/api/votes/runs', 'POST', { gameSessionId: gs ? gs.id : '' });
    await refresh();
  });

  document.addEventListener('click', async function (e) {
    var t = e.target;
    if (!t || !t.getAttribute) return;
    var pick = t.getAttribute('data-pick-ballot');
    if (pick) {
      var b = await GLL.jget('/api/votes/ballots/' + encodeURIComponent(pick));
      fillBallot(b);
      return;
    }
    var build = t.getAttribute('data-build-named');
    if (build) {
      var gs = await activeSession();
      if (gs) await GLL.jsend('/api/votes/ballots/' + encodeURIComponent(build) + '/build', 'POST', { gameSessionId: gs.id });
      await refresh();
      return;
    }
    var rm = t.getAttribute('data-remove-ballot');
    if (rm) {
      await GLL.jsend('/api/votes/ballots/' + encodeURIComponent(rm), 'DELETE');
      if (el('votes-ballot-name').value.trim() === rm) fillBallot(null);
      await refresh();
      return;
    }
    var rc = t.getAttribute('data-recount');
    if (rc) { await GLL.jsend('/api/votes/runs/' + rc + '/recount', 'POST'); await refresh(); }
    var cf = t.getAttribute('data-certify');
    if (cf) { await GLL.jsend('/api/votes/runs/' + cf + '/certify', 'POST'); await refresh(); }
  });

  renderOptions(null, 'Question');
  renderDemo(null);
  syncMethodRow();
  GLL.bind({ onRefresh: refresh });
  refresh().catch(function (err) { console.error(err); });
})();
