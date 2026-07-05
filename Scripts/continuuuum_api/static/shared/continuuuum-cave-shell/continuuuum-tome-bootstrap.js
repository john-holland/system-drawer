/**
 * Bootstrap a Continuuuum page with Cave shell + optional page init hook.
 */
(function (global) {
  'use strict';

  function mountPage(config) {
    config = config || {};
    if (!global.ContinuuuumCaveShell) {
      console.warn('ContinuuuumCaveShell not loaded');
      return Promise.resolve(null);
    }
    var shell = global.ContinuuuumCaveShell.init({
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

  global.ContinuuuumTomeBootstrap = { mountPage: mountPage };
})(typeof window !== 'undefined' ? window : globalThis);
