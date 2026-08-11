/**
 * Compatibility shim — prefer /static/shared/cron/cron-humanize.js
 * Keeps PayrollCronHumanize when the shared script already loaded.
 */
(function (global) {
  "use strict";
  if (global.CronHumanize && !global.PayrollCronHumanize) {
    global.PayrollCronHumanize = global.CronHumanize;
  }
  if (!global.CronHumanize && !global.PayrollCronHumanize) {
    console.warn(
      "[payroll] Load /static/shared/cron/cron-humanize.js before payroll.js"
    );
  }
})(typeof window !== "undefined" ? window : globalThis);
