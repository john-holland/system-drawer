(function () {
  'use strict';

  if (window.ContinuuuumNav) {
    ContinuuuumNav.mount({ root: '#continuuuum-nav-root', app: 'chat-lexicon' });
  }

  var words = [];

  function headers() {
    var h = { 'Content-Type': 'application/json' };
    if (window.ContinuuuumUserSession) {
      var extra = ContinuuuumUserSession.getHeaders({ 'Content-Type': 'application/json' });
      Object.keys(extra).forEach(function (k) { h[k] = extra[k]; });
    }
    return h;
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function renderWords() {
    document.getElementById('words').innerHTML = words.map(function (w, i) {
      return '<tr><td>' + esc(w.id) + '</td><td>' + esc(w.text) +
        '</td><td><button type="button" data-i="' + i + '">Remove</button></td></tr>';
    }).join('') || '<tr><td colspan="3">No words</td></tr>';
  }

  document.getElementById('words').onclick = function (ev) {
    var btn = ev.target.closest('button[data-i]');
    if (!btn) return;
    words.splice(parseInt(btn.getAttribute('data-i'), 10), 1);
    renderWords();
  };

  document.getElementById('btn-add').onclick = function () {
    var t = document.getElementById('new-word').value.trim();
    if (!t) return;
    words.push({ id: t.toLowerCase().replace(/[^a-z0-9_-]+/g, '-'), text: t, lemmaEntryId: null });
    document.getElementById('new-word').value = '';
    renderWords();
  };

  function applyDoc(doc) {
    document.getElementById('compose-mode').value = doc.composeMode || 'preview';
    words = ((doc.lexicon || {}).words) || [];
    var r = doc.historyRetention || {};
    document.getElementById('hot-bytes').value = r.hotMaxBytes || 52428800;
    document.getElementById('hot-count').value = r.hotMaxMessages || '';
    document.getElementById('hot-age').value = r.hotMaxAgeDays || '';
    document.getElementById('wh-bytes').value = r.warehouseMaxBytes == null ? '' : r.warehouseMaxBytes;
    document.getElementById('wh-count').value = r.warehouseMaxMessages || '';
    document.getElementById('wh-age').value = r.warehouseMaxAgeDays || '';
    document.getElementById('wh-keep').checked = r.warehouseKeepAfterHotTruncate !== false;
    renderWords();
  }

  document.getElementById('btn-load').onclick = function () {
    var pid = document.getElementById('product-id').value.trim();
    fetch('/api/chat/lexicon?productId=' + encodeURIComponent(pid), { headers: headers() })
      .then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        if (res.status >= 400) {
          document.getElementById('status').textContent = res.body.error || 'failed';
          document.getElementById('status').className = 'err';
          return;
        }
        applyDoc(res.body);
        document.getElementById('status').textContent = 'Loaded.';
        document.getElementById('status').className = 'hint';
      });
  };

  document.getElementById('btn-save').onclick = function () {
    var pid = document.getElementById('product-id').value.trim();
    var whBytes = document.getElementById('wh-bytes').value;
    fetch('/api/chat/lexicon?productId=' + encodeURIComponent(pid), {
      method: 'PUT',
      headers: headers(),
      body: JSON.stringify({
        composeMode: document.getElementById('compose-mode').value,
        lexicon: { words: words },
        historyRetention: {
          hotMaxBytes: Number(document.getElementById('hot-bytes').value) || 52428800,
          hotMaxMessages: document.getElementById('hot-count').value ? Number(document.getElementById('hot-count').value) : null,
          hotMaxAgeDays: document.getElementById('hot-age').value ? Number(document.getElementById('hot-age').value) : null,
          warehouseMaxBytes: whBytes === '' ? 'keep' : Number(whBytes),
          warehouseMaxMessages: document.getElementById('wh-count').value ? Number(document.getElementById('wh-count').value) : null,
          warehouseMaxAgeDays: document.getElementById('wh-age').value ? Number(document.getElementById('wh-age').value) : null,
          warehouseKeepAfterHotTruncate: document.getElementById('wh-keep').checked,
        },
      }),
    }).then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        if (res.status >= 400) {
          document.getElementById('status').textContent = res.body.error || 'failed';
          document.getElementById('status').className = 'err';
          return;
        }
        applyDoc(res.body);
        document.getElementById('status').textContent = 'Saved.';
        document.getElementById('status').className = 'hint';
      });
  };

  renderWords();
})();
