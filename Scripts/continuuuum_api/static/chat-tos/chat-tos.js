(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'chat-entitlements' });
  }

  var params = new URLSearchParams(location.search);
  var token = params.get('token') || '';
  var invite = null;
  var tos = null;

  function headers() {
    var h = { 'Content-Type': 'application/json' };
    if (window.ContinuuuumUserSession) {
      var extra = ContinuuuumUserSession.getHeaders({ 'Content-Type': 'application/json' });
      Object.keys(extra).forEach(function (k) { h[k] = extra[k]; });
    }
    return h;
  }

  function load() {
    var url = token ? '/api/chat/invites/' + encodeURIComponent(token) : '/api/chat/tos/current';
    fetch(url, { headers: headers() })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        if (data.invite) {
          invite = data.invite;
          tos = data.tos;
          document.getElementById('invite-meta').textContent =
            'Invite for ' + (invite.email || '') + (invite.payForThem ? ' — administrator is paying the $1 fee.' : ' — you pay a $1 convenience fee, credited to your withdrawable profit.');
          if (invite.userId) document.getElementById('user-id').value = invite.userId;
        } else {
          tos = data;
          document.getElementById('invite-meta').textContent =
            'Self-serve: $1 convenience fee is credited to your withdrawable profit. There is no separate refund.';
        }
        document.getElementById('tos-body').textContent = (tos && tos.body) || 'Terms unavailable.';
        document.getElementById('pay-note').textContent = invite && invite.payForThem
          ? 'Card collection is skipped because an administrator is paying. You must still sign.'
          : 'Paying $1 is consideration for these terms. That $1 is immediately credited as withdrawable profit.';
        document.getElementById('sign-form').hidden = false;
        if (window.ContinuuuumUserSession) {
          document.getElementById('user-id').value = document.getElementById('user-id').value || ContinuuuumUserSession.getUserId();
        }
      });
  }

  document.getElementById('sign-form').onsubmit = function (ev) {
    ev.preventDefault();
    var productId = (invite && invite.productId) || params.get('productId');
    if (!productId) {
      document.getElementById('result').textContent = 'productId required';
      document.getElementById('result').className = 'err';
      return;
    }
    fetch('/api/chat/entitlement/activate', {
      method: 'POST',
      headers: headers(),
      body: JSON.stringify({
        userId: document.getElementById('user-id').value,
        productId: productId,
        inviteToken: token || undefined,
        tosVersionId: tos && tos.id,
        soleUserAttested: document.getElementById('sole-user').checked,
        legalAgeAttested: document.getElementById('legal-age').checked,
      }),
    }).then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        var el = document.getElementById('result');
        if (res.status >= 400) {
          el.className = 'err';
          el.textContent = res.body.error || res.body.code || 'failed';
          return;
        }
        el.className = 'hint';
        el.textContent = 'Entitlement active. Profit balance: $' + (res.body.profitBalanceUsd != null ? res.body.profitBalanceUsd : '');
      });
  };

  load();
})();
