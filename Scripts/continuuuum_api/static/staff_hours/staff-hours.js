(function () {
  const msg = document.getElementById("sh-msg");
  function show(text, isErr) {
    msg.hidden = false;
    msg.textContent = text;
    msg.style.background = isErr ? "#ffe8e8" : "#eef6ff";
  }

  function cronLabel(expr) {
    if (!window.CronHumanize || !expr) return "";
    return CronHumanize.describe(expr);
  }

  async function load() {
    const res = await fetch("/api/airport/staff-hours");
    const data = await res.json();
    const ul = document.getElementById("sh-list");
    ul.innerHTML = "";
    (data.schedules || []).forEach((s) => {
      const li = document.createElement("li");
      const openH = cronLabel(s.openCron);
      const closeH = cronLabel(s.closeCron);
      li.innerHTML = `<span><strong>${s.buildingId}</strong> / ${s.role} — open <code>${s.openCron}</code>` +
        (openH ? `<span class="cron-human-inline">${openH}</span>` : "") +
        (s.closeCron ? ` close <code>${s.closeCron}</code>` : "") +
        (closeH ? `<span class="cron-human-inline">${closeH}</span>` : "") +
        `</span>`;
      const btn = document.createElement("button");
      btn.className = "sh-del";
      btn.textContent = "Delete";
      btn.onclick = async () => {
        await fetch(`/api/airport/staff-hours/${s.id}`, { method: "DELETE" });
        load();
      };
      li.appendChild(btn);
      ul.appendChild(li);
    });
  }

  document.getElementById("sh-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const body = Object.fromEntries(fd.entries());
    const res = await fetch("/api/airport/staff-hours", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      show((await res.json()).error || "Failed", true);
      return;
    }
    show("Staff hours saved");
    e.target.reset();
    load();
  });

  if (window.ContinuuuumNav && typeof ContinuuuumNav.mount === "function") {
    ContinuuuumNav.mount({ app: "staff-hours", theme: "light" });
  }

  if (window.CronHumanize) {
    CronHumanize.bindInput(
      document.getElementById("sh-open-cron"),
      document.getElementById("sh-open-cron-human")
    );
    CronHumanize.bindInput(
      document.getElementById("sh-close-cron"),
      document.getElementById("sh-close-cron-human")
    );
  }

  load();
})();
