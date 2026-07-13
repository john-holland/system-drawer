/** Client helpers for bulk auto-add single lemmas on script output. */
(function (global) {
  'use strict';

  function prioritySummary(settings) {
    const CS = global.ContinuuuumSettings;
    const order = CS ? CS.normalizePriority(settings?.autoAddPriority) : (settings?.autoAddPriority || []);
    const lines = order.map((t, i) => `${i + 1}. ${CS ? CS.typeLabel(t) : t}`);
    const req = settings?.newLemmaRequired
      ? '\nRequire new lemma when no other match: yes'
      : '\nRequire new lemma when no other match: no';
    return lines.join('\n') + req;
  }

  async function autoAddAllSingleLemmas(ctx) {
    const CS = global.ContinuuuumSettings;
    const settings = CS ? CS.getScriptOutput() : {
      autoAddPriority: ['builtin', 'prefab', 'localization', 'mod_slot', 'prompt_placeholder', 'new_lemma'],
      newLemmaRequired: false,
    };
    const draftId = ctx.draftId || ctx.activeId;
    if (!draftId) throw new Error('Load a draft first.');
    const scriptText = ctx.getScriptText ? ctx.getScriptText() : '';
    const summary = prioritySummary(settings);
    const ok = global.confirm(
      'Auto-add bindings for spans with a single unambiguous suggestion?\n\nPriority:\n' + summary,
    );
    if (!ok) return null;

    const api = ctx.api;
    const result = await api(`/drafts/episodes/${encodeURIComponent(draftId)}/auto-add-single-lemmas`, {
      method: 'POST',
      body: JSON.stringify({
        scriptText,
        settings: {
          autoAddPriority: settings.autoAddPriority,
          newLemmaRequired: !!settings.newLemmaRequired,
        },
      }),
    });

    if (ctx.loadClauseBindings) await ctx.loadClauseBindings(draftId);
    if (ctx.refreshEditor) await ctx.refreshEditor();
    if (ctx.onComplete) await ctx.onComplete(result);
    if (ctx.setStatus) {
      const by = result.byType || {};
      const parts = Object.keys(by).map((k) => `${k}: ${by[k]}`);
      ctx.setStatus(
        `Auto-add complete — added ${result.added || 0}, skipped ${result.skipped || 0}` +
        (parts.length ? ` (${parts.join(', ')})` : ''),
      );
    }
    return result;
  }

  const ContinuuuumScriptLemmaAuto = {
    prioritySummary,
    autoAddAllSingleLemmas,
  };

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = ContinuuuumScriptLemmaAuto;
  } else {
    global.ContinuuuumScriptLemmaAuto = ContinuuuumScriptLemmaAuto;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this);
