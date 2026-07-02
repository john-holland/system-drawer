(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount(document.getElementById('continuum-nav-root'), { app: 'project-calendar' });
  }

  var caveShell = window.ContinuumCaveShell
    ? window.ContinuumCaveShell.init({ tomeId: 'production-calendar-tome', presence: false })
    : null;

  async function caveMsg(message, payload) {
    if (!caveShell) throw new Error('ContinuumCaveShell not loaded');
    return caveShell.caveMessage(message, payload || {});
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  async function api(message, payload) {
    try {
      return await caveMsg(message, payload || {});
    } catch (e) {
      throw { status: e.status, body: e.body || {} };
    }
  }

  function daysBetween(a, b) {
    return Math.max(1, Math.round((new Date(b) - new Date(a)) / 86400000));
  }

  function addDays(isoDate, days) {
    if (!isoDate) return null;
    var d = new Date(isoDate.slice(0, 10) + 'T12:00:00Z');
    d.setUTCDate(d.getUTCDate() + Math.round(Number(days) || 0));
    return d.toISOString().slice(0, 10);
  }

  var state = { schedules: [], overlay: null, context: null, stories: [], budgetPlans: [] };

  function selectedSchedule() {
    var id = document.getElementById('filter-schedule').value;
    if (!id) return null;
    return state.schedules.find(function (s) { return s.id === id; }) || null;
  }

  function narrativeCalendarDate(narrativeT, ctx) {
    if (!ctx || !ctx.effectiveNarrativeStartDate) return null;
    var origin = (ctx.spatial4d && ctx.spatial4d.narrativeTOrigin) || 0;
    var delta = (Number(narrativeT) || 0) - origin;
    return addDays(ctx.effectiveNarrativeStartDate, delta);
  }

  function populateScheduleDropdown() {
    var sel = document.getElementById('filter-schedule');
    var prev = sel.value;
    sel.innerHTML = '<option value="">— select schedule —</option>';
    state.schedules.forEach(function (sched) {
      var opt = document.createElement('option');
      opt.value = sched.id;
      var name = sched.name || sched.id;
      var label = name;
      if (sched.start_date) label += ' · start ' + sched.start_date;
      label += ' [' + sched.id + ']';
      opt.textContent = label;
      sel.appendChild(opt);
    });
    if (prev && state.schedules.some(function (s) { return s.id === prev; })) {
      sel.value = prev;
    }
  }

  function populateBudgetDropdown() {
    var sel = document.getElementById('filter-budget');
    var prev = sel.value;
    sel.innerHTML = '<option value="">— none —</option>';
    state.budgetPlans.forEach(function (plan) {
      var opt = document.createElement('option');
      opt.value = plan.id;
      opt.textContent = (plan.name || plan.id) + ' ($' + (plan.capacity_usd || plan.total_usd || 0) + ')';
      sel.appendChild(opt);
    });
    var sched = selectedSchedule();
    if (sched && sched.budget_plan_id) {
      sel.value = sched.budget_plan_id;
    } else if (prev) {
      sel.value = prev;
    }
  }

  function updateOriginPanel(ctx) {
    var el = document.getElementById('spatial-origin-info');
    var eff = document.getElementById('effective-start');
    if (!ctx || !ctx.spatial4d) {
      el.textContent = 'No spatial 4D volumes found for this schedule scope.';
      eff.textContent = '';
      return;
    }
    var s4 = ctx.spatial4d;
    el.innerHTML =
      '<strong>Spatial 4D origin</strong><br>' +
      't₀ = ' + esc(s4.narrativeTOrigin) + ' narrative sec' +
      (s4.spatial4dTMin != null ? ' (volume t_min ' + esc(s4.spatial4dTMin) + ')' : '') +
      '<br>volumes: ' + esc(s4.spatial4dVolumeCount) +
      (s4.episodeIds && s4.episodeIds.length ? '<br>episodes: ' + esc(s4.episodeIds.join(', ')) : '');
    if (ctx.scheduleStartDate && ctx.spatial4d) {
      var t0 = ctx.spatial4d.narrativeTOrigin;
      eff.textContent =
        'Production schedule start: ' + ctx.scheduleStartDate +
        ' · spatial 4D t₀ = ' + t0 + ' sec' +
        ' → Narrative Day 1 (calendar): ' + (ctx.effectiveNarrativeStartDate || '—');
    } else if (ctx.spatial4d) {
      eff.textContent =
        'Spatial 4D t₀ = ' + ctx.spatial4d.narrativeTOrigin +
        ' sec — select a production schedule to map narrative calendar days.';
    } else {
      eff.textContent = 'Select a production schedule to anchor narrative calendar days.';
    }
  }

  function renderStories(stories, ctx, schedules) {
    var bars = document.getElementById('bars');
    bars.innerHTML = '';
    var sched = selectedSchedule();
    var filterSched = sched && sched.id;

    var heading = document.createElement('p');
    heading.style.fontSize = '0.85rem';
    heading.style.color = '#8b949e';
    var parts = [];
    if (sched) parts.push('Schedule: ' + (sched.name || sched.id));
    if (ctx && ctx.effectiveNarrativeStartDate) {
      parts.push('Narrative Day 1 = ' + ctx.effectiveNarrativeStartDate);
    }
    if (ctx && ctx.spatial4d) {
      parts.push('spatial 4D t₀ = ' + ctx.spatial4d.narrativeTOrigin);
    }
    if (ctx && state.overlay && state.overlay.scale_label) parts.push(state.overlay.scale_label);
    heading.textContent = parts.join(' · ') || 'No overlay configured';
    bars.appendChild(heading);

    var filtered = filterSched
      ? stories.filter(function (s) { return s.resaurce_schedule_id === filterSched; })
      : stories;

    filtered.forEach(function (s) {
      if (!s.calendar_start_date && s.narrative_t_start == null) return;
      var row = document.createElement('div');
      row.className = 'story-row';
      row.tabIndex = 0;
      row.setAttribute('role', 'button');
      row.setAttribute('aria-label', 'Open story: ' + (s.summary || s.id));
      row.innerHTML = '<div style="font-size:0.85rem;margin-top:0.5rem;color:#58a6ff">' + esc(s.summary || s.id) + '</div>';
      function openStory() {
        window.location.href = '/story-board/?story=' + encodeURIComponent(s.id);
      }
      row.addEventListener('click', openStory);
      row.addEventListener('keydown', function (ev) {
        if (ev.key === 'Enter' || ev.key === ' ') {
          ev.preventDefault();
          openStory();
        }
      });
      if (s.calendar_start_date) {
        var bar = document.createElement('div');
        bar.className = 'bar';
        var w = Math.min(100, daysBetween(s.calendar_start_date, s.calendar_end_date || s.calendar_start_date) * 3);
        bar.style.width = w + '%';
        bar.title = 'Calendar: ' + s.calendar_start_date + ' → ' + (s.calendar_end_date || '');
        row.appendChild(bar);
      }
      if (s.narrative_t_start != null && ctx) {
        var nb = document.createElement('div');
        nb.className = 'bar narrative';
        var origin = (ctx.spatial4d && ctx.spatial4d.narrativeTOrigin) || 0;
        var relStart = (Number(s.narrative_t_start) || 0) - origin;
        var relEnd = ((Number(s.narrative_t_end) || relStart + 1) - origin);
        nb.style.marginLeft = Math.min(90, Math.max(0, relStart * 2)) + '%';
        nb.style.width = Math.max(5, (relEnd - relStart) * 2) + '%';
        var calStart = narrativeCalendarDate(s.narrative_t_start, ctx);
        nb.title = 'Narrative t=' + s.narrative_t_start + (calStart ? ' ≈ ' + calStart : '');
        row.appendChild(nb);
      }
      bars.appendChild(row);
    });

    (schedules || []).forEach(function (sch) {
      if (filterSched && sch.id !== filterSched) return;
      (sch.milestones || []).forEach(function (m) {
        var row = document.createElement('div');
        var storyLinks = (m.continuum_story_ids || []).map(function (sid) {
          return '<a href="/story-board/?story=' + encodeURIComponent(sid) + '" style="color:#58a6ff;margin-right:0.35rem">' + esc(sid.slice(0, 14)) + '…</a>';
        }).join('') || '';
        row.innerHTML = '<div style="font-size:0.85rem;margin-top:0.5rem;color:#58a6ff">Milestone: ' + esc(m.label) +
          (storyLinks ? ' · ' + storyLinks : '') + '</div>';
        if (m.start_date) {
          var mb = document.createElement('div');
          mb.className = 'bar milestone';
          var w = Math.min(100, daysBetween(m.start_date, m.end_date || m.start_date) * 3);
          mb.style.width = w + '%';
          mb.title = m.start_date + ' → ' + (m.end_date || '');
          row.appendChild(mb);
        }
        bars.appendChild(row);
      });
    });

    var events = (state.overlay && state.overlay.events) || [];
    if (events.length) {
      var evHead = document.createElement('h3');
      evHead.textContent = 'Narrative events';
      evHead.style.fontSize = '0.9rem';
      bars.appendChild(evHead);
      events.forEach(function (ev) {
        var el = document.createElement('div');
        el.style.fontSize = '0.8rem';
        var origin = (ctx && ctx.spatial4d && ctx.spatial4d.narrativeTOrigin) || 0;
        var rel = (Number(ev.t) || 0) - origin;
        el.style.marginLeft = Math.min(90, Math.max(0, rel * 2)) + '%';
        var cal = ctx ? narrativeCalendarDate(ev.t, ctx) : null;
        el.textContent = 't=' + (ev.t != null ? ev.t : '?') + ' · ' + esc(ev.label || ev.title || '') +
          (cal ? ' (' + cal + ')' : '');
        bars.appendChild(el);
      });
    }
  }

  async function loadWaterGauge(planId) {
    var el = document.getElementById('water-gauge');
    if (!planId || !el) return;
    try {
      var data = await api('production_budget_water_level', { budget_plan_id: planId });
      var wl = data.water_level || data;
      var cap = wl.capacity_usd || 1;
      var level = wl.water_level_usd != null ? wl.water_level_usd : 0;
      var pct = Math.max(0, Math.min(100, (level / cap) * 100));
      el.innerHTML = '<div style="height:12px;background:#21262d;border-radius:6px;overflow:hidden">' +
        '<div style="height:100%;width:' + pct + '%;background:#4caf50"></div></div>' +
        '<small>$' + level.toFixed(0) + ' / $' + cap.toFixed(0) + '</small>';
    } catch (_) {
      el.textContent = '—';
    }
  }

  function previewEffectiveStart() {
    var sched = selectedSchedule();
    var offset = parseFloat(document.getElementById('narrative-offset').value) || 0;
    var start = sched && (sched.start_date || sched.startDate);
    var effective = start ? addDays(start, offset) : null;
    var ctx = state.context;
    var t0 = ctx && ctx.spatial4d ? ctx.spatial4d.narrativeTOrigin : null;
    var effEl = document.getElementById('effective-start');
    if (!start) {
      effEl.textContent = t0 != null
        ? 'Select a production schedule. Offset applies calendar days from schedule start to narrative t₀ (' + t0 + ' sec).'
        : 'Select a production schedule to preview narrative Day 1.';
      return effective;
    }
    effEl.textContent =
      'Preview: at spatial 4D t₀' + (t0 != null ? ' (' + t0 + ' sec)' : '') +
      ', Narrative Day 1 = ' + (effective || '—') +
      ' (schedule start ' + start + ' + ' + offset + ' calendar days)';
    if (state.context) {
      state.context.narrativeStartOffsetDays = offset;
      state.context.effectiveNarrativeStartDate = effective;
    }
    return effective;
  }

  async function load() {
    var schedFilter = document.getElementById('filter-schedule').value;
    var overlayPayload = {};
    if (schedFilter) overlayPayload.schedule_id = schedFilter;
    var storiesPayload = {};
    if (schedFilter) storiesPayload.resaurce_schedule_id = schedFilter;

    var settled = await Promise.allSettled([
      api('list_stories', storiesPayload),
      api('narrative_overlay_get', overlayPayload),
      api('production_schedule_list', {}),
      api('production_budget_list', {}),
      api('calendar_subscriptions_list', {}),
    ]);

    function pick(idx, fallback) {
      var r = settled[idx];
      if (r.status === 'fulfilled') return r.value;
      console.warn('project-calendar load partial failure', idx, r.reason);
      return fallback;
    }

    state.stories = (pick(0, {}).stories) || [];
    var overlayResult = pick(1, {});
    state.overlay = overlayResult.overlay;
    state.context = overlayResult.context || null;
    state.schedules = (pick(2, {}).production_schedules) || [];
    state.budgetPlans = (pick(3, {}).budget_plans) || [];
    var subs = (pick(4, {}).subscriptions) || [];

    populateScheduleDropdown();
    populateBudgetDropdown();

    if (state.overlay) {
      var savedSched = state.overlay.resaurce_schedule_id || state.overlay.resaurceScheduleId;
      if (savedSched && !schedFilter) {
        document.getElementById('filter-schedule').value = savedSched;
        return load();
      }
      if (savedSched) {
        document.getElementById('filter-schedule').value = savedSched;
      }
      document.getElementById('narrative-offset').value =
        state.overlay.narrative_start_offset_days != null
          ? state.overlay.narrative_start_offset_days
          : (state.overlay.narrativeStartOffsetDays != null ? state.overlay.narrativeStartOffsetDays : 0);
      document.getElementById('scale-label').value = state.overlay.scale_label || '';
    }

    document.getElementById('subs-list').textContent = subs.length
      ? subs.map(function (s) { return s.provider + ' · ' + s.id; }).join('\n')
      : 'No subscriptions';

    updateOriginPanel(state.context);
    previewEffectiveStart();
    renderStories(state.stories, state.context, state.schedules);
    loadWaterGauge(document.getElementById('filter-budget').value);
  }

  document.getElementById('btn-save-overlay').onclick = async function () {
    var schedId = document.getElementById('filter-schedule').value || null;
    if (!schedId) {
      alert('Select a production schedule before saving the narrative overlay.');
      return;
    }
    await api('narrative_overlay_save', {
      resaurceScheduleId: schedId,
      narrativeStartOffsetDays: parseFloat(document.getElementById('narrative-offset').value) || 0,
      scaleLabel: document.getElementById('scale-label').value,
      events: (state.overlay && state.overlay.events) || [],
    });
    load();
  };

  document.getElementById('btn-sync-cal').onclick = async function () {
    await api('calendar_sync_now', {});
    alert('Sync triggered');
  };

  document.getElementById('btn-add-sub').onclick = async function () {
    var prov = document.getElementById('sub-provider').value;
    await api('calendar_subscriptions_create', {
      provider: prov,
      cronExpr: '*/15 * * * *',
      targetUrl: document.getElementById('sub-target').value || null,
      resaurceScheduleId: document.getElementById('filter-schedule').value || null,
    });
    load();
  };

  document.getElementById('filter-schedule').onchange = load;
  document.getElementById('filter-budget').onchange = function () {
    loadWaterGauge(document.getElementById('filter-budget').value);
  };
  document.getElementById('narrative-offset').oninput = function () {
    previewEffectiveStart();
    if (state.context) {
      state.context.narrativeStartOffsetDays = parseFloat(document.getElementById('narrative-offset').value) || 0;
      var sched = selectedSchedule();
      if (sched && sched.start_date) {
        state.context.effectiveNarrativeStartDate = addDays(sched.start_date, state.context.narrativeStartOffsetDays);
      }
      updateOriginPanel(state.context);
      renderStories(state.stories, state.context, state.schedules);
    }
  };

  var scheduleModal = document.getElementById('schedule-modal-overlay');
  var milestoneHost = document.getElementById('milestone-rows');

  function selectedOptions(sel) {
    return Array.from(sel.selectedOptions || []).map(function (o) { return o.value; }).filter(Boolean);
  }

  function addMilestoneRow(data) {
    data = data || {};
    var row = document.createElement('div');
    row.className = 'milestone-row';
    row.innerHTML =
      '<label>Label<input class="ms-label" value="' + esc(data.label || '') + '" placeholder="Act I lock" /></label>' +
      '<label>Start<input type="date" class="ms-start" value="' + esc(data.startDate || '') + '" /></label>' +
      '<label>End<input type="date" class="ms-end" value="' + esc(data.endDate || '') + '" /></label>' +
      '<label><input type="checkbox" class="ms-create-story"' + (data.createStory !== false ? ' checked' : '') + ' /> Create story</label>' +
      '<button type="button" class="ms-remove">×</button>';
    row.querySelector('.ms-remove').onclick = function () { row.remove(); };
    milestoneHost.appendChild(row);
  }

  async function loadScheduleModalOptions() {
    var epSel = document.getElementById('sched-episodes');
    var drSel = document.getElementById('sched-drafts');
    var budSel = document.getElementById('sched-budget');
    epSel.innerHTML = '';
    drSel.innerHTML = '';
    budSel.innerHTML = '<option value="">— none —</option>';
    state.budgetPlans.forEach(function (p) {
      var opt = document.createElement('option');
      opt.value = p.id;
      opt.textContent = (p.name || p.id);
      budSel.appendChild(opt);
    });
    try {
      var eps = await fetch('/api/episodes').then(function (r) { return r.json(); });
      (eps.items || eps.episodes || []).forEach(function (ep) {
        var opt = document.createElement('option');
        opt.value = ep.id;
        opt.textContent = (ep.title || ep.id) + ' [' + ep.id.slice(0, 8) + '…]';
        epSel.appendChild(opt);
      });
    } catch (_) { /* ignore */ }
    try {
      var drafts = await fetch('/api/drafts/episodes').then(function (r) { return r.json(); });
      (Array.isArray(drafts) ? drafts : (drafts.items || [])).forEach(function (d) {
        var opt = document.createElement('option');
        opt.value = d.id;
        opt.textContent = (d.title || d.summary || d.id) + ' [draft]';
        drSel.appendChild(opt);
      });
    } catch (_) { /* ignore */ }
  }

  function openScheduleModal() {
    document.getElementById('sched-name').value = '';
    document.getElementById('sched-start').value = '';
    document.getElementById('sched-end').value = '';
    document.getElementById('schedule-modal-msg').textContent = '';
    milestoneHost.innerHTML = '';
    addMilestoneRow({ label: 'Kickoff', createStory: true });
    loadScheduleModalOptions();
    scheduleModal.classList.add('open');
    scheduleModal.setAttribute('aria-hidden', 'false');
  }

  function closeScheduleModal() {
    scheduleModal.classList.remove('open');
    scheduleModal.setAttribute('aria-hidden', 'true');
  }

  document.getElementById('btn-new-schedule').onclick = openScheduleModal;
  document.getElementById('btn-cancel-schedule').onclick = closeScheduleModal;
  document.getElementById('btn-add-milestone').onclick = function () { addMilestoneRow({ createStory: true }); };
  scheduleModal.addEventListener('click', function (ev) {
    if (ev.target === scheduleModal) closeScheduleModal();
  });

  document.getElementById('btn-create-schedule').onclick = async function () {
    var msg = document.getElementById('schedule-modal-msg');
    msg.textContent = 'Creating…';
    var milestones = Array.from(milestoneHost.querySelectorAll('.milestone-row')).map(function (row) {
      return {
        label: row.querySelector('.ms-label').value.trim(),
        startDate: row.querySelector('.ms-start').value,
        endDate: row.querySelector('.ms-end').value,
        createStory: row.querySelector('.ms-create-story').checked,
      };
    }).filter(function (m) { return m.label; });
    if (!document.getElementById('sched-name').value.trim()) {
      msg.textContent = 'Schedule name is required.';
      return;
    }
    if (!document.getElementById('sched-start').value) {
      msg.textContent = 'Production start date is required.';
      return;
    }
    var body = {
      name: document.getElementById('sched-name').value.trim(),
      startDate: document.getElementById('sched-start').value,
      endDate: document.getElementById('sched-end').value || null,
      budgetPlanId: document.getElementById('sched-budget').value || null,
      episodeIds: selectedOptions(document.getElementById('sched-episodes')),
      draftEpisodeIds: selectedOptions(document.getElementById('sched-drafts')),
      milestones: milestones,
    };
    try {
      var res = await fetch('/api/production/schedules/create-with-stories', {
        method: 'POST',
        headers: Object.assign(
          { 'Content-Type': 'application/json' },
          window.ContinuumUserSession ? ContinuumUserSession.getHeaders() : {},
        ),
        body: JSON.stringify(body),
      });
      var data = await res.json().catch(function () { return {}; });
      if (!res.ok) throw new Error(data.error || data.detail || res.statusText);
      closeScheduleModal();
      document.getElementById('filter-schedule').value = (data.production_schedule && data.production_schedule.id) || '';
      await load();
      if ((data.createdStories || []).length) {
        alert('Created schedule with ' + data.createdStories.length + ' milestone story/stories.');
      }
    } catch (e) {
      msg.textContent = e.message || 'Create failed';
    }
  };

  load().catch(console.error);
})();
