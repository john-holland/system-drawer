/**
 * Shell-agnostic Unity ↔ webtop message pump.
 * @see Scripts/telecom/docs/WEBTOP_BRIDGE.md
 */

const pending = new Map();
let requestSeq = 0;
let deviceContext = {};
let apiBase = '/api/telecom';

const listeners = {
  deviceContext: new Set(),
  spatialBinding: new Set(),
  bridgeResponse: new Set(),
};

function emit(type, payload) {
  (listeners[type] || new Set()).forEach((fn) => fn(payload));
}

export function onMessage(type, fn) {
  if (!listeners[type]) listeners[type] = new Set();
  listeners[type].add(fn);
  return () => listeners[type].delete(fn);
}

export function getDeviceContext() {
  return { ...deviceContext };
}

export function setApiBase(base) {
  apiBase = base.replace(/\/$/, '');
}

export function postToHost(message) {
  const payload = JSON.stringify(message);
  if (window.vuplex && window.vuplex.postMessage) {
    window.vuplex.postMessage(payload);
    return;
  }
  if (window.unityBridge && typeof window.unityBridge.send === 'function') {
    window.unityBridge.send(payload);
    return;
  }
  if (window.parent !== window) {
    window.parent.postMessage(payload, '*');
  }
}

export function handleHostMessage(raw) {
  let msg;
  try {
    msg = typeof raw === 'string' ? JSON.parse(raw) : raw;
  } catch {
    return;
  }
  if (!msg || !msg.action) return;
  if (msg.action === 'deviceContext') {
    deviceContext = msg.payload || {};
    emit('deviceContext', deviceContext);
    return;
  }
  if (msg.action === 'spatialBinding') {
    emit('spatialBinding', msg.payload || {});
    return;
  }
  if (msg.action === 'bridgeResponse' && msg.requestId) {
    const entry = pending.get(msg.requestId);
    if (entry) {
      pending.delete(msg.requestId);
      if (msg.error) entry.reject(new Error(msg.error));
      else entry.resolve(msg.payload);
    }
    emit('bridgeResponse', msg);
  }
}

export function apiRequest(method, path, body) {
  const inUnity = !!(window.unityBridge || window.vuplex);
  if (inUnity) {
    const requestId = `r${++requestSeq}`;
    return new Promise((resolve, reject) => {
      pending.set(requestId, { resolve, reject });
      postToHost({ action: 'api', requestId, method, path, body });
      setTimeout(() => {
        if (pending.has(requestId)) {
          pending.delete(requestId);
          reject(new Error('bridge timeout'));
        }
      }, 15000);
    });
  }
  return fetch(`${apiBase}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body != null ? JSON.stringify(body) : undefined,
  }).then(async (r) => {
    const data = await r.json().catch(() => ({}));
    if (!r.ok) throw new Error(data.error || r.statusText);
    return data;
  });
}

export function ring(payload) {
  postToHost({ action: 'ring', payload });
}

export function notifyVisual(payload) {
  postToHost({ action: 'notifyVisual', payload });
}

export function cctvFrame(payload) {
  postToHost({ action: 'cctvFrame', payload });
}

/** Publish focused .win frame centroids (CSS px) for Unity eyes / webtop BT. */
export function publishWindowCentroids(windows) {
  const list = (windows || []).map((w) => ({
    id: w.id || w.name || '',
    cx: w.cx,
    cy: w.cy,
    width: w.width,
    height: w.height,
  }));
  postToHost({ action: 'windowCentroids', payload: { windows: list } });
}

/** Collect getBoundingClientRect centers for .win elements. */
export function collectAndPublishWindowCentroids(root = document) {
  const nodes = root.querySelectorAll ? root.querySelectorAll('.win') : [];
  const windows = [];
  nodes.forEach((el, i) => {
    const r = el.getBoundingClientRect();
    windows.push({
      id: el.id || el.dataset.windowId || `win_${i}`,
      cx: r.left + r.width * 0.5,
      cy: r.top + r.height * 0.5,
      width: r.width,
      height: r.height,
    });
  });
  publishWindowCentroids(windows);
  return windows;
}

window.addEventListener('message', (ev) => handleHostMessage(ev.data));

export function initPump() {
  window.continuuuumTelecomPump = {
    apiRequest,
    ring,
    notifyVisual,
    cctvFrame,
    publishWindowCentroids,
    collectAndPublishWindowCentroids,
    onMessage,
    getDeviceContext,
    handleHostMessage,
  };
}
