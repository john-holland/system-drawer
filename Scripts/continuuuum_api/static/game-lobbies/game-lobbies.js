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
    el('cfg-json').value = typeof props === 'string' ? props : JSON.stringify(props || {}, null, 2);
  }

  function readConfigForm() {
    var props = {};
    try { props = JSON.parse(el('cfg-json').value || '{}'); } catch (err) { props = {}; }
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
      propertiesJson: props
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
  refresh().catch(function (err) { console.error(err); });
})();
