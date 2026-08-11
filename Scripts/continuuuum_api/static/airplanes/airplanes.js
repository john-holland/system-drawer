(function () {
  const msg = document.getElementById("ap-msg");
  function show(text, isErr) {
    msg.hidden = false;
    msg.textContent = text;
    msg.style.background = isErr ? "#ffe8e8" : "#eef6ff";
  }

  function cronLabel(expr) {
    if (!window.CronHumanize || !expr) return "";
    return CronHumanize.describe(expr);
  }

  async function loadSchedules() {
    const res = await fetch("/api/airport/airplane-schedules");
    const data = await res.json();
    const ul = document.getElementById("ap-schedule-list");
    ul.innerHTML = "";
    (data.schedules || []).forEach((s) => {
      const li = document.createElement("li");
      const human = cronLabel(s.cronExpr);
      li.innerHTML =
        `<span><strong>${s.airplaneId}</strong> / ${s.flightId} — <code>${s.cronExpr}</code> (${s.scheduleKind})` +
        (human ? `<span class="cron-human-inline">${human}</span>` : "") +
        `<span class="ap-meta">crew: ${s.airplaneCrewJson || "—"} | gate: ${s.gateCrewJson || "—"} | ground: ${s.groundCrewJson || "—"}</span></span>`;
      const btn = document.createElement("button");
      btn.className = "ap-del";
      btn.textContent = "Delete";
      btn.onclick = async () => {
        await fetch(`/api/airport/airplane-schedules/${s.id}`, { method: "DELETE" });
        loadSchedules();
      };
      li.appendChild(btn);
      ul.appendChild(li);
    });
  }

  document.getElementById("ap-schedule-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const body = Object.fromEntries(fd.entries());
    const res = await fetch("/api/airport/airplane-schedules", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      show((await res.json()).error || "Failed", true);
      return;
    }
    show("Airplane schedule saved");
    e.target.reset();
    loadSchedules();
  });

  if (window.ContinuuuumNav && typeof ContinuuuumNav.mount === "function") {
    ContinuuuumNav.mount({ app: "airplanes", theme: "light" });
  }

  if (window.CronHumanize) {
    CronHumanize.bindInput(
      document.getElementById("ap-cron"),
      document.getElementById("ap-cron-human")
    );
  }

  loadSchedules();
})();
