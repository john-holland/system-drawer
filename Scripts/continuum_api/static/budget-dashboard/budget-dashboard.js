(function () {
  'use strict';

  if (window.ContinuumNav) {
    ContinuumNav.mount({ root: '#continuum-nav-root', app: 'budget-dashboard' });
  }

  var caveShell = window.ContinuumCaveShell
    ? window.ContinuumCaveShell.init({ tomeId: 'budget-tome', presence: false })
    : null;

  function caveMsg(message, payload) {
    if (!caveShell) return Promise.reject(new Error('ContinuumCaveShell not loaded'));
    return caveShell.caveMessage(message, payload || {});
  }

  async function loadPlan(planId) {
    var water = await caveMsg('production_budget_water_level', { budget_plan_id: planId });
    var planBody = await caveMsg('production_budget_get', { budget_plan_id: planId });
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

    var jBody = await caveMsg('production_budget_journal_list', { budget_plan_id: planId });
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
    var j = await caveMsg('production_budget_publish_sheets', { budget_plan_id: id });
    alert(j.ok ? 'Published (or dry-run OK)' : (j.message || j.error || 'Failed'));
  };

  document.getElementById('btn-template').onclick = function () {
    var url = '/api/production/budget/template?download=1';
    fetch(url, { credentials: 'include' })
      .then(function (res) {
        if (!res.ok) throw new Error('Template download failed (' + res.status + ')');
        return res.blob();
      })
      .then(function (blob) {
        var objUrl = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = objUrl;
        a.download = 'continuum-budget-template.json';
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(function () { URL.revokeObjectURL(objUrl); }, 500);
      })
      .catch(function (e) { alert(e.message || e); });
  };

  var q = new URLSearchParams(location.search).get('plan');
  if (q) {
    document.getElementById('plan-id').value = q;
    loadPlan(q).catch(console.error);
  }
})();
