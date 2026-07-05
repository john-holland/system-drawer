(function (global) {
  'use strict';

  function init() {
    ContinuuuumNav.mount({ app: 'sql-viewer', theme: 'dark' });
    ContinuuuumTomeBootstrap.mountPage({
      tomeId: 'sql-viewer-tome',
      onReady: function (shell) {
        ContinuuuumSqlViewerTome.mount(document.getElementById('sv-root'), shell);
      },
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  global.ContinuuuumSqlViewer = { init: init };
})(typeof window !== 'undefined' ? window : globalThis);
