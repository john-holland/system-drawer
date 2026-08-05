(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'vehicle-inventory' });
  }

  var msgEl = document.getElementById('vi-msg');
  var listEl = document.getElementById('vi-list');

  function showMsg(text) {
    msgEl.hidden = false;
    msgEl.textContent = text;
  }

  async function refresh() {
    var r = await fetch('/api/civil/vehicle-inventory');
    var data = await r.json();
    listEl.innerHTML = '';
    (data.vehicles || []).forEach(function (v) {
      var li = document.createElement('li');
      var sections = (v.interiors || []).map(function (s) { return s.sectionName; }).join(', ');
      li.textContent =
        (v.displayName || v.vehicleId) +
        ' — size ' +
        (v.totalSize != null ? v.totalSize : '—') +
        ' — sections: ' +
        (sections || '—');
      listEl.appendChild(li);
    });
  }

  document.getElementById('vi-form').addEventListener('submit', async function (ev) {
    ev.preventDefault();
    var fd = new FormData(ev.target);
    var names = String(fd.get('sections') || '')
      .split(',')
      .map(function (x) { return x.trim(); })
      .filter(Boolean);
    var interiors = names.map(function (n) {
      return { sectionName: n, capacity: n.indexOf('hose') >= 0 ? 80 : 20, items: [] };
    });
    var body = {
      vehicleId: fd.get('vehicleId'),
      displayName: fd.get('displayName'),
      interiors: interiors,
    };
    var r = await fetch('/api/civil/vehicle-inventory', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!r.ok) {
      showMsg('Save failed');
      return;
    }
    showMsg('Vehicle saved');
    await refresh();
  });

  document.getElementById('vi-refresh').addEventListener('click', refresh);
  refresh();
})();
