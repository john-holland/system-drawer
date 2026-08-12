(function () {
  "use strict";

  var Session = window.ContinuuuumUserSession;

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ app: "payroll", theme: "light" });
  }

  var state = { companyId: null, monthDays: 30, summary: null };

  function $(id) {
    return document.getElementById(id);
  }

  function isAdmin() {
    return !!(Session && Session.isAdmin && Session.isAdmin());
  }

  function money(n) {
    if (n == null || Number.isNaN(Number(n))) return "—";
    return "$" + Number(n).toLocaleString(undefined, { maximumFractionDigits: 2 });
  }

  async function api(path, opts) {
    opts = opts || {};
    var headers =
      Session && Session.getHeaders
        ? Session.getHeaders({ "Content-Type": "application/json" })
        : { "Content-Type": "application/json" };
    var res = await fetch(path, {
      ...opts,
      headers: Object.assign({}, headers, opts.headers || {}),
      credentials: "include",
      body:
        opts.body && typeof opts.body !== "string"
          ? JSON.stringify(opts.body)
          : opts.body,
    });
    var data = await res.json().catch(function () {
      return {};
    });
    if (!res.ok) throw new Error(data.error || res.statusText);
    return data;
  }

  function syncAdminUi() {
    var admin = isAdmin();
    var form = $("pay-hwm-pct");
    if (!form) return;
    form.classList.toggle("pay-admin-locked", !admin);
    ["highWaterMarkUsd", "hwmRetainerPct"].forEach(function (name) {
      var el = form.elements[name];
      if (el) el.disabled = !admin;
    });
    var save = $("pay-hwm-save");
    if (save) save.disabled = !admin;
    var hint = $("pay-admin-hint");
    if (hint) {
      hint.hidden = admin;
      hint.textContent = admin
        ? ""
        : "Admin required to change HWM USD and retainer amounts — enable Admin in the header Dev panel (or pick the admin preset).";
    }
    document.querySelectorAll("[data-retainer-edit]").forEach(function (row) {
      row.hidden = !admin;
    });
  }

  async function saveRetainerAmount(retainerId, body) {
    if (!isAdmin()) {
      throw new Error(
        "Admin required — enable Admin in the header Dev panel, then try again"
      );
    }
    await api(
      "/api/payroll/companies/" +
        encodeURIComponent(state.companyId) +
        "/retainers/" +
        encodeURIComponent(retainerId),
      { method: "PATCH", body: body }
    );
    await loadDetail();
  }

  function updateCronHuman() {
    var primary = $("pay-cron-primary");
    var totals = $("pay-cron-totals");
    if (!primary || !window.PayrollCronHumanize) return;
    var cron = $("pay-ret-cron").value;
    var amt = $("pay-ret-amount").value;
    var result = PayrollCronHumanize.analyze(cron, amt, {
      monthDays: state.monthDays,
    });
    primary.textContent = result.narrative;
    totals.textContent = PayrollCronHumanize.formatTotalsRow(
      result.byMonthDays,
      amt
    );
  }

  function mountCronExamples() {
    var host = $("pay-cron-examples");
    if (!host || !window.PayrollCronHumanize) return;
    host.innerHTML = "";
    (PayrollCronHumanize.examples || []).forEach(function (ex) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "pay-cron-chip";
      btn.textContent = ex.label;
      btn.title = ex.blurb + " — " + ex.cron;
      btn.addEventListener("click", function () {
        $("pay-ret-cron").value = ex.cron;
        if (ex.amountUsd != null && $("pay-ret-mode").value === "fixed_cron") {
          $("pay-ret-amount").value = ex.amountUsd;
        }
        updateCronHuman();
      });
      host.appendChild(btn);
    });
  }

  function mountCronLens() {
    var host = $("pay-cron-lens");
    if (!host) return;
    host.querySelectorAll(".pay-lens-btn").forEach(function (btn) {
      btn.addEventListener("click", function () {
        state.monthDays = Number(btn.getAttribute("data-days")) || 30;
        host.querySelectorAll(".pay-lens-btn").forEach(function (b) {
          b.classList.toggle("is-active", b === btn);
        });
        updateCronHuman();
      });
    });
  }

  async function loadCompanies() {
    var data = await api("/api/payroll/companies");
    var ul = $("pay-company-list");
    ul.innerHTML = "";
    (data.companies || []).forEach(function (c) {
      var li = document.createElement("li");
      li.innerHTML =
        "<span><strong>" +
        c.name +
        "</strong> — " +
        c.phase +
        " · HWM% " +
        (c.hwmRetainerPct * 100).toFixed(1) +
        "%</span>";
      var btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = "Open";
      btn.onclick = function () {
        state.companyId = c.id;
        loadDetail().catch(alert);
      };
      li.appendChild(btn);
      ul.appendChild(li);
    });
  }

  function renderMembers(members) {
    var ul = $("pay-members");
    ul.innerHTML = "";
    (members || []).forEach(function (m) {
      var li = document.createElement("li");
      li.innerHTML =
        "<span>" +
        m.displayName +
        " <em>(" +
        m.role +
        ")</em>" +
        (m.gameplay ? " · gameplay" : "") +
        (m.technical ? " · technical" : "") +
        (!m.active ? " · inactive" : "") +
        "</span>";
      var toggles = document.createElement("span");
      ["gameplay", "technical"].forEach(function (flag) {
        var b = document.createElement("button");
        b.type = "button";
        b.textContent = flag + (m[flag] ? " ✓" : "");
        b.onclick = async function () {
          var body = {};
          body[flag] = !m[flag];
          await api(
            "/api/payroll/companies/" +
              encodeURIComponent(state.companyId) +
              "/members/" +
              encodeURIComponent(m.id),
            { method: "PATCH", body: body }
          );
          await loadDetail();
        };
        toggles.appendChild(b);
      });
      li.appendChild(toggles);
      ul.appendChild(li);
    });
  }

  function renderRetainers(retainers) {
    var ul = $("pay-retainers");
    ul.innerHTML = "";
    (retainers || []).forEach(function (r) {
      var li = document.createElement("li");
      var human =
        r.mode === "fixed_cron" && window.PayrollCronHumanize
          ? PayrollCronHumanize.describeRetainerSchedule(r.cronExpr, r.amountUsd)
          : r.mode === "percent"
            ? (Number(r.percent || 0) * 100).toFixed(1) + "% of income"
            : "";
      var info = document.createElement("span");
      info.innerHTML =
        "<strong>" +
        r.name +
        "</strong> [" +
        r.kind +
        "] " +
        r.mode +
        (r.autoTrack ? " · auto:" + r.autoTrack : "") +
        (r.amountLocked ? ' · <em>amount locked</em>' : "") +
        "<br/><small>" +
        human +
        (r.amountUsd != null && r.mode !== "percent"
          ? " · $" + Number(r.amountUsd).toLocaleString(undefined, { maximumFractionDigits: 2 })
          : "") +
        "</small>";
      li.appendChild(info);

      var edit = document.createElement("span");
      edit.className = "pay-retainer-edit";
      edit.setAttribute("data-retainer-edit", r.id);
      edit.hidden = !isAdmin();
      if (r.mode === "percent") {
        var pctIn = document.createElement("input");
        pctIn.type = "number";
        pctIn.step = "0.01";
        pctIn.min = "0";
        pctIn.max = "1";
        pctIn.value = r.percent != null ? r.percent : "";
        pctIn.title = "Retainer percent (0–1)";
        var savePct = document.createElement("button");
        savePct.type = "button";
        savePct.textContent = "Save %";
        savePct.onclick = function () {
          var v = pctIn.value === "" ? NaN : Number(pctIn.value);
          if (Number.isNaN(v)) {
            alert("Enter a percent");
            return;
          }
          saveRetainerAmount(r.id, { percent: v }).catch(alert);
        };
        edit.appendChild(pctIn);
        edit.appendChild(savePct);
      } else {
        var amtIn = document.createElement("input");
        amtIn.type = "number";
        amtIn.step = "0.01";
        amtIn.min = "0";
        amtIn.value = r.amountUsd != null ? r.amountUsd : "";
        amtIn.title = "Retainer amount USD";
        var saveAmt = document.createElement("button");
        saveAmt.type = "button";
        saveAmt.textContent = "Save $";
        saveAmt.onclick = function () {
          var v = amtIn.value === "" ? NaN : Number(amtIn.value);
          if (Number.isNaN(v)) {
            alert("Enter an amount");
            return;
          }
          saveRetainerAmount(r.id, { amountUsd: v, amountLocked: true }).catch(alert);
        };
        edit.appendChild(amtIn);
        edit.appendChild(saveAmt);
        if (
          r.amountLocked &&
          (r.kind === "service_unity" || r.kind === "service_cursor")
        ) {
          var unlock = document.createElement("button");
          unlock.type = "button";
          unlock.textContent = "Use seat formula";
          unlock.title = "Clear admin lock and recalculate from seats / band";
          unlock.onclick = function () {
            saveRetainerAmount(r.id, { amountLocked: false }).catch(alert);
          };
          edit.appendChild(unlock);
        }
      }
      li.appendChild(edit);
      ul.appendChild(li);
    });
  }

  async function loadDetail() {
    if (!state.companyId) return;
    var s = await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/summary"
    );
    $("pay-detail").hidden = false;
    $("pay-detail-name").textContent = s.name;
    $("pay-phase").textContent =
      "Phase: " +
      s.phase +
      " · lifetime " +
      money(s.lifetimeNetUsd) +
      " / HWM " +
      money(s.highWaterMarkUsd) +
      " · retainer " +
      (s.hwmRetainerPct * 100).toFixed(1) +
      "%";
    var pct = Math.max(0, Math.min(100, (s.hwmProgress || 0) * 100));
    $("pay-gauge-fill").style.width = pct + "%";
    $("pay-gauge-label").textContent =
      pct.toFixed(2) + "% to HWM · remaining " + money(s.hwmRemainingUsd);
    $("pay-balances").textContent =
      "Ops " + money(s.opsUsd) + " · Retainer " + money(s.retainerUsd);
    $("pay-budget").textContent = JSON.stringify(s.serviceBudget || {}, null, 2);
    var budget = s.serviceBudget || {};
    var entBanner = $("pay-unity-enterprise-banner");
    var entLabel = $("pay-unity-enterprise-label");
    if (entBanner && entLabel) {
      if (budget.unityEnterprise && budget.unityEnterpriseContactLabel) {
        entBanner.hidden = false;
        entLabel.textContent = budget.unityEnterpriseContactLabel;
      } else {
        entBanner.hidden = true;
        entLabel.textContent = "";
      }
    }
    state.summary = s;
    var form = $("pay-hwm-pct");
    form.highWaterMarkUsd.value = s.highWaterMarkUsd;
    form.hwmRetainerPct.value = s.hwmRetainerPct;
    form.unityEnterpriseOverrideUsd.value =
      s.unityEnterpriseOverrideUsd != null ? s.unityEnterpriseOverrideUsd : "";
    syncAdminUi();
    renderMembers(s.members);
    renderRetainers(s.retainers);
    var events = await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/events?limit=15"
    );
    $("pay-events").textContent = JSON.stringify(events.items || [], null, 2);
  }

  $("pay-create").addEventListener("submit", async function (e) {
    e.preventDefault();
    var fd = new FormData(e.target);
    var body = {
      name: fd.get("name"),
      highWaterMarkUsd: Number(fd.get("highWaterMarkUsd") || 100000),
      hwmRetainerPct: Number(fd.get("hwmRetainerPct") || 0.10),
    };
    var pid = fd.get("saurceProductId");
    if (pid) body.saurceProductId = pid;
    var c = await api("/api/payroll/companies", { method: "POST", body: body });
    e.target.reset();
    state.companyId = c.id;
    await loadCompanies();
    await loadDetail();
  });

  $("pay-hwm-pct").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (!state.companyId) return;
    if (!isAdmin()) {
      alert("Admin required to change HWM or retainer %");
      return;
    }
    var fd = new FormData(e.target);
    var body = {
      highWaterMarkUsd: Number(fd.get("highWaterMarkUsd")),
      hwmRetainerPct: Number(fd.get("hwmRetainerPct")),
    };
    var ov = fd.get("unityEnterpriseOverrideUsd");
    body.unityEnterpriseOverrideUsd = ov === "" ? null : Number(ov);
    await api("/api/payroll/companies/" + encodeURIComponent(state.companyId), {
      method: "PATCH",
      body: body,
    });
    await loadDetail();
  });

  $("pay-member").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (!state.companyId) return;
    var fd = new FormData(e.target);
    var role = fd.get("role");
    var body = {
      displayName: fd.get("displayName"),
      role: role,
      gameplay: fd.get("gameplay") === "on",
      technical: fd.get("technical") === "on",
    };
    // Soft defaults when toggles left unchecked: designer→gameplay, engineer→technical
    if (!body.gameplay && !body.technical) {
      if (role === "designer") body.gameplay = true;
      if (role === "engineer") body.technical = true;
    }
    await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/members",
      { method: "POST", body: body }
    );
    e.target.reset();
    await loadDetail();
  });

  $("pay-retainer").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (!state.companyId) return;
    var fd = new FormData(e.target);
    var mode = fd.get("mode");
    var body = {
      name: fd.get("name"),
      mode: mode,
      kind: "custom",
      cronExpr: fd.get("cronExpr"),
      autoTrack: fd.get("autoTrack") || null,
      forwardCompanyId: fd.get("forwardCompanyId") || null,
    };
    if (mode === "percent") body.percent = Number(fd.get("percent") || 0);
    else body.amountUsd = Number(fd.get("amountUsd") || 0);
    await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/retainers",
      { method: "POST", body: body }
    );
    e.target.reset();
    $("pay-ret-cron").value = "0 0 1 * *";
    updateCronHuman();
    await loadDetail();
  });

  $("pay-income").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (!state.companyId) return;
    var fd = new FormData(e.target);
    var body = { netUsd: Number(fd.get("netUsd")), source: "manual" };
    var note = String(fd.get("postNote") || "").trim();
    if (note) body.postNote = note;
    await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/income",
      { method: "POST", body: body }
    );
    e.target.reset();
    await loadDetail();
    await loadCompanies();
  });

  $("pay-draw").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (!state.companyId) return;
    var fd = new FormData(e.target);
    await api(
      "/api/payroll/companies/" + encodeURIComponent(state.companyId) + "/retainer/draw",
      {
        method: "POST",
        body: {
          amountUsd: Number(fd.get("amountUsd")),
          reason: fd.get("reason") || null,
        },
      }
    );
    e.target.reset();
    await loadDetail();
  });

  $("pay-ret-mode").addEventListener("change", function () {
    var pct = $("pay-ret-mode").value === "percent";
    $("pay-ret-pct").disabled = !pct;
    $("pay-ret-amount").disabled = pct;
    updateCronHuman();
  });
  ["pay-ret-cron", "pay-ret-amount"].forEach(function (id) {
    $(id).addEventListener("input", updateCronHuman);
  });

  $("pay-refresh").addEventListener("click", function () {
    loadCompanies()
      .then(function () {
        return state.companyId ? loadDetail() : null;
      })
      .catch(alert);
  });

  if (Session && Session.onChange) {
    Session.onChange(function () {
      syncAdminUi();
      if (state.companyId) loadDetail().catch(alert);
    });
  }

  mountCronExamples();
  mountCronLens();
  updateCronHuman();
  syncAdminUi();
  loadCompanies().catch(alert);
})();
