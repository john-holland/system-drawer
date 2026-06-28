(function () {
  'use strict';
  var API = (localStorage.getItem('lemmaApiBase') || location.origin).replace(/\/$/, '');

  function fetchJson(path) {
    return fetch(API + path).then(function (r) {
      if (!r.ok) throw new Error(r.status);
      return r.json();
    });
  }

  function load() {
    fetchJson('/api/society/planets').then(function (data) {
      var root = document.getElementById('sd-planets');
      var chain = Promise.resolve();
      (data.items || []).forEach(function (p) {
        chain = chain.then(function () {
          return fetchJson('/api/society/planets/' + encodeURIComponent(p.planetId) + '/cities').then(function (cdata) {
            var div = document.createElement('div');
            div.className = 'sd-card';
            div.innerHTML = '<strong>' + p.displayName + '</strong> (' + p.planetId + ')' +
              '<div class="sd-cities">' + (cdata.items || []).map(function (c) {
                return '<div><a href="/city-config?planetId=' + p.planetId + '&cityId=' + c.cityId + '">' +
                  c.displayName + '</a> — ' + (c.networkId || '') + '</div>';
              }).join('') + '</div>';
            root.appendChild(div);
            var first = (cdata.items || [])[0];
            if (first) {
              fetchJson('/api/society/cities/' + encodeURIComponent(first.cityId) + '/conditions/prompt').then(function (pr) {
                document.getElementById('sd-prompt').textContent = pr.prompt;
              });
              fetchJson('/api/society/cities/' + encodeURIComponent(first.cityId) + '/report').then(function (rep) {
                document.getElementById('sd-report').innerHTML = '<pre>' + JSON.stringify(rep, null, 2) + '</pre>';
              });
            }
          });
        });
      });
    });
  }

  if (window.ContinuumNav) ContinuumNav.mount('#continuum-nav-root', { app: 'society' });
  if (window.ContinuumTomeBootstrap) ContinuumTomeBootstrap.mountPage({ tomeId: 'society-tome' });
  load();
})();
