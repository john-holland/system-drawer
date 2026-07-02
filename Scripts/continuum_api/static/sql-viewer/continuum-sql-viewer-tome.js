(function (global) {
  'use strict';

  var T = function () { return global.ContinuumSqlViewerTemplates; };
  var TOME = 'sql-viewer-tome';

  function SqlViewerTome(root, shell) {
    this.root = root;
    this.shell = shell;
    this.robotCopy = shell && shell.robotCopy;
    this.state = 'gate';
    this.model = {
      schema: { objects: [] },
      selectedTable: null,
      previewOffset: 0,
      previewLimit: 100,
      recipes: [],
    };
    this.editor = null;
    this._bindDom();
    this._wireStaticEvents();
  }

  SqlViewerTome.prototype._bindDom = function () {
    var r = this.root;
    this.dom = {
      gate: r.querySelector('#sv-auth-gate'),
      layout: r.querySelector('#sv-layout'),
      tableList: r.querySelector('#sv-table-list'),
      tableFilter: r.querySelector('#sv-table-filter'),
      schemaPanel: r.querySelector('#sv-schema-panel'),
      resultsMeta: r.querySelector('#sv-results-meta'),
      gridHead: r.querySelector('#sv-grid thead'),
      gridBody: r.querySelector('#sv-grid tbody'),
      pagination: r.querySelector('#sv-pagination'),
      pageInfo: r.querySelector('#sv-page-info'),
      recipes: r.querySelector('#sv-recipes'),
      execMsg: r.querySelector('#sv-exec-msg'),
      editorEl: r.querySelector('#sv-editor'),
    };
  };

  SqlViewerTome.prototype._wireStaticEvents = function () {
    var self = this;
    this.root.querySelector('#sv-run').onclick = function () { self.runQuery(); };
    this.root.querySelector('#sv-validate').onclick = function () { self.validateQuery(); };
    this.root.querySelector('#sv-clear').onclick = function () {
      if (self.editor) self.editor.setValue('', -1);
      self.setExecMsg('', '');
    };
    this.dom.tableFilter.addEventListener('input', function (e) {
      self.renderTableList(e.target.value);
    });
    this.root.querySelector('#sv-prev').onclick = function () {
      if (!self.model.selectedTable) return;
      self.model.previewOffset = Math.max(0, self.model.previewOffset - self.model.previewLimit);
      self.loadPreview(self.model.selectedTable);
    };
    this.root.querySelector('#sv-next').onclick = function () {
      if (!self.model.selectedTable) return;
      self.model.previewOffset += self.model.previewLimit;
      self.loadPreview(self.model.selectedTable);
    };
    if (global.ContinuumUserSession) {
      global.ContinuumUserSession.onChange(function () { self.boot(); });
    }
  };

  SqlViewerTome.prototype.isAuthenticated = function () {
    if (!global.ContinuumUserSession) return false;
    var uid = (global.ContinuumUserSession.getUserId() || '').trim();
    return uid && uid.toLowerCase() !== 'anonymous';
  };

  SqlViewerTome.prototype.transition = function (next) {
    this.state = next;
    this.dom.gate.classList.toggle('hidden', next !== 'gate');
    this.dom.layout.classList.toggle('hidden', next === 'gate');
  };

  SqlViewerTome.prototype.send = function (machine, event, data) {
    if (!this.robotCopy) {
      return Promise.reject(new Error('RobotCopy not available'));
    }
    return this.robotCopy.sendMessage(TOME + '/' + machine, { event: event, data: data || {} })
      .then(function (resp) {
        if (resp && resp.result != null) return resp.result;
        if (resp && resp.error) {
          var err = new Error(resp.error);
          err.body = resp;
          throw err;
        }
        return resp;
      });
  };

  SqlViewerTome.prototype.renderTableList = function (filter) {
    var tpl = T();
    var q = (filter || '').trim().toLowerCase();
    var items = (this.model.schema.objects || []).filter(function (o) {
      return !q || o.name.toLowerCase().indexOf(q) >= 0;
    });
    this.dom.tableList.innerHTML = tpl.tableListItems(items, this.model.selectedTable);
    var self = this;
    this.dom.tableList.querySelectorAll('.sv-table-item').forEach(function (el) {
      el.onclick = function () { self.selectTable(el.getAttribute('data-name')); };
    });
  };

  SqlViewerTome.prototype.setExecMsg = function (text, kind) {
    var el = this.dom.execMsg;
    if (!el) return;
    el.textContent = text || '';
    el.className = 'sv-exec-msg' + (kind ? ' ' + kind : '');
  };

  SqlViewerTome.prototype.renderSchema = function (obj) {
    this.dom.schemaPanel.innerHTML = T().schemaInspector(obj);
  };

  SqlViewerTome.prototype.renderResults = function (columns, rows, metaText) {
    var tpl = T();
    this.dom.gridHead.innerHTML = tpl.resultGridHead(columns);
    this.dom.gridBody.innerHTML = tpl.resultGridBody(columns, rows);
    if (metaText) this.dom.resultsMeta.textContent = metaText;
  };

  SqlViewerTome.prototype.findObject = function (name) {
    return this.model.schema.objects.find(function (o) { return o.name === name; });
  };

  SqlViewerTome.prototype.loadSchema = function () {
    var self = this;
    return this.send('schemaMachine', 'LOAD').then(function (data) {
      self.model.schema = { objects: data.objects || [] };
      self.renderTableList(self.dom.tableFilter.value);
    });
  };

  SqlViewerTome.prototype.loadPreview = function (name) {
    var self = this;
    return this.send('previewMachine', 'LOAD', {
      tableName: name,
      limit: self.model.previewLimit,
      offset: self.model.previewOffset,
    }).then(function (data) {
      self.renderResults(
        data.columns,
        data.rows,
        'Preview ' + name + ' · ' + (data.rowCount || 0) + ' rows · ' +
          (data.elapsedMs || 0) + ' ms' + (data.truncated ? ' (truncated)' : '')
      );
      self.dom.pagination.hidden = false;
      self.dom.pageInfo.textContent = 'Offset ' + self.model.previewOffset + ', limit ' + self.model.previewLimit;
    });
  };

  SqlViewerTome.prototype.selectTable = function (name) {
    this.model.selectedTable = name;
    this.model.previewOffset = 0;
    this.renderTableList(this.dom.tableFilter.value);
    this.renderSchema(this.findObject(name));
    var self = this;
    this.loadPreview(name).catch(function (e) { self.setExecMsg(e.message, 'error'); });
    if (this.editor) {
      this.editor.setValue('SELECT * FROM "' + name.replace(/"/g, '""') + '" LIMIT 100', -1);
    }
  };

  SqlViewerTome.prototype.runQuery = function () {
    if (!this.editor) return;
    var sql = this.editor.getValue();
    var check = global.ContinuumSqlSafety.validateClientSql(sql);
    if (!check.ok) {
      this.setExecMsg(check.errors.join(' '), 'error');
      return;
    }
    var self = this;
    this.state = 'querying';
    this.setExecMsg('Running…', '');
    this.send('queryMachine', 'RUN', { sql: sql, limit: 500 }).then(function (data) {
      var warn = (data.warnings || []).length ? ' · ' + data.warnings.join(' ') : '';
      self.renderResults(
        data.columns,
        data.rows,
        (data.rowCount || 0) + ' rows · ' + (data.elapsedMs || 0) + ' ms' +
          (data.truncated ? ' (truncated)' : '') + warn
      );
      self.dom.pagination.hidden = true;
      self.setExecMsg('Query OK', 'ok');
      self.state = 'ready';
    }).catch(function (e) {
      self.setExecMsg(e.message, 'error');
      self.state = 'ready';
    });
  };

  SqlViewerTome.prototype.validateQuery = function () {
    if (!this.editor) return;
    var sql = this.editor.getValue();
    var check = global.ContinuumSqlSafety.validateClientSql(sql);
    if (!check.ok) {
      this.setExecMsg(check.errors.join(' '), 'error');
      return;
    }
    var self = this;
    this.send('validateMachine', 'VALIDATE', { sql: sql }).then(function (data) {
      if (data.ok) {
        var warn = (data.warnings || []).length ? ' ' + data.warnings.join(' ') : '';
        self.setExecMsg('Valid read-only SQL.' + warn, data.warnings && data.warnings.length ? 'warn' : 'ok');
      } else {
        self.setExecMsg((data.errors || ['Invalid SQL']).join(' '), 'error');
      }
    }).catch(function (e) {
      self.setExecMsg(e.message, 'error');
    });
  };

  SqlViewerTome.prototype.loadRecipes = function () {
    var self = this;
    return this.send('recipesMachine', 'LOAD').then(function (data) {
      self.model.recipes = data.items || [];
      self.dom.recipes.innerHTML = T().recipeButtons(self.model.recipes);
      var map = {};
      self.model.recipes.forEach(function (r) { map[r.id] = r; });
      self.dom.recipes.querySelectorAll('.sv-recipe-btn').forEach(function (btn) {
        btn.onclick = function () {
          var recipe = map[btn.getAttribute('data-id')];
          if (!recipe || !self.editor) return;
          self.editor.setValue(recipe.sql, -1);
          self.runQuery();
        };
      });
    });
  };

  SqlViewerTome.prototype.initEditor = function () {
    var el = this.dom.editorEl;
    if (!el || !global.ace) return;
    this.editor = global.ace.edit(el);
    this.editor.setTheme('ace/theme/tomorrow_night');
    this.editor.session.setMode('ace/mode/sql');
    this.editor.setOptions({
      fontSize: '13px',
      showPrintMargin: false,
      wrap: true,
      minLines: 8,
      maxLines: 20,
      useWorker: false,
    });
    this.editor.setValue(
      "SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') ORDER BY name LIMIT 100",
      -1
    );
    var self = this;
    this.editor.commands.addCommand({
      name: 'runQuery',
      bindKey: { win: 'Ctrl-Enter', mac: 'Command-Enter' },
      exec: function () { self.runQuery(); },
    });
  };

  SqlViewerTome.prototype.boot = function () {
    if (!this.isAuthenticated()) {
      this.transition('gate');
      return Promise.resolve();
    }
    this.transition('booting');
    var self = this;
    return Promise.all([this.loadSchema(), this.loadRecipes()])
      .then(function () {
        self.state = 'ready';
        self.setExecMsg('', '');
      })
      .catch(function (e) {
        if (e.status === 401 || (e.body && e.body.code === 'auth_required')) {
          self.transition('gate');
        } else {
          self.setExecMsg(e.message, 'error');
          self.state = 'ready';
        }
      });
  };

  SqlViewerTome.prototype.start = function () {
    this.initEditor();
    return this.boot();
  };

  function mount(root, shell) {
    if (!root) return null;
    var tome = new SqlViewerTome(root, shell);
    tome.start();
    return tome;
  }

  global.ContinuumSqlViewerTome = { mount: mount, SqlViewerTome: SqlViewerTome };
})(typeof window !== 'undefined' ? window : globalThis);
