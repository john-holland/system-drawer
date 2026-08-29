(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'players' });
  }

  var q = new URLSearchParams(location.search);
  var sessionId = q.get('sessionId') || '';
  var lobby = q.get('lobby') || '';
  var ctx = document.getElementById('players-context');
  if (ctx) {
    ctx.textContent = 'Session ' + (sessionId || '—') + (lobby ? ' in lobby ' + lobby : '') + '.';
  }
  var back = document.getElementById('players-back');
  if (back && sessionId) {
    back.href = '/votes?gameSessionId=' + encodeURIComponent(sessionId);
  }
})();
