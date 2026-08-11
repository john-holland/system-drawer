(function () {
  const msg = document.getElementById("ta-msg");
  function show(text, isErr) {
    msg.hidden = false;
    msg.textContent = text;
    msg.style.background = isErr ? "#ffe8e8" : "#eef6ff";
  }

  function cronLabel(expr) {
    if (!window.CronHumanize || !expr) return "";
    return CronHumanize.describe(expr);
  }

  async function loadVehicles() {
    const res = await fetch("/api/transit/vehicle-schedules");
    const data = await res.json();
    const ul = document.getElementById("ta-vehicle-list");
    ul.innerHTML = "";
    (data.schedules || []).forEach((s) => {
      const li = document.createElement("li");
      const human = cronLabel(s.cronExpr);
      li.innerHTML = `<span><strong>${s.vehicleId}</strong> / ${s.routeId} — <code>${s.cronExpr}</code> (${s.scheduleKind})` +
        (human ? `<span class="cron-human-inline">${human}</span>` : "") +
        `</span>`;
      const btn = document.createElement("button");
      btn.className = "ta-del";
      btn.textContent = "Delete";
      btn.onclick = async () => {
        await fetch(`/api/transit/vehicle-schedules/${s.id}`, { method: "DELETE" });
        loadVehicles();
        loadMap();
      };
      li.appendChild(btn);
      ul.appendChild(li);
    });
  }

  async function loadBuildings() {
    const res = await fetch("/api/transit/building-schedules");
    const data = await res.json();
    const ul = document.getElementById("ta-building-list");
    ul.innerHTML = "";
    (data.schedules || []).forEach((s) => {
      const li = document.createElement("li");
      const human = cronLabel(s.cronExpr);
      li.innerHTML = `<span><strong>${s.stationId}</strong> — <code>${s.cronExpr}</code> (${s.kind})` +
        (human ? `<span class="cron-human-inline">${human}</span>` : "") +
        `</span>`;
      const btn = document.createElement("button");
      btn.className = "ta-del";
      btn.textContent = "Delete";
      btn.onclick = async () => {
        await fetch(`/api/transit/building-schedules/${s.id}`, { method: "DELETE" });
        loadBuildings();
      };
      li.appendChild(btn);
      ul.appendChild(li);
    });
  }

  async function loadMap() {
    const res = await fetch("/api/transit/routes");
    const data = await res.json();
    document.getElementById("ta-routes-map").textContent = JSON.stringify(data.vehicleRoutes || {}, null, 2);
  }

  document.getElementById("ta-vehicle-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const body = Object.fromEntries(fd.entries());
    const res = await fetch("/api/transit/vehicle-schedules", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      show((await res.json()).error || "Failed", true);
      return;
    }
    show("Vehicle schedule saved");
    e.target.reset();
    loadVehicles();
    loadMap();
  });

  document.getElementById("ta-building-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const body = Object.fromEntries(fd.entries());
    const res = await fetch("/api/transit/building-schedules", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      show((await res.json()).error || "Failed", true);
      return;
    }
    show("Building schedule saved");
    e.target.reset();
    loadBuildings();
  });

  document.getElementById("ta-refresh-map").onclick = loadMap;

  if (window.ContinuuuumNav && typeof ContinuuuumNav.mount === "function") {
    ContinuuuumNav.mount({ app: "transit", theme: "light" });
  }

  if (window.CronHumanize) {
    CronHumanize.bindInput(
      document.getElementById("ta-vehicle-cron"),
      document.getElementById("ta-vehicle-cron-human")
    );
    CronHumanize.bindInput(
      document.getElementById("ta-building-cron"),
      document.getElementById("ta-building-cron-human")
    );
  }

  loadVehicles();
  loadBuildings();
  loadMap();
})();
