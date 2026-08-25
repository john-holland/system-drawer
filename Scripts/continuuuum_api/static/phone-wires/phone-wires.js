(function () {
  'use strict';
  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'phone-wires' });
  }

  var msg = document.getElementById('pw-msg');
  function show(t) {
    msg.hidden = !t;
    msg.textContent = t || '';
  }

  async function load() {
    var q = new URLSearchParams();
    var pole = document.getElementById('pw-pole').value;
    var wire = document.getElementById('pw-wire').value;
    var lot = document.getElementById('pw-lot').value;
    if (pole) q.set('poleId', pole);
    if (wire) q.set('wireId', wire);
    if (lot) q.set('intersectionLotId', lot);
    var r = await fetch('/api/civil/phone-wire-associations?' + q.toString());
    var data = await r.json();
    var tb = document.querySelector('#pw-table tbody');
    tb.innerHTML = '';
    (data.associations || []).forEach(function (a) {
      var tr = document.createElement('tr');
      tr.innerHTML = '<td>' + (a.pole_id || '') + '</td><td>' + (a.wire_id || '') +
        '</td><td>' + (a.intersection_lot_id || '') + '</td><td>' + (a.wire_end_kind || '') +
        '</td><td>' + (a.t01 != null ? a.t01 : '') + '</td><td>' + (a.updated_by || '') + ' ' + (a.updated_at || '') + '</td>';
      tb.appendChild(tr);
    });
  }

  document.getElementById('pw-refresh').addEventListener('click', function () {
    load().catch(function (e) { show(e.message); });
  });
  document.getElementById('pw-auto').addEventListener('click', async function () {
    var r = await fetch('/api/civil/phone-wire-associations/auto', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        poleId: document.getElementById('pw-pole').value,
        toPoleId: document.getElementById('pw-wire').value,
        intersectionLotId: document.getElementById('pw-lot').value
      })
    });
    var data = await r.json();
    show('Auto returned ' + (data.associations || []).length + ' row(s)');
    await load();
  });
  document.getElementById('pw-form').addEventListener('submit', async function (ev) {
    ev.preventDefault();
    var fd = new FormData(ev.target);
    var body = {};
    fd.forEach(function (v, k) { body[k] = v; });
    var r = await fetch('/api/civil/phone-wire-associations', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    show(r.ok ? 'Saved' : 'Save failed');
    await load();
  });
  load().catch(function (e) { show(e.message); });
})();
