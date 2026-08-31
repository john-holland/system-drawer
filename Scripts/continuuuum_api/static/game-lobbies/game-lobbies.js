(function () {
  'use strict';

  var offset = 0;
  var limit = 50;
  var total = 0;
  var tab = 'lobbies';
  var timer = null;
  var GLL = window.GameLobbyList;

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'game-lobbies' });
  }

  function el(id) { return document.getElementById(id); }

  function qs() {
    var p = new URLSearchParams();
    var q = el('gl-q').value.trim();
    var lobby = el('gl-lobby').value.trim();
    var kind = el('gl-kind').value;
    if (q) p.set('q', q);
    if (lobby) p.set('lobby', lobby);
    if (el('gl-live').checked) p.set('live', '1');
    if (kind) p.set('contentKind', kind);
    p.set('limit', String(limit));
    p.set('offset', String(offset));
    return p.toString();
  }

  function showTab(name) {
    tab = name;
    document.querySelectorAll('.gl-tabs button').forEach(function (b) {
      b.classList.toggle('active', b.getAttribute('data-tab') === name);
    });
    document.querySelectorAll('.gl-panel').forEach(function (p) {
      p.classList.toggle('active', p.id === 'tab-' + name);
    });
    refresh();
  }

  function fillConfigForm(c) {
    el('cfg-id').value = (c && c.id) || '';
    el('cfg-name').value = (c && c.name) || '';
    el('cfg-type').value = (c && c.lobbyTypeId) || '';
    el('cfg-kind').value = (c && c.contentKind) || 'game_mode';
    el('cfg-content').value = (c && c.contentId) || '';
    el('cfg-size').value = (c && c.gameSize) || 8;
    el('cfg-mode').value = (c && c.mode) || 'SinglePlayer';
    el('cfg-min').value = (c && c.minPlayersToStart) || 1;
    el('cfg-spec').value = (c && c.maxSpectators) || 4;
    el('cfg-password').checked = !!(c && c.requirePassword);
    el('cfg-spectators').checked = !c || c.allowSpectators !== false;
    var props = c && c.propertiesJson;
    if (typeof props === 'string') {
      try { props = JSON.parse(props); } catch (err) { props = {}; }
    }
    props = props || {};
    el('cfg-json').value = JSON.stringify(props, null, 2);
    el('cfg-runtime').value = (c && c.runtimeKind) || props.runtimeKind || 'minecraft';
    el('cfg-addr').value = (c && c.advertiseAddress) || props.advertiseAddress || '';
    el('cfg-gport').value = props.gamePort || (el('cfg-runtime').value === 'minecraft' ? 25565 : 7777);
    el('cfg-lport').value = props.lobbyPort || 7780;
    el('cfg-qport').value = props.queryPort || '';
    el('cfg-rport').value = props.rconPort || '';
    el('cfg-motd').value = props.motd || '';
    el('cfg-ver').value = props.version || '';
    el('cfg-online').checked = props.onlineMode !== false;
    el('cfg-whitelist').checked = !!props.whitelist;
    el('cfg-world').value = props.worldName || 'world';
    el('cfg-seed').value = props.seed || '';
    el('cfg-diff').value = props.difficulty || 'normal';
    el('cfg-gm').value = props.gamemode || 'survival';
    el('cfg-loader').value = props.modLoader || 'neoforge';
    el('cfg-scene').value = props.scenePath || '';
    el('cfg-hostagent').value = props.hostAgent || '';
    el('cfg-build').value = props.buildId || '';
    el('cfg-uproject').value = props.project || '';
    el('cfg-umap').value = props.map || '';
  }

  function hostingFromForm() {
    return {
      advertiseAddress: el('cfg-addr').value.trim(),
      gamePort: Number(el('cfg-gport').value) || null,
      lobbyPort: Number(el('cfg-lport').value) || 7780,
      queryPort: el('cfg-qport').value ? Number(el('cfg-qport').value) : null,
      rconPort: el('cfg-rport').value ? Number(el('cfg-rport').value) : null,
      motd: el('cfg-motd').value.trim(),
      version: el('cfg-ver').value.trim(),
      onlineMode: el('cfg-online').checked,
      whitelist: el('cfg-whitelist').checked,
      worldName: el('cfg-world').value.trim() || 'world',
      seed: el('cfg-seed').value.trim(),
      difficulty: el('cfg-diff').value.trim() || 'normal',
      gamemode: el('cfg-gm').value.trim() || 'survival',
      modLoader: el('cfg-loader').value.trim() || 'neoforge',
      scenePath: el('cfg-scene').value.trim(),
      hostAgent: el('cfg-hostagent').value.trim(),
      buildId: el('cfg-build').value.trim(),
      project: el('cfg-uproject').value.trim(),
      map: el('cfg-umap').value.trim(),
      runtimeKind: el('cfg-runtime').value
    };
  }

  function readConfigForm() {
    var props = {};
    try { props = JSON.parse(el('cfg-json').value || '{}'); } catch (err) { props = {}; }
    Object.assign(props, hostingFromForm());
    var body = {
      name: el('cfg-name').value.trim(),
      lobbyTypeId: el('cfg-type').value.trim(),
      contentKind: el('cfg-kind').value,
      contentId: el('cfg-content').value.trim(),
      gameSize: Number(el('cfg-size').value),
      mode: el('cfg-mode').value,
      minPlayersToStart: Number(el('cfg-min').value),
      maxSpectators: Number(el('cfg-spec').value),
      requirePassword: el('cfg-password').checked,
      allowSpectators: el('cfg-spectators').checked,
      propertiesJson: props,
      runtimeKind: el('cfg-runtime').value,
      tenantId: 'minecraftuuuum',
      advertiseAddress: props.advertiseAddress,
      lobbyPort: props.lobbyPort,
      gamePort: props.gamePort
    };
    if (el('cfg-id').value) body.id = el('cfg-id').value;
    return body;
  }

  async function loadConfigs() {
    var configs = await GLL.jget('/api/game-lobby-configs');
    var root = el('gl-configs');
    root.innerHTML = '';
    (configs || []).forEach(function (c) {
      var box = document.createElement('div');
      box.className = 'gl-config';
      box.innerHTML =
        '<strong>' + (c.name || c.id) + '</strong> ' +
        (c.lobbyTypeId || '') + ' / ' + (c.contentKind || '') + ' ' + (c.contentId || '') +
        '<div>size ' + (c.gameSize || 8) + ' · ' + (c.mode || '') + '</div>' +
        '<button type="button" data-edit-config="' + c.id + '">Edit</button>';
      root.appendChild(box);
    });
  }

  async function loadLobbies(filtered) {
    var data = await GLL.fetchAll();
    GLL.render(el('gl-lobbies'), {
      configs: data.configs,
      lobbies: data.lobbies,
      filteredSessionIds: (filtered || []).map(function (s) { return s.id; })
    }, { showVotes: false });
  }

  async function loadGraph() {
    if (typeof d3 === 'undefined') return;
    var data = await GLL.jget('/api/game-sessions/graph?' + qs());
    var svg = d3.select('#gl-graph');
    svg.selectAll('*').remove();
    var width = svg.node().clientWidth || 900;
    var height = 480;
    svg.attr('viewBox', '0 0 ' + width + ' ' + height).style('cursor', 'grab');
    var g = svg.append('g');
    var zoom = d3.zoom()
      .scaleExtent([0.25, 4])
      .on('start', function () { svg.style('cursor', 'grabbing'); })
      .on('end', function () { svg.style('cursor', 'grab'); })
      .on('zoom', function (event) { g.attr('transform', event.transform); });
    svg.call(zoom);
    svg.on('dblclick.zoom', null);
    var lobbyColor = {};
    var palette = ['#6af', '#fa6', '#6f9', '#c8f', '#fc6'];
    var nodes = (data.nodes || []).map(function (n) {
      if (!lobbyColor[n.lobbySessionName]) {
        lobbyColor[n.lobbySessionName] = palette[Object.keys(lobbyColor).length % palette.length];
      }
      return { id: n.id, label: n.displayName || n.id, peckingOrder: n.peckingOrder, lobby: n.lobbySessionName, color: lobbyColor[n.lobbySessionName] };
    });
    var links = data.links || [];
    var sim = d3.forceSimulation(nodes)
      .force('link', d3.forceLink(links).id(function (d) { return d.id; }).distance(90))
      .force('charge', d3.forceManyBody().strength(-140))
      .force('center', d3.forceCenter(width / 2, height / 2));
    var link = g.append('g').selectAll('line').data(links).join('line').attr('class', 'gl-link');
    var node = g.append('g').selectAll('g').data(nodes).join('g').attr('class', 'gl-node');
    node.append('circle').attr('r', 7).attr('fill', function (d) { return d.color; }).on('click', function (_e, d) {
      location.href = '/votes?gameSessionId=' + encodeURIComponent(d.id);
    });
    node.append('text').text(function (d) { return d.label; }).attr('x', 10).attr('y', 4);
    sim.on('tick', function () {
      link.attr('x1', function (d) { return d.source.x; })
        .attr('y1', function (d) { return d.source.y; })
        .attr('x2', function (d) { return d.target.x; })
        .attr('y2', function (d) { return d.target.y; });
      node.attr('transform', function (d) { return 'translate(' + d.x + ',' + d.y + ')'; });
    });
  }

  async function refresh() {
    if (tab === 'configure') {
      await loadConfigs();
      return;
    }
    var page = await GLL.jget('/api/game-sessions?' + qs());
    total = page.total || 0;
    el('gl-page').textContent = (offset + 1) + '–' + Math.min(offset + limit, total) + ' / ' + total;
    if (tab === 'graph') await loadGraph();
    else await loadLobbies(page.items || []);
  }

  document.querySelector('.gl-tabs').addEventListener('click', function (e) {
    var t = e.target.getAttribute('data-tab');
    if (t) showTab(t);
  });

  el('gl-config-form').addEventListener('submit', async function (e) {
    e.preventDefault();
    var body = readConfigForm();
    if (body.id) await GLL.jsend('/api/game-lobby-configs/' + encodeURIComponent(body.id), 'PUT', body);
    else await GLL.jsend('/api/game-lobby-configs', 'POST', body);
    fillConfigForm(null);
    await loadConfigs();
  });

  el('cfg-new').addEventListener('click', function () { fillConfigForm(null); });

  el('gl-prev').addEventListener('click', async function () {
    offset = Math.max(0, offset - limit);
    await refresh();
  });
  el('gl-next').addEventListener('click', async function () {
    if (offset + limit < total) { offset += limit; await refresh(); }
  });

  ['gl-q', 'gl-lobby', 'gl-kind', 'gl-live'].forEach(function (id) {
    el(id).addEventListener('input', function () {
      clearTimeout(timer);
      timer = setTimeout(function () { offset = 0; refresh(); }, 250);
    });
    el(id).addEventListener('change', function () { offset = 0; refresh(); });
  });

  GLL.bind({ onRefresh: refresh });
  fetch('/api/payroll/tenants/minecraftuuuum/split')
    .then(function (r) { return r.json(); })
    .then(function (s) {
      el('gl-retainer').textContent =
        'Tenant ' + (s.tenantId || 'minecraftuuuum') +
        ': creator ' + Math.round((s.creatorPct || 0.7) * 100) + '%' +
        ' · Mojang/Microsoft ' + Math.round((s.platformPct || 0.3) * 100) + '%' +
        ' · Continuuuum HWM ' + Math.round((s.continuuuumHwmPct || 0.1) * 100) + '%' +
        ' · Unity sub ' + (s.serviceUnityEnabled ? 'on' : 'off') +
        ' · Cursor sub ' + (s.serviceCursorEnabled ? 'on' : 'off') +
        ' · Unreal sub ' + (s.serviceUnrealEnabled ? 'on' : 'off');
    })
    .catch(function () {
      el('gl-retainer').textContent = 'Tenant retainer unavailable (payroll not reachable).';
    });
  fetch('/api/tenant/oauth-connections?tenant=minecraftuuuum')
    .then(function (r) { return r.json(); })
    .then(function (data) {
      var ms = (data.items || []).filter(function (i) { return i.provider === 'microsoft'; })[0];
      if (!ms) return;
      el('oauth-client').value = ms.clientId || '';
      el('oauth-azure').value = ms.azureTenant || '';
      el('oauth-redirect').value = ms.redirectUri || '';
      el('oauth-scopes').value = (ms.scopes || []).join(',');
      el('oauth-status').textContent = ms.status || '';
    })
    .catch(function () {});
  el('oauth-save').addEventListener('click', async function () {
    var scopes = el('oauth-scopes').value.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
    var row = await GLL.jsend('/api/tenant/oauth-connections?tenant=minecraftuuuum', 'PUT', {
      provider: 'microsoft',
      clientId: el('oauth-client').value.trim(),
      azureTenant: el('oauth-azure').value.trim(),
      redirectUri: el('oauth-redirect').value.trim(),
      scopes: scopes,
      extra: { online_mode: true, secretRef: 'MICROSOFT_OAUTH_CLIENT_SECRET' },
      status: el('oauth-client').value.trim() ? 'configured' : 'disconnected'
    });
    el('oauth-status').textContent = row.status || 'saved';
  });
  refresh().catch(function (err) { console.error(err); });
})();
