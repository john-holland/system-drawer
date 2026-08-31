(function (global) {
  "use strict";

  var FACES = ["north", "south", "east", "west", "up", "down"];
  var MINECRAFT = {
    preset: "minecraft",
    pixelGrid: 16,
    blockMeters: 1,
    texelsPerMeter: 16,
    voxelCell: 0.0625,
    skinLayout: "64x64",
    maxBones: 33,
    snapToGrid: true
  };
  var CONTINUUUUM = {
    preset: "continuuuum",
    pixelGrid: 16,
    blockMeters: 1,
    texelsPerMeter: 16,
    voxelCell: 0.0625,
    skinLayout: "custom",
    maxBones: 256,
    snapToGrid: false
  };

  function el(tag, attrs, html) {
    var n = document.createElement(tag);
    if (attrs) {
      Object.keys(attrs).forEach(function (k) {
        if (k === "className") n.className = attrs[k];
        else n.setAttribute(k, attrs[k]);
      });
    }
    if (html != null) n.innerHTML = html;
    return n;
  }

  function applyGranInputs(root, g) {
    root.querySelector("[data-g=pixelGrid]").value = g.pixelGrid;
    root.querySelector("[data-g=blockMeters]").value = g.blockMeters;
    root.querySelector("[data-g=blocksPerMeter]").value =
      g.blockMeters ? (1 / g.blockMeters).toFixed(4).replace(/\.?0+$/, "") : 1;
    root.querySelector("[data-g=texelsPerMeter]").value = g.texelsPerMeter;
    root.querySelector("[data-g=voxelCell]").value = g.voxelCell;
    root.querySelector("[data-g=skinLayout]").value = g.skinLayout;
    root.querySelector("[data-g=maxBones]").value = g.maxBones;
    root.querySelector("[data-g=snapToGrid]").checked = !!g.snapToGrid;
    markPreset(root);
  }

  function readGran(root) {
    var g = {
      pixelGrid: Number(root.querySelector("[data-g=pixelGrid]").value),
      blockMeters: Number(root.querySelector("[data-g=blockMeters]").value),
      texelsPerMeter: Number(root.querySelector("[data-g=texelsPerMeter]").value),
      voxelCell: Number(root.querySelector("[data-g=voxelCell]").value),
      skinLayout: root.querySelector("[data-g=skinLayout]").value,
      maxBones: Number(root.querySelector("[data-g=maxBones]").value),
      snapToGrid: root.querySelector("[data-g=snapToGrid]").checked
    };
    if (g.blockMeters <= 0) g.blockMeters = 1;
    g.blocksPerMeter = 1 / g.blockMeters;
    var same = function (p) {
      return (
        g.pixelGrid === p.pixelGrid &&
        Math.abs(g.blockMeters - p.blockMeters) < 1e-6 &&
        g.texelsPerMeter === p.texelsPerMeter &&
        Math.abs(g.voxelCell - p.voxelCell) < 1e-6 &&
        g.skinLayout === p.skinLayout &&
        g.maxBones === p.maxBones &&
        g.snapToGrid === p.snapToGrid
      );
    };
    g.preset = same(MINECRAFT) ? "minecraft" : same(CONTINUUUUM) ? "continuuuum" : "custom";
    return g;
  }

  function markPreset(root) {
    var g = readGran(root);
    var lab = root.querySelector("[data-g=presetLabel]");
    if (lab) lab.textContent = "preset=" + g.preset;
    return g;
  }

  function mount(host, opts) {
    opts = opts || {};
    host.innerHTML = "";
    host.classList.add("itm-sheet");
    var gran = el("div", { className: "itm-gran-wrap" });
    gran.innerHTML =
      "<h3>Voxel conversion</h3>" +
      "<p class=\"wa-meta\">Spatial — not webcam timeline ticks. Minecraft: 1 block = 1 m.</p>" +
      "<p><button type=\"button\" data-act=\"mc\">Minecraft</button> " +
      "<button type=\"button\" data-act=\"cc\">Continuuuum</button> " +
      "<span class=\"wa-meta\" data-g=\"presetLabel\">preset=minecraft</span></p>" +
      "<div class=\"itm-gran\">" +
      "<label>pixelGrid <input data-g=\"pixelGrid\" type=\"number\" min=\"8\" max=\"64\" value=\"16\" /></label>" +
      "<label>blockMeters <input data-g=\"blockMeters\" type=\"number\" step=\"0.01\" value=\"1\" /></label>" +
      "<label>blocksPerMeter <input data-g=\"blocksPerMeter\" type=\"number\" step=\"0.01\" value=\"1\" /></label>" +
      "<label>texelsPerMeter <input data-g=\"texelsPerMeter\" type=\"number\" value=\"16\" /></label>" +
      "<label>voxelCell <input data-g=\"voxelCell\" type=\"number\" step=\"0.001\" value=\"0.0625\" /></label>" +
      "<label>skinLayout <select data-g=\"skinLayout\"><option value=\"64x64\">64×64</option>" +
      "<option value=\"64x32\">64×32</option><option value=\"custom\">custom</option></select></label>" +
      "<label>maxBones <input data-g=\"maxBones\" type=\"number\" min=\"1\" value=\"33\" /></label>" +
      "<label>snapToGrid <input data-g=\"snapToGrid\" type=\"checkbox\" checked /></label>" +
      "<label>assignment <select data-g=\"assignment\"><option value=\"mediapipe\">mediapipe</option>" +
      "<option value=\"mocapanything\">mocapanything</option><option value=\"custom\">custom</option></select></label>" +
      "<label>Modly prompt <input data-g=\"prompt\" placeholder=\"low-poly character\" /></label>" +
      "<label>steps <input data-g=\"steps\" type=\"number\" value=\"20\" min=\"1\" /></label>" +
      "<label>mesh format <select data-g=\"meshFormat\"><option value=\"glb\">glb</option>" +
      "<option value=\"obj\">obj</option></select></label>" +
      "</div>";
    host.appendChild(gran);

    var axis = el("div", { className: "itm-axis" });
    axis.innerHTML = "<h3>Axis-dependent art (PixelLight faces)</h3>";
    FACES.forEach(function (face) {
      var box = el("div", { className: "itm-face", "data-face": face });
      box.innerHTML =
        "<h4>" + face + "</h4>" +
        "<label>image <input type=\"file\" data-face-img=\"" + face + "\" accept=\"image/*\" /></label>" +
        "<label>mask <input type=\"file\" data-face-mask=\"" + face + "\" accept=\"image/*\" /></label>" +
        "<label>UV origin X <input data-face-uvx=\"" + face + "\" type=\"number\" step=\"0.01\" value=\"0\" /></label>" +
        "<label>UV origin Y <input data-face-uvy=\"" + face + "\" type=\"number\" step=\"0.01\" value=\"0\" /></label>" +
        "<label>flip U <input type=\"checkbox\" data-face-flipu=\"" + face + "\" /></label>" +
        "<label>flip V <input type=\"checkbox\" data-face-flipv=\"" + face + "\" /></label>" +
        "<label>PixelLight fold <input type=\"checkbox\" data-face-fold=\"" + face + "\" /></label>";
      axis.appendChild(box);
    });
    host.appendChild(axis);

    gran.querySelector("[data-act=mc]").onclick = function () {
      applyGranInputs(host, MINECRAFT);
      if (opts.onChange) opts.onChange(values(host));
    };
    gran.querySelector("[data-act=cc]").onclick = function () {
      applyGranInputs(host, CONTINUUUUM);
      if (opts.onChange) opts.onChange(values(host));
    };
    var bm = host.querySelector("[data-g=blockMeters]");
    var bpm = host.querySelector("[data-g=blocksPerMeter]");
    bm.addEventListener("input", function () {
      var n = Number(bm.value);
      if (n > 0) bpm.value = String(1 / n);
      markPreset(host);
      if (opts.onChange) opts.onChange(values(host));
    });
    bpm.addEventListener("input", function () {
      var n = Number(bpm.value);
      if (n > 0) bm.value = String(1 / n);
      markPreset(host);
      if (opts.onChange) opts.onChange(values(host));
    });
    host.querySelectorAll("[data-g]").forEach(function (inp) {
      if (inp === bm || inp === bpm) return;
      inp.addEventListener("input", function () {
        markPreset(host);
        if (opts.onChange) opts.onChange(values(host));
      });
      inp.addEventListener("change", function () {
        markPreset(host);
        if (opts.onChange) opts.onChange(values(host));
      });
    });
    markPreset(host);
    return host;
  }

  function axisArt(root) {
    var out = {};
    FACES.forEach(function (face) {
      out[face] = {
        uvOriginX: Number(root.querySelector("[data-face-uvx=\"" + face + "\"]").value) || 0,
        uvOriginY: Number(root.querySelector("[data-face-uvy=\"" + face + "\"]").value) || 0,
        flipU: root.querySelector("[data-face-flipu=\"" + face + "\"]").checked,
        flipV: root.querySelector("[data-face-flipv=\"" + face + "\"]").checked,
        fold: root.querySelector("[data-face-fold=\"" + face + "\"]").checked
      };
    });
    return out;
  }

  function appendFaceFiles(fd, root) {
    FACES.forEach(function (face) {
      var img = root.querySelector("[data-face-img=\"" + face + "\"]");
      var mask = root.querySelector("[data-face-mask=\"" + face + "\"]");
      if (img && img.files && img.files[0]) fd.append("face_" + face, img.files[0]);
      if (mask && mask.files && mask.files[0]) fd.append("mask_" + face, mask.files[0]);
    });
  }

  function values(root) {
    var g = readGran(root);
    return {
      granularity: g,
      assignment: root.querySelector("[data-g=assignment]").value,
      prompt: root.querySelector("[data-g=prompt]").value,
      steps: Number(root.querySelector("[data-g=steps]").value) || 20,
      meshFormat: root.querySelector("[data-g=meshFormat]").value,
      axisArt: axisArt(root)
    };
  }

  global.ItmSheet = {
    FACES: FACES,
    MINECRAFT: MINECRAFT,
    CONTINUUUUM: CONTINUUUUM,
    mount: mount,
    values: values,
    applyGranularity: applyGranInputs,
    appendFaceFiles: appendFaceFiles
  };
})(window);
