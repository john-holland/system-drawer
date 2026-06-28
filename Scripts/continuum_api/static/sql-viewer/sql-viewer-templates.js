(function (global) {
  'use strict';

  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/"/g, '&quot;');
  }

  function tableListItems(objects, selectedName) {
    return (objects || []).map(function (o) {
      var cls = 'sv-table-item' + (selectedName === o.name ? ' active' : '');
      return '<div class="' + cls + '" data-name="' + esc(o.name) + '">' +
        esc(o.name) + '<span class="type-badge">' + esc(o.type) + '</span></div>';
    }).join('');
  }

  function schemaInspector(obj) {
    if (!obj) {
      return '<p class="muted">Select a table to inspect columns, indexes, and foreign keys.</p>';
    }
    var html = '<p><strong>' + esc(obj.name) + '</strong> <span class="type-badge">' + esc(obj.type) + '</span></p>';
    if (obj.schemaFile) {
      html += '<p><code>' + esc(obj.schemaFile) + '</code></p>';
    }
    html += '<h3>Columns</h3><dl>';
    (obj.columns || []).forEach(function (col) {
      html += '<dt>' + esc(col.name) + '</dt><dd>' + esc(col.type) +
        (col.pk ? ' PK' : '') +
        (col.notnull ? ' NOT NULL' : '') +
        (col.defaultValue != null ? ' DEFAULT ' + esc(col.defaultValue) : '') +
        '</dd>';
    });
    html += '</dl>';
    if ((obj.indexes || []).length) {
      html += '<h3>Indexes</h3><ul>';
      obj.indexes.forEach(function (idx) {
        html += '<li><code>' + esc(idx.name) + '</code> (' + esc((idx.columns || []).join(', ')) + ')' +
          (idx.unique ? ' UNIQUE' : '') + '</li>';
      });
      html += '</ul>';
    }
    if ((obj.foreignKeys || []).length) {
      html += '<h3>Foreign keys</h3><ul>';
      obj.foreignKeys.forEach(function (fk) {
        html += '<li>' + esc(fk.from) + ' → ' + esc(fk.table) + '.' + esc(fk.to) + '</li>';
      });
      html += '</ul>';
    }
    if (obj.ddl) {
      html += '<h3>DDL</h3><pre class="sv-ddl">' + esc(obj.ddl) + '</pre>';
    }
    return html;
  }

  function resultGridHead(columns) {
    return '<tr>' + (columns || []).map(function (c) {
      return '<th>' + esc(c) + '</th>';
    }).join('') + '</tr>';
  }

  function resultGridBody(columns, rows) {
    if (!rows || !rows.length) {
      return '<tr><td colspan="' + Math.max((columns || []).length, 1) + '">No rows</td></tr>';
    }
    return rows.map(function (row) {
      return '<tr>' + row.map(function (cell) {
        return '<td title="' + esc(cell) + '">' + esc(cell == null ? 'NULL' : cell) + '</td>';
      }).join('') + '</tr>';
    }).join('');
  }

  function recipeButtons(items) {
    return (items || []).map(function (r) {
      return '<button type="button" class="sv-recipe-btn" data-id="' + esc(r.id) + '">' +
        esc(r.label) + '<small>' + esc(r.description || r.id) + '</small></button>';
    }).join('');
  }

  global.ContinuumSqlViewerTemplates = {
    esc: esc,
    tableListItems: tableListItems,
    schemaInspector: schemaInspector,
    resultGridHead: resultGridHead,
    resultGridBody: resultGridBody,
    recipeButtons: recipeButtons,
  };
})(typeof window !== 'undefined' ? window : globalThis);
