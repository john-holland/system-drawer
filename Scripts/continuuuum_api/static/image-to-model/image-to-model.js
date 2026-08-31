(function () {
  "use strict";

  var Session = window.ContinuuuumUserSession;
  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ app: "image-to-model", theme: "dark" });
  }

  var sheetHost = document.getElementById("itm-sheet");
  ItmSheet.mount(sheetHost, { onChange: refreshPreview });

  var features = [];
  var webglPreview = "/continuuuum_editor/index.html";
  var webglBuild = false;

  function $(id) {
    return document.getElementById(id);
  }

  function apiBase() {
    if (window.ContinuuuumNav && typeof ContinuuuumNav.apiBase === "function") {
      return ContinuuuumNav.apiBase();
    }
    return location.origin;
  }

  function headers(json) {
    var h =
      Session && Session.getHeaders
        ? Session.getHeaders(json ? { "Content-Type": "application/json" } : {})
        : json
          ? { "Content-Type": "application/json" }
          : {};
    return h;
  }

  function checkedFeats() {
    return Array.from(document.querySelectorAll("#feat-checks input[data-feat]:checked")).map(function (x) {
      return x.dataset.feat;
    });
  }

  function refreshPreview() {
    var v = ItmSheet.values(sheetHost);
    var q = new URLSearchParams({
      artworkId: $("artworkId").value || "",
      apiBase: apiBase(),
      granularity: JSON.stringify(v.granularity),
      blockMeters: String(v.granularity.blockMeters),
      modelSpec: checkedFeats().join(",")
    });
    $("webgl").src = webglPreview + (webglPreview.indexOf("?") >= 0 ? "&" : "?") + q.toString();
    $("webgl-hint").textContent = webglBuild
      ? "Continuuuum editor WebGL build detected."
      : "Placeholder until a continuuuum_editor_webgl/Build is present. Set CONTINUUUUM_REPO or drop a build.";
  }

  function renderFeatures() {
    $("feat-box").innerHTML = features
      .map(function (f) {
        return (
          "<option value=\"" +
          f.id +
          "\">" +
          f.label +
          " · " +
          f.kind +
          (f.available ? " · available" : " · missing") +
          "</option>"
        );
      })
      .join("");
    $("feat-checks").innerHTML = features
      .map(function (f) {
        return (
          '<label class="itm-check"><input type="checkbox" data-feat="' +
          f.id +
          '" /> ' +
          f.label +
          (f.available ? "" : ' <span class="wa-kind">offline</span>') +
          "</label>"
        );
      })
      .join("");
    $("feat-props").innerHTML = features
      .map(function (f) {
        var extra = "";
        if (f.id.indexOf("modly") === 0) extra = "<p class=\"wa-meta\">Prompt / steps / format live in the asset sheet.</p>";
        if (f.id === "voxel-ragdoll") extra = "<p class=\"wa-meta\">Attaches VoxelRagdollActor at blockMeters scale (Minecraft default 1 m / block).</p>";
        if (f.id === "pixellight") extra = "<p class=\"wa-meta\">Six-face PixelLight params are in Axis-dependent art.</p>";
        return (
          '<div class="itm-props" hidden data-prop="' +
          f.id +
          '"><h4>' +
          f.label +
          "</h4><p class=\"wa-meta\">" +
          (f.hint || "") +
          "</p>" +
          extra +
          "</div>"
        );
      })
      .join("");
    document.querySelectorAll("#feat-checks input[data-feat]").forEach(function (cb) {
      cb.onchange = function () {
        var panel = document.querySelector('[data-prop="' + cb.dataset.feat + '"]');
        if (panel) panel.hidden = !cb.checked;
        refreshPreview();
      };
    });
  }

  fetch(apiBase() + "/api/image-to-model/features", { headers: headers(false), credentials: "include" })
    .then(function (r) {
      return r.json();
    })
    .then(function (m) {
      features = m.features || [];
      webglPreview = m.webglPreview || webglPreview;
      webglBuild = !!m.webglBuild;
      if (m.granularityMinecraft) ItmSheet.applyGranularity(sheetHost, m.granularityMinecraft);
      renderFeatures();
      refreshPreview();
    })
    .catch(function () {
      renderFeatures();
      refreshPreview();
    });

  function previewFile(input, img) {
    var f = input.files[0];
    if (!f) {
      img.hidden = true;
      return;
    }
    img.src = URL.createObjectURL(f);
    img.hidden = false;
  }
  $("src-image").addEventListener("change", function () {
    previewFile($("src-image"), $("img-prev"));
  });
  $("src-mask").addEventListener("change", function () {
    previewFile($("src-mask"), $("mask-prev"));
  });
  $("artworkId").addEventListener("input", refreshPreview);

  $("itm-form").onsubmit = async function (ev) {
    ev.preventDefault();
    var fd = new FormData();
    var id = $("artworkId").value;
    if (id) fd.set("artworkId", id);
    var img = $("src-image").files[0];
    var mask = $("src-mask").files[0];
    if (img) fd.set("image", img);
    if (mask) fd.set("mask", mask);
    var v = ItmSheet.values(sheetHost);
    fd.set("granularity", JSON.stringify(v.granularity));
    fd.set("axis", JSON.stringify(v.axisArt));
    ItmSheet.appendFaceFiles(fd, sheetHost);
    $("itm-status").textContent = "storing…";
    var r = await fetch(apiBase() + "/api/image-to-model/media", {
      method: "POST",
      body: fd,
      credentials: "include",
      headers: headers(false)
    });
    var rec = await r.json();
    if (rec.artworkId) $("artworkId").value = rec.artworkId;
    $("itm-status").textContent = rec.error || JSON.stringify(rec.media || rec, null, 0);
    refreshPreview();
  };

  $("modly-run").onclick = async function () {
    var artworkId = $("artworkId").value;
    if (!artworkId) {
      $("modly-status").textContent = "store an image first";
      return;
    }
    var v = ItmSheet.values(sheetHost);
    $("modly-status").textContent = "invoking Modly…";
    var r = await fetch(apiBase() + "/api/image-to-model/modly", {
      method: "POST",
      credentials: "include",
      headers: headers(true),
      body: JSON.stringify({
        artworkId: artworkId,
        t: -1,
        prompt: v.prompt,
        meshFormat: v.meshFormat,
        steps: v.steps,
        granularity: v.granularity
      })
    });
    var rec = await r.json();
    $("modly-status").textContent = rec.ok
      ? "cached " + rec.bytes + " bytes"
      : rec.hint || rec.error || "unavailable";
    refreshPreview();
  };
})();
