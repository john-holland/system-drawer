/**
 * D3-powered multisort panel with drag-reorder and visibility checkboxes.
 * ContinuumMultisort.mount(el, { storageKey, dimensions, onChange })
 */
(function (global) {
  'use strict';

  function loadState(key, dimensions) {
    try {
      const raw = localStorage.getItem(key);
      if (raw) {
        const saved = JSON.parse(raw);
        if (Array.isArray(saved) && saved.length) return saved;
      }
    } catch (_) {}
    return dimensions.map((d, i) => ({
      id: d.id,
      field: d.field,
      label: d.label,
      visible: d.visible !== false,
      asc: d.asc !== false,
      order: i,
    }));
  }

  function saveState(key, state) {
    try {
      localStorage.setItem(key, JSON.stringify(state));
    } catch (_) {}
  }

  function compareValues(a, b, asc) {
    if (a == null && b == null) return 0;
    if (a == null) return 1;
    if (b == null) return -1;
    if (Array.isArray(a) && Array.isArray(b)) {
      a = a.join(',');
      b = b.join(',');
    }
    if (typeof a === 'boolean' || typeof b === 'boolean') {
      a = a ? 1 : 0;
      b = b ? 1 : 0;
    }
    const sa = String(a).toLowerCase();
    const sb = String(b).toLowerCase();
    if (sa < sb) return asc ? -1 : 1;
    if (sa > sb) return asc ? 1 : -1;
    return 0;
  }

  function fieldValue(item, field) {
    if (!field) return '';
    if (field === 'isBuiltIn') return item.isBuiltIn ? 'Built-in' : 'Custom';
    if (field === 'components') return (item.components || []).join(',') || '(none)';
    if (field === 'alpha') return (item.term || item.lemmaTerm || '').charAt(0).toUpperCase() || '#';
    return item[field];
  }

  function sortItems(items, sortState) {
    const active = sortState.filter(s => s.visible).sort((a, b) => a.order - b.order);
    const copy = items.slice();
    copy.sort((a, b) => {
      for (const dim of active) {
        const field = dim.field === 'alpha' ? 'term' : dim.field;
        const va = fieldValue(a, dim.field === 'alpha' ? 'alpha' : field);
        const vb = fieldValue(b, dim.field === 'alpha' ? 'alpha' : field);
        const c = compareValues(va, vb, dim.asc);
        if (c !== 0) return c;
      }
      return String(a.term || a.lemmaTerm || '').localeCompare(String(b.term || b.lemmaTerm || ''));
    });
    return copy;
  }

  function groupItems(items, sortState) {
    const active = sortState.filter(s => s.visible).sort((a, b) => a.order - b.order);
    if (!active.length) return [{ key: 'All', items }];
    const groups = [];
    let currentKey = null;
    let bucket = [];
    for (const item of items) {
      const parts = active.map(dim => {
        const v = fieldValue(item, dim.field);
        return v == null || v === '' ? '(empty)' : String(v);
      });
      const key = parts.join(' › ');
      if (key !== currentKey) {
        if (bucket.length) groups.push({ key: currentKey, items: bucket });
        currentKey = key;
        bucket = [item];
      } else {
        bucket.push(item);
      }
    }
    if (bucket.length) groups.push({ key: currentKey, items: bucket });
    return groups;
  }

  function mount(el, options) {
    const storageKey = options.storageKey || 'continuum-multisort';
    const dimensions = options.dimensions || [];
    let sortState = loadState(storageKey, dimensions);

    el.innerHTML = '';
    el.classList.add('continuum-multisort');
    const title = document.createElement('h3');
    title.textContent = options.title || 'Sort & group';
    el.appendChild(title);
    const list = document.createElement('ul');
    list.className = 'continuum-multisort-list';
    el.appendChild(list);

    function notify() {
      saveState(storageKey, sortState);
      if (options.onChange) options.onChange(sortState.slice());
    }

    function renderList() {
      list.innerHTML = '';
      const ordered = sortState.slice().sort((a, b) => a.order - b.order);
      ordered.forEach((dim, idx) => {
        const li = document.createElement('li');
        li.className = 'continuum-multisort-item';
        li.dataset.id = dim.id;
        li.innerHTML =
          '<span class="continuum-multisort-grip">⋮⋮</span>' +
          '<span class="continuum-multisort-label"></span>' +
          '<button type="button" class="continuum-multisort-dir"></button>' +
          '<input type="checkbox" class="continuum-multisort-vis" title="Visible in sort/group">';
        li.querySelector('.continuum-multisort-label').textContent = dim.label;
        const dirBtn = li.querySelector('.continuum-multisort-dir');
        dirBtn.textContent = dim.asc ? '↑' : '↓';
        dirBtn.onclick = () => {
          dim.asc = !dim.asc;
          renderList();
          notify();
        };
        const vis = li.querySelector('.continuum-multisort-vis');
        vis.checked = dim.visible;
        vis.onchange = () => {
          dim.visible = vis.checked;
          notify();
        };
        list.appendChild(li);
      });

      if (global.d3 && global.d3.select) {
        const drag = global.d3.drag()
          .on('start', function () {
            global.d3.select(this).classed('dragging', true);
          })
          .on('drag', function (event) {
            const y = event.y;
            const siblings = [...list.children];
            const thisId = this.dataset.id;
            const thisIdx = siblings.indexOf(this);
            let targetIdx = thisIdx;
            siblings.forEach((sib, i) => {
              if (sib === this) return;
              const rect = sib.getBoundingClientRect();
              const mid = rect.top + rect.height / 2;
              if (event.sourceEvent.clientY < mid && i < targetIdx) targetIdx = i;
              else if (event.sourceEvent.clientY > mid && i > targetIdx) targetIdx = i + 1;
            });
            if (targetIdx !== thisIdx && targetIdx >= 0) {
              list.insertBefore(this, siblings[targetIdx] || null);
            }
          })
          .on('end', function () {
            global.d3.select(this).classed('dragging', false);
            const ids = [...list.children].map(li => li.dataset.id);
            ids.forEach((id, order) => {
              const d = sortState.find(x => x.id === id);
              if (d) d.order = order;
            });
            notify();
          });
        global.d3.select(list).selectAll('.continuum-multisort-item').call(drag);
      }
    }

    renderList();
    notify();

    return {
      getState: () => sortState.slice(),
      sortItems: items => sortItems(items, sortState),
      groupItems: items => groupItems(sortItems(items, sortState), sortState),
    };
  }

  global.ContinuumMultisort = { mount, sortItems, groupItems, fieldValue };
})(typeof window !== 'undefined' ? window : globalThis);
