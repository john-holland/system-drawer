(function (global) {
  'use strict';

  var BANNED = /^\s*(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|REPLACE|ATTACH|DETACH|VACUUM|REINDEX|TRUNCATE|GRANT|REVOKE|MERGE)\b/i;
  var ALLOWED_ROOT = /^\s*(SELECT|WITH|EXPLAIN|PRAGMA)\b/i;
  var MULTI_STMT = /;\s*\S/;

  function stripComments(sql) {
    return String(sql || '')
      .replace(/\/\*[\s\S]*?\*\//g, ' ')
      .replace(/--[^\n]*/g, ' ');
  }

  function validateClientSql(sql) {
    var errors = [];
    var cleaned = stripComments(sql).trim();
    if (!cleaned) {
      errors.push('SQL is empty.');
      return { ok: false, errors: errors };
    }
    if (MULTI_STMT.test(cleaned)) {
      errors.push('Only a single SQL statement is allowed.');
    }
    if (BANNED.test(cleaned)) {
      errors.push('Statement contains a forbidden keyword.');
    }
    if (!ALLOWED_ROOT.test(cleaned)) {
      errors.push('Statement must start with SELECT, WITH, EXPLAIN, or PRAGMA.');
    }
    return { ok: errors.length === 0, errors: errors };
  }

  global.ContinuumSqlSafety = { validateClientSql: validateClientSql };
})(typeof window !== 'undefined' ? window : globalThis);
