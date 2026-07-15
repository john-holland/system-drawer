/** Continuuuum settings — localStorage-backed, group-name panels. */
(function (global) {
  'use strict';

  const STORAGE_KEY = 'continuuuumSettings';

  const AUTO_ADD_TYPES = [
    'builtin',
    'prefab',
    'localization',
    'mod_slot',
    'prompt_placeholder',
    'new_lemma',
  ];

  const DEFAULT_SCRIPT_OUTPUT = {
    autoAddPriority: AUTO_ADD_TYPES.slice(),
    newLemmaRequired: false,
  };

  const GROUPS = [
    { id: 'script-output', label: 'Script Output', enabled: true },
    { id: 'lemma-library', label: 'Lemma Library', enabled: true },
    { id: 'table-read', label: 'Table Read', enabled: false },
  ];

  const DEFAULT_LEMMA_LIBRARY = {
    lmStudioBaseUrl: 'http://localhost:1234/v1',
    defaultModelId: 'mistralai/codestral-22b-v0.1',
    maxConcurrentBuilds: 1,
    batchOutputDir: 'Library/LemmaBuild/batches',
    defaultEngine: 'unity',
  };

  function deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  function defaultSettings() {
    return {
      scriptOutput: deepClone(DEFAULT_SCRIPT_OUTPUT),
      lemmaLibrary: deepClone(DEFAULT_LEMMA_LIBRARY),
    };
  }

  function normalizePriority(list) {
    const out = [];
    const seen = new Set();
    (list || []).forEach((t) => {
      if (AUTO_ADD_TYPES.includes(t) && !seen.has(t)) {
        out.push(t);
        seen.add(t);
      }
    });
    AUTO_ADD_TYPES.forEach((t) => {
      if (!seen.has(t)) {
        out.push(t);
        seen.add(t);
      }
    });
    return out.slice(0, AUTO_ADD_TYPES.length);
  }

  function loadRaw() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return defaultSettings();
      const parsed = JSON.parse(raw);
      return {
        ...defaultSettings(),
        ...parsed,
        scriptOutput: {
          ...DEFAULT_SCRIPT_OUTPUT,
          ...(parsed.scriptOutput || {}),
          autoAddPriority: normalizePriority(parsed.scriptOutput?.autoAddPriority),
        },
        lemmaLibrary: {
          ...DEFAULT_LEMMA_LIBRARY,
          ...(parsed.lemmaLibrary || {}),
        },
      };
    } catch (_) {
      return defaultSettings();
    }
  }

  function saveRaw(settings) {
    const next = {
      ...defaultSettings(),
      ...settings,
      scriptOutput: {
        ...DEFAULT_SCRIPT_OUTPUT,
        ...(settings.scriptOutput || {}),
        autoAddPriority: normalizePriority(settings.scriptOutput?.autoAddPriority),
      },
      lemmaLibrary: {
        ...DEFAULT_LEMMA_LIBRARY,
        ...(settings.lemmaLibrary || {}),
      },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    return next;
  }

  /** Swap duplicate type assignments between two slot indices (0-based). */
  function swapPrioritySlots(priority, slotIndex, newType) {
    const list = normalizePriority(priority);
    const other = list.indexOf(newType);
    if (other >= 0 && other !== slotIndex) {
      const prev = list[slotIndex];
      list[slotIndex] = newType;
      list[other] = prev;
    } else {
      list[slotIndex] = newType;
    }
    return normalizePriority(list);
  }

  function movePrioritySlot(priority, fromIndex, direction) {
    const list = normalizePriority(priority);
    const toIndex = fromIndex + direction;
    if (toIndex < 0 || toIndex >= list.length) return list;
    const tmp = list[fromIndex];
    list[fromIndex] = list[toIndex];
    list[toIndex] = tmp;
    return list;
  }

  function typeLabel(typeId) {
    const labels = {
      builtin: 'Built-in lemma',
      prefab: 'Prefab / USC asset',
      localization: 'Localization',
      mod_slot: 'Mayor Dog mod slot',
      prompt_placeholder: 'Prompt placeholder',
      new_lemma: 'New lemma',
    };
    return labels[typeId] || typeId;
  }

  const ContinuuuumSettings = {
    STORAGE_KEY,
    AUTO_ADD_TYPES,
    GROUPS,
    DEFAULT_LEMMA_LIBRARY,
    defaultSettings,
    normalizePriority,
    swapPrioritySlots,
    movePrioritySlot,
    typeLabel,
    load() {
      return loadRaw();
    },
    save(settings) {
      return saveRaw(settings);
    },
    get(groupKey) {
      const all = loadRaw();
      return all[groupKey] || null;
    },
    getScriptOutput() {
      return loadRaw().scriptOutput;
    },
    saveScriptOutput(scriptOutput) {
      const all = loadRaw();
      all.scriptOutput = {
        ...DEFAULT_SCRIPT_OUTPUT,
        ...scriptOutput,
        autoAddPriority: normalizePriority(scriptOutput?.autoAddPriority),
      };
      return saveRaw(all).scriptOutput;
    },
    getLemmaLibrary() {
      return loadRaw().lemmaLibrary;
    },
    saveLemmaLibrary(lemmaLibrary) {
      const all = loadRaw();
      all.lemmaLibrary = {
        ...DEFAULT_LEMMA_LIBRARY,
        ...lemmaLibrary,
      };
      return saveRaw(all).lemmaLibrary;
    },
  };

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = ContinuuuumSettings;
  } else {
    global.ContinuuuumSettings = ContinuuuumSettings;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this);
