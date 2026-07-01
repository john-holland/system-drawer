(function () {
  'use strict';

  const STATUSES = ['new', 'grooming', 'in_progress', 'in_review', 'submitted', 'completed'];
  const ASSET_KINDS = ['continuum', 'unity', 'legal', 'usc', 'spatial_4d', 'lemma', 'prefab'];
  const board = document.getElementById('board');
  const modalOverlay = document.getElementById('story-modal-overlay');
  const modalBody = document.getElementById('story-modal-body');
  const modalTitle = document.getElementById('story-modal-title');

  let dragStoryId = null;
  let activeStoryId = null;

  if (window.ContinuumNav) {
    ContinuumNav.mount(document.getElementById('continuum-nav-root'), { app: 'story-board' });
  }

  var caveShell = window.ContinuumCaveShell
    ? window.ContinuumCaveShell.init({ tomeId: 'story-board-tome', presence: false })
    : null;

  async function caveMsg(message, payload) {
    if (!caveShell) throw new Error('ContinuumCaveShell not loaded');
    try {
      return await caveShell.caveMessage(message, payload || {});
    } catch (e) {
      throw { status: e.status, body: e.body || {} };
    }
  }
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function esc(s) {
    if (e.body && e.body.buildErrors) return JSON.stringify(e.body.buildErrors);
    if (e.body && e.body.legalCollisionWarnings) return JSON.stringify(e.body.legalCollisionWarnings);
    return (e.body && e.body.error) || 'Request failed';
  }

  function openModal() {
    modalOverlay.classList.add('open');
    modalOverlay.setAttribute('aria-hidden', 'false');
  }

  function closeModal() {
    modalOverlay.classList.remove('open');
    modalOverlay.setAttribute('aria-hidden', 'true');
    modalBody.innerHTML = '';
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

  function bindDetailHandlers(id, s) {
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
          resaurceScheduleId: document.getElementById('detail-schedule').value || null,
          resaurceBudgetPlanId: document.getElementById('detail-budget').value || null,
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

    document.getElementById('btn-open-chat').onclick = function () {
      localStorage.setItem('continuumChatOpen', '1');
      if (s.resaurce_chat_room_id) {
        localStorage.setItem('continuumChatStoryRoom', s.resaurce_chat_room_id);
      }
      var panel = document.getElementById('continuum-chat-panel');
      if (panel) {
        panel.style.display = 'flex';
        var roomInp = panel.querySelector('#continuum-chat-room');
        if (roomInp && s.resaurce_chat_room_id) roomInp.value = s.resaurce_chat_room_id;
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
      localStorage.setItem('continuumChatStoryRoom', s.resaurce_chat_room_id);
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
      '<fieldset><legend>GitHub / Jira</legend>' +
      '<p>Provider <input id="detail-ext-provider" value="' + esc(s.external_provider || 'none') + '" /> ' +
      'Key <input id="detail-ext-key" value="' + esc(s.external_key || '') + '" /></p>' +
      '<p>URL <input id="detail-ext-url" style="width:100%" value="' + esc(s.external_url || '') + '" /></p>' +
      '<p>GitHub project # <input id="detail-gh-proj" type="number" value="' + esc(s.github_project_number || '') + '" /> ' +
      'Jira project <input id="detail-jira-proj" value="' + esc(s.jira_project_key || '') + '" /> ' +
      'Issue type <input id="detail-jira-type" value="' + esc(s.jira_issue_type || '') + '" /></p>' +
      '</fieldset>' +
      '<p>Schedule ID <input id="detail-schedule" value="' + esc(s.resaurce_schedule_id || '') + '" /> ' +
      'Budget plan <input id="detail-budget" value="' + esc(s.resaurce_budget_plan_id || '') + '" /></p>' +
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
      '<button type="button" id="btn-save">Save</button></div>';

    bindDetailHandlers(id, s);
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
