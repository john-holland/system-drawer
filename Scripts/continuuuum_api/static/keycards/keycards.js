(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'keycards' });
  }

  var msgEl = document.getElementById('kc-msg');
  var listEl = document.getElementById('kc-list');
  var coList = document.getElementById('co-list');

  function showMsg(text, isErr) {
    msgEl.hidden = false;
    msgEl.textContent = text;
    msgEl.classList.toggle('err', !!isErr);
  }

  function splitCsv(s) {
    return String(s || '')
      .split(',')
      .map(function (x) { return x.trim(); })
      .filter(Boolean);
  }

  async function refreshKeycards() {
    var r = await fetch('/api/civil/keycards');
    var data = await r.json();
    listEl.innerHTML = '';
    (data.keycards || []).forEach(function (k) {
      var li = document.createElement('li');
      li.textContent =
        (k.label || k.keycardId) +
        ' → ' +
        (k.boundNodeId || '(unbound)') +
        ' | actors: ' +
        ((k.actorIdsAtNode || []).join(', ') || '—');
      listEl.appendChild(li);
    });
  }

  async function refreshCompanies() {
    var r = await fetch('/api/civil/companies');
    var data = await r.json();
    coList.innerHTML = '';
    (data.companies || []).forEach(function (c) {
      var li = document.createElement('li');
      li.textContent = (c.displayName || c.companyId) + ' (' + c.companyId + ')';
      coList.appendChild(li);
    });
  }

  document.getElementById('kc-form').addEventListener('submit', async function (ev) {
    ev.preventDefault();
    var fd = new FormData(ev.target);
    var body = {
      keycardId: fd.get('keycardId'),
      label: fd.get('label'),
      boundNodeId: fd.get('boundNodeId'),
      allowedNodeIds: splitCsv(fd.get('allowedNodeIds') || fd.get('boundNodeId')),
      actorIdsAtNode: splitCsv(fd.get('actorIdsAtNode')),
    };
    var r = await fetch('/api/civil/keycards', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!r.ok) {
      showMsg('Save failed', true);
      return;
    }
    showMsg('Keycard saved');
    await refreshKeycards();
  });

  document.getElementById('co-form').addEventListener('submit', async function (ev) {
    ev.preventDefault();
    var fd = new FormData(ev.target);
    var id = fd.get('companyId');
    var r = await fetch('/api/civil/companies/' + encodeURIComponent(id), {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        displayName: fd.get('displayName'),
        parentCompanyId: fd.get('parentCompanyId'),
      }),
    });
    if (!r.ok) {
      showMsg('Company save failed', true);
      return;
    }
    showMsg('Company saved');
    await refreshCompanies();
  });

  document.getElementById('kc-refresh').addEventListener('click', function () {
    refreshKeycards();
    refreshCompanies();
  });

  refreshKeycards();
  refreshCompanies();
})();
