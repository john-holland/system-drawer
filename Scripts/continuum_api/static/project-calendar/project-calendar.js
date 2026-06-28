(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount(document.getElementById('continuum-nav-root'), { app: 'project-calendar' });
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
  }

  async function api(path, opts) {
    var r = await fetch(path, opts || {});
    if (!r.ok) {
      var err = await r.json().catch(function () { return {}; });
      throw { status: r.status, body: err };
    }
    return r.json();
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
    sel.innerHTML = '<option value="">— all schedules —</option>';
    state.schedules.forEach(function (sched) {
      var opt = document.createElement('option');
      opt.value = sched.id;
      var label = sched.name || sched.id;
      if (sched.start_date) label += ' (' + sched.start_date + ')';
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
    if (ctx.scheduleStartDate) {
      eff.textContent = 'Schedule start: ' + ctx.scheduleStartDate +
        ' → narrative Day 1: ' + (ctx.effectiveNarrativeStartDate || '—');
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
      row.innerHTML = '<div style="font-size:0.85rem;margin-top:0.5rem">' + esc(s.summary || s.id) + '</div>';
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
        row.innerHTML = '<div style="font-size:0.85rem;margin-top:0.5rem;color:#58a6ff">Milestone: ' + esc(m.label) + '</div>';
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
      var data = await api('/api/production/budget/' + encodeURIComponent(planId) + '/water-level');
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
    document.getElementById('effective-start').textContent = start
      ? 'Preview: narrative Day 1 = ' + (effective || '—') + ' (schedule ' + start + ' + ' + offset + 'd)'
      : 'Select a schedule to preview narrative Day 1.';
    return effective;
  }

  async function load() {
    var schedFilter = document.getElementById('filter-schedule').value;
    var overlayUrl = '/api/narrative-timeline-overlay';
    if (schedFilter) overlayUrl += '?schedule_id=' + encodeURIComponent(schedFilter);
    var storiesUrl = '/api/stories';
    if (schedFilter) storiesUrl += '?resaurce_schedule_id=' + encodeURIComponent(schedFilter);

    var results = await Promise.all([
      api(storiesUrl),
      api(overlayUrl),
      api('/api/production/schedules'),
      api('/api/production/budget').catch(function () { return { budget_plans: [] }; }),
      api('/api/calendar/subscriptions').catch(function () { return { subscriptions: [] }; }),
    ]);

    state.stories = results[0].stories || [];
    state.overlay = results[1].overlay;
    state.context = results[1].context || null;
    state.schedules = results[2].production_schedules || [];
    state.budgetPlans = results[3].budget_plans || [];
    var subs = results[4].subscriptions || [];

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
    await api('/api/narrative-timeline-overlay', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        resaurceScheduleId: schedId,
        narrativeStartOffsetDays: parseFloat(document.getElementById('narrative-offset').value) || 0,
        scaleLabel: document.getElementById('scale-label').value,
        events: (state.overlay && state.overlay.events) || [],
      }),
    });
    load();
  };

  document.getElementById('btn-sync-cal').onclick = async function () {
    await api('/api/calendar/sync-now', { method: 'POST' });
    alert('Sync triggered');
  };

  document.getElementById('btn-add-sub').onclick = async function () {
    var prov = document.getElementById('sub-provider').value;
    await api('/api/calendar/subscriptions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        provider: prov,
        cronExpr: '*/15 * * * *',
        targetUrl: document.getElementById('sub-target').value || null,
        resaurceScheduleId: document.getElementById('filter-schedule').value || null,
      }),
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

  load().catch(console.error);
})();
