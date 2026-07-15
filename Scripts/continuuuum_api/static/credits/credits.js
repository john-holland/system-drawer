(function () {
  'use strict';

  var NEW_ID = '';

  var ENTRY_TYPE_HINTS = {
    manual: 'Add a blank manual credit row you can edit below.',
    work_order: 'Import / upsert assignees from work orders for the project id.',
    hr: 'Import / upsert employees from Resaurce HR.',
  };

  var state = {
    lists: [],
    listId: NEW_ID,
    list: null,
    selectedEntryId: null,
    previewScale: 1,
    previewY: 0,
    previewLines: [],
    raf: 0,
  };

  function $(id) {
    return document.getElementById(id);
  }

  function isNewMode() {
    return !state.listId;
  }

  function setStatus(msg) {
    $('cr-status').textContent = msg || '';
  }

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

  function isVisible(e) {
    return !!(e.showFullName || e.showNickname);
  }

  function displayName(e) {
    var parts = [];
    if (e.showFullName && e.fullName) parts.push(e.fullName);
    if (e.showNickname && e.nickName) parts.push('"' + e.nickName + '"');
    return parts.join(' ') || '(unnamed)';
  }

  function sourceKindLabel(kind) {
    if (kind === 'work_order') return 'work order';
    if (kind === 'hr') return 'hr';
    return 'manual';
  }

  function sectionSpeed(sectionId) {
    var secs = (state.list && state.list.sections) || [];
    for (var i = 0; i < secs.length; i++) {
      if (secs[i].id === sectionId) return Number(secs[i].scrollSpeed) || 40;
    }
    return 40;
  }

  function syncMetaControls() {
    var createSave = $('cr-create-save');
    createSave.textContent = isNewMode() ? 'Create' : 'Save';
    $('cr-update-list').disabled = isNewMode();
    $('cr-save-entry').disabled = isNewMode() || !state.selectedEntryId;
    $('cr-add-entry').disabled = isNewMode();
    if (isNewMode()) {
      $('cr-sql-link').href = '/sql-viewer?recipe=credits_warehouse_history';
    } else {
      $('cr-sql-link').href =
        '/sql-viewer?recipe=credits_warehouse_history&q=' + encodeURIComponent(state.listId);
    }
  }

  function resetNewForm() {
    state.listId = NEW_ID;
    state.list = null;
    state.selectedEntryId = null;
    $('cr-title-input').value = '';
    $('cr-list-id').value = '';
    $('cr-episode').value = '';
    $('cr-title').textContent = 'New credits list';
    $('cr-entry-form').classList.add('hidden');
    $('cr-entries').innerHTML = '';
    $('cr-preview-track').innerHTML = '';
    state.previewLines = [];
    syncMetaControls();
    renderListNav();
    setStatus('New list — enter a title and Create');
  }

  function loadLists(preferId) {
    return api('/api/credits/lists').then(function (data) {
      state.lists = data.lists || [];
      renderListNav();
      if (preferId) {
        return selectList(preferId);
      }
      if (!state.listId) {
        resetNewForm();
      }
    });
  }

  function renderListNav() {
    var ul = $('cr-list');
    ul.innerHTML = '';

    var newLi = document.createElement('li');
    newLi.textContent = 'New';
    newLi.className = 'cr-list-new' + (isNewMode() ? ' active' : '');
    newLi.onclick = function () {
      resetNewForm();
    };
    ul.appendChild(newLi);

    state.lists.forEach(function (l) {
      var li = document.createElement('li');
      li.textContent = l.title || '(untitled)';
      if (l.id === state.listId) li.className = 'active';
      li.onclick = function () {
        selectList(l.id);
      };
      ul.appendChild(li);
    });
  }

  function selectList(id) {
    if (!id) {
      resetNewForm();
      return Promise.resolve();
    }
    state.listId = id;
    state.selectedEntryId = null;
    $('cr-entry-form').classList.add('hidden');
    syncMetaControls();
    return api('/api/credits/lists/' + encodeURIComponent(id) + '?includeHidden=1').then(function (data) {
      state.list = data;
      $('cr-title').textContent = data.title || '(untitled)';
      $('cr-title-input').value = data.title || '';
      $('cr-list-id').value = data.id || '';
      $('cr-episode').value = data.episodeId || '';
      renderEntries();
      rebuildPreview();
      renderListNav();
      syncMetaControls();
      setStatus('Loaded ' + (data.entries || []).length + ' entries');
    });
  }

  function createOrSaveList() {
    var title = $('cr-title-input').value.trim();
    var episodeId = $('cr-episode').value.trim() || null;
    if (isNewMode()) {
      return api('/api/credits/lists', {
        method: 'POST',
        body: { title: title || 'Credits', episodeId: episodeId, source: 'web' },
      }).then(function (list) {
        return loadLists(list.id).then(function () {
          setStatus('Created list');
        });
      });
    }
    return api('/api/credits/lists/' + encodeURIComponent(state.listId), {
      method: 'PATCH',
      body: { title: title || 'Credits', episodeId: episodeId, source: 'web' },
    }).then(function (data) {
      state.list = data;
      $('cr-title').textContent = data.title || '(untitled)';
      $('cr-title-input').value = data.title || '';
      $('cr-list-id').value = data.id || '';
      $('cr-episode').value = data.episodeId || '';
      return loadLists(state.listId).then(function () {
        setStatus('Saved list settings');
      });
    });
  }

  function renderEntries() {
    var root = $('cr-entries');
    root.innerHTML = '';
    var entries = (state.list && state.list.entries) || [];
    entries.forEach(function (e) {
      var card = document.createElement('div');
      card.className = 'cr-entry-card' + (isVisible(e) ? '' : ' hidden-entry');
      if (e.id === state.selectedEntryId) card.className += ' selected';
      card.innerHTML =
        '<strong>' +
        escapeHtml(displayName(e)) +
        '</strong>' +
        '<span class="cr-badge kind">' +
        escapeHtml(sourceKindLabel(e.sourceKind)) +
        '</span>' +
        (isVisible(e) ? '' : '<span class="cr-badge">hidden</span>') +
        '<div class="meta">' +
        escapeHtml(e.company || '') +
        (e.years ? ' · ' + escapeHtml(e.years) : '') +
        '</div>';
      card.onclick = function () {
        selectEntry(e.id);
      };
      root.appendChild(card);
    });
  }

  function escapeHtml(s) {
    return String(s || '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function selectEntry(id) {
    state.selectedEntryId = id;
    var e = ((state.list && state.list.entries) || []).find(function (x) {
      return x.id === id;
    });
    if (!e) return;
    $('cr-entry-form').classList.remove('hidden');
    $('ef-source-kind').value = e.sourceKind || 'manual';
    $('ef-full').value = e.fullName || '';
    $('ef-nick').value = e.nickName || '';
    $('ef-show-full').checked = !!e.showFullName;
    $('ef-show-nick').checked = !!e.showNickname;
    $('ef-company').value = e.company || '';
    $('ef-quote').value = e.quote || '';
    $('ef-rights').value = e.rightsMarks || '';
    $('ef-years').value = e.years || '';
    $('ef-speed').value = e.scrollSpeed != null ? e.scrollSpeed : '';
    $('ef-section-speed').value = sectionSpeed(e.sectionId);
    renderEntries();
    rebuildPreview();
    syncMetaControls();
  }

  function gatherEntryPatch() {
    var speedRaw = $('ef-speed').value;
    return {
      fullName: $('ef-full').value,
      nickName: $('ef-nick').value,
      showFullName: $('ef-show-full').checked,
      showNickname: $('ef-show-nick').checked,
      company: $('ef-company').value,
      quote: $('ef-quote').value,
      rightsMarks: $('ef-rights').value,
      years: $('ef-years').value,
      scrollSpeed: speedRaw === '' ? null : Number(speedRaw),
      sourceKind: $('ef-source-kind').value || 'manual',
      source: 'web',
    };
  }

  function saveEntry() {
    if (!state.selectedEntryId || isNewMode()) return Promise.resolve();
    var patch = gatherEntryPatch();
    var secSpeed = Number($('ef-section-speed').value);
    var e = state.list.entries.find(function (x) {
      return x.id === state.selectedEntryId;
    });
    var keepId = state.selectedEntryId;
    var p = api('/api/credits/entries/' + encodeURIComponent(state.selectedEntryId), {
      method: 'PATCH',
      body: patch,
    });
    if (e && !isNaN(secSpeed)) {
      p = p.then(function () {
        return api('/api/credits/sections/' + encodeURIComponent(e.sectionId), {
          method: 'PATCH',
          body: { scrollSpeed: secSpeed, source: 'web' },
        });
      });
    }
    return p
      .then(function () {
        return selectList(state.listId);
      })
      .then(function () {
        selectEntry(keepId);
        setStatus('Saved entry');
      });
  }

  function addEntryByType() {
    if (isNewMode()) return Promise.resolve();
    var type = $('cr-entry-type').value || 'manual';
    if (type === 'manual') {
      return api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/entries', {
        method: 'POST',
        body: {
          fullName: '',
          showFullName: true,
          showNickname: false,
          sourceKind: 'manual',
          source: 'web',
        },
      }).then(function (entry) {
        return selectList(state.listId).then(function () {
          selectEntry(entry.id);
          setStatus('Added manual entry');
        });
      });
    }
    var mode = type === 'hr' ? 'hr' : 'work_orders';
    var episodeId = $('cr-episode').value.trim() || null;
    setStatus('Importing from ' + mode + '…');
    return api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/update-list', {
      method: 'POST',
      body: { mode: mode, episodeId: episodeId, source: 'web' },
    }).then(function (data) {
      state.list = data;
      $('cr-episode').value = data.episodeId || episodeId || '';
      renderEntries();
      rebuildPreview();
      var s = data.updateSummary || {};
      setStatus(
        'Entry import (' + mode + '): +' + (s.added || 0) + ' added, ' + (s.updated || 0) + ' updated'
      );
    });
  }

  function rebuildPreview() {
    var track = $('cr-preview-track');
    track.innerHTML = '';
    state.previewLines = [];
    if (!state.list) return;
    var entries = (state.list.entries || []).filter(isVisible);
    if (state.selectedEntryId) {
      entries = entries
        .map(function (e) {
          if (e.id !== state.selectedEntryId) return e;
          var live = gatherEntryPatch();
          return Object.assign({}, e, live);
        })
        .filter(isVisible);
    }
    entries.forEach(function (e) {
      var line = document.createElement('div');
      line.className = 'cr-preview-line';
      var speed = e.scrollSpeed != null ? Number(e.scrollSpeed) : sectionSpeed(e.sectionId);
      line.innerHTML =
        '<span>' +
        escapeHtml(displayName(e)) +
        '</span>' +
        '<span class="meta">' +
        escapeHtml([e.company, e.years, e.rightsMarks].filter(Boolean).join(' · ')) +
        (e.quote ? ' — ' + escapeHtml(e.quote) : '') +
        ' · ' +
        speed +
        'px/s</span>';
      track.appendChild(line);
      state.previewLines.push({ el: line, speed: speed });
    });
    state.previewY = 0;
    track.style.transform = 'translateY(0)';
  }

  function tickPreview(ts) {
    if (!tickPreview.last) tickPreview.last = ts;
    var dt = Math.min(0.05, (ts - tickPreview.last) / 1000);
    tickPreview.last = ts;
    var base = 40;
    if (state.previewLines.length) {
      var sum = 0;
      state.previewLines.forEach(function (l) {
        sum += l.speed || 40;
      });
      base = sum / state.previewLines.length;
    }
    state.previewY += base * state.previewScale * dt;
    var track = $('cr-preview-track');
    var frame = track.parentElement;
    var h = track.offsetHeight || 1;
    var fh = frame.offsetHeight || 360;
    if (state.previewY > h + fh) state.previewY = 0;
    track.style.transform = 'translateY(' + -state.previewY + 'px)';
    state.raf = requestAnimationFrame(tickPreview);
  }

  function updateEntryTypeHint() {
    var type = $('cr-entry-type').value || 'manual';
    $('cr-entry-type-hint').textContent = ENTRY_TYPE_HINTS[type] || ENTRY_TYPE_HINTS.manual;
  }

  function bind() {
    ContinuuuumNav.mount({ app: 'credits', theme: 'dark' });

    $('cr-create-save').onclick = function () {
      createOrSaveList().catch(function (err) {
        setStatus(String(err.message || err));
      });
    };

    $('cr-update-list').onclick = function () {
      if (isNewMode()) return;
      var mode = $('cr-update-mode').value;
      var episodeId = $('cr-episode').value.trim() || null;
      setStatus('Updating…');
      api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/update-list', {
        method: 'POST',
        body: { mode: mode, episodeId: episodeId, source: 'web' },
      })
        .then(function (data) {
          state.list = data;
          $('cr-episode').value = data.episodeId || episodeId || '';
          renderEntries();
          rebuildPreview();
          var s = data.updateSummary || {};
          setStatus(
            'Update list: +' + (s.added || 0) + ' added, ' + (s.updated || 0) + ' updated (' + mode + ')'
          );
        })
        .catch(function (err) {
          setStatus(String(err.message || err));
        });
    };

    $('cr-save-entry').onclick = function () {
      saveEntry().catch(function (err) {
        setStatus(String(err.message || err));
      });
    };

    $('cr-add-entry').onclick = function () {
      addEntryByType().catch(function (err) {
        setStatus(String(err.message || err));
      });
    };

    $('cr-entry-type').addEventListener('change', updateEntryTypeHint);
    updateEntryTypeHint();

    ['ef-show-full', 'ef-show-nick', 'ef-speed', 'ef-section-speed', 'ef-full', 'ef-nick'].forEach(
      function (id) {
        $(id).addEventListener('input', rebuildPreview);
        $(id).addEventListener('change', rebuildPreview);
      }
    );

    $('cr-preview-scale').addEventListener('input', function () {
      state.previewScale = Number($('cr-preview-scale').value) || 1;
    });

    loadLists()
      .then(function () {
        if (!state.listId) resetNewForm();
      })
      .catch(function (err) {
        setStatus(String(err.message || err));
      });
    state.raf = requestAnimationFrame(tickPreview);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bind);
  } else {
    bind();
  }
})();
