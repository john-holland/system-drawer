(function () {
  'use strict';

  const STATUSES = ['new', 'grooming', 'in_progress', 'in_review', 'submitted', 'completed'];
  const ASSET_KINDS = ['continuuuum', 'unity', 'legal', 'usc', 'spatial_4d', 'lemma', 'prefab'];
  const board = document.getElementById('board');
  const modalOverlay = document.getElementById('story-modal-overlay');
  const modalBody = document.getElementById('story-modal-body');
  const modalTitle = document.getElementById('story-modal-title');

  let dragStoryId = null;
  let activeStoryId = null;
  const state = { schedules: [], budgetPlans: [], autocompleteDisposers: [] };

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount(document.getElementById('continuuuum-nav-root'), { app: 'story-board' });
  }

  var caveShell = window.ContinuuuumCaveShell
    ? window.ContinuuuumCaveShell.init({ tomeId: 'story-board-tome', presence: false })
    : null;

  async function caveMsg(message, payload) {
    if (!caveShell) throw new Error('ContinuuuumCaveShell not loaded');
    try {
      return await caveShell.caveMessage(message, payload || {});
    } catch (e) {
      throw { status: e.status, body: e.body || {} };
    }
  }

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function formatError(e) {
    if (e.body && e.body.buildErrors) return JSON.stringify(e.body.buildErrors);
    if (e.body && e.body.legalCollisionWarnings) return JSON.stringify(e.body.legalCollisionWarnings);
    return (e.body && e.body.error) || 'Request failed';
  }

  function userHeaders() {
    return {
      'Content-Type': 'application/json',
      'X-User-ID': (window.ContinuuuumUserSession && ContinuuuumUserSession.getUserId()) || 'anonymous',
    };
  }

  async function loadProductionCatalog() {
    var settled = await Promise.allSettled([
      fetch('/api/production/schedules', { headers: userHeaders() }).then(function (r) { return r.json(); }),
      fetch('/api/production/budget', { headers: userHeaders() }).then(function (r) { return r.json(); }),
    ]);
    var schedBody = settled[0].status === 'fulfilled' ? settled[0].value : {};
    var budgetBody = settled[1].status === 'fulfilled' ? settled[1].value : {};
    state.schedules = schedBody.production_schedules || [];
    state.budgetPlans = budgetBody.budget_plans || [];
    populateToolbarScheduleDropdown();
  }

  function scheduleLabel(sched) {
    if (!sched) return '';
    var name = sched.name || sched.id;
    var label = name;
    if (sched.start_date) label += ' · start ' + sched.start_date;
    label += ' [' + sched.id + ']';
    return label;
  }

  function budgetLabel(plan) {
    if (!plan) return '';
    var cap = plan.capacity_usd != null ? plan.capacity_usd : plan.total_usd;
    var suffix = cap != null ? ' ($' + cap + ')' : '';
    return (plan.name || plan.id) + suffix + ' [' + plan.id + ']';
  }

  function populateToolbarScheduleDropdown() {
    var sel = document.getElementById('filter-schedule');
    if (!sel) return;
    var prev = sel.value;
    sel.innerHTML = '<option value="">All schedules</option>';
    state.schedules.forEach(function (sched) {
      var opt = document.createElement('option');
      opt.value = sched.id;
      opt.textContent = scheduleLabel(sched);
      sel.appendChild(opt);
    });
    if (prev && state.schedules.some(function (s) { return s.id === prev; })) {
      sel.value = prev;
    }
  }

  function catalogWithStoryFallback(items, currentId, kind) {
    var list = (items || []).slice();
    if (!currentId || list.some(function (it) { return it.id === currentId; })) return list;
    var stub = { id: currentId, name: currentId };
    if (kind === 'budget') stub.capacity_usd = null;
    list.unshift(stub);
    return list;
  }

  function disposeAutocompletes() {
    state.autocompleteDisposers.forEach(function (fn) { fn(); });
    state.autocompleteDisposers = [];
  }

  function mountAutocomplete(hostEl, options) {
    hostEl.innerHTML = '';
    var wrap = document.createElement('div');
    wrap.className = 'autocomplete-wrap';
    var input = document.createElement('input');
    input.type = 'search';
    input.autocomplete = 'off';
    input.placeholder = options.placeholder || 'Search…';
    input.spellcheck = false;
    var list = document.createElement('div');
    list.className = 'autocomplete-list';
    list.hidden = true;
    wrap.appendChild(input);
    wrap.appendChild(list);
    hostEl.appendChild(wrap);

    var selectedId = options.value || '';
    var selectedItem = options.items.find(function (item) { return options.getId(item) === selectedId; });
    input.value = selectedItem ? options.getLabel(selectedItem) : (selectedId || '');

    function setSelection(item) {
      if (!item) {
        selectedId = '';
        input.value = '';
      } else {
        selectedId = options.getId(item);
        input.value = options.getLabel(item);
      }
      list.hidden = true;
      if (options.onChange) options.onChange(selectedId, item || null);
    }

    function renderList() {
      var q = input.value.trim().toLowerCase();
      var matches = options.items.filter(function (item) {
        var label = options.getLabel(item).toLowerCase();
        var id = options.getId(item).toLowerCase();
        return !q || label.indexOf(q) !== -1 || id.indexOf(q) !== -1;
      }).slice(0, 12);
      list.innerHTML = '';
      if (options.allowClear !== false) {
        var clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.className = 'autocomplete-item';
        clearBtn.textContent = options.clearLabel || '— none —';
        clearBtn.onclick = function () { setSelection(null); };
        list.appendChild(clearBtn);
      }
      matches.forEach(function (item) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'autocomplete-item';
        btn.innerHTML = esc(options.getLabel(item)) + '<small>' + esc(options.getId(item)) + '</small>';
        btn.onclick = function () { setSelection(item); };
        list.appendChild(btn);
      });
      list.hidden = matches.length === 0 && options.allowClear === false;
    }

    input.addEventListener('focus', function () {
      renderList();
      list.hidden = false;
    });
    input.addEventListener('input', function () {
      selectedId = '';
      renderList();
      list.hidden = false;
      if (options.onChange) options.onChange('', null);
    });
    input.addEventListener('keydown', function (ev) {
      if (ev.key === 'Escape') {
        list.hidden = true;
        input.blur();
      }
    });
    document.addEventListener('click', onDocClick);
    function onDocClick(ev) {
      if (!wrap.contains(ev.target)) list.hidden = true;
    }
    state.autocompleteDisposers.push(function () {
      document.removeEventListener('click', onDocClick);
    });

    return {
      getValue: function () { return selectedId; },
      setItems: function (items) { options.items = items || []; },
      setValue: function (id) {
        selectedId = id || '';
        var item = options.items.find(function (it) { return options.getId(it) === selectedId; });
        input.value = item ? options.getLabel(item) : (selectedId || '');
      },
    };
  }

  async function ensureStoryChatRoom(storyId, story) {
    if (story && story.resaurce_chat_room_id) return story.resaurce_chat_room_id;
    var res = await fetch('/api/stories/' + encodeURIComponent(storyId) + '/ensure-chat', {
      method: 'POST',
      headers: userHeaders(),
    });
    var data = await res.json().catch(function () { return {}; });
    if (!res.ok) {
      var msg = data.detail || data.error || ('Chat unavailable (' + res.status + ')');
      throw new Error(msg);
    }
    return data.chatRoomId;
  }

  function openStoryChatPanel(roomId) {
    if (!roomId) return;
    if (window.ContinuuuumNav && ContinuuuumNav.openChat) {
      ContinuuuumNav.openChat({ roomId: roomId, kind: 'story' });
      return;
    }
    localStorage.removeItem('continuuuumChatTableReadRoom');
    localStorage.setItem('continuuuumChatStoryRoom', roomId);
    localStorage.setItem('continuuuumChatOpen', '1');
    var panel = document.getElementById('continuuuum-chat-panel');
    if (panel) {
      panel.style.display = 'flex';
      if (panel._continuuuumRefreshChat) panel._continuuuumRefreshChat();
    }
  }

  function openModal() {
    modalOverlay.classList.add('open');
    modalOverlay.setAttribute('aria-hidden', 'false');
  }

  function closeModal() {
    modalOverlay.classList.remove('open');
    modalOverlay.setAttribute('aria-hidden', 'true');
    modalBody.innerHTML = '';
    disposeAutocompletes();
    activeStoryId = null;
    setStoryQuery(null, true);
  }

  function setStoryQuery(storyId, replace) {
    var url = new URL(location.href);
    if (storyId) {
      url.searchParams.set('story', storyId);
    } else {
      url.searchParams.delete('story');
    }
    var method = replace ? 'replaceState' : 'pushState';
    history[method]({ storyId: storyId || null }, '', url.pathname + url.search);
  }

  modalOverlay.addEventListener('click', function (ev) {
    if (ev.target === modalOverlay) closeModal();
  });
  var modalPanel = modalOverlay.querySelector('.story-modal');
  if (modalPanel) {
    modalPanel.addEventListener('click', function (ev) { ev.stopPropagation(); });
  }
  document.getElementById('story-modal-close').onclick = closeModal;
  document.addEventListener('keydown', function (ev) {
    if (ev.key === 'Escape' && modalOverlay.classList.contains('open')) closeModal();
  });

  function assetRefLink(kind, ref) {
    if (!ref) return '';
    var r = typeof ref === 'string' ? (function () { try { return JSON.parse(ref); } catch (_) { return {}; } })() : ref;
    if (kind === 'spatial_4d' && (r.table || r.tableName)) {
      return '<a class="chip" href="/sql-viewer?recipe=work_orders_with_assets&amp;q=' + esc(r.table || r.tableName) + '">spatial4d:' + esc(r.table || r.tableName) + '</a>';
    }
    if (kind === 'lemma' && (r.lemmaEntryId || r.entryId)) {
      var eid = r.lemmaEntryId || r.entryId;
      return '<a class="chip" href="/lemma-library?entryId=' + encodeURIComponent(eid) + '">lemma:' + esc(eid) + '</a>';
    }
    if (kind === 'legal' && (r.legalCaseId || r.caseId)) {
      return '<a class="chip" href="/legal-tracker/?case=' + encodeURIComponent(r.legalCaseId || r.caseId) + '">legal</a>';
    }
    if (kind === 'prefab' && r.prefabPath) {
      return '<span class="chip">' + esc(r.prefabPath) + '</span>';
    }
    return '<span class="chip">' + esc(JSON.stringify(r).slice(0, 60)) + '</span>';
  }

  function setupColumnDrop(col, status) {
    col.addEventListener('dragover', function (ev) {
      ev.preventDefault();
      ev.dataTransfer.dropEffect = 'move';
      col.classList.add('col-drop-target');
    });
    col.addEventListener('dragleave', function (ev) {
      if (!col.contains(ev.relatedTarget)) col.classList.remove('col-drop-target');
    });
    col.addEventListener('drop', async function (ev) {
      ev.preventDefault();
      col.classList.remove('col-drop-target');
      var storyId = ev.dataTransfer.getData('text/story-id') || dragStoryId;
      if (!storyId) return;
      try {
        await caveMsg('patch_story', { story_id: storyId, status: status });
        await loadStories();
        if (activeStoryId === storyId) showDetail(storyId);
      } catch (e) {
        alert(formatError(e));
        loadStories();
      }
    });
  }

  function setupCardDrag(card, story) {
    card.dataset.storyId = story.id;
    var handle = card.querySelector('.card-drag-handle');
    if (!handle) return;

    handle.draggable = true;

    handle.addEventListener('dragstart', function (ev) {
      dragStoryId = story.id;
      card.classList.add('card-dragging');
      ev.dataTransfer.effectAllowed = 'move';
      ev.dataTransfer.setData('text/story-id', story.id);
    });

    handle.addEventListener('dragend', function () {
      card.classList.remove('card-dragging');
      dragStoryId = null;
      board.querySelectorAll('.col-drop-target').forEach(function (el) {
        el.classList.remove('col-drop-target');
      });
    });

    card.addEventListener('click', function (ev) {
      if (ev.target.closest('.card-drag-handle')) return;
      showDetail(story.id);
    });
  }

  function renderBoard(stories) {
    board.innerHTML = '';
    STATUSES.forEach(function (status) {
      var col = document.createElement('div');
      col.className = 'col';
      col.dataset.status = status;
      col.innerHTML = '<h3>' + status.replace(/_/g, ' ') + ' <small>(' +
        stories.filter(function (s) { return s.status === status; }).length + ')</small></h3>';

      var cardsHost = document.createElement('div');
      cardsHost.className = 'col-cards';
      stories.filter(function (s) { return s.status === status; }).forEach(function (s) {
        var card = document.createElement('div');
        card.className = 'card';
        var errHint = (s.buildErrors && s.buildErrors.length) ? ' <span class="errors" title="Build errors">⚠</span>' : '';
        card.innerHTML =
          '<span class="card-drag-handle" title="Drag to move column" aria-label="Drag">⠿</span>' +
          '<div class="card-body"><strong>' + esc(s.summary || s.id) + '</strong>' + errHint +
          '<br><small>value: ' + esc(s.story_value || 0) + '</small></div>';
        setupCardDrag(card, s);
        cardsHost.appendChild(card);
      });
      col.appendChild(cardsHost);
      setupColumnDrop(col, status);
      board.appendChild(col);
    });
  }

  async function loadStories() {
    await loadProductionCatalog();
    var sched = document.getElementById('filter-schedule').value.trim();
    var payload = {};
    if (sched) payload.resaurce_schedule_id = sched;
    var data = await caveMsg('list_stories', payload);
    renderBoard(data.stories || []);
  }

  async function loadWorkOrders(storyId) {
    var data = await caveMsg('list_work_orders', { story_id: storyId });
    return data.workOrders || [];
  }

  function bindDetailHandlers(id, s, refs) {
    refs = refs || {};
    document.getElementById('btn-save').onclick = async function () {
      try {
        await caveMsg('patch_story', {
          story_id: id,
          status: document.getElementById('detail-status').value,
          description: document.getElementById('detail-desc').value,
          storyValue: parseFloat(document.getElementById('detail-value').value) || 0,
          externalProvider: document.getElementById('detail-ext-provider').value,
          externalKey: document.getElementById('detail-ext-key').value,
          externalUrl: document.getElementById('detail-ext-url').value,
          githubProjectNumber: document.getElementById('detail-gh-proj').value
            ? parseInt(document.getElementById('detail-gh-proj').value, 10) : null,
          jiraProjectKey: document.getElementById('detail-jira-proj').value,
          jiraIssueType: document.getElementById('detail-jira-type').value,
          resaurceScheduleId: refs.scheduleAc ? refs.scheduleAc.getValue() || null : null,
          resaurceBudgetPlanId: refs.budgetAc ? refs.budgetAc.getValue() || null : null,
        });
        await loadStories();
        showDetail(id);
      } catch (e) {
        alert(formatError(e));
      }
    };

    document.getElementById('btn-assign').onclick = async function () {
      var uid = document.getElementById('detail-assignee').value.trim();
      if (!uid) return;
      await caveMsg('story_assignees_add', { story_id: id, userId: uid });
      showDetail(id);
    };

    document.getElementById('btn-watcher').onclick = async function () {
      var uid = document.getElementById('detail-watcher').value.trim();
      if (!uid) return;
      await caveMsg('story_watchers_add', { story_id: id, userId: uid });
      showDetail(id);
    };

    modalBody.querySelectorAll('.btn-rm-assignee').forEach(function (btn) {
      btn.onclick = async function () {
        await caveMsg('story_assignees_remove', { story_id: id, userId: btn.dataset.uid });
        showDetail(id);
      };
    });

    modalBody.querySelectorAll('.btn-rm-watcher').forEach(function (btn) {
      btn.onclick = async function () {
        await caveMsg('story_watchers_remove', { story_id: id, userId: btn.dataset.uid });
        showDetail(id);
      };
    });

    document.getElementById('btn-open-chat').onclick = async function () {
      var btn = document.getElementById('btn-open-chat');
      btn.disabled = true;
      try {
        var roomId = await ensureStoryChatRoom(id, s);
        s.resaurce_chat_room_id = roomId;
        openStoryChatPanel(roomId);
      } catch (e) {
        alert(e.message || 'Could not open story chat');
      } finally {
        btn.disabled = false;
      }
    };

    document.getElementById('btn-validate').onclick = async function () {
      try {
        var r = await caveMsg('story_validate_causality', { story_id: id });
        alert(r.ok ? 'OK' : JSON.stringify(r.buildErrors));
      } catch (e) {
        alert(JSON.stringify((e.body && e.body.buildErrors) || e.body || e));
      }
      showDetail(id);
    };

    document.getElementById('btn-add-wo').onclick = async function () {
      var refRaw = document.getElementById('wo-asset-ref').value.trim();
      var assetRef = {};
      if (refRaw) {
        try { assetRef = JSON.parse(refRaw); } catch (_) { alert('Invalid asset ref JSON'); return; }
      }
      var wo = await caveMsg('create_work_order', {
        storyId: id,
        episodeId: document.getElementById('wo-episode').value.trim() || undefined,
        assetKind: document.getElementById('wo-asset-kind').value,
        assetRef: assetRef,
        narrativeType: 'linear',
        status: 'pending',
      });
      if (wo.legalCollisionWarnings && wo.legalCollisionWarnings.length) {
        alert('Legal warnings: ' + wo.legalCollisionWarnings.map(function (w) { return w.message; }).join('; '));
      }
      showDetail(id);
    };

    modalBody.querySelectorAll('.btn-wo-test').forEach(function (btn) {
      btn.onclick = async function () {
        try {
          var r = await caveMsg('run_work_order_causality', { work_order_id: btn.dataset.wo });
          alert(r.ok ? 'Pass' : JSON.stringify(r.buildErrors));
        } catch (e) {
          alert(JSON.stringify((e.body && e.body.buildErrors) || e.body || e));
        }
        showDetail(id);
      };
    });

    var btnClone = document.getElementById('btn-clone-story');
    if (btnClone) {
      btnClone.onclick = async function () {
        if (!confirm('Clone this story into a new card? Work orders are not copied.')) return;
        try {
          var cloned = await caveMsg('story_clone', {
            story_id: id,
            resaurceScheduleId: refs.scheduleAc ? refs.scheduleAc.getValue() || null : s.resaurce_schedule_id,
            resaurceBudgetPlanId: refs.budgetAc ? refs.budgetAc.getValue() || null : s.resaurce_budget_plan_id,
          });
          await loadStories();
          if (cloned && cloned.id) showDetail(cloned.id);
        } catch (e) {
          alert(formatError(e));
        }
      };
    }

    var btnReopen = document.getElementById('btn-reopen-story');
    if (btnReopen) {
      btnReopen.onclick = async function () {
        var reason = prompt('Reason for reopening (optional):', 'Reopening submitted story for more work.');
        if (reason === null) return;
        try {
          await caveMsg('story_reopen', { story_id: id, reason: reason });
          await loadStories();
          showDetail(id);
        } catch (e) {
          alert(formatError(e));
        }
      };
    }
  }

  async function showDetail(id, replaceHistory) {
    activeStoryId = id;
    modalTitle.textContent = 'Story';
    modalBody.innerHTML = '<p>Loading…</p>';
    openModal();
    setStoryQuery(id, !!replaceHistory);

    var s;
    var wos;
    try {
      s = await caveMsg('get_story', { story_id: id });
      wos = await loadWorkOrders(id);
    } catch (e) {
      modalBody.innerHTML = '<p class="errors">Failed to load story: ' + esc(formatError(e)) + '</p>';
      return;
    }
    if (s.resaurce_chat_room_id) {
      localStorage.setItem('continuuuumChatStoryRoom', s.resaurce_chat_room_id);
    }

    modalTitle.textContent = s.summary || s.id;

    var woHtml = wos.map(function (wo) {
      var ref = wo.asset_ref_json;
      var kind = wo.asset_kind || '';
      return '<div class="wo-row">' +
        '<a class="chip" href="/sql-viewer?recipe=work_orders_with_assets&amp;highlight=' + esc(wo.id) + '">' + esc(wo.id) + '</a> ' +
        '<span>' + esc(wo.status) + '</span> ' +
        (kind ? '<span class="chip kind">' + esc(kind) + '</span> ' : '') +
        assetRefLink(kind, ref) +
        ' <button type="button" class="btn-wo-test" data-wo="' + esc(wo.id) + '">Test</button>' +
        '</div>';
    }).join('') || '<em>none</em>';

    var assigneeList = (s.assignees || []).map(function (a) {
      return '<span class="chip">' + esc(a.user_id) +
        ' <button type="button" class="btn-rm-assignee" data-uid="' + esc(a.user_id) + '">×</button></span>';
    }).join(' ') || '<em>none</em>';

    var watcherList = (s.watchers || []).map(function (w) {
      return '<span class="chip">' + esc(w.user_id) +
        ' <button type="button" class="btn-rm-watcher" data-uid="' + esc(w.user_id) + '">×</button></span>';
    }).join(' ') || '<em>none</em>';

    var errs = (s.buildErrors || []).map(function (e) { return e.message; }).join('; ');

    modalBody.innerHTML =
      '<p><label>Description<br><textarea id="detail-desc" rows="3" style="width:100%">' + esc(s.description || '') + '</textarea></label></p>' +
      '<p>Status: <select id="detail-status">' + STATUSES.map(function (st) {
        return '<option value="' + st + '"' + (st === s.status ? ' selected' : '') + '>' + st.replace(/_/g, ' ') + '</option>';
      }).join('') + '</select></p>' +
      '<p>Story value: <input id="detail-value" type="number" step="0.01" value="' + esc(s.story_value || 0) + '" /></p>' +
      '<fieldset><legend>Production</legend>' +
      '<div class="production-row">' +
      '<div><label for="detail-schedule-host">Schedule</label><div id="detail-schedule-host"></div></div>' +
      '<div><label for="detail-budget-host">Budget plan</label><div id="detail-budget-host"></div></div>' +
      '</div></fieldset>' +
      '<fieldset><legend>GitHub / Jira</legend>' +
      '<p>Provider <input id="detail-ext-provider" value="' + esc(s.external_provider || 'none') + '" /> ' +
      'Key <input id="detail-ext-key" value="' + esc(s.external_key || '') + '" /></p>' +
      '<p>URL <input id="detail-ext-url" style="width:100%" value="' + esc(s.external_url || '') + '" /></p>' +
      '<p>GitHub project # <input id="detail-gh-proj" type="number" value="' + esc(s.github_project_number || '') + '" /> ' +
      'Jira project <input id="detail-jira-proj" value="' + esc(s.jira_project_key || '') + '" /> ' +
      'Issue type <input id="detail-jira-type" value="' + esc(s.jira_issue_type || '') + '" /></p>' +
      '</fieldset>' +
      '<p>Assignees: ' + assigneeList + '</p>' +
      '<p><input id="detail-assignee" placeholder="user id" /> <button type="button" id="btn-assign">Add assignee</button></p>' +
      '<p>Watchers: ' + watcherList + '</p>' +
      '<p><input id="detail-watcher" placeholder="watcher user id" /> <button type="button" id="btn-watcher">Add watcher</button></p>' +
      '<p>Work orders:<br>' + woHtml + '</p>' +
      '<fieldset><legend>New work order</legend>' +
      '<p>Episode <input id="wo-episode" placeholder="episode id" /> ' +
      'Kind <select id="wo-asset-kind">' + ASSET_KINDS.map(function (k) {
        return '<option value="' + k + '">' + k + '</option>';
      }).join('') + '</select></p>' +
      '<p>Asset ref JSON <input id="wo-asset-ref" style="width:100%" placeholder=\'{"lemmaEntryId":"..."}\' /></p>' +
      '<button type="button" id="btn-add-wo">Create &amp; link WO</button></fieldset>' +
      (errs ? '<p class="errors">Build errors: ' + esc(errs) + '</p>' : '') +
      '<div class="story-modal-actions">' +
      '<button type="button" id="btn-open-chat">Open chat</button> ' +
      '<button type="button" id="btn-validate">Validate causality</button> ' +
      (s.status === 'submitted' ? '<button type="button" id="btn-reopen-story">Reopen</button> ' : '') +
      '<button type="button" id="btn-clone-story">Clone to new</button> ' +
      '<button type="button" id="btn-save">Save</button></div>';

    disposeAutocompletes();
    var scheduleAc = mountAutocomplete(document.getElementById('detail-schedule-host'), {
      items: catalogWithStoryFallback(state.schedules, s.resaurce_schedule_id, 'schedule'),
      value: s.resaurce_schedule_id || '',
      placeholder: 'Search schedules…',
      clearLabel: '— no schedule —',
      getId: function (item) { return item.id; },
      getLabel: scheduleLabel,
    });
    var budgetAc = mountAutocomplete(document.getElementById('detail-budget-host'), {
      items: catalogWithStoryFallback(state.budgetPlans, s.resaurce_budget_plan_id, 'budget'),
      value: s.resaurce_budget_plan_id || '',
      placeholder: 'Search budget plans…',
      clearLabel: '— no budget —',
      getId: function (item) { return item.id; },
      getLabel: budgetLabel,
    });

    bindDetailHandlers(id, s, { scheduleAc: scheduleAc, budgetAc: budgetAc });
  }

  document.getElementById('btn-refresh').onclick = loadStories;
  document.getElementById('filter-schedule').addEventListener('change', loadStories);

  document.getElementById('btn-new').onclick = async function () {
    var summary = prompt('Story summary');
    if (!summary) return;
    try {
      var r = await caveMsg('create_story', { summary: summary, storyValue: 1 });
      if (r.legalCollisionWarnings && r.legalCollisionWarnings.length) {
        alert('Legal warnings: ' + r.legalCollisionWarnings.map(function (w) { return w.message; }).join('; '));
      }
      await loadStories();
      if (r.id) showDetail(r.id);
    } catch (e) {
      alert(formatError(e));
    }
  };

  window.addEventListener('popstate', function () {
    var storyId = new URLSearchParams(location.search).get('story');
    if (storyId) {
      showDetail(storyId, true);
    } else if (modalOverlay.classList.contains('open')) {
      closeModal();
    }
  });

  loadStories()
    .then(function () {
      var storyId = new URLSearchParams(location.search).get('story');
      if (storyId) showDetail(storyId, true);
    })
    .catch(function (e) { console.error(e); });
})();
