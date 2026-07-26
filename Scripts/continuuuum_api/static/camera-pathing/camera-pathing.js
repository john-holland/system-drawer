(function (global) {
  'use strict';

  var API = (localStorage.getItem('lemmaApiBase') || location.origin).replace(/\/$/, '');
  var activeSceneId = null;
  var sceneCache = [];
  var activePathLabel = '';

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  function fetchJson(path, opts) {
    var headers = Object.assign({ 'Content-Type': 'application/json' }, ContinuuuumUserSession.getHeaders(), (opts && opts.headers) || {});
    return fetch(API + path, Object.assign({}, opts || {}, { headers: headers }))
      .then(function (r) { if (!r.ok) throw new Error(r.status + ' ' + path); return r.json(); });
  }

  function sceneLabel(s) {
    return (s.shotId || s.id) + ' — ' + (s.focusMode || '');
  }

  function fillSceneSelect(items) {
    var sel = document.getElementById('cp-scene-select');
    if (!sel) return;
    var prev = sel.value || activeSceneId || '';
    sel.innerHTML = '<option value="">—</option>' + items.map(function (s) {
      return '<option value="' + esc(s.id) + '">' + esc(sceneLabel(s)) + '</option>';
    }).join('');
    if (prev && items.some(function (s) { return s.id === prev; })) sel.value = prev;
  }

  function extractBlobOptions(obj, keys) {
    var out = [];
    if (!obj || typeof obj !== 'object') return out;
    keys.forEach(function (key) {
      var arr = obj[key];
      if (!Array.isArray(arr)) return;
      arr.forEach(function (item, i) {
        if (item == null) return;
        if (typeof item === 'string') {
          out.push({ value: 'blob:' + key + ':' + i, label: item, sceneId: null });
          return;
        }
        var id = item.id || item.name || item.cameraId || item.pathId || String(i);
        var label = item.label || item.name || item.shotId || id;
        out.push({
          value: 'blob:' + key + ':' + id,
          label: String(label),
          sceneId: item.sceneId || null,
        });
      });
    });
    return out;
  }

  function buildPathOptions(scene, allScenes) {
    var opts = [];
    opts = opts.concat(extractBlobOptions(scene.topology, ['cameras', 'paths', 'rigs']));
    opts = opts.concat(extractBlobOptions(scene.rigPose, ['cameras', 'paths', 'rigs']));
    if (opts.length) return opts;

    var episodeId = scene.episodeId;
    var siblings = (allScenes || []).filter(function (s) {
      if (!episodeId) return s.id === scene.id;
      return s.episodeId === episodeId;
    });
    if (!siblings.length) siblings = [scene];
    siblings.forEach(function (s) {
      opts.push({
        value: 'scene:' + s.id,
        label: (s.shotId || s.id) + (s.focusMode ? ' (' + s.focusMode + ')' : ''),
        sceneId: s.id,
      });
    });

    var poseLabel = null;
    if (scene.rigPose && typeof scene.rigPose === 'object') {
      poseLabel = scene.rigPose.name || scene.rigPose.cameraName || scene.rigPose.mode || null;
    }
    if (poseLabel || scene.focusMode) {
      opts.unshift({
        value: 'camera:default',
        label: 'Camera: ' + (poseLabel || scene.focusMode || 'default'),
        sceneId: scene.id,
      });
    }
    return opts;
  }

  function fillPathSelect(scene) {
    var sel = document.getElementById('cp-path-select');
    if (!sel) return;
    var opts = buildPathOptions(scene, sceneCache);
    var prev = sel.value;
    sel.innerHTML = opts.map(function (o) {
      return '<option value="' + esc(o.value) + '" data-scene="' + esc(o.sceneId || '') + '">' + esc(o.label) + '</option>';
    }).join('') || '<option value="">—</option>';
    if (prev && opts.some(function (o) { return o.value === prev; })) {
      sel.value = prev;
    } else if (opts.length) {
      var match = opts.find(function (o) { return o.sceneId === scene.id; });
      sel.value = match ? match.value : opts[0].value;
    }
    var chosen = opts.find(function (o) { return o.value === sel.value; });
    activePathLabel = chosen ? chosen.label : '';
  }

  function loadScenes() {
    var ep = document.getElementById('cp-episode').value.trim();
    var q = ep ? '?episodeId=' + encodeURIComponent(ep) : '';
    fetchJson('/api/camera/scenes' + q).then(function (data) {
      var el = document.getElementById('cp-scenes');
      var items = data.items || [];
      sceneCache = items;
      fillSceneSelect(items);
      if (!items.length) {
        el.innerHTML = '<p>No scenes.</p>';
        return;
      }
      el.innerHTML = items.map(function (s) {
        return '<div class="cp-scene" data-id="' + esc(s.id) + '"><strong>' + esc(s.shotId || s.id) + '</strong> — ' + esc(s.focusMode) + '</div>';
      }).join('');
      el.querySelectorAll('.cp-scene').forEach(function (row) {
        row.addEventListener('click', function () { showScene(row.getAttribute('data-id')); });
      });
      if (activeSceneId && items.some(function (s) { return s.id === activeSceneId; })) {
        showScene(activeSceneId);
      } else if (items.length === 1) {
        showScene(items[0].id);
      }
    }).catch(function (e) { document.getElementById('cp-scenes').textContent = e.message; });
  }

  function showScene(id) {
    activeSceneId = id;
    var sceneSel = document.getElementById('cp-scene-select');
    if (sceneSel && sceneSel.value !== id) sceneSel.value = id;

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
      if (!sceneCache.some(function (s) { return s.id === scene.id; })) {
        sceneCache.push(scene);
        fillSceneSelect(sceneCache);
      }
      fillPathSelect(scene);

      var el = document.getElementById('cp-detail');
      el.innerHTML =
        '<h2>' + esc(scene.shotId || scene.id) + '</h2>' +
        (activePathLabel ? '<p class="cp-active-path">Active camera / path: <strong>' + esc(activePathLabel) + '</strong></p>' : '') +
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
  document.getElementById('cp-scene-select').addEventListener('change', function () {
    var id = document.getElementById('cp-scene-select').value;
    if (id) showScene(id);
  });
  document.getElementById('cp-path-select').addEventListener('change', function () {
    var sel = document.getElementById('cp-path-select');
    var opt = sel.options[sel.selectedIndex];
    activePathLabel = opt ? opt.textContent : '';
    var sceneId = opt && opt.getAttribute('data-scene');
    if (sceneId && sceneId !== activeSceneId) {
      showScene(sceneId);
      return;
    }
    var detailH = document.querySelector('#cp-detail h2');
    var pathEl = document.querySelector('#cp-detail .cp-active-path');
    if (pathEl) {
      pathEl.innerHTML = 'Active camera / path: <strong>' + esc(activePathLabel) + '</strong>';
    } else if (detailH && activePathLabel) {
      var p = document.createElement('p');
      p.className = 'cp-active-path';
      p.innerHTML = 'Active camera / path: <strong>' + esc(activePathLabel) + '</strong>';
      detailH.insertAdjacentElement('afterend', p);
    }
  });

  if (global.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'camera' });
    if (window.ContinuuuumTomeBootstrap) ContinuuuumTomeBootstrap.mountPage({ tomeId: 'camera-tome' });
  }

  var m = location.pathname.match(/\/camera-scenes\/([^/]+)/);
  if (m) showScene(m[1]);
  else loadScenes();
})(typeof window !== 'undefined' ? window : globalThis);
