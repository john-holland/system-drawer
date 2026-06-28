(function (global) {
  'use strict';

  function init() {
    ContinuumNav.mount({ app: 'sql-viewer', theme: 'dark' });
    ContinuumTomeBootstrap.mountPage({
      tomeId: 'sql-viewer-tome',
      onReady: function (shell) {
        ContinuumSqlViewerTome.mount(document.getElementById('sv-root'), shell);
      },
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  global.ContinuumSqlViewer = { init: init };
})(typeof window !== 'undefined' ? window : globalThis);
