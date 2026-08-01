(() => {
  const apiBase = () => localStorage.lemmaApiBase || location.origin;
  const cityEl = () => document.getElementById("stn-city");

  let meta = { kinds: ["cooking", "train", "bus", "computer", "generic"], assignTypes: ["building", "vehicle", "persona"] };
  let stations = [];
  let modalCtx = null;

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: "#continuuuum-nav-root", app: "stations", theme: "light" });
  }

  async function api(path, opts) {
    const r = await fetch(`${apiBase()}${path}`, {
      headers: { "Content-Type": "application/json", ...(opts && opts.headers) },
      ...opts,
    });
    if (!r.ok) {
      let detail = `${r.status} ${path}`;
      try {
        const body = await r.json();
        if (body && body.error) detail = body.error;
      } catch (_) {}
      throw new Error(detail);
    }
    return r.json();
  }

  function esc(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/"/g, "&quot;");
  }

  function cityId() {
    return (cityEl().value || "demo-city").trim() || "demo-city";
  }

  function showMsg(text, isErr) {
    const el = document.getElementById("stn-msg");
    if (!el) return;
    if (!text) {
      el.hidden = true;
      el.textContent = "";
      return;
    }
    el.hidden = false;
    el.classList.toggle("err", !!isErr);
    el.textContent = text;
  }

  function showTab(name) {
    document.querySelectorAll(".stn-tabs button").forEach((b) => {
      b.classList.toggle("active", b.dataset.tab === name);
    });
    document.querySelectorAll(".stn-panel").forEach((p) => {
      p.classList.toggle("active", p.id === `tab-${name}`);
    });
    if (name === "treemap") loadTreemap();
    if (name === "graph") loadGraph();
    if (name === "placards") loadPlacards();
    if (name === "commodities") loadCommodities();
    if (name === "assignments") loadAssignments();
  }

  function parseConfig(raw) {
    if (raw && typeof raw === "object") return raw;
    if (typeof raw === "string") {
      const t = raw.trim();
      if (!t) return {};
      try {
        return JSON.parse(t);
      } catch (_) {
        return {};
      }
    }
    return {};
  }

  function toPutStation(s) {
    return {
      stableId: s.stable_id || s.stableId,
      name: s.name,
      kind: s.kind,
      buildingStableId: s.building_stable_id || s.buildingStableId || null,
      vehicleId: s.vehicle_id || s.vehicleId || null,
      parentStationId: s.parent_station_id || s.parentStationId || null,
      levelId: s.level_id || s.levelId || "",
      staffingWeight: Number(s.staffing_weight ?? s.staffingWeight ?? 1),
      config: parseConfig(s.config_json ?? s.config),
      commodities: (s.commodities || []).map((c) => ({
        commodityKey: c.commodity_key || c.commodityKey,
        cronExpr: c.cron_expr || c.cronExpr || "",
        oneShotAt: c.one_shot_at || c.oneShotAt || null,
        surgeMult: Number(c.surge_mult ?? c.surgeMult ?? 1),
        quantity: Number(c.quantity ?? 1),
        price: Number(c.price ?? 0),
        availability: c.availability === undefined ? true : !!c.availability,
      })),
      assignments: (s.assignments || []).map((a) => ({
        assignType: a.assign_type || a.assignType || "persona",
        refId: a.ref_id || a.refId || "",
        role: a.role || "",
        peckingOrder: Number(a.pecking_order ?? a.peckingOrder ?? 100),
      })),
    };
  }

  async function putStations(next) {
    const data = await api(`/api/stations?cityId=${encodeURIComponent(cityId())}`, {
      method: "PUT",
      body: JSON.stringify({ cityId: cityId(), stations: next.map(toPutStation) }),
    });
    stations = data.stations || [];
    return stations;
  }

  async function fetchStations() {
    const data = await api(`/api/stations?cityId=${encodeURIComponent(cityId())}`);
    stations = data.stations || [];
    if (data.kinds && data.kinds.length) meta.kinds = data.kinds;
    return stations;
  }

  function field(label, html) {
    return `<label>${esc(label)}${html}</label>`;
  }

  function openModal(title, sub, fieldsHtml, ctx) {
    modalCtx = ctx;
    document.getElementById("stn-modal-title").textContent = title;
    document.getElementById("stn-modal-sub").textContent = sub || "";
    document.getElementById("stn-modal-form").innerHTML = fieldsHtml;
    document.getElementById("stn-modal").hidden = false;
  }

  function closeModal() {
    modalCtx = null;
    document.getElementById("stn-modal").hidden = true;
  }

  function formData() {
    const form = document.getElementById("stn-modal-form");
    const fd = new FormData(form);
    const out = {};
    for (const [k, v] of fd.entries()) out[k] = v;
    const avail = form.querySelector('[name="availability"]');
    if (avail) out.availability = avail.checked;
    return out;
  }

  function stationOptions(selectedStableId) {
    return stations
      .map(
        (s, i) =>
          `<option value="${i}" ${
            (selectedStableId != null && s.stable_id === selectedStableId) ||
            (typeof selectedStableId === "number" && selectedStableId === i)
              ? "selected"
              : ""
          }>${esc(s.name)} (${esc(s.stable_id)})</option>`
      )
      .join("");
  }

  function kindOptions(selected) {
    return (meta.kinds || [])
      .map((k) => `<option value="${esc(k)}" ${k === selected ? "selected" : ""}>${esc(k)}</option>`)
      .join("");
  }

  function assignTypeOptions(selected) {
    return (meta.assignTypes || [])
      .map((t) => `<option value="${esc(t)}" ${t === selected ? "selected" : ""}>${esc(t)}</option>`)
      .join("");
  }

  async function loadTreemap() {
    if (typeof d3 === "undefined") return;
    const data = await api(`/api/stations/treemap?cityId=${encodeURIComponent(cityId())}`);
    const rootData = data.treemap;
    const svg = d3.select("#stn-treemap");
    svg.selectAll("*").remove();
    const width = svg.node().clientWidth || 900;
    const height = 480;
    svg.attr("viewBox", `0 0 ${width} ${height}`);
    const root = d3
      .hierarchy(rootData)
      .sum((d) => d.value || 0)
      .sort((a, b) => (b.value || 0) - (a.value || 0));
    d3.treemap().size([width, height]).padding(2)(root);
    const colors = d3.scaleOrdinal(d3.schemeTableau10);
    const leaf = svg
      .selectAll("g")
      .data(root.leaves())
      .join("g")
      .attr("transform", (d) => `translate(${d.x0},${d.y0})`);
    leaf
      .append("rect")
      .attr("class", "stn-tm-cell")
      .attr("width", (d) => Math.max(0, d.x1 - d.x0))
      .attr("height", (d) => Math.max(0, d.y1 - d.y0))
      .attr("fill", (d) => colors(d.data.kind || d.parent?.data?.name || "x"));
    leaf
      .append("text")
      .attr("class", "stn-tm-label")
      .attr("x", 4)
      .attr("y", 14)
      .text((d) => d.data.name || "");
  }

  async function loadGraph() {
    if (typeof d3 === "undefined") return;
    const data = await api(`/api/stations/assemblage?cityId=${encodeURIComponent(cityId())}`);
    const svg = d3.select("#stn-graph");
    svg.selectAll("*").remove();
    const width = svg.node().clientWidth || 900;
    const height = 480;
    svg.attr("viewBox", `0 0 ${width} ${height}`).style("cursor", "grab");
    const root = svg.append("g");
    const zoom = d3
      .zoom()
      .scaleExtent([0.25, 4])
      .on("start", () => svg.style("cursor", "grabbing"))
      .on("end", () => svg.style("cursor", "grab"))
      .on("zoom", (event) => root.attr("transform", event.transform));
    svg.call(zoom);
    svg.on("dblclick.zoom", null);
    const sim = d3
      .forceSimulation(data.nodes)
      .force("link", d3.forceLink(data.links).id((d) => d.id).distance(80))
      .force("charge", d3.forceManyBody().strength(-160))
      .force("center", d3.forceCenter(width / 2, height / 2));
    const link = root.append("g").selectAll("line").data(data.links).join("line").attr("class", "stn-link");
    const node = root
      .append("g")
      .selectAll("g")
      .data(data.nodes)
      .join("g")
      .call(
        d3
          .drag()
          .on("start", (event, d) => {
            if (!event.active) sim.alphaTarget(0.3).restart();
            d.fx = d.x;
            d.fy = d.y;
          })
          .on("drag", (event, d) => {
            d.fx = event.x;
            d.fy = event.y;
          })
          .on("end", (event, d) => {
            if (!event.active) sim.alphaTarget(0);
            d.fx = null;
            d.fy = null;
          })
      );
    node
      .append("circle")
      .attr("r", 7)
      .attr("class", (d) =>
        d.nodeType === "station"
          ? "stn-node-station"
          : d.nodeType === "building"
            ? "stn-node-building"
            : d.nodeType === "vehicle"
              ? "stn-node-vehicle"
              : "stn-node-city"
      );
    node
      .append("text")
      .text((d) => d.label)
      .attr("x", 10)
      .attr("y", 4)
      .attr("font-size", 11);
    sim.on("tick", () => {
      link
        .attr("x1", (d) => d.source.x)
        .attr("y1", (d) => d.source.y)
        .attr("x2", (d) => d.target.x)
        .attr("y2", (d) => d.target.y);
      node.attr("transform", (d) => `translate(${d.x},${d.y})`);
    });
  }

  async function loadPlacards() {
    const panel = document.getElementById("tab-placards");
    await fetchStations();
    const rows = stations
      .map(
        (s, i) =>
          `<tr>
            <td>${esc(s.name)}</td><td>${esc(s.kind)}</td><td>${esc(s.stable_id)}</td>
            <td>${esc(s.building_stable_id || "")}</td><td>${esc(s.vehicle_id || "")}</td>
            <td>${esc(s.staffing_weight)}</td>
            <td>${(s.commodities || []).length}</td>
            <td>${(s.assignments || []).length}</td>
            <td><button type="button" data-edit-stn="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `
      <div class="stn-actions">
        <button type="button" id="btn-add-stn">Add placard</button>
      </div>
      <table class="stn-table">
        <thead><tr>
          <th>Name</th><th>Kind</th><th>Stable id</th><th>Building</th><th>Vehicle</th>
          <th>Weight</th><th>Commodities</th><th>Assignments</th><th></th>
        </tr></thead>
        <tbody>${rows || "<tr><td colspan=9>No stations</td></tr>"}</tbody>
      </table>`;
    document.getElementById("btn-add-stn").onclick = () => openPlacardModal("new");
    panel.querySelectorAll("[data-edit-stn]").forEach((btn) => {
      btn.onclick = () => openPlacardModal(Number(btn.dataset.editStn));
    });
  }

  function flattenCommodities() {
    const rows = [];
    stations.forEach((s, si) => {
      (s.commodities || []).forEach((c, ci) => {
        rows.push({ stationIndex: si, commodityIndex: ci, station: s, commodity: c });
      });
    });
    return rows;
  }

  function flattenAssignments() {
    const rows = [];
    stations.forEach((s, si) => {
      (s.assignments || []).forEach((a, ai) => {
        rows.push({ stationIndex: si, assignmentIndex: ai, station: s, assignment: a });
      });
    });
    return rows;
  }

  async function loadCommodities() {
    const panel = document.getElementById("tab-commodities");
    await fetchStations();
    const flat = flattenCommodities();
    const rows = flat
      .map(
        (r, i) =>
          `<tr>
            <td>${esc(r.station.name)}</td>
            <td>${esc(r.commodity.commodity_key)}</td>
            <td>${esc(r.commodity.cron_expr || "")}</td>
            <td>${esc(r.commodity.surge_mult)}</td>
            <td>${esc(r.commodity.quantity)}</td>
            <td>${esc(r.commodity.price)}</td>
            <td>${r.commodity.availability ? "yes" : "no"}</td>
            <td><button type="button" data-edit-cmd="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `
      <div class="stn-actions">
        <button type="button" id="btn-add-cmd">Add commodity</button>
      </div>
      <table class="stn-table">
        <thead><tr>
          <th>Station</th><th>Key</th><th>Cron</th><th>Surge</th><th>Qty</th><th>Price</th><th>Avail</th><th></th>
        </tr></thead>
        <tbody>${rows || "<tr><td colspan=8>No commodities</td></tr>"}</tbody>
      </table>`;
    document.getElementById("btn-add-cmd").onclick = () => openCommodityModal("new");
    panel.querySelectorAll("[data-edit-cmd]").forEach((btn) => {
      btn.onclick = () => openCommodityModal(Number(btn.dataset.editCmd));
    });
  }

  async function loadAssignments() {
    const panel = document.getElementById("tab-assignments");
    await fetchStations();
    const flat = flattenAssignments();
    const rows = flat
      .map(
        (r, i) =>
          `<tr>
            <td>${esc(r.station.name)}</td>
            <td>${esc(r.assignment.assign_type)}</td>
            <td>${esc(r.assignment.ref_id)}</td>
            <td>${esc(r.assignment.role)}</td>
            <td>${esc(r.assignment.pecking_order)}</td>
            <td><button type="button" data-edit-asg="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `
      <div class="stn-actions">
        <button type="button" id="btn-add-asg">Add assignment</button>
      </div>
      <table class="stn-table">
        <thead><tr>
          <th>Station</th><th>Type</th><th>Ref</th><th>Role</th><th>Pecking</th><th></th>
        </tr></thead>
        <tbody>${rows || "<tr><td colspan=6>No assignments</td></tr>"}</tbody>
      </table>`;
    document.getElementById("btn-add-asg").onclick = () => openAssignmentModal("new");
    panel.querySelectorAll("[data-edit-asg]").forEach((btn) => {
      btn.onclick = () => openAssignmentModal(Number(btn.dataset.editAsg));
    });
  }

  function openPlacardModal(index) {
    const isNew = index === "new";
    const s = isNew
      ? {
          name: "",
          kind: "generic",
          stable_id: "",
          building_stable_id: "",
          vehicle_id: "",
          parent_station_id: "",
          level_id: "demo-level",
          staffing_weight: 1,
          config_json: "{}",
        }
      : stations[index] || {};
    const cfg =
      typeof s.config_json === "string"
        ? s.config_json
        : JSON.stringify(parseConfig(s.config_json || s.config), null, 0);
    openModal(
      isNew ? "Add placard" : "Edit placard",
      "Save replaces all placards for this city (keeps sibling commodities/assignments).",
      [
        field("Name", `<input name="name" type="text" required value="${esc(s.name || "")}" />`),
        field("Stable id", `<input name="stableId" type="text" value="${esc(s.stable_id || "")}" ${isNew ? "" : "readonly"} />`),
        field("Kind", `<select name="kind">${kindOptions(s.kind || "generic")}</select>`),
        field("Building stable id", `<input name="buildingStableId" type="text" value="${esc(s.building_stable_id || "")}" />`),
        field("Vehicle id", `<input name="vehicleId" type="text" value="${esc(s.vehicle_id || "")}" />`),
        field("Parent station id", `<input name="parentStationId" type="text" value="${esc(s.parent_station_id || "")}" />`),
        field("Level id", `<input name="levelId" type="text" value="${esc(s.level_id || "")}" />`),
        field("Staffing weight", `<input name="staffingWeight" type="number" step="0.1" value="${esc(s.staffing_weight ?? 1)}" />`),
        field("Config JSON", `<textarea name="configJson">${esc(cfg || "{}")}</textarea>`),
      ].join(""),
      { kind: "placard", index }
    );
  }

  function openCommodityModal(index) {
    const isNew = index === "new";
    const flat = flattenCommodities();
    const row = isNew
      ? {
          stationIndex: 0,
          commodity: {
            commodity_key: "",
            cron_expr: "0 */6 * * *",
            surge_mult: 1,
            quantity: 1,
            price: 0,
            availability: 1,
          },
        }
      : flat[index];
    if (!row) return;
    const c = row.commodity;
    const stationSelect =
      stations.length === 0
        ? `<p class="stn-modal-sub">Add a placard first.</p>`
        : field(
            "Station",
            `<select name="stationIndex">${stationOptions(
              isNew ? 0 : row.stationIndex
            )}</select>`
          );
    openModal(
      isNew ? "Add commodity" : "Edit commodity",
      "Save writes the full station list for this city.",
      [
        stationSelect,
        field("Commodity key", `<input name="commodityKey" type="text" required value="${esc(c.commodity_key || "")}" />`),
        field("Cron", `<input name="cronExpr" type="text" value="${esc(c.cron_expr || "")}" />`),
        field("Surge mult", `<input name="surgeMult" type="number" step="0.01" value="${esc(c.surge_mult ?? 1)}" />`),
        field("Quantity", `<input name="quantity" type="number" step="0.01" value="${esc(c.quantity ?? 1)}" />`),
        field("Price", `<input name="price" type="number" step="0.01" value="${esc(c.price ?? 0)}" />`),
        `<label class="stn-check"><input name="availability" type="checkbox" ${c.availability ? "checked" : ""} /> Available</label>`,
      ].join(""),
      {
        kind: "commodity",
        index,
        stationIndex: row.stationIndex,
        commodityIndex: isNew ? "new" : row.commodityIndex,
      }
    );
  }

  function openAssignmentModal(index) {
    const isNew = index === "new";
    const flat = flattenAssignments();
    const row = isNew
      ? {
          stationIndex: 0,
          assignment: { assign_type: "persona", ref_id: "", role: "", pecking_order: 50 },
        }
      : flat[index];
    if (!row) return;
    const a = row.assignment;
    const stationSelect =
      stations.length === 0
        ? `<p class="stn-modal-sub">Add a placard first.</p>`
        : field(
            "Station",
            `<select name="stationIndex">${stationOptions(
              isNew ? 0 : row.stationIndex
            )}</select>`
          );
    openModal(
      isNew ? "Add assignment" : "Edit assignment",
      "Save writes the full station list for this city.",
      [
        stationSelect,
        field("Assign type", `<select name="assignType">${assignTypeOptions(a.assign_type || "persona")}</select>`),
        field("Ref id", `<input name="refId" type="text" required value="${esc(a.ref_id || "")}" />`),
        field("Role", `<input name="role" type="text" value="${esc(a.role || "")}" />`),
        field("Pecking order", `<input name="peckingOrder" type="number" value="${esc(a.pecking_order ?? 50)}" />`),
      ].join(""),
      {
        kind: "assignment",
        index,
        stationIndex: row.stationIndex,
        assignmentIndex: isNew ? "new" : row.assignmentIndex,
      }
    );
  }

  async function saveModal() {
    if (!modalCtx) return;
    const f = formData();
    try {
      if (modalCtx.kind === "placard") {
        let cfg = {};
        try {
          cfg = parseConfig(f.configJson || "{}");
        } catch (_) {
          cfg = {};
        }
        const row = {
          name: f.name || "Station",
          kind: f.kind || "generic",
          stable_id: f.stableId || `stn-${(f.kind || "generic").toLowerCase()}-${Date.now().toString(36)}`,
          building_stable_id: f.buildingStableId || null,
          vehicle_id: f.vehicleId || null,
          parent_station_id: f.parentStationId || null,
          level_id: f.levelId || "",
          staffing_weight: Number(f.staffingWeight || 1),
          config_json: JSON.stringify(cfg),
          commodities: [],
          assignments: [],
        };
        const next = stations.map((s) => ({ ...s }));
        if (modalCtx.index === "new") {
          next.push(row);
        } else {
          const prev = next[modalCtx.index] || {};
          next[modalCtx.index] = {
            ...prev,
            ...row,
            commodities: prev.commodities || [],
            assignments: prev.assignments || [],
          };
        }
        await putStations(next);
        closeModal();
        await loadPlacards();
        showMsg("Placard saved.");
      } else if (modalCtx.kind === "commodity") {
        if (!stations.length) {
          showMsg("Add a placard first.", true);
          return;
        }
        const next = stations.map((s) => ({
          ...s,
          commodities: (s.commodities || []).map((c) => ({ ...c })),
          assignments: (s.assignments || []).map((a) => ({ ...a })),
        }));
        const destSi = Number(f.stationIndex ?? modalCtx.stationIndex ?? 0);
        const cmd = {
          commodity_key: f.commodityKey || "labor",
          cron_expr: f.cronExpr || "",
          surge_mult: Number(f.surgeMult || 1),
          quantity: Number(f.quantity || 0),
          price: Number(f.price || 0),
          availability: !!f.availability,
        };
        if (modalCtx.commodityIndex === "new") {
          if (!next[destSi]) throw new Error("Invalid station");
          next[destSi].commodities.push(cmd);
        } else {
          const srcSi = modalCtx.stationIndex;
          const ci = modalCtx.commodityIndex;
          if (srcSi === destSi) {
            next[srcSi].commodities[ci] = cmd;
          } else {
            next[srcSi].commodities.splice(ci, 1);
            next[destSi].commodities.push(cmd);
          }
        }
        await putStations(next);
        closeModal();
        await loadCommodities();
        showMsg("Commodity saved.");
      } else if (modalCtx.kind === "assignment") {
        if (!stations.length) {
          showMsg("Add a placard first.", true);
          return;
        }
        const next = stations.map((s) => ({
          ...s,
          commodities: (s.commodities || []).map((c) => ({ ...c })),
          assignments: (s.assignments || []).map((a) => ({ ...a })),
        }));
        const destSi = Number(f.stationIndex ?? modalCtx.stationIndex ?? 0);
        const asg = {
          assign_type: f.assignType || "persona",
          ref_id: f.refId || "",
          role: f.role || "",
          pecking_order: Number(f.peckingOrder || 100),
        };
        if (modalCtx.assignmentIndex === "new") {
          if (!next[destSi]) throw new Error("Invalid station");
          next[destSi].assignments.push(asg);
        } else {
          const srcSi = modalCtx.stationIndex;
          const ai = modalCtx.assignmentIndex;
          if (srcSi === destSi) {
            next[srcSi].assignments[ai] = asg;
          } else {
            next[srcSi].assignments.splice(ai, 1);
            next[destSi].assignments.push(asg);
          }
        }
        await putStations(next);
        closeModal();
        await loadAssignments();
        showMsg("Assignment saved.");
      }
    } catch (e) {
      showMsg(String(e.message || e), true);
    }
  }

  document.getElementById("stn-tabs").onclick = (e) => {
    const b = e.target.closest("button[data-tab]");
    if (!b) return;
    showTab(b.dataset.tab);
  };
  cityEl().onchange = () => {
    showMsg("");
    showTab(document.querySelector(".stn-tabs button.active")?.dataset.tab || "treemap");
  };
  document.getElementById("stn-modal-cancel").onclick = closeModal;
  document.getElementById("stn-modal-save").onclick = () => saveModal();
  document.getElementById("stn-modal").addEventListener("click", (e) => {
    if (e.target.id === "stn-modal") closeModal();
  });

  (async () => {
    try {
      meta = await api("/api/stations/meta");
    } catch (_) {}
    showTab("treemap");
  })();
})();
