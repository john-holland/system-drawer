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
    opts: null,
    phase: 'idle',
    accumulatedMs: 0,
    segmentStartedAt: 0,
    timerId: null,
  };

  function pad2(n) {
    return (n < 10 ? '0' : '') + n;
  }

  function formatDuration(ms) {
    var total = Math.max(0, Math.floor((ms || 0) / 1000));
    var h = Math.floor(total / 3600);
    var m = Math.floor((total % 3600) / 60);
    var s = total % 60;
    if (h > 0) return h + ':' + pad2(m) + ':' + pad2(s);
    return pad2(m) + ':' + pad2(s);
  }

  function elapsedMs() {
    if (active.phase === 'recording' && active.segmentStartedAt) {
      return active.accumulatedMs + (Date.now() - active.segmentStartedAt);
    }
    return active.accumulatedMs;
  }

  function setUploadStatus(msg) {
    var el = document.getElementById('tr-upload-status');
    if (el) el.textContent = msg;
  }

  function updateTimerLabel() {
    var value = document.getElementById('tr-rec-timer-value');
    if (value) value.textContent = formatDuration(elapsedMs());
    var live = document.getElementById('tr-rec-live');
    if (live) live.hidden = active.phase === 'idle';
    var dot = document.getElementById('tr-rec-live-dot');
    if (dot) dot.hidden = active.phase !== 'recording';
  }

  function startTimer() {
    stopTimer();
    updateTimerLabel();
    active.timerId = setInterval(updateTimerLabel, 250);
  }

  function stopTimer() {
    if (active.timerId) {
      clearInterval(active.timerId);
      active.timerId = null;
    }
  }

  function attachPreview() {
    var well = document.getElementById('tr-rec-preview-well');
    var video = document.getElementById('tr-rec-preview');
    if (!well || !video) return;
    if (active.mediaKind === 'video' && active.stream && active.phase !== 'idle') {
      well.hidden = false;
      if (video.srcObject !== active.stream) video.srcObject = active.stream;
      video.play().catch(function () { /* autoplay may be blocked until gesture */ });
    } else {
      well.hidden = true;
      video.srcObject = null;
    }
  }

  function sessionEnded() {
    var snap = active.opts && active.opts.snapshot;
    return !!(snap && snap.session && snap.session.status !== 'active');
  }

  function syncControls() {
    var ended = sessionEnded();
    var idle = active.phase === 'idle';
    var recording = active.phase === 'recording';
    var paused = active.phase === 'paused';
    var audio = document.getElementById('tr-rec-audio');
    var video = document.getElementById('tr-rec-video');
    var stop = document.getElementById('tr-rec-stop');
    var save = document.getElementById('tr-rec-save');
    var more = document.getElementById('tr-rec-more');
    var clear = document.getElementById('tr-rec-clear');
    if (audio) audio.disabled = ended || !idle;
    if (video) video.disabled = ended || !idle;
    if (stop) stop.disabled = !recording;
    if (save) save.disabled = idle;
    if (more) more.disabled = ended || !paused;
    if (clear) clear.disabled = idle;
    attachPreview();
    updateTimerLabel();
  }

  function uploadPart(blob, ctx) {
    var fd = new FormData();
    fd.append('document_type', ctx.mediaKind);
    fd.append('file', blob, 'part-' + ctx.partIndex + '.webm');
    fd.append('type_metadata', JSON.stringify({
      table_read_session_id: ctx.sessionId,
      recording_id: ctx.recordingId,
      part_index: ctx.partIndex,
      participant_user_id: global.ContinuuuumUserSession ? global.ContinuuuumUserSession.getUserId() : 'anonymous',
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
    attachPreview();
  }

  function waitForUploads() {
    if (!active.uploadPending) return Promise.resolve();
    return new Promise(function (resolve) {
      var n = 0;
      var id = setInterval(function () {
        n += 1;
        if (!active.uploadPending || n > 80) {
          clearInterval(id);
          resolve();
        }
      }, 100);
    });
  }

  function bindRecorder(rec) {
    rec.ondataavailable = function (ev) {
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
  }

  function beginMediaRecorder() {
    var mime = active.mediaKind === 'video' ? 'video/webm' : 'audio/webm';
    if (!MediaRecorder.isTypeSupported(mime)) mime = '';
    active.recorder = new MediaRecorder(active.stream, mime ? { mimeType: mime } : undefined);
    bindRecorder(active.recorder);
    active.recorder.start(30000);
    active.segmentStartedAt = Date.now();
    active.phase = 'recording';
    startTimer();
    syncControls();
  }

  function startRecording(opts) {
    opts = opts || active.opts || {};
    active.opts = opts;
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
        active.accumulatedMs = 0;
        attachPreview();
        return opts.api('/table-read/sessions/' + encodeURIComponent(opts.sessionId) + '/recordings', {
          method: 'POST',
          body: JSON.stringify({ mediaKind: mediaKind }),
        });
      })
      .then(function (rec) {
        active.recordingId = rec.id;
        beginMediaRecorder();
        if (active.onStatus) active.onStatus('Recording ' + mediaKind + '…');
      })
      .catch(function (err) {
        stopTracks();
        resetLocal();
        throw err;
      });
  }

  function pauseRecorder() {
    return new Promise(function (resolve) {
      if (!active.recorder || active.recorder.state === 'inactive') {
        if (active.phase === 'recording') {
          active.accumulatedMs = elapsedMs();
          active.segmentStartedAt = 0;
          active.phase = 'paused';
          stopTimer();
          updateTimerLabel();
        }
        syncControls();
        return resolve();
      }
      var rec = active.recorder;
      rec.onstop = function () {
        active.recorder = null;
        active.accumulatedMs = elapsedMs();
        active.segmentStartedAt = 0;
        active.phase = 'paused';
        stopTimer();
        updateTimerLabel();
        syncControls();
        resolve();
      };
      try { rec.requestData(); } catch (_) { /* ignore */ }
      rec.stop();
    });
  }

  function resumeRecording() {
    if (active.phase !== 'paused' || !active.stream || !active.recordingId) {
      return Promise.reject(new Error('No paused clip'));
    }
    beginMediaRecorder();
    if (active.onStatus) active.onStatus('Recording ' + active.mediaKind + '…');
    return Promise.resolve();
  }

  function finalizeRecording() {
    if (!active.recordingId || !active.sessionId || !active.api) return Promise.resolve();
    return active.api('/table-read/sessions/' + encodeURIComponent(active.sessionId) +
      '/recordings/' + encodeURIComponent(active.recordingId) + '/finalize', { method: 'POST' })
      .catch(function () { /* best effort */ });
  }

  function discardRecording() {
    if (!active.recordingId || !active.sessionId || !active.api) return Promise.resolve();
    return active.api('/table-read/sessions/' + encodeURIComponent(active.sessionId) +
      '/recordings/' + encodeURIComponent(active.recordingId) + '/discard', { method: 'POST' })
      .catch(function () { /* best effort */ });
  }

  function resetLocal() {
    stopTimer();
    active.recorder = null;
    active.recordingId = null;
    active.partIndex = 0;
    active.accumulatedMs = 0;
    active.segmentStartedAt = 0;
    active.phase = 'idle';
    active.mediaKind = 'audio';
    updateTimerLabel();
    syncControls();
  }

  function stopRecording() {
    return pauseRecorder().then(function () {
      stopTracks();
      return finalizeRecording();
    }).then(function () {
      resetLocal();
    });
  }

  function saveClip() {
    return pauseRecorder()
      .then(waitForUploads)
      .then(function () {
        stopTracks();
        return finalizeRecording();
      })
      .then(function () {
        resetLocal();
        if (active.onStatus) active.onStatus('Clip saved');
        if (global.ContinuuuumTableRead) global.ContinuuuumTableRead.refreshSession();
      });
  }

  function clearClip() {
    return new Promise(function (resolve) {
      if (!active.recorder || active.recorder.state === 'inactive') return resolve();
      var rec = active.recorder;
      rec.onstop = function () {
        active.recorder = null;
        resolve();
      };
      rec.stop();
    }).then(function () {
      return discardRecording();
    }).then(function () {
      stopTracks();
      resetLocal();
      if (active.onStatus) active.onStatus('Clip cleared');
      if (global.ContinuuuumTableRead) global.ContinuuuumTableRead.refreshSession();
    });
  }

  function syncList(opts) {
    var list = document.getElementById('tr-rec-list');
    if (!list) return;
    list.innerHTML = '';
    (opts.snapshot && opts.snapshot.recordings || []).forEach(function (r) {
      var li = document.createElement('li');
      li.textContent = r.userId + ' · ' + r.mediaKind + ' · ' + r.status + ' · ' + r.partCount + ' parts';
      list.appendChild(li);
    });
  }

  function ensurePanel(el) {
    if (el.querySelector('#tr-rec-preview')) return;
    el.innerHTML =
      '<h3>Recording</h3>' +
      '<div class="tr-rec-live" id="tr-rec-live" hidden>' +
        '<div class="tr-rec-preview-well" id="tr-rec-preview-well" hidden>' +
          '<video id="tr-rec-preview" autoplay muted playsinline></video>' +
        '</div>' +
        '<p class="tr-rec-timer" id="tr-rec-timer">' +
          '<span class="tr-rec-live-dot" id="tr-rec-live-dot" hidden>●</span>' +
          '<span id="tr-rec-timer-value">00:00</span>' +
        '</p>' +
      '</div>' +
      '<div class="tr-rec-buttons">' +
        '<button type="button" id="tr-rec-audio">Record audio</button>' +
        '<button type="button" id="tr-rec-video">Record video</button>' +
        '<button type="button" id="tr-rec-stop" disabled>Stop</button>' +
        '<button type="button" id="tr-rec-save" disabled>Save clip</button>' +
        '<button type="button" id="tr-rec-more" disabled>Record more</button>' +
        '<button type="button" id="tr-rec-clear" disabled>Clear clip</button>' +
      '</div>' +
      '<p id="tr-upload-status" class="tr-muted"></p>' +
      '<ul id="tr-rec-list" class="tr-rec-list"></ul>';
  }

  function bindPanelOnce(el) {
    if (el.getAttribute('data-tr-rec-bound') === '1') return;
    el.setAttribute('data-tr-rec-bound', '1');
    document.getElementById('tr-rec-audio').onclick = function () {
      startRecording(Object.assign({}, active.opts, { mediaKind: 'audio' }))
        .catch(function (e) { if (active.onStatus) active.onStatus(e.message, true); });
    };
    document.getElementById('tr-rec-video').onclick = function () {
      startRecording(Object.assign({}, active.opts, { mediaKind: 'video' }))
        .catch(function (e) { if (active.onStatus) active.onStatus(e.message, true); });
    };
    document.getElementById('tr-rec-stop').onclick = function () {
      pauseRecorder().then(function () {
        if (active.onStatus) active.onStatus('Take paused — save, record more, or clear');
      });
    };
    document.getElementById('tr-rec-save').onclick = function () {
      saveClip().catch(function (e) { if (active.onStatus) active.onStatus(e.message, true); });
    };
    document.getElementById('tr-rec-more').onclick = function () {
      resumeRecording().catch(function (e) { if (active.onStatus) active.onStatus(e.message, true); });
    };
    document.getElementById('tr-rec-clear').onclick = function () {
      clearClip().catch(function (e) { if (active.onStatus) active.onStatus(e.message, true); });
    };
  }

  function renderPanel(el, opts) {
    if (!el) return;
    active.opts = opts;
    active.api = opts.api;
    active.headers = opts.headers;
    active.onStatus = opts.onStatus;
    if (opts.sessionId) active.sessionId = opts.sessionId;
    ensurePanel(el);
    bindPanelOnce(el);
    syncList(opts);
    syncControls();
  }

  function flushOnLeave() {
    if (active.recorder && active.recorder.state === 'recording') {
      try { active.recorder.requestData(); } catch (_) { /* ignore */ }
    }
  }

  global.ContinuuuumTableReadRecorder = {
    startRecording: startRecording,
    stopRecording: stopRecording,
    saveClip: saveClip,
    clearClip: clearClip,
    resumeRecording: resumeRecording,
    formatDuration: formatDuration,
    renderPanel: renderPanel,
    flushOnLeave: flushOnLeave,
  };
})(typeof window !== 'undefined' ? window : globalThis);
