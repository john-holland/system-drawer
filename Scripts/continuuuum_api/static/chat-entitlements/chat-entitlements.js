(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'chat-entitlements' });
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function headers() {
    var h = { 'Content-Type': 'application/json' };
    if (window.ContinuuuumUserSession) {
      var extra = ContinuuuumUserSession.getHeaders({ 'Content-Type': 'application/json' });
      Object.keys(extra).forEach(function (k) { h[k] = extra[k]; });
    }
    return h;
  }

  function load() {
    fetch('/api/admin/chat/invites', { headers: headers() })
      .then(function (r) {
        if (r.status === 403) {
          document.getElementById('admin-hint').hidden = false;
          return { items: [] };
        }
        return r.json();
      })
      .then(function (data) {
        var rows = (data.items || []).map(function (i) {
          return '<tr><td>' + esc(i.email) + '</td><td>' + esc(i.productId) + '</td><td>' +
            (i.payForThem ? 'yes' : 'no') + '</td><td>' + esc(i.acceptedAt || '') + '</td><td><code>' +
            esc(i.inviteUrl) + '</code></td></tr>';
        });
        document.getElementById('invites').innerHTML = rows.join('') || '<tr><td colspan="5">None</td></tr>';
      });
  }

  document.getElementById('invite-form').onsubmit = function (ev) {
    ev.preventDefault();
    fetch('/api/admin/chat/invites', {
      method: 'POST',
      headers: headers(),
      body: JSON.stringify({
        email: document.getElementById('email').value,
        userId: document.getElementById('user-id').value || null,
        productId: document.getElementById('product-id').value,
        payerLegalEntity: document.getElementById('legal-entity').value || null,
        payForThem: document.getElementById('pay-for-them').checked,
      }),
    }).then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        var el = document.getElementById('result');
        if (res.status >= 400) {
          el.textContent = res.body.error || 'failed';
          el.className = 'err';
          return;
        }
        el.className = 'hint';
        el.textContent = 'Invite URL: ' + (res.body.inviteUrl || '');
        load();
      });
  };

  load();
})();
