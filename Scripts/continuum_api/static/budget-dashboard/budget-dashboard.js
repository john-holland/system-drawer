(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount(document.getElementById('continuum-nav-root'), { app: 'budget-dashboard' });
  }

  async function loadPlan(planId) {
    var waterRes = await fetch('/api/production/budget/' + encodeURIComponent(planId) + '/water-level');
    var water = await waterRes.json();
    var planRes = await fetch('/api/production/budget/' + encodeURIComponent(planId));
    var planBody = await planRes.json();
    var plan = planBody.budget_plan || planBody;
    var wl = water.water_level || water;
    var capacity = wl.capacity_usd || plan.capacity_usd || plan.total_usd || 1;
    var level = wl.water_level_usd != null ? wl.water_level_usd : plan.water_level_usd;
    var pct = Math.max(0, Math.min(100, (level / capacity) * 100));
    document.getElementById('gauge-wrap').hidden = false;
    document.getElementById('plan-name').textContent = plan.name || planId;
    document.getElementById('gauge-fill').style.width = pct + '%';
    var threshold = wl.low_water_threshold_usd || plan.low_water_threshold_usd || 0;
    var lowPct = capacity > 0 ? (threshold / capacity) * 100 : 0;
    document.getElementById('gauge-low').style.left = lowPct + '%';
    document.getElementById('gauge-label').textContent =
      'Level $' + level.toFixed(2) + ' / capacity $' + capacity.toFixed(2) +
      (wl.alerts && wl.alerts.length ? ' — ' + wl.alerts.map(function (a) { return a.type; }).join(', ') : '');

    var jRes = await fetch('/api/production/budget/' + encodeURIComponent(planId) + '/journal');
    var jBody = await jRes.json();
    document.getElementById('journal').textContent = JSON.stringify(jBody.journal_entries || jBody, null, 2);
  }

  document.getElementById('btn-load').onclick = function () {
    var id = document.getElementById('plan-id').value.trim();
    if (!id) return;
    loadPlan(id).catch(function (e) { alert(e.message || e); });
  };

  document.getElementById('btn-sheets').onclick = async function () {
    var id = document.getElementById('plan-id').value.trim();
    if (!id) return;
    var r = await fetch('/api/production/budget/' + encodeURIComponent(id) + '/publish-sheets', { method: 'POST' });
    var j = await r.json();
    alert(j.ok ? 'Published (or dry-run OK)' : (j.message || j.error || 'Failed'));
  };

  var q = new URLSearchParams(location.search).get('plan');
  if (q) {
    document.getElementById('plan-id').value = q;
    loadPlan(q).catch(console.error);
  }
})();
