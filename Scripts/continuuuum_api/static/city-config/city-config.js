(function () {
  'use strict';

  var API = (localStorage.getItem('lemmaApiBase') || location.origin).replace(/\/$/, '');
  var planetId = new URLSearchParams(location.search).get('planetId') || 'earth';
  var cityId = new URLSearchParams(location.search).get('cityId');
  var buildingTypes = [];
  var saveTimer = null;

  function fetchJson(path, opts) {
    return fetch(API + path, opts || {}).then(function (r) {
      if (!r.ok) throw new Error(r.status + ' ' + path);
      return r.json();
    });
  }

  function mode() {
    var el = document.querySelector('input[name="cc-mode"]:checked');
    return el ? el.value : 'forward';
  }

  function commodities() {
    var out = {};
    document.querySelectorAll('[data-commodity]').forEach(function (inp) {
      out[inp.getAttribute('data-commodity')] = parseFloat(inp.value);
    });
    return out;
  }

  function debounceSave() {
    if (!cityId) return;
    clearTimeout(saveTimer);
    saveTimer = setTimeout(function () {
      fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/config', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          citySizeSqm: parseFloat(document.getElementById('cc-size').value),
          annualBudgetUsd: parseFloat(document.getElementById('cc-budget').value),
          allowDebt: document.getElementById('cc-debt').checked,
          commodityIndices: commodities(),
        }),
      }).catch(function (e) { console.warn(e); });
    }, 400);
  }

  function loadPlanets() {
    return fetchJson('/api/society/planets').then(function (data) {
      var sel = document.getElementById('cc-planet');
      sel.innerHTML = (data.items || []).map(function (p) {
        return '<option value="' + p.planetId + '"' + (p.planetId === planetId ? ' selected' : '') + '>' + p.displayName + '</option>';
      }).join('');
      sel.onchange = function () {
        planetId = sel.value;
        loadCities();
      };
    });
  }

  function loadCities() {
    return fetchJson('/api/society/planets/' + encodeURIComponent(planetId) + '/cities').then(function (data) {
      var sel = document.getElementById('cc-city');
      var items = data.items || [];
      if (!cityId && items.length) cityId = items[0].cityId;
      sel.innerHTML = items.map(function (c) {
        return '<option value="' + c.cityId + '"' + (c.cityId === cityId ? ' selected' : '') + '>' + c.displayName + '</option>';
      }).join('');
      sel.onchange = function () {
        cityId = sel.value;
        loadCity();
      };
      if (cityId) loadCity();
    });
  }

  function loadBuildingTypes() {
    return fetchJson('/api/society/building-types').then(function (data) {
      buildingTypes = data.items || [];
    });
  }

  function loadCity() {
    if (!cityId) return;
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/config').then(function (cfg) {
      document.getElementById('cc-size').value = cfg.citySizeSqm;
      document.getElementById('cc-budget').value = cfg.annualBudgetUsd;
      document.getElementById('cc-size-val').textContent = Number(cfg.citySizeSqm).toLocaleString();
      document.getElementById('cc-budget-val').textContent = '$' + Number(cfg.annualBudgetUsd).toLocaleString();
      document.getElementById('cc-debt').checked = cfg.allowDebt;
      var comm = cfg.commodityIndices || {};
      document.querySelectorAll('[data-commodity]').forEach(function (inp) {
        var k = inp.getAttribute('data-commodity');
        if (comm[k] != null) inp.value = comm[k];
      });
    });
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/zones').then(function (z) {
      document.getElementById('cc-zones').value = JSON.stringify(z, null, 2);
    });
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/network').then(function (n) {
      document.getElementById('cc-network').innerHTML =
        '<strong>Network</strong> ' + n.networkId + '<br>IPv6 ' + n.ipv6CityPrefix +
        '<br><a href="/network-definitions">Network definitions</a>';
    }).catch(function () {
      document.getElementById('cc-network').textContent = 'No network binding';
    });
    refreshMap();
    refreshBuildings();
  }

  function refreshMap() {
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/spatial-map').then(function (data) {
      if (window.CitySpatialMap) CitySpatialMap.render('city-spatial-map', data);
    });
  }

  function refreshBuildings() {
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/building-registry').then(function (data) {
      var tbody = document.querySelector('#cc-buildings tbody');
      tbody.innerHTML = (data.items || []).map(function (b) {
        return '<tr><td>' + (b.display_name || b.stable_id) + '</td><td>' + (b.building_type_id || '') +
          '</td><td>' + (b.zone_id || '') + '</td><td>' + (b.opex_usd || 0) + '</td></tr>';
      }).join('');
    });
  }

  function solve() {
    var body = {
      mode: mode(),
      citySizeSqm: parseFloat(document.getElementById('cc-size').value),
      annualBudgetUsd: parseFloat(document.getElementById('cc-budget').value),
      allowDebt: document.getElementById('cc-debt').checked,
      commodityIndices: commodities(),
    };
    try { body.zoneDocument = JSON.parse(document.getElementById('cc-zones').value); } catch (_) {}
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/zoning/solve', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }).then(function (r) {
      var html = '<strong>Zoning</strong><br>';
      if (r.solvedAnnualBudgetUsd) html += 'Required budget: $' + r.solvedAnnualBudgetUsd.toLocaleString() + '<br>';
      if (r.solvedCitySizeSqm) html += 'Max size: ' + r.solvedCitySizeSqm.toLocaleString() + ' sq m<br>';
      if (r.debtProjectionUsd) html += 'Debt projection: $' + r.debtProjectionUsd.toLocaleString();
      document.getElementById('cc-preview').innerHTML = html;
      refreshMap();
    });
  }

  function initCommodities() {
    var fs = document.getElementById('cc-commodities');
    ['water', 'power', 'steel', 'labor', 'healthcare_commodity'].forEach(function (k) {
      var lbl = document.createElement('label');
      lbl.innerHTML = k + ' <input type="range" data-commodity="' + k + '" min="0.5" max="2" step="0.1" value="1">';
      fs.appendChild(lbl);
    });
    fs.querySelectorAll('input').forEach(function (inp) { inp.addEventListener('input', debounceSave); });
  }

  document.getElementById('cc-size').addEventListener('input', function (e) {
    document.getElementById('cc-size-val').textContent = Number(e.target.value).toLocaleString();
    debounceSave();
  });
  document.getElementById('cc-budget').addEventListener('input', function (e) {
    document.getElementById('cc-budget-val').textContent = '$' + Number(e.target.value).toLocaleString();
    debounceSave();
  });
  document.getElementById('cc-debt').addEventListener('change', debounceSave);
  document.getElementById('cc-solve').addEventListener('click', solve);
  document.getElementById('cc-save-zones').addEventListener('click', function () {
    var doc;
    try { doc = JSON.parse(document.getElementById('cc-zones').value); } catch (e) { alert(e); return; }
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/zones', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ document: doc }),
    }).then(function () { alert('Zones saved'); });
  });
  document.getElementById('cc-new-city').addEventListener('click', function () {
    var name = prompt('City name');
    if (!name) return;
    fetchJson('/api/society/planets/' + encodeURIComponent(planetId) + '/cities', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ displayName: name }),
    }).then(function (c) {
      cityId = c.cityId;
      loadCities();
    });
  });
  document.getElementById('cc-add-building').addEventListener('click', function () {
    var type = buildingTypes[0] ? buildingTypes[0].buildingTypeId : 'city_hall';
    fetchJson('/api/society/cities/' + encodeURIComponent(cityId) + '/building-registry', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ buildingTypeId: type }),
    }).then(refreshBuildings);
  });

  initCommodities();
  if (window.ContinuuuumNav) ContinuuuumNav.mount('#continuuuum-nav-root', { app: 'cities' });
  if (window.ContinuuuumTomeBootstrap) ContinuuuumTomeBootstrap.mountPage({ tomeId: 'society-tome' });
  loadBuildingTypes().then(loadPlanets).then(loadCities);
})();
