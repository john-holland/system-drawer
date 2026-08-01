(() => {
  const apiBase = () => localStorage.lemmaApiBase || location.origin;
  let restaurantId = null;
  let meta = { orderStatuses: [] };
  let menuItems = [];
  let orders = [];
  let commoditySchedules = [];
  let retinueMembers = [];
  let modalCtx = null; // { kind, index | 'new', id? }

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: "#continuuuum-nav-root", app: "restaurants", theme: "light" });
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

  function showMsg(text, isErr) {
    const el = document.getElementById("rest-msg");
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
    document.querySelectorAll(".rest-tabs button").forEach((b) => {
      b.classList.toggle("active", b.dataset.tab === name);
    });
    document.querySelectorAll(".rest-panel").forEach((p) => {
      p.classList.toggle("active", p.id === `tab-${name}`);
    });
    if (name === "chef") loadChefGraph();
    if (name === "retinue") loadRetinue();
  }

  function parseHints(raw) {
    if (Array.isArray(raw)) return raw;
    if (typeof raw === "string") {
      const t = raw.trim();
      if (!t) return [];
      try {
        const j = JSON.parse(t);
        if (Array.isArray(j)) return j.map(String);
      } catch (_) {}
      return t.split(/[,;]/).map((x) => x.trim()).filter(Boolean);
    }
    return [];
  }

  function hintsDisplay(item) {
    const h = parseHints(item.chef_card_hints || item.chef_card_hints_json || item.chefCardHints);
    return h.join(", ");
  }

  async function loadRestaurants() {
    const data = await api("/api/restaurant/list");
    const sel = document.getElementById("rest-select");
    sel.innerHTML = "";
    (data.restaurants || []).forEach((r) => {
      const o = document.createElement("option");
      o.value = r.id;
      o.textContent = `${r.name} (#${r.id})`;
      sel.appendChild(o);
    });
    restaurantId = sel.value ? Number(sel.value) : null;
    sel.onchange = () => {
      restaurantId = Number(sel.value);
      refreshAll();
    };
  }

  async function loadMenu() {
    const panel = document.getElementById("tab-menu");
    if (!restaurantId) {
      panel.innerHTML = "<p>No restaurant.</p>";
      return;
    }
    const data = await api(`/api/restaurant/${restaurantId}/menu`);
    menuItems = data.menu || [];
    const rows = menuItems
      .map(
        (m, i) =>
          `<tr>
            <td>${esc(m.name)}</td><td>${esc(m.category)}</td><td>${esc(m.price)}</td>
            <td>${m.available ? "yes" : "no"}</td><td>${esc(hintsDisplay(m))}</td>
            <td><button type="button" data-edit-menu="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `
      <div class="rest-actions">
        <button type="button" id="btn-add-menu">Add menu item</button>
        <button type="button" id="btn-seed-order">Create demo order</button>
      </div>
      <table class="rest-table"><thead><tr>
        <th>Name</th><th>Category</th><th>Price</th><th>Avail</th><th>Chef hints</th><th></th>
      </tr></thead>
      <tbody>${rows || "<tr><td colspan=6>Empty menu</td></tr>"}</tbody></table>`;
    document.getElementById("btn-add-menu").onclick = () => openMenuModal("new");
    panel.querySelectorAll("[data-edit-menu]").forEach((btn) => {
      btn.onclick = () => openMenuModal(Number(btn.dataset.editMenu));
    });
    document.getElementById("btn-seed-order").onclick = async () => {
      const first = menuItems[0];
      await api(`/api/restaurant/${restaurantId}/orders`, {
        method: "POST",
        body: JSON.stringify({
          ticketLabel: "demo",
          lines: first
            ? [{ menuItemId: first.id, name: first.name, qty: 1 }]
            : [{ name: "open item", qty: 1 }],
        }),
      });
      showTab("orders");
      await loadOrders();
      showMsg("Demo order created.");
    };
  }

  async function loadOrders() {
    const panel = document.getElementById("tab-orders");
    if (!restaurantId) return;
    const data = await api(`/api/restaurant/${restaurantId}/orders`);
    orders = data.orders || [];
    const rows = orders
      .map((o, i) => {
        const lines = (o.lines || []).map((l) => l.name).join(", ");
        return `<tr>
          <td>#${esc(o.id)}</td><td>${esc(o.ticket_label || "")}</td>
          <td>${esc(o.status)}</td><td>${esc(lines)}</td>
          <td><button type="button" data-edit-order="${i}">Edit</button></td>
        </tr>`;
      })
      .join("");
    panel.innerHTML = `<div class="rest-actions">
        <button type="button" id="btn-add-order">New order</button>
      </div>
      <table class="rest-table"><thead><tr>
        <th>Id</th><th>Ticket</th><th>Status</th><th>Lines</th><th></th>
      </tr></thead>
      <tbody>${rows || "<tr><td colspan=5>No orders</td></tr>"}</tbody></table>`;
    document.getElementById("btn-add-order").onclick = () => openOrderModal("new");
    panel.querySelectorAll("[data-edit-order]").forEach((btn) => {
      btn.onclick = () => openOrderModal(Number(btn.dataset.editOrder));
    });
  }

  async function loadCommodities() {
    const panel = document.getElementById("tab-commodities");
    if (!restaurantId) return;
    const data = await api(`/api/restaurant/${restaurantId}/commodities`);
    commoditySchedules = data.schedules || [];
    const rows = commoditySchedules
      .map(
        (s, i) =>
          `<tr>
            <td>${esc(s.commodity_key)}</td><td>${esc(s.cron_expr || "")}</td>
            <td>${esc(s.surge_mult)}</td><td>${esc(s.quantity)}</td><td>${esc(s.price)}</td>
            <td><button type="button" data-edit-cmd="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `<div class="rest-actions">
        <button type="button" id="btn-add-cmd">Add schedule</button>
      </div>
      <table class="rest-table"><thead><tr>
        <th>Key</th><th>Cron</th><th>Surge</th><th>Qty</th><th>Price</th><th></th>
      </tr></thead>
      <tbody>${rows || "<tr><td colspan=6>No schedules</td></tr>"}</tbody></table>`;
    document.getElementById("btn-add-cmd").onclick = () => openCommodityModal("new");
    panel.querySelectorAll("[data-edit-cmd]").forEach((btn) => {
      btn.onclick = () => openCommodityModal(Number(btn.dataset.editCmd));
    });
  }

  async function loadRetinue() {
    const panel = document.getElementById("tab-retinue");
    if (!restaurantId) return;
    const data = await api(`/api/restaurant/${restaurantId}/retinue`);
    retinueMembers = data.retinue || [];
    const rows = retinueMembers
      .map(
        (m, i) =>
          `<tr>
            <td>${esc(m.persona_key)}</td><td>${esc(m.role)}</td><td>${esc(m.pecking_order)}</td>
            <td>${esc(m.pay_rate)}</td><td>${esc(m.duty_cron || "")}</td><td>${esc(m.waypoint_group || "")}</td>
            <td><button type="button" data-edit-ret="${i}">Edit</button></td>
          </tr>`
      )
      .join("");
    panel.innerHTML = `<div class="rest-actions">
        <button type="button" id="btn-add-retinue">Add staff</button>
      </div>
      <table class="rest-table"><thead><tr>
        <th>Persona</th><th>Role</th><th>Pecking</th><th>Pay</th><th>Duty cron</th><th>Waypoint</th><th></th>
      </tr></thead>
      <tbody>${rows || "<tr><td colspan=7>No staff</td></tr>"}</tbody></table>
      <p class="rest-treemap-caption">Pecking treemap (derived managers + sibling tiles; no stored parent ids)</p>
      <svg id="retinue-treemap" width="100%" height="420"></svg>`;
    document.getElementById("btn-add-retinue").onclick = () => openRetinueModal("new");
    panel.querySelectorAll("[data-edit-ret]").forEach((btn) => {
      btn.onclick = () => openRetinueModal(Number(btn.dataset.editRet));
    });
    await renderRetinueTreemap();
  }

  function field(label, html) {
    return `<label>${esc(label)}${html}</label>`;
  }

  function openModal(title, sub, fieldsHtml, ctx) {
    modalCtx = ctx;
    document.getElementById("rest-modal-title").textContent = title;
    document.getElementById("rest-modal-sub").textContent = sub || "";
    document.getElementById("rest-modal-form").innerHTML = fieldsHtml;
    const backdrop = document.getElementById("rest-modal");
    backdrop.hidden = false;
  }

  function closeModal() {
    modalCtx = null;
    document.getElementById("rest-modal").hidden = true;
  }

  function openMenuModal(index) {
    const isNew = index === "new";
    const m = isNew
      ? { name: "", category: "entree", price: 0, available: 1, sku: "", chef_card_hints_json: "[]" }
      : menuItems[index] || {};
    openModal(
      isNew ? "Add menu item" : "Edit menu item",
      "Save writes the full menu via PUT.",
      [
        field("Name", `<input name="name" type="text" required value="${esc(m.name || "")}" />`),
        field("SKU", `<input name="sku" type="text" value="${esc(m.sku || "")}" />`),
        field("Category", `<input name="category" type="text" value="${esc(m.category || "entree")}" />`),
        field("Price", `<input name="price" type="number" step="0.01" value="${esc(m.price ?? 0)}" />`),
        field(
          "Chef hints (comma-separated)",
          `<input name="hints" type="text" value="${esc(hintsDisplay(m))}" />`
        ),
        `<label class="rest-check"><input name="available" type="checkbox" ${m.available ? "checked" : ""} /> Available</label>`,
      ].join(""),
      { kind: "menu", index }
    );
  }

  function openOrderModal(index) {
    const isNew = index === "new";
    const statuses = meta.orderStatuses || ["queued", "prep", "plating", "served", "cancelled"];
    const o = isNew
      ? { ticket_label: "", status: "queued", notes: "", lines: [] }
      : orders[index] || {};
    const opts = statuses
      .map((s) => `<option value="${esc(s)}" ${s === o.status ? "selected" : ""}>${esc(s)}</option>`)
      .join("");
    const lineHint = isNew
      ? field(
          "Line item name",
          `<input name="lineName" type="text" value="${esc(menuItems[0]?.name || "open item")}" />`
        )
      : `<p class="rest-modal-sub">Lines: ${esc((o.lines || []).map((l) => l.name).join(", ") || "(none)")}</p>`;
    openModal(
      isNew ? "New order" : `Edit order #${o.id}`,
      isNew ? "Creates a queued ticket." : "Updates status / ticket label.",
      [
        field("Ticket label", `<input name="ticketLabel" type="text" value="${esc(o.ticket_label || "")}" />`),
        isNew ? "" : field("Status", `<select name="status">${opts}</select>`),
        field("Notes", `<textarea name="notes">${esc(o.notes || "")}</textarea>`),
        lineHint,
      ].join(""),
      { kind: "order", index, id: o.id }
    );
  }

  function openCommodityModal(index) {
    const isNew = index === "new";
    const s = isNew
      ? { commodity_key: "", cron_expr: "0 * * * *", surge_mult: 1, quantity: 1, price: 0 }
      : commoditySchedules[index] || {};
    openModal(
      isNew ? "Add commodity schedule" : "Edit commodity schedule",
      "Save replaces all schedules for this restaurant.",
      [
        field("Commodity key", `<input name="commodityKey" type="text" required value="${esc(s.commodity_key || "")}" />`),
        field("Cron", `<input name="cronExpr" type="text" value="${esc(s.cron_expr || "")}" />`),
        field("Surge mult", `<input name="surgeMult" type="number" step="0.01" value="${esc(s.surge_mult ?? 1)}" />`),
        field("Quantity", `<input name="quantity" type="number" step="0.01" value="${esc(s.quantity ?? 1)}" />`),
        field("Price", `<input name="price" type="number" step="0.01" value="${esc(s.price ?? 0)}" />`),
      ].join(""),
      { kind: "commodity", index }
    );
  }

  function openRetinueModal(index) {
    const isNew = index === "new";
    const m = isNew
      ? {
          persona_key: "",
          role: "line-chef",
          pecking_order: 50,
          pay_rate: 15,
          duty_cron: "",
          waypoint_group: "",
        }
      : retinueMembers[index] || {};
    openModal(
      isNew ? "Add retinue member" : "Edit retinue member",
      "Save replaces the full retinue list.",
      [
        field("Persona key", `<input name="personaKey" type="text" required value="${esc(m.persona_key || "")}" />`),
        field("Role", `<input name="role" type="text" value="${esc(m.role || "")}" />`),
        field("Pecking order", `<input name="peckingOrder" type="number" value="${esc(m.pecking_order ?? 50)}" />`),
        field("Pay rate", `<input name="payRate" type="number" step="0.01" value="${esc(m.pay_rate ?? 0)}" />`),
        field("Duty cron", `<input name="dutyCron" type="text" value="${esc(m.duty_cron || "")}" />`),
        field("Waypoint group", `<input name="waypointGroup" type="text" value="${esc(m.waypoint_group || "")}" />`),
      ].join(""),
      { kind: "retinue", index }
    );
  }

  function formData() {
    const form = document.getElementById("rest-modal-form");
    const fd = new FormData(form);
    const out = {};
    for (const [k, v] of fd.entries()) out[k] = v;
    const avail = form.querySelector('[name="available"]');
    if (avail) out.available = avail.checked;
    return out;
  }

  async function saveModal() {
    if (!modalCtx || !restaurantId) return;
    const f = formData();
    try {
      if (modalCtx.kind === "menu") {
        const row = {
          sku: f.sku || undefined,
          name: f.name || "Item",
          category: f.category || "entree",
          price: Number(f.price || 0),
          available: !!f.available,
          chefCardHints: parseHints(f.hints),
        };
        const next = menuItems.map((m) => ({
          sku: m.sku,
          name: m.name,
          category: m.category,
          price: m.price,
          available: !!m.available,
          chefCardHints: parseHints(m.chef_card_hints_json || m.chef_card_hints),
          sortOrder: m.sort_order,
        }));
        if (modalCtx.index === "new") next.push(row);
        else next[modalCtx.index] = { ...next[modalCtx.index], ...row };
        await api(`/api/restaurant/${restaurantId}/menu`, {
          method: "PUT",
          body: JSON.stringify({ menu: next }),
        });
        closeModal();
        await loadMenu();
        showMsg("Menu saved.");
      } else if (modalCtx.kind === "order") {
        if (modalCtx.index === "new") {
          await api(`/api/restaurant/${restaurantId}/orders`, {
            method: "POST",
            body: JSON.stringify({
              ticketLabel: f.ticketLabel || "",
              notes: f.notes || "",
              lines: [{ name: f.lineName || "open item", qty: 1 }],
            }),
          });
        } else {
          await api(`/api/restaurant/${restaurantId}/orders/${modalCtx.id}/status`, {
            method: "PATCH",
            body: JSON.stringify({
              status: f.status,
              ticketLabel: f.ticketLabel || "",
              notes: f.notes || "",
            }),
          });
        }
        closeModal();
        await loadOrders();
        showMsg("Order saved.");
      } else if (modalCtx.kind === "commodity") {
        const row = {
          commodityKey: f.commodityKey || "item",
          cronExpr: f.cronExpr || "",
          surgeMult: Number(f.surgeMult || 1),
          quantity: Number(f.quantity || 0),
          price: Number(f.price || 0),
        };
        const next = commoditySchedules.map((s) => ({
          commodityKey: s.commodity_key,
          cronExpr: s.cron_expr,
          surgeMult: s.surge_mult,
          quantity: s.quantity,
          price: s.price,
        }));
        if (modalCtx.index === "new") next.push(row);
        else next[modalCtx.index] = row;
        await api(`/api/restaurant/${restaurantId}/commodities`, {
          method: "PUT",
          body: JSON.stringify({ schedules: next }),
        });
        closeModal();
        await loadCommodities();
        showMsg("Commodities saved.");
      } else if (modalCtx.kind === "retinue") {
        const row = {
          personaKey: f.personaKey || "staff",
          role: f.role || "line-chef",
          peckingOrder: Number(f.peckingOrder || 100),
          payRate: Number(f.payRate || 0),
          dutyCron: f.dutyCron || "",
          waypointGroup: f.waypointGroup || "",
        };
        const next = retinueMembers.map((m) => ({
          personaKey: m.persona_key,
          role: m.role,
          peckingOrder: m.pecking_order,
          payRate: m.pay_rate,
          dutyCron: m.duty_cron,
          waypointGroup: m.waypoint_group,
        }));
        if (modalCtx.index === "new") next.push(row);
        else next[modalCtx.index] = row;
        await api(`/api/restaurant/${restaurantId}/retinue`, {
          method: "PUT",
          body: JSON.stringify({ retinue: next }),
        });
        closeModal();
        await loadRetinue();
        showMsg("Retinue saved.");
      }
    } catch (e) {
      showMsg(String(e.message || e), true);
    }
  }

  async function renderRetinueTreemap() {
    if (!restaurantId || typeof d3 === "undefined") return;
    const svg = d3.select("#retinue-treemap");
    if (svg.empty()) return;
    const data = await api(`/api/restaurant/${restaurantId}/retinue/treemap`);
    const rootData = data.treemap || { name: "retinue", children: [] };
    svg.selectAll("*").remove();
    const width = svg.node().clientWidth || 900;
    const height = 420;
    svg.attr("viewBox", `0 0 ${width} ${height}`);
    const root = d3
      .hierarchy(rootData)
      .sum((d) => d.value || 0)
      .sort((a, b) => (b.value || 0) - (a.value || 0));
    d3.treemap().size([width, height]).paddingInner(2).paddingOuter(3)(root);
    const colors = {
      manager: "#2d4a3e",
      "manager-self": "#3d6b5a",
      staff: "#3a5a8c",
      unassigned: "#8a7a4a",
    };
    const leaf = svg
      .selectAll("g")
      .data(root.leaves())
      .join("g")
      .attr("transform", (d) => `translate(${d.x0},${d.y0})`);
    leaf
      .append("rect")
      .attr("class", "rest-tm-cell")
      .attr("width", (d) => Math.max(0, d.x1 - d.x0))
      .attr("height", (d) => Math.max(0, d.y1 - d.y0))
      .attr("fill", (d) => colors[d.data.kind] || "#6a756e");
    leaf
      .append("text")
      .attr("class", "rest-tm-label")
      .attr("x", 4)
      .attr("y", 14)
      .text((d) => {
        const p = d.data.pecking_order != null ? ` (${d.data.pecking_order})` : "";
        return `${d.data.name || ""}${p}`;
      });
  }

  async function loadChefGraph() {
    if (!restaurantId || typeof d3 === "undefined") return;
    const data = await api(`/api/restaurant/${restaurantId}/chef-card-graph`);
    const svg = d3.select("#chef-graph");
    svg.selectAll("*").remove();
    const width = svg.node().clientWidth || 900;
    const height = 420;
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
      .force("link", d3.forceLink(data.links).id((d) => d.id).distance(90))
      .force("charge", d3.forceManyBody().strength(-180))
      .force("center", d3.forceCenter(width / 2, height / 2));
    const link = root.append("g").selectAll("line").data(data.links).join("line").attr("class", "rest-link");
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
      .attr("r", 8)
      .attr("class", (d) =>
        d.kind === "menu" ? "rest-node-menu" : d.kind === "retinue" ? "rest-node-retinue" : "rest-node-activity"
      );
    node.append("text").text((d) => d.label).attr("x", 10).attr("y", 4).attr("font-size", 11);
    sim.on("tick", () => {
      link
        .attr("x1", (d) => d.source.x)
        .attr("y1", (d) => d.source.y)
        .attr("x2", (d) => d.target.x)
        .attr("y2", (d) => d.target.y);
      node.attr("transform", (d) => `translate(${d.x},${d.y})`);
    });
  }

  async function refreshAll() {
    showMsg("");
    await Promise.all([loadMenu(), loadOrders(), loadCommodities(), loadRetinue()]);
  }

  document.getElementById("rest-tabs").onclick = (e) => {
    const b = e.target.closest("button[data-tab]");
    if (!b) return;
    showTab(b.dataset.tab);
  };
  document.getElementById("rest-modal-cancel").onclick = closeModal;
  document.getElementById("rest-modal-save").onclick = () => saveModal();
  document.getElementById("rest-modal").addEventListener("click", (e) => {
    if (e.target.id === "rest-modal") closeModal();
  });

  (async () => {
    try {
      meta = await api("/api/restaurant/meta");
    } catch (_) {
      meta = { orderStatuses: ["queued", "prep", "plating", "served", "cancelled"] };
    }
    await loadRestaurants();
    await refreshAll();
  })();
})();
