(function () {
  const msg = document.getElementById("sh-msg");
  function show(text, isErr) {
    msg.hidden = false;
    msg.textContent = text;
    msg.style.background = isErr ? "#ffe8e8" : "#eef6ff";
  }

  async function load() {
    const res = await fetch("/api/airport/staff-hours");
    const data = await res.json();
    const ul = document.getElementById("sh-list");
    ul.innerHTML = "";
    (data.schedules || []).forEach((s) => {
      const li = document.createElement("li");
      li.innerHTML = `<span><strong>${s.buildingId}</strong> / ${s.role} — open <code>${s.openCron}</code>` +
        (s.closeCron ? ` close <code>${s.closeCron}</code>` : "") +
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

  load();
})();
