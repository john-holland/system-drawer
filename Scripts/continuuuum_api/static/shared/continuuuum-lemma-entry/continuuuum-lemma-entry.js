/* Shared lemma entry display — links to /lemma-library#entry/{id} */
(function (global) {
  function escHtml(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  }

  function normalize(raw) {
    if (!raw) return null;
    if (typeof raw === 'string') {
      const entryId = raw.trim();
      return entryId ? { entryId, term: entryId } : null;
    }
    const entryId = String(raw.entryId || raw.id || raw.entry_id || raw.propertyValue || raw.property_value || '').trim();
    if (!entryId) return null;
    return {
      entryId,
      term: String(raw.term || raw.lemmaTerm || raw._term || entryId).trim() || entryId,
      posTag: raw.posTag || raw.pos_tag || '',
      languageCode: raw.languageCode || raw.language_code || '',
      isBuiltIn: !!(raw.isBuiltIn || raw.is_builtin),
    };
  }

  function entryUrl(entryId) {
    if (!entryId) return '#';
    return `/lemma-library#entry/${encodeURIComponent(String(entryId))}`;
  }

  function open(entryId, opts) {
    opts = opts || {};
    const url = entryUrl(entryId);
    if (!entryId || url === '#') return;
    if (opts.sameTab) {
      global.location.href = url;
      return;
    }
    global.open(url, opts.target || '_blank', 'noopener,noreferrer');
  }

  function linkHtml(entry, opts) {
    opts = opts || {};
    const n = normalize(entry);
    if (!n) return escHtml(opts.fallback || '—');
    const target = opts.sameTab ? '' : ' target="_blank" rel="noopener noreferrer"';
    const cls = ['continuuuum-lemma-entry-link', opts.className].filter(Boolean).join(' ');
    const label = opts.label || n.term;
    const title = opts.title || `Open lemma: ${n.term}`;
    let html = `<a href="${escHtml(entryUrl(n.entryId))}" class="${escHtml(cls)}"${target} title="${escHtml(title)}">${escHtml(label)}</a>`;
    if (opts.showId) {
      html += ` <code class="continuuuum-lemma-entry-id">${escHtml(n.entryId)}</code>`;
    }
    return html;
  }

  function chipHtml(entry, opts) {
    opts = opts || {};
    return linkHtml(entry, {
      ...opts,
      className: ['continuuuum-lemma-entry-chip', opts.className].filter(Boolean).join(' '),
    });
  }

  function createLink(entry, opts) {
    opts = opts || {};
    const n = normalize(entry);
    const wrap = document.createElement('span');
    wrap.className = 'continuuuum-lemma-entry-wrap';
    if (!n) {
      wrap.textContent = opts.fallback || '—';
      return wrap;
    }
    const a = document.createElement('a');
    a.href = entryUrl(n.entryId);
    a.className = ['continuuuum-lemma-entry-link', opts.className].filter(Boolean).join(' ');
    a.title = opts.title || `Open lemma: ${n.term}`;
    a.textContent = opts.label || n.term;
    if (!opts.sameTab) {
      a.target = opts.target || '_blank';
      a.rel = 'noopener noreferrer';
    }
    if (opts.onClick) {
      a.addEventListener('click', (ev) => {
        if (opts.onClick(ev, n) === false) ev.preventDefault();
      });
    }
    wrap.appendChild(a);
    if (opts.showId) {
      const code = document.createElement('code');
      code.className = 'continuuuum-lemma-entry-id';
      code.textContent = n.entryId;
      wrap.appendChild(document.createTextNode(' '));
      wrap.appendChild(code);
    }
    return wrap;
  }

  function createChip(entry, opts) {
    opts = opts || {};
    return createLink(entry, {
      ...opts,
      className: ['continuuuum-lemma-entry-chip', opts.className].filter(Boolean).join(' '),
    });
  }

  function renderInline(parent, entries, opts) {
    opts = opts || {};
    if (!parent) return;
    parent.innerHTML = '';
    const items = (entries || []).map(normalize).filter(Boolean);
    if (!items.length) {
      if (opts.emptyHtml) {
        parent.innerHTML = opts.emptyHtml;
        return;
      }
      const empty = document.createElement('span');
      empty.className = 'continuuuum-lemma-entry-empty';
      empty.textContent = opts.emptyLabel || 'None';
      parent.appendChild(empty);
      return;
    }
    items.forEach((n, idx) => {
      parent.appendChild(createChip(n, opts));
      if (opts.separator && idx < items.length - 1) {
        parent.appendChild(document.createTextNode(opts.separator));
      }
    });
  }

  function renderList(parent, entries, opts) {
    opts = opts || {};
    if (!parent) return;
    parent.innerHTML = '';
    const ul = document.createElement('ul');
    ul.className = ['continuuuum-lemma-entry-list', opts.className].filter(Boolean).join(' ');
    const items = (entries || []).map(normalize).filter(Boolean);
    if (!items.length) {
      const li = document.createElement('li');
      li.className = 'continuuuum-lemma-entry-empty';
      li.textContent = opts.emptyLabel || 'No lemmas';
      ul.appendChild(li);
    } else {
      items.forEach((n) => {
        const li = document.createElement('li');
        li.appendChild(createLink(n, { showId: !!opts.showId, ...opts }));
        ul.appendChild(li);
      });
    }
    parent.appendChild(ul);
  }

  global.ContinuuuumLemmaEntry = {
    escHtml,
    normalize,
    entryUrl,
    open,
    linkHtml,
    chipHtml,
    createLink,
    createChip,
    renderInline,
    renderList,
  };
})(typeof window !== 'undefined' ? window : globalThis);
