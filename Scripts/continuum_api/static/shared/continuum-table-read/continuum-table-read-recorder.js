/* Table read A/V recorder with periodic USC upload */
(function (global) {
  'use strict';

  var active = {
    recorder: null,
    stream: null,
    recordingId: null,
    sessionId: null,
    mediaKind: 'audio',
    partIndex: 0,
    uploadPending: 0,
    api: null,
    headers: null,
    onStatus: null,
  };

  function setUploadStatus(msg) {
    var el = document.getElementById('tr-upload-status');
    if (el) el.textContent = msg;
  }

  function uploadPart(blob, ctx) {
    var fd = new FormData();
    fd.append('document_type', ctx.mediaKind);
    fd.append('file', blob, 'part-' + ctx.partIndex + '.webm');
    fd.append('type_metadata', JSON.stringify({
      table_read_session_id: ctx.sessionId,
      recording_id: ctx.recordingId,
      part_index: ctx.partIndex,
      participant_user_id: global.ContinuumUserSession ? global.ContinuumUserSession.getUserId() : 'anonymous',
    }));
    var hdrs = ctx.headers ? ctx.headers({}) : {};
    delete hdrs['Content-Type'];
    active.uploadPending += 1;
    setUploadStatus('Uploading part ' + ctx.partIndex + '…');
    return fetch('/api/table-read/usc-upload', { method: 'POST', headers: hdrs, body: fd })
      .then(function (r) { return r.json().then(function (d) { if (!r.ok) throw new Error(d.error || 'upload failed'); return d; }); })
      .then(function (data) {
        return ctx.api('/table-read/sessions/' + encodeURIComponent(ctx.sessionId) +
          '/recordings/' + encodeURIComponent(ctx.recordingId) + '/parts', {
          method: 'POST',
          body: JSON.stringify({ libraryDocId: data.id, partIndex: ctx.partIndex }),
        });
      })
      .finally(function () {
        active.uploadPending -= 1;
        setUploadStatus(active.uploadPending ? 'Uploading…' : 'Upload idle');
      });
  }

  function stopTracks() {
    if (active.stream) {
      active.stream.getTracks().forEach(function (t) { t.stop(); });
      active.stream = null;
    }
  }

  function finalizeRecording() {
    if (!active.recordingId || !active.sessionId || !active.api) return Promise.resolve();
    return active.api('/table-read/sessions/' + encodeURIComponent(active.sessionId) +
      '/recordings/' + encodeURIComponent(active.recordingId) + '/finalize', { method: 'POST' })
      .catch(function () { /* best effort */ });
  }

  function startRecording(opts) {
    opts = opts || {};
    var mediaKind = opts.mediaKind || 'audio';
    var video = mediaKind === 'video';
    var constraints = video ? { audio: true, video: true } : { audio: true };
    return navigator.mediaDevices.getUserMedia(constraints)
      .then(function (stream) {
        active.stream = stream;
        active.mediaKind = mediaKind;
        active.sessionId = opts.sessionId;
        active.api = opts.api;
        active.headers = opts.headers;
        active.onStatus = opts.onStatus;
        active.partIndex = 0;
        return opts.api('/table-read/sessions/' + encodeURIComponent(opts.sessionId) + '/recordings', {
          method: 'POST',
          body: JSON.stringify({ mediaKind: mediaKind }),
        });
      })
      .then(function (rec) {
        active.recordingId = rec.id;
        var mime = mediaKind === 'video' ? 'video/webm' : 'audio/webm';
        if (!MediaRecorder.isTypeSupported(mime)) mime = '';
        active.recorder = new MediaRecorder(active.stream, mime ? { mimeType: mime } : undefined);
        active.recorder.ondataavailable = function (ev) {
          if (!ev.data || !ev.data.size) return;
          var idx = active.partIndex++;
          uploadPart(ev.data, {
            sessionId: active.sessionId,
            recordingId: active.recordingId,
            mediaKind: active.mediaKind,
            partIndex: idx,
            api: active.api,
            headers: active.headers,
          }).catch(function (e) {
            if (active.onStatus) active.onStatus('Upload failed: ' + e.message, true);
          });
        };
        active.recorder.start(30000);
        if (active.onStatus) active.onStatus('Recording ' + mediaKind + '…');
      });
  }

  function stopRecording() {
    return new Promise(function (resolve) {
      if (!active.recorder || active.recorder.state === 'inactive') {
        stopTracks();
        return finalizeRecording().then(resolve);
      }
      active.recorder.onstop = function () {
        stopTracks();
        finalizeRecording().then(resolve);
      };
      try { active.recorder.requestData(); } catch (_) { /* ignore */ }
      active.recorder.stop();
      active.recorder = null;
    });
  }

  function renderPanel(el, opts) {
    if (!el) return;
    var snap = opts.snapshot;
    var ended = snap && snap.session && snap.session.status !== 'active';
    el.innerHTML =
      '<h3>Recording</h3>' +
      '<div class="tr-rec-buttons">' +
        '<button type="button" id="tr-rec-audio"' + (ended ? ' disabled' : '') + '>Record audio</button>' +
        '<button type="button" id="tr-rec-video"' + (ended ? ' disabled' : '') + '>Record video</button>' +
        '<button type="button" id="tr-rec-stop" disabled>Stop</button>' +
      '</div>' +
      '<p id="tr-upload-status" class="tr-muted"></p>' +
      '<ul id="tr-rec-list" class="tr-rec-list"></ul>';
    var list = document.getElementById('tr-rec-list');
    (snap && snap.recordings || []).forEach(function (r) {
      var li = document.createElement('li');
      li.textContent = r.userId + ' · ' + r.mediaKind + ' · ' + r.status + ' · ' + r.partCount + ' parts';
      list.appendChild(li);
    });
    document.getElementById('tr-rec-audio').onclick = function () {
      startRecording({ sessionId: opts.sessionId, api: opts.api, headers: opts.headers, onStatus: opts.onStatus, mediaKind: 'audio' })
        .then(function () { document.getElementById('tr-rec-stop').disabled = false; });
    };
    document.getElementById('tr-rec-video').onclick = function () {
      startRecording({ sessionId: opts.sessionId, api: opts.api, headers: opts.headers, onStatus: opts.onStatus, mediaKind: 'video' })
        .then(function () { document.getElementById('tr-rec-stop').disabled = false; });
    };
    document.getElementById('tr-rec-stop').onclick = function () {
      stopRecording().then(function () {
        document.getElementById('tr-rec-stop').disabled = true;
        if (opts.onStatus) opts.onStatus('Recording stopped');
        if (global.ContinuumTableRead) global.ContinuumTableRead.refreshSession();
      });
    };
  }

  function flushOnLeave() {
    if (active.recorder && active.recorder.state === 'recording') {
      try { active.recorder.requestData(); } catch (_) { /* ignore */ }
    }
  }

  global.ContinuumTableReadRecorder = {
    startRecording: startRecording,
    stopRecording: stopRecording,
    renderPanel: renderPanel,
    flushOnLeave: flushOnLeave,
  };
})(typeof window !== 'undefined' ? window : globalThis);
