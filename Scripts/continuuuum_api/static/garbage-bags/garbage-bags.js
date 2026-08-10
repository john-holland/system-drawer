(function () {
  'use strict';

  var NEW_ID = '';
  var state = { bags: [], bagId: NEW_ID, bag: null };

  function $(id) { return document.getElementById(id); }

  function isNewMode() { return !state.bagId; }

  function setStatus(msg) { $('gb-status').textContent = msg || ''; }

  function api(path, opts) {
    opts = opts || {};
    return fetch(path, {
      method: opts.method || 'GET',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: opts.body ? JSON.stringify(opts.body) : undefined,
    }).then(function (r) {
      return r.json().then(function (j) {
        if (!r.ok) throw new Error((j && j.error) || r.statusText);
        return j;
      });
    });
  }

  function resetNewForm() {
    state.bagId = NEW_ID;
    state.bag = {
      title: '',
      defaultMassKg: 8,
      commodities: [{ key: 'mixed', weight: 1 }],
    };
    renderList();
    renderForm();
  }

  function loadBags() {
    return api('/api/garbage-bags').then(function (data) {
      state.bags = data.bags || [];
      if (!state.bagId && state.bags.length) {
        /* keep new mode until user picks */
      }
      renderList();
      renderForm();
    }).catch(function (e) { setStatus(e.message); });
  }

  function renderList() {
    var ul = $('gb-list');
    ul.innerHTML = '';
    var newLi = document.createElement('li');
    newLi.textContent = 'New';
    newLi.className = isNewMode() ? 'active' : '';
    newLi.onclick = resetNewForm;
    ul.appendChild(newLi);
    state.bags.forEach(function (b) {
      var li = document.createElement('li');
      li.textContent = (b.isDefault ? '★ ' : '') + (b.title || b.id);
      if (b.id === state.bagId) li.className = 'active';
      li.onclick = function () {
        state.bagId = b.id;
        state.bag = JSON.parse(JSON.stringify(b));
        renderList();
        renderForm();
      };
      ul.appendChild(li);
    });
  }

  function renderForm() {
    var b = state.bag || { title: '', defaultMassKg: 8, commodities: [] };
    $('gb-title').value = b.title || '';
    $('gb-title').disabled = !!(b.isDefault);
    $('gb-mass').value = b.defaultMassKg != null ? b.defaultMassKg : 8;
    $('gb-save').textContent = isNewMode() ? 'Create' : 'Save';
    var box = $('gb-commodities');
    box.innerHTML = '';
    (b.commodities || []).forEach(function (c, i) {
      var row = document.createElement('div');
      row.className = 'gb-commodity';
      row.innerHTML = '<input data-i="' + i + '" data-f="key" value="' + (c.key || '') + '" />' +
        '<input data-i="' + i + '" data-f="weight" type="number" step="0.05" value="' + (c.weight != null ? c.weight : 1) + '" />';
      box.appendChild(row);
    });
    box.querySelectorAll('input').forEach(function (inp) {
      inp.onchange = function () {
        var i = +inp.getAttribute('data-i');
        var f = inp.getAttribute('data-f');
        state.bag.commodities[i][f] = f === 'weight' ? parseFloat(inp.value) : inp.value;
      };
    });
  }

  function collectForm() {
    return {
      title: $('gb-title').value,
      defaultMassKg: parseFloat($('gb-mass').value) || 8,
      commodities: (state.bag && state.bag.commodities) || [],
    };
  }

  $('gb-new').onclick = resetNewForm;
  $('gb-add-commodity').onclick = function () {
    if (!state.bag) state.bag = { commodities: [] };
    if (!state.bag.commodities) state.bag.commodities = [];
    state.bag.commodities.push({ key: 'scrap', weight: 0.1 });
    renderForm();
  };
  $('gb-save').onclick = function () {
    var body = collectForm();
    if (isNewMode()) {
      api('/api/garbage-bags', { method: 'POST', body: body }).then(function (bag) {
        state.bagId = bag.id;
        state.bag = bag;
        setStatus('Created ' + bag.id);
        return loadBags();
      }).catch(function (e) { setStatus(e.message); });
    } else {
      api('/api/garbage-bags/' + state.bagId, { method: 'PATCH', body: body }).then(function (bag) {
        state.bag = bag;
        setStatus('Saved');
        return loadBags();
      }).catch(function (e) { setStatus(e.message); });
    }
  };
  $('sf-save').onclick = function () {
    api('/api/sanitation/facilities', {
      method: 'POST',
      body: {
        facilityId: $('sf-id').value || 'san_1',
        companyId: $('sf-company').value,
        ipv6CityPrefix: $('sf-ip').value,
      },
    }).then(function () { setStatus('Facility config saved'); })
      .catch(function (e) { setStatus(e.message); });
  };

  if (window.ContinuuuumNav && typeof ContinuuuumNav.mount === 'function') {
    ContinuuuumNav.mount({ app: 'garbage-bags', theme: 'dark' });
  }

  resetNewForm();
  loadBags();
})();
