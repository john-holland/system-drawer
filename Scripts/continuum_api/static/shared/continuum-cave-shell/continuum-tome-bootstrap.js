/**
 * Bootstrap a Continuum page with Cave shell + optional page init hook.
 */
(function (global) {
  'use strict';

  function mountPage(config) {
    config = config || {};
    if (!global.ContinuumCaveShell) {
      console.warn('ContinuumCaveShell not loaded');
      return Promise.resolve(null);
    }
    var shell = global.ContinuumCaveShell.init({
      tomeId: config.tomeId,
      presence: config.presence !== false,
    });
    if (typeof config.onReady === 'function') {
      try {
        config.onReady(shell);
      } catch (e) {
        console.error(e);
      }
    }
    return Promise.resolve(shell);
  }

  global.ContinuumTomeBootstrap = { mountPage: mountPage };
})(typeof window !== 'undefined' ? window : globalThis);
