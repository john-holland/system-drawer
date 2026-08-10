(function () {
  "use strict";

  const state = {
    scope: "common5000",
    q: "",
    missingDefinition: false,
    notImplemented: false,
    assetStore: false,
    rankMin: "",
    rankMax: "",
    limit: 50,
    offset: 0,
    total: 0,
  };

  function $(id) {
    return document.getElementById(id);
  }

  function esc(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/"/g, "&quot;");
  }

  function lemmaBuildHref(term, posTag) {
    const q = new URLSearchParams();
    q.set("lemma", term);
    q.set("engine", "unity");
    if (posTag) q.set("partOfSpeech", posTag);
    return "/lemma-build/?" + q.toString();
  }

  function pctBar(label, value, percent, primary) {
    const p = Math.max(0, Math.min(100, Number(percent) || 0));
    return (
      '<div class="lc-stat">' +
      "<strong>" +
      esc(String(p)) +
      "%</strong>" +
      "<span>" +
      esc(label) +
      " (" +
      esc(String(value)) +
      ")</span>" +
      '<div class="lc-bar' +
      (primary ? " is-primary" : "") +
      '"><i style="width:' +
      p +
      '%"></i></div>' +
      "</div>"
    );
  }

  function updatePrimesBanner() {
    const banner = $("lc-primes-banner");
    if (!banner) return;
    banner.classList.toggle("hidden", state.scope !== "primes");
  }

  async function loadSummary() {
    const res = await fetch("/api/lemma-completion/summary?scope=" + encodeURIComponent(state.scope));
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || "summary failed");
    updatePrimesBanner();
    const progressed = data.progressed != null ? data.progressed : data.implemented;
    $("lc-stats").innerHTML =
      pctBar("Overall (builtin or implemented)", progressed, data.percentOverall, true) +
      pctBar("NSM defined", data.defined, data.percentDefined, false) +
      pctBar("Marked builtin", data.builtin, data.percentBuiltin, false) +
      pctBar("Implemented", data.implemented, data.percentImplemented, false) +
      pctBar("Asset store benefit", data.assetStore, data.percentAssetStore, false) +
      '<div class="lc-stat"><strong>' +
      esc(String(data.total)) +
      "</strong><span>Total in scope</span></div>";
  }

  function readFilters() {
    state.q = $("lc-q").value.trim();
    state.missingDefinition = $("lc-missing").checked;
    state.notImplemented = $("lc-not-impl").checked;
    state.assetStore = $("lc-asset").checked;
    state.rankMin = $("lc-rank-min").value;
    state.rankMax = $("lc-rank-max").value;
    state.scope = $("lc-scope").value;
  }

  async function loadEntries() {
    readFilters();
    const q = new URLSearchParams();
    q.set("limit", String(state.limit));
    q.set("offset", String(state.offset));
    if (state.q) q.set("q", state.q);
    if (state.missingDefinition) q.set("missingDefinition", "1");
    if (state.notImplemented) q.set("notImplemented", "1");
    if (state.assetStore) q.set("assetStore", "1");
    if (state.rankMin) q.set("rankMin", state.rankMin);
    if (state.rankMax) q.set("rankMax", state.rankMax);
    if (state.scope === "primes") q.set("isPrime", "1");
    if (state.scope === "common5000") q.set("hasRank", "1");

    const res = await fetch("/api/lemma-completion/entries?" + q.toString());
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || "entries failed");

    const items = data.items || [];
    state.total = data.total || 0;
    const tbody = $("lc-tbody");
    tbody.innerHTML = items
      .map((item) => {
        const href = lemmaBuildHref(item.term, item.posTag);
        return (
          "<tr data-id=\"" +
          esc(item.id) +
          "\">" +
          "<td>" +
          esc(item.rank == null ? "—" : item.rank) +
          "</td>" +
          "<td><a class=\"lc-term-link\" href=\"" +
          esc(href) +
          "\">" +
          esc(item.term) +
          "</a>" +
          (item.isPrime ? ' <span class="lc-yes">prime</span>' : "") +
          "</td>" +
          "<td class=\"" +
          (item.isDefined ? "lc-yes" : "lc-no") +
          "\">" +
          (item.isDefined ? "yes" : "no") +
          "</td>" +
          "<td><input type=\"checkbox\" data-flag=\"isBuiltin\" " +
          (item.isBuiltin ? "checked " : "") +
          "/></td>" +
          "<td><input type=\"checkbox\" data-flag=\"isImplemented\" " +
          (item.isImplemented ? "checked " : "") +
          "/></td>" +
          "<td><input type=\"checkbox\" data-flag=\"benefitsFromAssetStore\" " +
          (item.benefitsFromAssetStore ? "checked " : "") +
          "/></td>" +
          "<td class=\"lc-def\" title=\"" +
          esc(item.nsmDefinition || "") +
          "\">" +
          esc(item.nsmDefinition || "") +
          "</td>" +
          "</tr>"
        );
      })
      .join("");

    const page = Math.floor(state.offset / state.limit) + 1;
    const pages = Math.max(1, Math.ceil(state.total / state.limit));
    $("lc-page-label").textContent = "Page " + page + " / " + pages + " (" + state.total + ")";
    $("lc-prev").disabled = state.offset <= 0;
    $("lc-next").disabled = state.offset + state.limit >= state.total;
  }

  async function patchFlag(id, flag, value) {
    const body = {};
    body[flag] = value;
    const res = await fetch("/api/lemma-completion/entries/" + encodeURIComponent(id), {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      throw new Error(data.error || "patch failed");
    }
  }

  async function refresh() {
    await loadSummary();
    await loadEntries();
  }

  function bind() {
    $("lc-refresh").addEventListener("click", () => {
      state.offset = 0;
      refresh().catch(alert);
    });
    $("lc-seed").addEventListener("click", async () => {
      const res = await fetch("/api/lemma-completion/seed", { method: "POST" });
      const data = await res.json();
      if (!res.ok) return alert(data.error || "seed failed");
      state.offset = 0;
      await refresh();
    });
    $("lc-sync-builtins").addEventListener("click", async () => {
      const res = await fetch("/api/lemma-completion/sync-builtins", { method: "POST" });
      const data = await res.json();
      if (!res.ok) return alert(data.error || "sync failed");
      state.offset = 0;
      await refresh();
    });
    $("lc-scope").addEventListener("change", () => {
      state.offset = 0;
      refresh().catch(alert);
    });
    ["lc-q", "lc-missing", "lc-not-impl", "lc-asset", "lc-rank-min", "lc-rank-max"].forEach((id) => {
      $(id).addEventListener("change", () => {
        state.offset = 0;
        loadEntries().catch(alert);
      });
    });
    $("lc-q").addEventListener("keydown", (e) => {
      if (e.key === "Enter") {
        state.offset = 0;
        loadEntries().catch(alert);
      }
    });
    $("lc-prev").addEventListener("click", () => {
      state.offset = Math.max(0, state.offset - state.limit);
      loadEntries().catch(alert);
    });
    $("lc-next").addEventListener("click", () => {
      state.offset += state.limit;
      loadEntries().catch(alert);
    });
    $("lc-tbody").addEventListener("change", async (e) => {
      const input = e.target;
      if (!(input instanceof HTMLInputElement) || input.type !== "checkbox") return;
      const flag = input.getAttribute("data-flag");
      const tr = input.closest("tr");
      const id = tr && tr.getAttribute("data-id");
      if (!flag || !id) return;
      try {
        await patchFlag(id, flag, input.checked);
        await loadSummary();
      } catch (err) {
        input.checked = !input.checked;
        alert(String(err.message || err));
      }
    });
  }

  // Expose for tests
  window.LemmaCompletionPage = { lemmaBuildHref, pctBar };

  if (window.ContinuuuumNav) {
    window.ContinuuuumNav.mount({ app: "lemma-completion" });
  }

  bind();
  refresh().catch((e) => {
    console.error(e);
    $("lc-stats").textContent = "Failed to load — try Seed list. " + e;
  });
})();
