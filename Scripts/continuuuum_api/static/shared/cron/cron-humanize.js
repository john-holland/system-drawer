/**
 * Plain-English cron helper (5-field cron) for Continuuuum SPAs + payroll.
 *
 * Compound schedules: separate expressions with `;` or newlines; narrative joins
 * with "and"; fire counts sum (mixed interval + weekly). Pure interval + pure
 * interval on the same clock uses GCD of periods for a combined cadence.
 *
 * Month lenses: 28 / 29 / 30 (avg) / 31 day counts.
 * Globals: CronHumanize (primary), PayrollCronHumanize (alias).
 */
(function (global) {
  "use strict";

  var DOW = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
  var DOW_MAP = { sun: 0, mon: 1, tue: 2, wed: 3, thu: 4, fri: 5, sat: 6 };
  var MONTH_DAYS = [28, 29, 30, 31];

  var EXAMPLES = [
    {
      id: "monthly",
      label: "Monthly",
      cron: "0 0 1 * *",
      amountUsd: 40,
      blurb: "$40 per month",
    },
    {
      id: "nth_weekday",
      label: "2nd Tuesday",
      cron: "0 0 * * 2#2",
      amountUsd: 40,
      blurb: "$40 every second Tuesday",
    },
    {
      id: "weekly_time",
      label: "Weekly Mon 8",
      cron: "0 8 * * 1",
      amountUsd: 0,
      blurb: "once a week on Monday at 08:00",
    },
    {
      id: "every_n_hours",
      label: "Every 15h",
      cron: "0 */15 * * *",
      amountUsd: 0,
      blurb: "every 15 hours",
    },
    {
      id: "daily",
      label: "Daily",
      cron: "0 9 * * *",
      amountUsd: 40,
      blurb: "$40 daily at 09:00",
    },
    {
      id: "compound_money",
      label: "Month + 2nd Tue",
      cron: "0 0 1 * *;0 0 * * 2#2",
      amountUsd: 40,
      blurb: "$40 per month and every second Tuesday",
    },
    {
      id: "compound_interval",
      label: "15h + Mon 8",
      cron: "0 */15 * * *;0 8 * * 1",
      amountUsd: 0,
      blurb: "every 15 hours and once a week on Monday at 08:00",
    },
  ];

  function gcd(a, b) {
    a = Math.abs(Math.floor(a));
    b = Math.abs(Math.floor(b));
    while (b) {
      var t = b;
      b = a % b;
      a = t;
    }
    return a || 1;
  }

  function parseDowToken(field) {
    var t = String(field).toLowerCase();
    if (DOW_MAP[t] != null) return DOW_MAP[t];
    var n = parseInt(t, 10);
    return Number.isNaN(n) ? null : ((n % 7) + 7) % 7;
  }

  function pad2(n) {
    var s = String(n);
    return s.length === 1 ? "0" + s : s;
  }

  function formatMoney(n) {
    return (
      "$" +
      Number(n).toLocaleString(undefined, {
        maximumFractionDigits: 2,
        minimumFractionDigits: Number.isInteger(n) ? 0 : 2,
      })
    );
  }

  function ordinal(n) {
    var s = String(n);
    if (s === "1") return "first";
    if (s === "2") return "second";
    if (s === "3") return "third";
    if (s === "4") return "fourth";
    if (s === "5") return "fifth";
    return s + "th";
  }

  function splitExprs(cronExpr) {
    return String(cronExpr || "")
      .split(/[;\n]+/)
      .map(function (s) {
        return s.trim();
      })
      .filter(Boolean);
  }

  /**
   * Expand a cron field into { values, step, star, raw }.
   * Star means all; step set means star-slash-n or n/m style.
   */
  function parseField(field, min, max) {
    var raw = String(field).trim();
    var out = { raw: raw, star: false, step: null, values: null, nth: null };
    if (raw === "*" || raw === "?") {
      out.star = true;
      return out;
    }
    // nth weekday: 2#2 or tue#2
    if (raw.indexOf("#") >= 0) {
      var segs = raw.split("#");
      out.nth = parseInt(segs[1], 10);
      out.values = [parseDowToken(segs[0])];
      return out;
    }
    if (raw.indexOf("/") >= 0) {
      var parts = raw.split("/");
      var base = parts[0];
      var step = parseInt(parts[1], 10);
      out.step = Number.isNaN(step) ? 1 : step;
      if (base === "*" || base === "?") {
        out.star = true;
      } else if (base.indexOf("-") >= 0) {
        var rng = base.split("-");
        var a = parseInt(rng[0], 10);
        var b = parseInt(rng[1], 10);
        out.values = [];
        for (var i = a; i <= b; i += out.step) out.values.push(i);
        out.step = null;
      } else {
        var start = parseInt(base, 10);
        out.values = [];
        for (var j = Number.isNaN(start) ? min : start; j <= max; j += out.step) {
          out.values.push(j);
        }
        // Keep step for interval detection when base was */n style already handled;
        // for "0/15" treat as stepped from 0.
        if (base !== "*" && out.values.length > 1) {
          // period = step when covering full range from start
        }
      }
      return out;
    }
    if (raw.indexOf(",") >= 0) {
      out.values = raw.split(",").map(function (x) {
        var d = parseDowToken(x);
        if (d != null && (min === 0 && max === 6)) return d;
        return parseInt(x, 10);
      });
      return out;
    }
    if (raw.indexOf("-") >= 0) {
      var r = raw.split("-");
      var lo = parseDowToken(r[0]);
      var hi = parseDowToken(r[1]);
      if (lo == null) lo = parseInt(r[0], 10);
      if (hi == null) hi = parseInt(r[1], 10);
      out.values = [];
      for (var k = lo; k <= hi; k++) out.values.push(k);
      return out;
    }
    var single = parseDowToken(raw);
    if (single != null && min === 0 && max === 6) {
      out.values = [single];
    } else {
      out.values = [parseInt(raw, 10)];
    }
    return out;
  }

  function isStar(f) {
    return f && (f.star || f.raw === "*" || f.raw === "?");
  }

  function classifyPart(minute, hour, dom, mon, dow) {
    // Pure minute interval: */n * * * *
    if (minute.step && isStar(hour) && isStar(dom) && isStar(mon) && isStar(dow)) {
      return { kind: "interval", periodMinutes: minute.step, label: "every " + minute.step + " minutes" };
    }
    // Pure hour interval: 0 */n * * *  or */n on hour with minute fixed
    if (
      minute.values &&
      minute.values.length === 1 &&
      !minute.step &&
      hour.step &&
      isStar(dom) &&
      isStar(mon) &&
      isStar(dow)
    ) {
      var ph = hour.step * 60;
      return {
        kind: "interval",
        periodMinutes: ph,
        label: hour.step === 1 ? "every hour" : "every " + hour.step + " hours",
      };
    }
    // Hourly: 0 * * * *
    if (
      minute.values &&
      minute.values.length === 1 &&
      minute.values[0] === 0 &&
      isStar(hour) &&
      isStar(dom) &&
      isStar(mon) &&
      isStar(dow)
    ) {
      return { kind: "interval", periodMinutes: 60, label: "every hour" };
    }
    // Minutely: * * * * *
    if (isStar(minute) && isStar(hour) && isStar(dom) && isStar(mon) && isStar(dow)) {
      return { kind: "interval", periodMinutes: 1, label: "every minute" };
    }
    // Nth weekday
    if (dow.nth != null && isStar(dom) && isStar(mon)) {
      var dayName = DOW[dow.values[0] != null ? dow.values[0] : 0] || "weekday";
      return {
        kind: "nth_weekday",
        firesPerMonth: 1,
        label: "every " + ordinal(dow.nth) + " " + dayName,
        timeLabel: timeSuffix(minute, hour),
      };
    }
    // Monthly DOM (single or list) with dow star
    if (!isStar(dom) && isStar(mon) && isStar(dow) && !dom.step) {
      var days = dom.values || [];
      var fires = Math.max(1, days.length);
      var domLabel;
      if (days.length === 1 && days[0] === 1) domLabel = "per month";
      else if (days.length === 1) domLabel = "on day " + days[0] + " of each month";
      else domLabel = "on days " + days.join(", ") + " of each month";
      return {
        kind: "monthly",
        firesPerMonth: fires,
        label: domLabel,
        timeLabel: timeSuffix(minute, hour),
      };
    }
    // Hours window every day: * 6-22 * * *
    if (
      isStar(minute) &&
      !isStar(hour) &&
      hour.values &&
      !hour.step &&
      isStar(dom) &&
      isStar(mon) &&
      isStar(dow)
    ) {
      return {
        kind: "window",
        label: "hours " + hour.raw + " every day",
        countable: false,
      };
    }
    // Weekly DOW (single or range/list), dom star — optional hour window
    if (isStar(dom) && isStar(mon) && !isStar(dow) && dow.nth == null) {
      var set = dow.values || [];
      var weeks = set.length || 1;
      var names = set.map(function (d) {
        return DOW[d] || String(d);
      });
      var when =
        names.length === 1
          ? "once a week on " + names[0]
          : names.length === 5 && set[0] === 1 && set[4] === 5
            ? "on weekdays"
            : "on " + names.join(", ");
      var hoursBit =
        isStar(minute) && !isStar(hour) && hour.values && !hour.step
          ? " hours " + hour.raw
          : "";
      var isWindow = hoursBit.length > 0;
      return {
        kind: isWindow ? "window" : "weekly",
        weekdays: weeks,
        label: when + hoursBit + (isWindow ? "" : timeSuffix(minute, hour)),
        timeLabel: timeSuffix(minute, hour),
        countable: !isWindow,
      };
    }
    // Daily at time: specific hour/minute, dom/mon/dow star
    if (isStar(dom) && isStar(mon) && isStar(dow) && !isStar(hour) && !hour.step && minute.values && minute.values.length === 1) {
      return {
        kind: "daily",
        label: "daily" + timeSuffix(minute, hour),
      };
    }
    return {
      kind: "custom",
      label: "on `" + [minute.raw, hour.raw, dom.raw, mon.raw, dow.raw].join(" ") + "`",
      firesPerMonth: 1,
    };
  }

  function timeSuffix(minute, hour) {
    if (isStar(hour)) return "";
    var h = hour.values && hour.values.length === 1 ? hour.values[0] : null;
    var m =
      minute.values && minute.values.length === 1
        ? minute.values[0]
        : isStar(minute)
          ? 0
          : null;
    if (h == null) return "";
    return " at " + pad2(h) + ":" + pad2(m == null ? 0 : m);
  }

  function analyzeOne(expr) {
    var parts = expr.trim().split(/\s+/);
    if (parts.length < 5) {
      return {
        expr: expr,
        kind: "invalid",
        narrative: "on cron `" + expr + "`",
        periodMinutes: null,
        estimateFires: function () {
          return 0;
        },
      };
    }
    var minute = parseField(parts[0], 0, 59);
    var hour = parseField(parts[1], 0, 23);
    var dom = parseField(parts[2], 1, 31);
    var mon = parseField(parts[3], 1, 12);
    var dow = parseField(parts[4], 0, 6);
    // Re-parse dow for names when not # form
    if (parts[4].indexOf("#") < 0 && /[a-z]/i.test(parts[4])) {
      dow = parseField(parts[4], 0, 6);
    }
    var c = classifyPart(minute, hour, dom, mon, dow);
    var narrative = c.label;
    var countable = c.countable !== false;

    function estimateFires(dayCount) {
      if (!countable) return null;
      var days = Number(dayCount) || 30;
      if (c.kind === "interval" && c.periodMinutes) {
        return Math.floor((days * 24 * 60) / c.periodMinutes);
      }
      if (c.kind === "daily") return days;
      if (c.kind === "weekly") {
        return Math.floor(days / 7) * (c.weekdays || 1);
      }
      if (c.kind === "monthly" || c.kind === "nth_weekday") {
        return c.firesPerMonth || 1;
      }
      if (c.kind === "custom") return c.firesPerMonth || 1;
      return 1;
    }

    return {
      expr: expr,
      kind: c.kind,
      narrative: narrative,
      periodMinutes: c.periodMinutes || null,
      countable: countable,
      estimateFires: estimateFires,
    };
  }

  function moneyPrefix(amountUsd, pieceNarrative, kind) {
    var amt = Number(amountUsd);
    if (!(amt > 0)) return pieceNarrative;
    var money = formatMoney(amt);
    // "per month" already reads well with money first
    if (kind === "monthly" && pieceNarrative === "per month") {
      return money + " per month";
    }
    if (kind === "nth_weekday" || kind === "monthly" || kind === "daily" || kind === "weekly") {
      return money + " " + pieceNarrative;
    }
    if (kind === "interval") {
      return money + " " + pieceNarrative;
    }
    return money + " " + pieceNarrative;
  }

  /**
   * When every piece is a pure interval, combine via GCD of periods (shared clock).
   * Otherwise sum independent fire estimates (interval + weekly → 48+4=52).
   */
  function combineFires(analyzed, dayCount) {
    var countable = analyzed.filter(function (a) {
      return a.countable !== false;
    });
    if (!countable.length) return null;
    var allInterval =
      countable.length > 0 &&
      countable.every(function (a) {
        return a.kind === "interval" && a.periodMinutes;
      });
    if (allInterval && countable.length > 1) {
      var g = countable[0].periodMinutes;
      for (var i = 1; i < countable.length; i++) {
        g = gcd(g, countable[i].periodMinutes);
      }
      return Math.floor((dayCount * 24 * 60) / g);
    }
    var total = 0;
    for (var j = 0; j < countable.length; j++) {
      var f = countable[j].estimateFires(dayCount);
      if (f == null) continue;
      total += f;
    }
    return total;
  }

  function parenthetical(amountUsd, fires, monthDays) {
    var amt = Number(amountUsd);
    var lens = monthDays === 30 ? "avg 30 day" : monthDays + " day";
    if (amt > 0) {
      return (
        "(" + formatMoney(amt * fires) + " / month " + lens + ")"
      );
    }
    return "(" + fires + " occurrences per month " + lens + ")";
  }

  function analyze(cronExpr, amountUsd, opts) {
    opts = opts || {};
    var monthDays = opts.monthDays != null ? Number(opts.monthDays) : 30;
    if (MONTH_DAYS.indexOf(monthDays) < 0) monthDays = 30;

    var exprs = splitExprs(cronExpr);
    var analyzed = exprs.map(analyzeOne);
    // Money once on the first clause for compounds: "$40 per month and every second Tuesday"
    var narrParts = analyzed.map(function (a, idx) {
      if (idx === 0) return moneyPrefix(amountUsd, a.narrative, a.kind);
      return a.narrative;
    });
    var narrativeCore = narrParts.length ? narrParts.join(" and ") : "on an unspecified schedule";

    var fires = combineFires(analyzed, monthDays);
    var amt = Number(amountUsd) || 0;
    var narrative = narrativeCore;
    if (fires != null) {
      narrative = narrativeCore + " " + parenthetical(amountUsd, fires, monthDays);
    } else if (analyzed.some(function (a) {
      return a.kind === "window";
    })) {
      narrative = narrativeCore + " (active hours window)";
    }

    var byMonthDays = {};
    MONTH_DAYS.forEach(function (d) {
      var f = combineFires(analyzed, d);
      var rowAmt = Number(amountUsd) || 0;
      byMonthDays[d] = {
        fires: f,
        monthlyTotalUsd: f != null && rowAmt > 0 ? rowAmt * f : null,
        label:
          f != null
            ? parenthetical(amountUsd, f, d)
            : "(active hours window)",
      };
    });

    return {
      narrative: narrative,
      narrativeCore: narrativeCore,
      parts: analyzed.map(function (a, i) {
        return {
          expr: a.expr,
          kind: a.kind,
          narrative: narrParts[i],
          periodMinutes: a.periodMinutes,
        };
      }),
      fires: fires,
      monthlyTotalUsd: fires != null && amt > 0 ? amt * fires : null,
      monthDays: monthDays,
      byMonthDays: byMonthDays,
      examples: EXAMPLES,
    };
  }

  function humanizeCron(cronExpr, amountUsd, opts) {
    return analyze(cronExpr, amountUsd, opts).narrative;
  }

  function describeRetainerSchedule(cronExpr, amountUsd, opts) {
    return humanizeCron(cronExpr, amountUsd, opts || { monthDays: 30 });
  }

  function firesIn30Days(cronExpr, amountUsd) {
    var a = analyze(cronExpr, amountUsd, { monthDays: 30 });
    return {
      fires: a.fires,
      monthlyTotal: a.monthlyTotalUsd != null ? a.monthlyTotalUsd : a.fires,
      label: a.narrativeCore,
    };
  }

  function formatTotalsRow(byMonthDays, amountUsd) {
    var amt = Number(amountUsd) || 0;
    return MONTH_DAYS.map(function (d) {
      var row = byMonthDays[d];
      if (!row) return d + "d: —";
      if (row.fires == null) return d + "d: window";
      if (amt > 0 && row.monthlyTotalUsd != null) {
        return d + "d: " + formatMoney(row.monthlyTotalUsd);
      }
      return d + "d: " + row.fires;
    }).join(" · ");
  }

  function describe(cronExpr, opts) {
    opts = opts || {};
    var amount = opts.amountUsd != null ? opts.amountUsd : opts.amount;
    return analyze(cronExpr, amount, opts).narrative;
  }

  /**
   * Wire an <input> to a label element. opts.amount / opts.amountEl for money mode;
   * opts.monthDays / opts.getMonthDays for lens.
   */
  function bindInput(inputEl, labelEl, opts) {
    opts = opts || {};
    if (!inputEl || !labelEl) return function () {};

    function refresh() {
      var amount = opts.amount;
      if (opts.amountEl && opts.amountEl.value !== undefined) {
        amount = opts.amountEl.value;
      }
      var monthDays = opts.monthDays != null ? opts.monthDays : 30;
      if (typeof opts.getMonthDays === "function") {
        monthDays = opts.getMonthDays();
      }
      var result = analyze(inputEl.value, amount, { monthDays: monthDays });
      labelEl.textContent = result.narrative;
      if (typeof opts.onAnalyze === "function") opts.onAnalyze(result);
    }

    inputEl.addEventListener("input", refresh);
    if (opts.amountEl) opts.amountEl.addEventListener("input", refresh);
    refresh();
    return refresh;
  }

  var api = {
    analyze: analyze,
    humanizeCron: humanizeCron,
    describe: describe,
    describeRetainerSchedule: describeRetainerSchedule,
    firesIn30Days: firesIn30Days,
    formatTotalsRow: formatTotalsRow,
    bindInput: bindInput,
    examples: EXAMPLES,
    MONTH_DAYS: MONTH_DAYS,
    gcd: gcd,
  };

  global.CronHumanize = api;
  global.PayrollCronHumanize = api;
})(typeof window !== "undefined" ? window : globalThis);
