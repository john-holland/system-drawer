(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'voting-places' });
  }

  function el(id) { return document.getElementById(id); }

  async function jget(url) {
    var r = await fetch(url);
    return r.json();
  }

  async function jsend(url, method, body) {
    var r = await fetch(url, {
      method: method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body || {})
    });
    var data = await r.json().catch(function () { return {}; });
    if (!r.ok) throw new Error(data.error || r.statusText);
    return data;
  }

  function readProps(raw) {
    if (typeof raw === 'string') {
      try { return JSON.parse(raw || '{}'); } catch (err) { return {}; }
    }
    return raw && typeof raw === 'object' ? raw : {};
  }

  function fill(p) {
    el('vp-id').value = (p && p.id) || '';
    el('vp-name').value = (p && p.name) || '';
    el('vp-lobby').value = (p && p.lobbyId) || '';
    var props = readProps(p && p.propertiesJson);
    el('vp-feeder-policy').value = props.feederPolicy || 'addressOrRandom';
    el('vp-booth-layout').value = props.boothLayout || 'single';
    el('vp-feeder-count').value = props.feederCount != null ? props.feederCount : 1;
    el('vp-json').value = JSON.stringify(props, null, 2);
  }

  async function refresh() {
    var lobbies = await jget('/api/game-lobbies');
    var qLobby = new URLSearchParams(location.search).get('lobbyId') || '';
    var sel = el('vp-lobby');
    sel.innerHTML = '<option value="">(none)</option>';
    (lobbies || []).forEach(function (lb) {
      var o = document.createElement('option');
      o.value = lb.name;
      o.textContent = (lb.displayName || lb.name) + (lb.configId ? '' : '');
      sel.appendChild(o);
    });
    if (qLobby) sel.value = qLobby;
    var places = await jget('/api/voting-places' + (qLobby ? '?lobbyId=' + encodeURIComponent(qLobby) : ''));
    var root = el('vp-list');
    root.innerHTML = '';
    (places || []).forEach(function (p) {
      var d = document.createElement('div');
      d.className = 'votes-card';
      d.innerHTML =
        '<strong>' + (p.name || p.id) + '</strong> lobby ' + (p.lobbyId || '—') +
        ' <button type="button" data-edit="' + p.id + '">Edit</button>';
      root.appendChild(d);
    });
    root._places = places || [];
  }

  el('vp-form').addEventListener('submit', async function (e) {
    e.preventDefault();
    var err = el('vp-err');
    err.hidden = true;
    try {
      var props = JSON.parse(el('vp-json').value || '{}');
      if (!props || typeof props !== 'object' || Array.isArray(props)) props = {};
      props.feederPolicy = el('vp-feeder-policy').value || 'addressOrRandom';
      props.boothLayout = el('vp-booth-layout').value || 'single';
      props.feederCount = Number(el('vp-feeder-count').value) || 1;
      var body = { name: el('vp-name').value.trim(), lobbyId: el('vp-lobby').value, propertiesJson: props };
      if (el('vp-id').value) await jsend('/api/voting-places/' + encodeURIComponent(el('vp-id').value), 'PUT', body);
      else await jsend('/api/voting-places', 'POST', body);
      fill(null);
      await refresh();
    } catch (ex) {
      err.hidden = false;
      err.textContent = ex.message || String(ex);
    }
  });
  el('vp-new').addEventListener('click', function () { fill(null); });
  document.addEventListener('click', function (e) {
    var id = e.target && e.target.getAttribute && e.target.getAttribute('data-edit');
    if (!id) return;
    var places = el('vp-list')._places || [];
    var p = places.find(function (x) { return x.id === id; });
    if (p) fill(p);
  });

  refresh().catch(function (err) { console.error(err); });
})();
