(function (global) {
  'use strict';

  var API = (localStorage.getItem('lemmaApiBase') || location.origin).replace(/\/$/, '');
  var activeSceneId = null;

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  function fetchJson(path, opts) {
    var headers = Object.assign({ 'Content-Type': 'application/json' }, ContinuuuumUserSession.getHeaders(), (opts && opts.headers) || {});
    return fetch(API + path, Object.assign({}, opts || {}, { headers: headers }))
      .then(function (r) { if (!r.ok) throw new Error(r.status + ' ' + path); return r.json(); });
  }

  function loadScenes() {
    var ep = document.getElementById('cp-episode').value.trim();
    var q = ep ? '?episodeId=' + encodeURIComponent(ep) : '';
    fetchJson('/api/camera/scenes' + q).then(function (data) {
      var el = document.getElementById('cp-scenes');
      var items = data.items || [];
      if (!items.length) { el.innerHTML = '<p>No scenes.</p>'; return; }
      el.innerHTML = items.map(function (s) {
        return '<div class="cp-scene" data-id="' + esc(s.id) + '"><strong>' + esc(s.shotId || s.id) + '</strong> — ' + esc(s.focusMode) + '</div>';
      }).join('');
      el.querySelectorAll('.cp-scene').forEach(function (row) {
        row.addEventListener('click', function () { showScene(row.getAttribute('data-id')); });
      });
    }).catch(function (e) { document.getElementById('cp-scenes').textContent = e.message; });
  }

  function showScene(id) {
    activeSceneId = id;
    if (location.hash.indexOf('comment-') === 1) {
      /* deep link scroll handled after comments load */
    }
    fetchJson('/api/camera/scenes/' + id).then(function (scene) {
      return Promise.all([
        scene,
        fetchJson('/api/camera/scenes/' + id + '/comments'),
        fetchJson('/api/camera/hints/' + id).catch(function () { return {}; }),
      ]);
    }).then(function (parts) {
      var scene = parts[0];
      var comments = parts[1].items || [];
      var hints = parts[2] || {};
      var el = document.getElementById('cp-detail');
      el.innerHTML =
        '<h2>' + esc(scene.shotId || scene.id) + '</h2>' +
        '<p>Mode: ' + esc(scene.focusMode) + ' · ML memorability: ' + esc(scene.memorabilityMl) + '</p>' +
        '<p>Hints: ' + esc(JSON.stringify(hints.modeHintBias || [])) + '</p>' +
        '<div class="cp-stars">Rate: ' + [1,2,3,4,5].map(function (n) {
          return '<button type="button" data-score="' + n + '">' + n + '★</button>';
        }).join('') + '</div>' +
        '<p><button type="button" id="cp-up">▲</button> <button type="button" id="cp-down">▼</button></p>' +
        '<div class="cp-comments"><h3>Comments</h3>' +
        comments.map(function (c) {
          var cls = c.parentCommentId ? 'cp-comment reply' : 'cp-comment';
          return '<div class="' + cls + '" id="comment-' + esc(c.id) + '">' +
            '<div><strong>' + esc(c.authorUserId) + '</strong> ' + esc(c.bodyText) + '</div>' +
            '<a class="cp-link" href="' + esc(c.directLink) + '">link</a> ' +
            '<button type="button" data-reply="' + esc(c.id) + '">Reply</button></div>';
        }).join('') +
        '<textarea id="cp-comment" rows="3" style="width:100%"></textarea>' +
        '<button type="button" id="cp-post">Post</button></div>';

      el.querySelectorAll('[data-score]').forEach(function (btn) {
        btn.addEventListener('click', function () {
          fetchJson('/api/camera/scenes/' + id + '/rate', { method: 'POST', body: JSON.stringify({ score: Number(btn.getAttribute('data-score')) }) })
            .then(function () { showScene(id); });
        });
      });
      document.getElementById('cp-up').onclick = function () {
        fetchJson('/api/camera/scenes/' + id + '/vote', { method: 'POST', body: JSON.stringify({ vote: 1 }) });
      };
      document.getElementById('cp-down').onclick = function () {
        fetchJson('/api/camera/scenes/' + id + '/vote', { method: 'POST', body: JSON.stringify({ vote: -1 }) });
      };
      document.getElementById('cp-post').onclick = function () {
        var text = document.getElementById('cp-comment').value.trim();
        if (!text) return;
        fetchJson('/api/camera/scenes/' + id + '/comments', { method: 'POST', body: JSON.stringify({ bodyText: text }) })
          .then(function () { showScene(id); });
      };
      el.querySelectorAll('[data-reply]').forEach(function (btn) {
        btn.addEventListener('click', function () {
          var text = prompt('Reply (use @user for mentions)');
          if (!text) return;
          fetchJson('/api/camera/scenes/' + id + '/comments/' + btn.getAttribute('data-reply') + '/reply', {
            method: 'POST', body: JSON.stringify({ bodyText: text }),
          }).then(function () { showScene(id); });
        });
      });

      var hash = location.hash.replace('#', '');
      if (hash.indexOf('comment-') === 0) {
        var anchor = document.getElementById(hash);
        if (anchor) anchor.scrollIntoView();
      }
    });
  }

  document.getElementById('cp-load').addEventListener('click', loadScenes);
  if (global.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'camera' });
    if (window.ContinuuuumTomeBootstrap) ContinuuuumTomeBootstrap.mountPage({ tomeId: 'camera-tome' });
  }

  var m = location.pathname.match(/\/camera-scenes\/([^/]+)/);
  if (m) showScene(m[1]);
  else loadScenes();
})(typeof window !== 'undefined' ? window : globalThis);
