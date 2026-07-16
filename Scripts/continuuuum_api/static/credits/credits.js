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

  function sections() {
    return (state.list && state.list.sections) || [];
  }

  function sectionById(sectionId) {
    return sections().find(function (s) {
      return s.id === sectionId;
    });
  }

  function sectionSpeed(sectionId) {
    var s = sectionById(sectionId);
    return s && Number(s.scrollSpeed) > 0 ? Number(s.scrollSpeed) : 40;
  }

  function entrySpeed(e) {
    if (e && e.scrollSpeed != null && e.scrollSpeed !== '' && !isNaN(Number(e.scrollSpeed))) {
      return Number(e.scrollSpeed);
    }
    return sectionSpeed(e && e.sectionId);
  }

  function fillSectionSelects(selectedId) {
    var opts = sections()
      .slice()
      .sort(function (a, b) {
        return (a.sortOrder || 0) - (b.sortOrder || 0);
      });
    ['cr-entry-section', 'ef-section-id'].forEach(function (id) {
      var sel = $(id);
      if (!sel) return;
      var keep = selectedId || sel.value;
      sel.innerHTML = opts
        .map(function (s) {
          return (
            '<option value="' +
            escapeHtml(s.id) +
            '"' +
            (s.id === keep ? ' selected' : '') +
            '>' +
            escapeHtml(s.title || '(untitled group)') +
            '</option>'
          );
        })
        .join('');
      if (!sel.value && opts[0]) sel.value = opts[0].id;
    });
  }

  function syncMetaControls() {
    $('cr-create-save').textContent = isNewMode() ? 'Create' : 'Save';
    $('cr-update-list').disabled = isNewMode();
    var entrySaveDisabled = isNewMode() || !state.selectedEntryId;
    $('cr-save-entry').disabled = entrySaveDisabled;
    var formSave = $('cr-save-entry-form');
    if (formSave) formSave.disabled = entrySaveDisabled;
    $('cr-add-entry').disabled = isNewMode();
    $('cr-add-section').disabled = isNewMode();
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
    $('cr-sections').innerHTML = '';
    $('cr-preview-track').innerHTML = '';
    fillSectionSelects();
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

  function selectList(id, opts) {
    opts = opts || {};
    if (!id) {
      resetNewForm();
      return Promise.resolve();
    }
    var keepEntryId = opts.keepEntryId || null;
    state.listId = id;
    if (!keepEntryId) {
      state.selectedEntryId = null;
      $('cr-entry-form').classList.add('hidden');
    }
    syncMetaControls();
    return api('/api/credits/lists/' + encodeURIComponent(id) + '?includeHidden=1').then(function (data) {
      state.list = data;
      $('cr-title').textContent = data.title || '(untitled)';
      $('cr-title-input').value = data.title || '';
      $('cr-list-id').value = data.id || '';
      $('cr-episode').value = data.episodeId || '';
      var keepSectionId = null;
      if (keepEntryId) {
        var kept = (data.entries || []).find(function (x) {
          return x.id === keepEntryId;
        });
        keepSectionId = kept && kept.sectionId;
      }
      fillSectionSelects(keepSectionId || undefined);
      renderSections();
      renderEntries();
      rebuildPreview();
      renderListNav();
      if (keepEntryId) {
        selectEntry(keepEntryId);
      } else {
        syncMetaControls();
      }
      if (!opts.quiet) {
        setStatus('Loaded ' + (data.entries || []).length + ' entries');
      }
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

  function renderSections() {
    var root = $('cr-sections');
    root.innerHTML = '';
    var secs = sections()
      .slice()
      .sort(function (a, b) {
        return (a.sortOrder || 0) - (b.sortOrder || 0);
      });
    if (!secs.length) {
      root.innerHTML = '<p class="cr-hint">No groups yet.</p>';
      return;
    }
    secs.forEach(function (s) {
      var row = document.createElement('div');
      row.className = 'cr-section-row';
      row.dataset.sectionId = s.id;
      row.innerHTML =
        '<label>Group name<input class="cr-sec-title" type="text" value="' +
        escapeHtml(s.title || '') +
        '" /></label>' +
        '<label>Default speed<input class="cr-sec-speed" type="number" min="1" step="1" value="' +
        escapeHtml(s.scrollSpeed != null ? s.scrollSpeed : 40) +
        '" /></label>' +
        '<button type="button" class="cr-sec-save">Save group</button>';
      row.querySelector('.cr-sec-save').onclick = function () {
        saveSection(s.id, row).catch(function (err) {
          setStatus(String(err.message || err));
        });
      };
      root.appendChild(row);
    });
  }

  function saveSection(sectionId, rowEl) {
    var title = rowEl.querySelector('.cr-sec-title').value.trim() || 'Section';
    var speed = Number(rowEl.querySelector('.cr-sec-speed').value);
    if (isNaN(speed) || speed <= 0) speed = 40;
    return api('/api/credits/sections/' + encodeURIComponent(sectionId), {
      method: 'PATCH',
      body: { title: title, scrollSpeed: speed, source: 'web' },
    }).then(function () {
      return selectList(state.listId).then(function () {
        setStatus('Saved group "' + title + '"');
      });
    });
  }

  function addSection() {
    if (isNewMode()) return Promise.resolve();
    var title = $('cr-new-section-title').value.trim() || 'Section';
    return api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/sections', {
      method: 'POST',
      body: { title: title, scrollSpeed: 40, source: 'web' },
    }).then(function () {
      $('cr-new-section-title').value = '';
      return selectList(state.listId).then(function () {
        setStatus('Added group "' + title + '"');
      });
    });
  }

  function renderEntries() {
    var root = $('cr-entries');
    root.innerHTML = '';
    var entries = (state.list && state.list.entries) || [];
    var secs = sections()
      .slice()
      .sort(function (a, b) {
        return (a.sortOrder || 0) - (b.sortOrder || 0);
      });
    var bySection = {};
    entries.forEach(function (e) {
      var key = e.sectionId || '_none';
      if (!bySection[key]) bySection[key] = [];
      bySection[key].push(e);
    });

    function appendGroup(title, list) {
      if (!list || !list.length) return;
      var header = document.createElement('div');
      header.className = 'cr-entry-group-header';
      header.textContent = title || '(untitled group)';
      root.appendChild(header);
      list
        .slice()
        .sort(function (a, b) {
          return (a.sortOrder || 0) - (b.sortOrder || 0);
        })
        .forEach(function (e) {
          var card = document.createElement('div');
          card.className = 'cr-entry-card' + (isVisible(e) ? '' : ' hidden-entry');
          if (e.id === state.selectedEntryId) card.className += ' selected';
          var speed = entrySpeed(e);
          var hasOwn = e.scrollSpeed != null && e.scrollSpeed !== '';
          card.innerHTML =
            '<strong>' +
            escapeHtml(displayName(e)) +
            '</strong>' +
            '<span class="cr-badge kind">' +
            escapeHtml(sourceKindLabel(e.sourceKind)) +
            '</span>' +
            (isVisible(e) ? '' : '<span class="cr-badge">hidden</span>') +
            '<span class="cr-badge speed">' +
            escapeHtml(String(speed)) +
            ' px/s' +
            (hasOwn ? '' : ' (group)') +
            '</span>' +
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

    secs.forEach(function (s) {
      appendGroup(s.title, bySection[s.id]);
      delete bySection[s.id];
    });
    Object.keys(bySection).forEach(function (k) {
      appendGroup('Ungrouped', bySection[k]);
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
    fillSectionSelects(e.sectionId);
    $('cr-entry-form').classList.remove('hidden');
    $('ef-section-id').value = e.sectionId || '';
    $('ef-source-kind').value = e.sourceKind || 'manual';
    $('ef-full').value = e.fullName || '';
    $('ef-nick').value = e.nickName || '';
    $('ef-show-full').checked = !!e.showFullName;
    $('ef-show-nick').checked = !!e.showNickname;
    $('ef-company').value = e.company || '';
    $('ef-quote').value = e.quote || '';
    $('ef-rights').value = e.rightsMarks || '';
    $('ef-years').value = e.years || '';
    $('ef-speed').value = e.scrollSpeed != null && e.scrollSpeed !== '' ? e.scrollSpeed : '';
    $('ef-speed').placeholder = 'inherits ' + sectionSpeed(e.sectionId);
    renderEntries();
    rebuildPreview();
    syncMetaControls();
  }

  function gatherEntryPatch() {
    var speedRaw = String($('ef-speed').value || '').trim();
    var scrollSpeed = null;
    if (speedRaw !== '') {
      scrollSpeed = Number(speedRaw);
      if (isNaN(scrollSpeed)) scrollSpeed = null;
    }
    return {
      fullName: $('ef-full').value,
      nickName: $('ef-nick').value,
      showFullName: $('ef-show-full').checked,
      showNickname: $('ef-show-nick').checked,
      company: $('ef-company').value,
      quote: $('ef-quote').value,
      rightsMarks: $('ef-rights').value,
      years: $('ef-years').value,
      sectionId: $('ef-section-id').value || undefined,
      scrollSpeed: scrollSpeed,
      sourceKind: $('ef-source-kind').value || 'manual',
      source: 'web',
    };
  }

  function saveEntry() {
    if (!state.selectedEntryId || isNewMode()) return Promise.resolve();
    var patch = gatherEntryPatch();
    var keepId = state.selectedEntryId;
    setStatus('Saving entry…');
    return api('/api/credits/entries/' + encodeURIComponent(keepId), {
      method: 'PATCH',
      body: patch,
    }).then(function (updated) {
      return selectList(state.listId, { keepEntryId: keepId, quiet: true }).then(function () {
        var spd =
          updated && updated.scrollSpeed != null
            ? updated.scrollSpeed + ' px/s'
            : 'group default';
        setStatus('Saved entry (speed: ' + spd + ')');
      });
    });
  }

  function addEntryByType() {
    if (isNewMode()) return Promise.resolve();
    var type = $('cr-entry-type').value || 'manual';
    var sectionId = $('cr-entry-section').value || undefined;
    if (type === 'manual') {
      var defaultSpeed = sectionSpeed(sectionId);
      return api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/entries', {
        method: 'POST',
        body: {
          fullName: '',
          showFullName: true,
          showNickname: false,
          sourceKind: 'manual',
          sectionId: sectionId,
          scrollSpeed: defaultSpeed,
          source: 'web',
        },
      }).then(function (entry) {
        return selectList(state.listId).then(function () {
          selectEntry(entry.id);
          setStatus('Added manual entry @ ' + defaultSpeed + ' px/s');
        });
      });
    }
    var mode = type === 'hr' ? 'hr' : 'work_orders';
    var episodeId = $('cr-episode').value.trim() || null;
    setStatus('Importing from ' + mode + '…');
    return api('/api/credits/lists/' + encodeURIComponent(state.listId) + '/update-list', {
      method: 'POST',
      body: { mode: mode, episodeId: episodeId, sectionId: sectionId, source: 'web' },
    }).then(function (data) {
      state.list = data;
      $('cr-episode').value = data.episodeId || episodeId || '';
      fillSectionSelects(sectionId);
      renderSections();
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
      var speed = entrySpeed(e);
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
      state.previewLines.push({ el: line, speed: speed, y: 0 });
    });
    state.previewY = 0;
    track.style.transform = 'translateY(0)';
  }

  function tickPreview(ts) {
    if (!tickPreview.last) tickPreview.last = ts;
    var dt = Math.min(0.05, (ts - tickPreview.last) / 1000);
    tickPreview.last = ts;
    var base = 40;
    if (state.selectedEntryId && state.previewLines.length) {
      var sel = ((state.list && state.list.entries) || []).find(function (e) {
        return e.id === state.selectedEntryId;
      });
      if (sel) {
        var live = gatherEntryPatch();
        base = entrySpeed(Object.assign({}, sel, live));
      } else {
        base = state.previewLines[0].speed || 40;
      }
    } else if (state.previewLines.length) {
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
          fillSectionSelects();
          renderSections();
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

    function onSaveEntry() {
      saveEntry().catch(function (err) {
        setStatus(String(err.message || err));
      });
    }
    $('cr-save-entry').onclick = onSaveEntry;
    $('cr-save-entry-form').onclick = onSaveEntry;

    $('cr-add-entry').onclick = function () {
      addEntryByType().catch(function (err) {
        setStatus(String(err.message || err));
      });
    };

    $('cr-add-section').onclick = function () {
      addSection().catch(function (err) {
        setStatus(String(err.message || err));
      });
    };

    $('cr-entry-type').addEventListener('change', updateEntryTypeHint);
    updateEntryTypeHint();

    ['ef-show-full', 'ef-show-nick', 'ef-speed', 'ef-full', 'ef-nick'].forEach(function (id) {
      $(id).addEventListener('input', rebuildPreview);
      $(id).addEventListener('change', rebuildPreview);
    });

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
