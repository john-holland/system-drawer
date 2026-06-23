const apps = new Map();
let zIndex = 10;

export function registerApp(def) {
  apps.set(def.id, def);
  const bar = document.getElementById('taskbar-apps');
  if (!bar) return;
  const btn = document.createElement('button');
  btn.type = 'button';
  btn.textContent = def.title;
  btn.onclick = () => openWindow(def.id);
  bar.appendChild(btn);
}

export function openWindow(appId) {
  const def = apps.get(appId);
  if (!def) return;
  const host = document.getElementById('windows');
  const win = document.createElement('div');
  win.className = 'win';
  win.style.left = `${20 + (zIndex % 5) * 24}px`;
  win.style.top = `${20 + (zIndex % 5) * 18}px`;
  win.style.zIndex = String(++zIndex);
  const head = document.createElement('div');
  head.className = 'win-head';
  head.innerHTML = `<span>${def.title}</span><button type="button">×</button>`;
  head.querySelector('button').onclick = () => win.remove();
  const body = document.createElement('div');
  body.className = 'win-body';
  win.appendChild(head);
  win.appendChild(body);
  host.appendChild(win);
  def.mount(body);
  let drag = false;
  let ox = 0;
  let oy = 0;
  head.addEventListener('mousedown', (e) => {
    if (e.target.tagName === 'BUTTON') return;
    drag = true;
    ox = e.clientX - win.offsetLeft;
    oy = e.clientY - win.offsetTop;
    win.style.zIndex = String(++zIndex);
  });
  window.addEventListener('mousemove', (e) => {
    if (!drag) return;
    win.style.left = `${e.clientX - ox}px`;
    win.style.top = `${e.clientY - oy}px`;
  });
  window.addEventListener('mouseup', () => { drag = false; });
}
