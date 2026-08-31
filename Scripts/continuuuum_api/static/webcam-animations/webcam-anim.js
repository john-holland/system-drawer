(function () {
  "use strict";

  var Session = window.ContinuuuumUserSession;
  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ app: "webcam-animations", theme: "dark" });
  }

  var pollTimer = null;
  var liveStream = null;
  var filePreviewUrl = null;
  var catalogModels = [];
  var detectorProfiles = [];

  function $(id) {
    return document.getElementById(id);
  }

  function apiBase() {
    if (window.ContinuuuumNav && typeof ContinuuuumNav.apiBase === "function") {
      return ContinuuuumNav.apiBase();
    }
    return location.origin;
  }

  function webGlPreviewUrl(doc) {
    if (doc && doc.previewUrl) return doc.previewUrl;
    var base = "/continuuuum_editor/index.html";
    var params = new URLSearchParams({
      docId: doc.libraryDocId || doc.id || "",
      apiBase: apiBase(),
      subsection: (doc.subsection || (doc.type_metadata && doc.type_metadata.subsection)) || "",
      startMs: String(doc.timelineStartMs || 0),
      endMs: String(doc.timelineEndMs || 0),
    });
    return base + "?" + params.toString();
  }

  async function api(path, opts) {
    opts = opts || {};
    var headers =
      Session && Session.getHeaders
        ? Session.getHeaders({ "Content-Type": "application/json" })
        : { "Content-Type": "application/json" };
    if (opts.body instanceof FormData) {
      delete headers["Content-Type"];
    }
    var res = await fetch(path, {
      ...opts,
      headers: Object.assign({}, headers, opts.headers || {}),
      credentials: "include",
      body:
        opts.body && typeof opts.body !== "string" && !(opts.body instanceof FormData)
          ? JSON.stringify(opts.body)
          : opts.body,
    });
    var data = await res.json().catch(function () {
      return {};
    });
    if (!res.ok) {
      throw new Error(data.error || res.statusText || "request failed");
    }
    return data;
  }

  function fillDetectorProfiles(selected) {
    var sel = $("wa-detector-profile");
    if (!sel) return;
    var enabled = detectorProfiles.filter(function (p) {
      return p.enabled !== false && p.id;
    });
    var want = selected || (enabled[0] && enabled[0].id) || "";
    sel.innerHTML = enabled
      .map(function (p) {
        return (
          '<option value="' +
          p.id +
          '"' +
          (p.id === want ? " selected" : "") +
          ">" +
          (p.label || p.id) +
          "</option>"
        );
      })
      .join("");
    updatePinnedSpec();
  }

  function derivedProfileSpec(profileId) {
    var p = detectorProfiles.find(function (x) {
      return x.id === profileId;
    });
    if (!p) return "";
    return p.poseEngine === "mocapanything" ? p.mocapSpec || "" : p.mediapipeSpec || "";
  }

  function updatePinnedSpec() {
    var sel = $("wa-detector-profile");
    var pinned = $("wa-pinned-spec");
    if (!pinned) return;
    pinned.value = sel ? derivedProfileSpec(sel.value) : "";
  }

  function fillModelSpecSelect(selected) {
    var sel = $("wa-model-spec");
    if (!sel) return;
    var enabled = catalogModels.filter(function (m) {
      return m.enabled !== false && m.id;
    });
    var ids = enabled.map(function (m) {
      return m.id;
    });
    var want = selected || "";
    var custom = want && ids.indexOf(want) < 0;
    sel.innerHTML = enabled
      .map(function (m) {
        var pick = m.id === want && !custom ? " selected" : "";
        return (
          '<option value="' +
          m.id +
          '"' +
          pick +
          ">" +
          (m.label || m.id) +
          " (" +
          m.kind +
          ")</option>"
        );
      })
      .join("");
    sel.innerHTML +=
      '<option value="__custom__"' + (custom || !enabled.length ? " selected" : "") + ">Custom…</option>";
    var customInput = $("wa-model-spec-custom");
    if (customInput && custom) customInput.value = want;
  }

  function resolvedModelSpec(form) {
    var sel = form.querySelector('[name="model_spec"]') || $("wa-model-spec");
    var custom = (form.querySelector('[name="model_spec_custom"]') || $("wa-model-spec-custom") || {})
      .value;
    custom = (custom || "").trim();
    if (!sel || sel.value === "__custom__") return custom;
    return sel.value;
  }

  function stopPoll() {
    if (pollTimer) {
      clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  function maybePoll(rows) {
    var busy = (rows || []).some(function (r) {
      return r.queueStatus === "queued" || r.queueStatus === "running";
    });
    if (busy && !pollTimer) {
      pollTimer = setInterval(function () {
        refresh().catch(function (err) {
          $("wa-status").textContent = String(err.message || err);
        });
      }, 3000);
    } else if (!busy) {
      stopPoll();
    }
  }

  function recordingAction(doc) {
    var status = doc.queueStatus || "none";
    if (status === "queued" || status === "running") {
      var span = document.createElement("span");
      span.className = "wa-in-progress";
      span.textContent = "In progress (" + status + ")";
      return span;
    }
    if (status === "failed") {
      var err = document.createElement("span");
      err.className = "wa-failed";
      err.textContent = doc.queueError || doc.error || "failed";
      return err;
    }
    var link = document.createElement("a");
    link.href = webGlPreviewUrl(doc);
    link.textContent = "View";
    link.target = "_blank";
    link.rel = "noopener";
    return link;
  }

  async function refresh() {
    var list = $("wa-list");
    list.innerHTML = "";
    var rows = await api("/api/webcam-animations?kind=webcam_anim_recording");
    rows.forEach(function (doc) {
      var li = document.createElement("li");
      var meta = doc.type_metadata || {};
      var title = document.createElement("div");
      title.innerHTML =
        "<strong>" +
        (doc.subsection || meta.subsection || doc.id) +
        "</strong> <span class=\"wa-kind\">" +
        (doc.webcamAnimKind || "") +
        "</span><div class=\"wa-meta\">" +
        (doc.model_spec || "") +
        " · " +
        (doc.timelineStartMs || 0) +
        "–" +
        (doc.timelineEndMs || 0) +
        " ms</div>";
      li.appendChild(title);
      li.appendChild(recordingAction(doc));
      list.appendChild(li);
    });
    maybePoll(rows);
  }

  async function loadCatalog() {
    try {
      var data = await api("/api/webcam-animations/models");
      catalogModels = data.models || [];
      detectorProfiles = data.detectorProfiles || [];
      fillDetectorProfiles("");
      fillModelSpecSelect(($("wa-model-spec-custom") || {}).value || "");
    } catch (err) {
      $("wa-status").textContent = String(err.message || err);
    }
  }

  function attachLivePreview() {
    var v = $("wa-live");
    if (v && liveStream) {
      v.srcObject = liveStream;
      v.play().catch(function () {});
    }
  }

  var startBtn = $("wa-webcam-start");
  if (startBtn) {
    startBtn.addEventListener("click", async function () {
      try {
        liveStream = await navigator.mediaDevices.getUserMedia({ video: true });
        attachLivePreview();
        $("wa-status").textContent = "Webcam on.";
      } catch (err) {
        $("wa-status").textContent = String(err.message || err);
      }
    });
  }

  var stopBtn = $("wa-webcam-stop");
  if (stopBtn) {
    stopBtn.addEventListener("click", function () {
      if (liveStream) {
        liveStream.getTracks().forEach(function (t) {
          t.stop();
        });
        liveStream = null;
      }
      var v = $("wa-live");
      if (v) v.srcObject = null;
    });
  }

  var fileInput = $("wa-file");
  if (fileInput) {
    fileInput.addEventListener("change", function () {
      if (filePreviewUrl) {
        URL.revokeObjectURL(filePreviewUrl);
        filePreviewUrl = null;
      }
      var file = fileInput.files && fileInput.files[0];
      var preview = $("wa-file-preview");
      if (file && preview) {
        filePreviewUrl = URL.createObjectURL(file);
        preview.src = filePreviewUrl;
      }
    });
  }

  var profileSel = $("wa-detector-profile");
  if (profileSel) {
    profileSel.addEventListener("change", updatePinnedSpec);
  }

  var voxelSheet = $("wa-voxel-sheet");
  var voxelCb = $("wa-voxel-ragdoll");
  if (voxelSheet && window.ItmSheet) {
    ItmSheet.mount(voxelSheet);
    if (voxelCb) {
      voxelCb.addEventListener("change", function () {
        voxelSheet.hidden = !voxelCb.checked;
        if (voxelCb.checked) {
          ItmSheet.applyGranularity(voxelSheet, Object.assign({}, ItmSheet.MINECRAFT, { snapToGrid: true }));
        }
      });
    }
  }

  $("wa-form").addEventListener("submit", async function (ev) {
    ev.preventDefault();
    var form = ev.target;
    var fd = new FormData(form);
    var body = {
      kind: "webcam_anim_recording",
      webcamAnimKind: fd.get("webcamAnimKind"),
      detectorProfileId: fd.get("detectorProfileId") || "",
      model_spec: (fd.get("detectorProfileId") && derivedProfileSpec(fd.get("detectorProfileId"))) || resolvedModelSpec(form),
      subsection: fd.get("subsection"),
      animationListIndex: Number(fd.get("animationListIndex") || 0),
      timelineStartMs: Number(fd.get("timelineStartMs") || 0),
      timelineEndMs: Number(fd.get("timelineEndMs") || 0),
      granularity: fd.get("granularity"),
      targetHint: fd.get("targetHint"),
      libraryDocId: fd.get("libraryDocId") || "",
      species: fd.get("species") || "",
    };
    if (voxelCb && voxelCb.checked && window.ItmSheet && voxelSheet) {
      var sheet = ItmSheet.values(voxelSheet);
      body.voxelRagdoll = true;
      body.spatialGranularity = sheet.granularity;
      body.axisArt = sheet.axisArt;
      body.assignment = sheet.assignment;
    }
    $("wa-status").textContent = "Enqueueing…";
    try {
      var saved;
      var file = form.querySelector('input[name="file"]').files[0];
      if (file) {
        var upload = new FormData();
        upload.append("type_metadata", JSON.stringify(body));
        upload.append("file", file, file.name);
        saved = await api("/api/webcam-animations", { method: "POST", body: upload });
      } else {
        saved = await api("/api/webcam-animations", { method: "POST", body: body });
      }
      $("wa-status").textContent =
        "Queued " + saved.id + (saved.queueStatus ? " (" + saved.queueStatus + ")" : "");
      form.reset();
      fillModelSpecSelect(body.model_spec);
      await refresh();
    } catch (err) {
      $("wa-status").textContent = String(err.message || err);
    }
  });

  $("wa-refresh").addEventListener("click", function () {
    refresh().catch(function (err) {
      $("wa-status").textContent = String(err.message || err);
    });
  });

  loadCatalog()
    .then(function () {
      return refresh();
    })
    .catch(function (err) {
      $("wa-status").textContent = String(err.message || err);
    });
})();
