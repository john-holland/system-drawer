(function () {
  "use strict";

  const state = {
    engine: "unity",
    sessionId: null,
    messages: [],
    modelId: "",
    lastAssistant: "",
  };

  const $ = (id) => document.getElementById(id);

  function setStatus(el, text, isError) {
    if (!el) return;
    el.textContent = text || "";
    el.style.color = isError ? "#e88" : "#8ec8b0";
  }

  function formPayload() {
    const comps = ($("lb-comp").value || "")
      .split("\n")
      .map((l) => l.trim())
      .filter(Boolean)
      .map((l) => {
        const [entryId, sortOrder] = l.split("|");
        return { entryId: (entryId || "").trim(), sortOrder: Number(sortOrder || 0) };
      });
    const properties = [];
    ($("lb-props").value || "")
      .split("\n")
      .map((l) => l.trim())
      .filter(Boolean)
      .forEach((l) => {
        const i = l.indexOf("=");
        if (i > 0) {
          properties.push({
            propertyKey: l.slice(0, i).trim(),
            propertyValue: l.slice(i + 1).trim(),
          });
        }
      });
    return {
      lemma: ($("lb-lemma").value || "").trim(),
      partOfSpeech: ($("lb-pos").value || "").trim(),
      posTag: ($("lb-pos").value || "").trim(),
      mechanicalRole: $("lb-role").value,
      outputTier: Number($("lb-tier").value || 0),
      functionalDescription: ($("lb-func").value || "").trim(),
      mechanismPrompt: ($("lb-mech").value || "").trim(),
      synonyms: ($("lb-syn").value || "")
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean),
      compositionChildren: comps,
      properties,
      engine: state.engine,
    };
  }

  function applyForm(data) {
    if (!data || typeof data !== "object") return;
    if (data.lemma != null) $("lb-lemma").value = data.lemma;
    if (data.partOfSpeech != null || data.posTag != null) {
      $("lb-pos").value = data.partOfSpeech || data.posTag;
    }
    if (data.mechanicalRole != null) $("lb-role").value = data.mechanicalRole;
    if (data.outputTier != null) {
      $("lb-tier").value = String(data.outputTier);
      $("lb-tier-val").textContent = String(data.outputTier);
    }
    if (data.functionalDescription != null) $("lb-func").value = data.functionalDescription;
    if (data.mechanismPrompt != null) $("lb-mech").value = data.mechanismPrompt;
    if (Array.isArray(data.synonyms)) $("lb-syn").value = data.synonyms.join(", ");
    if (Array.isArray(data.compositionChildren)) {
      $("lb-comp").value = data.compositionChildren
        .map((c) => `${c.entryId || ""}|${c.sortOrder != null ? c.sortOrder : 0}`)
        .join("\n");
    }
    if (Array.isArray(data.properties)) {
      $("lb-props").value = data.properties
        .map((p) => `${p.propertyKey || p.key || ""}=${p.propertyValue || p.value || ""}`)
        .join("\n");
    } else if (data.properties && typeof data.properties === "object") {
      $("lb-props").value = Object.entries(data.properties)
        .map(([k, v]) => `${k}=${v}`)
        .join("\n");
    }
    if (data.engine) setEngine(String(data.engine), true);
  }

  function setEngine(id, silent) {
    const next = id === "haxe" ? "haxe" : id === "unreal" ? "unreal" : "unity";
    if (next === "unreal") return;
    if (!silent && state.engine !== next && state.messages.length) {
      if (!window.confirm("Switch engine? Transcript is kept; next send uses the new system prompt.")) {
        return;
      }
    }
    state.engine = next;
    document.querySelectorAll(".lb-engine-btn").forEach((btn) => {
      btn.classList.toggle("is-active", btn.getAttribute("data-engine") === next);
    });
  }

  function renderMessages() {
    const root = $("lb-messages");
    root.innerHTML = "";
    state.messages.forEach((m) => {
      const div = document.createElement("div");
      div.className = "lb-msg " + (m.role === "user" ? "user" : "assistant");
      div.textContent = m.content || "";
      root.appendChild(div);
    });
    root.scrollTop = root.scrollHeight;
  }

  async function ensureSession() {
    if (state.sessionId) return state.sessionId;
    const form = formPayload();
    const res = await fetch("/api/lemma-build/sessions", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        lemmaPhrase: form.lemma || "untitled",
        engine: state.engine,
        modelId: state.modelId || undefined,
      }),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || "session failed");
    state.sessionId = data.id || data.sessionId;
    return state.sessionId;
  }

  async function refreshFiles() {
    const box = $("lb-files");
    box.innerHTML = "";
    if (!state.sessionId) return;
    const res = await fetch("/api/lemma-build/sessions/" + encodeURIComponent(state.sessionId));
    const data = await res.json();
    if (!res.ok) return;
    const files = data.files || [];
    files.forEach((f) => {
      const name = typeof f === "string" ? f : f.name;
      if (!name) return;
      const a = document.createElement("a");
      a.href =
        "/api/lemma-build/sessions/" +
        encodeURIComponent(state.sessionId) +
        "/files/" +
        encodeURIComponent(name);
      a.textContent = name;
      a.download = name;
      box.appendChild(a);
    });
  }

  function formatChatError(data, status) {
    const err = (data && data.error) || "chat failed";
    const detail = data && data.detail ? String(data.detail) : "";
    if (err === "chat_failed" && /model_unreachable|10061|refused/i.test(detail)) {
      return (
        "LM Studio not reachable at http://localhost:1234/v1 — start LM Studio, load a model, " +
        "and enable the local server. (" +
        detail +
        ")"
      );
    }
    return detail ? err + ": " + detail : err + " (" + status + ")";
  }

  function buildPromptFromForm() {
    const form = formPayload();
    const lemma = form.lemma || "untitled";
    return [
      "Build a Continuuuum lemma mechanism for `" + lemma + "`.",
      "Engine: " + (form.engine || state.engine) + ".",
      "Part of speech: " + (form.partOfSpeech || "unknown") + ".",
      "Mechanical role: " + (form.mechanicalRole || "AtomicAction") + ".",
      "Output tier: " + String(form.outputTier != null ? form.outputTier : 0) + ".",
      form.functionalDescription ? "Functional description: " + form.functionalDescription : "",
      form.mechanismPrompt ? "Mechanism prompt: " + form.mechanismPrompt : "",
      "Respond with a ```json lemma-mechanism-descriptor fence (lemma, posTag, mechanicalRole, outputTier, functionalDescription, compositionChildren, properties).",
      "Prefer composition from existing builtins when possible.",
    ]
      .filter(Boolean)
      .join("\n");
  }

  async function sendChat(optionalText) {
    const text =
      typeof optionalText === "string" && optionalText.trim()
        ? optionalText.trim()
        : ($("lb-input").value || "").trim();
    if (!text) return;
    setStatus($("lb-chat-status"), "Sending…");
    $("lb-send").disabled = true;
    if ($("lb-build-now")) $("lb-build-now").disabled = true;
    try {
      await ensureSession();
      state.messages.push({ role: "user", content: text });
      if (typeof optionalText !== "string") $("lb-input").value = "";
      renderMessages();
      const form = formPayload();
      const res = await fetch("/api/lemma-build/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId: state.sessionId,
          engine: state.engine,
          modelId: state.modelId || undefined,
          messages: state.messages,
          form,
        }),
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(formatChatError(data, res.status));
      const assistant = data.assistant || data.content || data.message || "";
      if (assistant) {
        state.messages.push({ role: "assistant", content: assistant });
        state.lastAssistant = assistant;
      }
      if (data.sessionId) state.sessionId = data.sessionId;
      if (data.descriptor) {
        applyForm(data.descriptor);
        setStatus($("lb-form-status"), "Descriptor applied from build");
      }
      renderMessages();
      await refreshFiles();
      setStatus($("lb-chat-status"), "ok");
    } catch (e) {
      setStatus($("lb-chat-status"), String(e.message || e), true);
      setStatus($("lb-form-status"), String(e.message || e), true);
    } finally {
      $("lb-send").disabled = false;
      if ($("lb-build-now")) $("lb-build-now").disabled = false;
    }
  }

  async function buildNow() {
    const form = formPayload();
    if (!form.lemma) {
      setStatus($("lb-form-status"), "Enter a lemma / phrase first", true);
      $("lb-lemma").focus();
      return;
    }
    setStatus($("lb-form-status"), "Building…");
    await sendChat(buildPromptFromForm());
  }

  async function applyDescriptor() {
    const text = state.lastAssistant || (state.messages.filter((m) => m.role === "assistant").pop() || {}).content;
    if (!text) {
      setStatus($("lb-form-status"), "No assistant message yet", true);
      return;
    }
    try {
      const res = await fetch("/api/lemma-build/parse-descriptor", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "parse failed");
      applyForm(data.descriptor || data);
      setStatus($("lb-form-status"), "Descriptor applied");
    } catch (e) {
      setStatus($("lb-form-status"), String(e.message || e), true);
    }
  }

  async function openEditor() {
    const form = formPayload();
    try {
      const res = await fetch("/api/deeplink", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          window: "System Drawer/Lemmas/Lemma Build",
          form,
        }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "deeplink failed");
      setStatus($("lb-form-status"), "Deeplink written — focus Unity");
    } catch (e) {
      setStatus($("lb-form-status"), String(e.message || e), true);
    }
  }

  function hydrateFromQuery() {
    const q = new URLSearchParams(window.location.search);
    const form = {};
    ["lemma", "partOfSpeech", "mechanicalRole", "functionalDescription", "mechanismPrompt", "engine"].forEach(
      (k) => {
        if (q.has(k)) form[k] = q.get(k);
      }
    );
    if (q.has("outputTier")) form.outputTier = Number(q.get("outputTier"));
    if (q.has("synonyms")) form.synonyms = q.get("synonyms").split(",").map((s) => s.trim()).filter(Boolean);
    applyForm(form);
  }

  async function loadSettings() {
    try {
      const res = await fetch("/api/lemma-build/settings");
      const data = await res.json();
      if (res.ok) {
        state.modelId = data.defaultModelId || data.default_model_id || "";
        $("lb-model-label").textContent = state.modelId || "default model";
        if (data.defaultEngine || data.default_engine) {
          setEngine(data.defaultEngine || data.default_engine, true);
        }
      }
    } catch (_) {
      /* ignore */
    }
  }

  async function probeLmStudio() {
    const el = $("lb-lm-status");
    if (!el) return;
    el.textContent = "LM Studio…";
    el.classList.remove("is-ok", "is-bad");
    try {
      const res = await fetch("/api/lemma-build/lm-status");
      const data = await res.json().catch(() => ({}));
      if (!data.reachable) {
        el.textContent = "LM Studio offline";
        el.classList.add("is-bad");
        el.title = data.detail || data.error || "Start LM Studio local server on :1234";
        return;
      }
      const ids = Array.isArray(data.models) ? data.models : [];
      if (!ids.length) {
        el.textContent = "LM Studio: no model loaded";
        el.classList.add("is-bad");
        return;
      }
      el.textContent = "LM Studio ok (" + (data.modelCount || ids.length) + ")";
      el.classList.add("is-ok");
      el.title = ids.slice(0, 5).join(", ");
      if (!state.modelId && ids[0]) {
        state.modelId = typeof ids[0] === "string" ? ids[0] : ids[0].id || state.modelId;
        $("lb-model-label").textContent = state.modelId;
      }
    } catch (e) {
      el.textContent = "LM Studio offline";
      el.classList.add("is-bad");
      el.title = String(e.message || e);
    }
  }

  function bind() {
    document.querySelectorAll(".lb-engine-btn").forEach((btn) => {
      btn.addEventListener("click", () => setEngine(btn.getAttribute("data-engine")));
    });
    $("lb-tier").addEventListener("input", () => {
      $("lb-tier-val").textContent = $("lb-tier").value;
    });
    $("lb-send").addEventListener("click", () => sendChat());
    $("lb-build-now").addEventListener("click", buildNow);
    $("lb-input").addEventListener("keydown", (e) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendChat();
      }
    });
    $("lb-clear").addEventListener("click", () => {
      state.messages = [];
      state.lastAssistant = "";
      renderMessages();
      setStatus($("lb-chat-status"), "cleared");
    });
    $("lb-save-batch").addEventListener("click", async () => {
      try {
        await ensureSession();
        await refreshFiles();
        setStatus($("lb-chat-status"), "Batch: " + state.sessionId);
      } catch (e) {
        setStatus($("lb-chat-status"), String(e.message || e), true);
      }
    });
    $("lb-apply").addEventListener("click", applyDescriptor);
    $("lb-open-editor").addEventListener("click", openEditor);
  }

  bind();
  hydrateFromQuery();
  loadSettings().then(probeLmStudio);
  if (window.ContinuuuumNav) {
    window.ContinuuuumNav.mount({ app: "lemma-build", theme: "dark" });
  }
})();
