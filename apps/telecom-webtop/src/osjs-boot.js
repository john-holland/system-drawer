import { initPump, onMessage, apiRequest, ring, getDeviceContext } from './telecom-message-pump.js';
import { registerApp, openWindow } from './desktop-shell.js';

initPump();

onMessage('deviceContext', (ctx) => {
  const el = document.getElementById('device-status');
  if (el) el.textContent = [ctx.phone, ctx.ipv6Full].filter(Boolean).join(' · ') || 'ready';
});

registerApp({
  id: 'TelecomDialer',
  title: 'Dialer',
  mount(container) {
    container.innerHTML = `
      <div class="dialer">
        <label>Number <input id="dial-num" placeholder="1-1-555-555-5555"></label>
        <button type="button" id="dial-call">Call</button>
        <button type="button" id="dial-discover">Discover</button>
        <pre id="dial-out"></pre>
      </div>`;
    container.querySelector('#dial-call').onclick = () => {
      const phone = container.querySelector('#dial-num').value.trim();
      ring({ direction: 'outgoing', phone });
      container.querySelector('#dial-out').textContent = 'Ringing ' + phone;
    };
    container.querySelector('#dial-discover').onclick = async () => {
      const phone = container.querySelector('#dial-num').value.trim();
      try {
        const r = await apiRequest('POST', '/discover', { phone });
        container.querySelector('#dial-out').textContent = JSON.stringify(r, null, 2);
      } catch (e) {
        container.querySelector('#dial-out').textContent = e.message;
      }
    };
  },
});

registerApp({
  id: 'TelecomTerminal',
  title: 'Terminal',
  mount(container) {
    const ctx = getDeviceContext();
    container.innerHTML = `<pre>${JSON.stringify(ctx, null, 2) || 'Waiting for device context…'}</pre>`;
    onMessage('deviceContext', (c) => {
      container.innerHTML = `<pre>${JSON.stringify(c, null, 2)}</pre>`;
    });
  },
});

registerApp({
  id: 'TelecomNetwork',
  title: 'Network',
  async mount(container) {
    container.textContent = 'Loading…';
    try {
      const nets = await apiRequest('GET', '/networks');
      const devs = await apiRequest('GET', '/devices');
      container.innerHTML = `<h3>Networks</h3><pre>${JSON.stringify(nets, null, 2)}</pre>
        <h3>Devices</h3><pre>${JSON.stringify(devs, null, 2)}</pre>`;
    } catch (e) {
      container.textContent = e.message;
    }
  },
});

registerApp({
  id: 'TelecomBrowser',
  title: 'Intranet',
  mount(container) {
    const siteId = 'corp-intranet';
    container.innerHTML = `<iframe src="/api/telecom/sites/${siteId}/index.html" style="width:100%;height:280px;border:0;background:#fff"></iframe>`;
  },
});

openWindow('TelecomDialer');
openWindow('TelecomTerminal');
