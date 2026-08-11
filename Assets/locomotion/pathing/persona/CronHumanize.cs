using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Plain-English cron narratives (occurrence mode). Twin of
/// Scripts/continuuuum_api/static/shared/cron/cron-humanize.js.
/// Compound expressions separated by ';' or newlines; fires sum unless all pure intervals (GCD).
/// </summary>
public static class CronHumanize
{
    static readonly string[] DowNames =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    public static string Describe(string cronExpr, int monthDays = 30)
    {
        if (string.IsNullOrWhiteSpace(cronExpr))
            return "on an unspecified schedule";

        var parts = SplitExprs(cronExpr);
        if (parts.Count == 0)
            return "on an unspecified schedule";

        var analyzed = new List<Part>(parts.Count);
        foreach (var p in parts)
            analyzed.Add(AnalyzeOne(p));

        var core = new StringBuilder();
        for (var i = 0; i < analyzed.Count; i++)
        {
            if (i > 0) core.Append(" and ");
            core.Append(analyzed[i].Narrative);
        }

        var fires = CombineFires(analyzed, monthDays);
        if (fires.HasValue)
        {
            var lens = monthDays == 30 ? "avg 30 day" : monthDays + " day";
            return core + " (" + fires.Value + " occurrences per month " + lens + ")";
        }

        foreach (var a in analyzed)
        {
            if (a.Kind == Kind.Window)
                return core + " (active hours window)";
        }

        return core.ToString();
    }

    static List<string> SplitExprs(string cronExpr)
    {
        var list = new List<string>();
        foreach (var chunk in cronExpr.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = chunk.Trim();
            if (t.Length > 0) list.Add(t);
        }
        return list;
    }

    enum Kind { Interval, Monthly, NthWeekday, Weekly, Daily, Window, Custom, Invalid }

    sealed class Field
    {
        public string Raw;
        public bool Star;
        public int? Step;
        public List<int> Values;
        public int? Nth;
    }

    sealed class Part
    {
        public Kind Kind;
        public string Narrative;
        public int? PeriodMinutes;
        public bool Countable = true;
        public int Weekdays = 1;
        public int FiresPerMonth = 1;
        public Func<int, int?> EstimateFires;
    }

    static Field ParseField(string field, int min, int max, bool dowMode)
    {
        var raw = (field ?? "").Trim();
        var outF = new Field { Raw = raw };
        if (raw == "*" || raw == "?")
        {
            outF.Star = true;
            return outF;
        }

        if (raw.IndexOf('#') >= 0)
        {
            var segs = raw.Split('#');
            outF.Nth = ParseInt(segs[1]);
            outF.Values = new List<int> { ParseDowToken(segs[0]) ?? 0 };
            return outF;
        }

        if (raw.IndexOf('/') >= 0)
        {
            var parts = raw.Split('/');
            var step = ParseInt(parts[1]) ?? 1;
            outF.Step = step;
            var bas = parts[0];
            if (bas == "*" || bas == "?")
            {
                outF.Star = true;
                return outF;
            }
            if (bas.IndexOf('-') >= 0)
            {
                var rng = bas.Split('-');
                var a = ParseInt(rng[0]) ?? min;
                var b = ParseInt(rng[1]) ?? max;
                outF.Values = new List<int>();
                for (var i = a; i <= b; i += step) outF.Values.Add(i);
                outF.Step = null;
                return outF;
            }
            var start = ParseInt(bas) ?? min;
            outF.Values = new List<int>();
            for (var j = start; j <= max; j += step) outF.Values.Add(j);
            return outF;
        }

        if (raw.IndexOf(',') >= 0)
        {
            outF.Values = new List<int>();
            foreach (var x in raw.Split(','))
            {
                if (dowMode)
                    outF.Values.Add(ParseDowToken(x) ?? ParseInt(x) ?? 0);
                else
                    outF.Values.Add(ParseInt(x) ?? 0);
            }
            return outF;
        }

        if (raw.IndexOf('-') >= 0)
        {
            var r = raw.Split('-');
            int lo, hi;
            if (dowMode)
            {
                lo = ParseDowToken(r[0]) ?? ParseInt(r[0]) ?? min;
                hi = ParseDowToken(r[1]) ?? ParseInt(r[1]) ?? max;
            }
            else
            {
                lo = ParseInt(r[0]) ?? min;
                hi = ParseInt(r[1]) ?? max;
            }
            outF.Values = new List<int>();
            for (var k = lo; k <= hi; k++) outF.Values.Add(k);
            return outF;
        }

        if (dowMode)
        {
            var d = ParseDowToken(raw);
            outF.Values = new List<int> { d ?? ParseInt(raw) ?? 0 };
        }
        else
            outF.Values = new List<int> { ParseInt(raw) ?? 0 };
        return outF;
    }

    static int? ParseInt(string s)
    {
        if (int.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return null;
    }

    static int? ParseDowToken(string field)
    {
        if (string.IsNullOrEmpty(field)) return null;
        var t = field.Trim().ToLowerInvariant();
        switch (t)
        {
            case "sun": return 0;
            case "mon": return 1;
            case "tue": return 2;
            case "wed": return 3;
            case "thu": return 4;
            case "fri": return 5;
            case "sat": return 6;
        }
        var n = ParseInt(t);
        if (!n.HasValue) return null;
        return ((n.Value % 7) + 7) % 7;
    }

    static bool IsStar(Field f) => f != null && (f.Star || f.Raw == "*" || f.Raw == "?");

    static string Pad2(int n) => n < 10 ? "0" + n : n.ToString(CultureInfo.InvariantCulture);

    static string Ordinal(int n)
    {
        switch (n)
        {
            case 1: return "first";
            case 2: return "second";
            case 3: return "third";
            case 4: return "fourth";
            case 5: return "fifth";
            default: return n + "th";
        }
    }

    static string TimeSuffix(Field minute, Field hour)
    {
        if (IsStar(hour)) return "";
        if (hour.Values == null || hour.Values.Count != 1) return "";
        var h = hour.Values[0];
        var m = 0;
        if (minute.Values != null && minute.Values.Count == 1) m = minute.Values[0];
        return " at " + Pad2(h) + ":" + Pad2(m);
    }

    static Part AnalyzeOne(string expr)
    {
        var bits = expr.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (bits.Length < 5)
        {
            return new Part
            {
                Kind = Kind.Invalid,
                Narrative = "on cron `" + expr + "`",
                Countable = false,
                EstimateFires = _ => null
            };
        }

        var minute = ParseField(bits[0], 0, 59, false);
        var hour = ParseField(bits[1], 0, 23, false);
        var dom = ParseField(bits[2], 1, 31, false);
        var mon = ParseField(bits[3], 1, 12, false);
        var dow = ParseField(bits[4], 0, 6, true);

        // Minute interval
        if (minute.Step.HasValue && IsStar(hour) && IsStar(dom) && IsStar(mon) && IsStar(dow))
        {
            var pm = minute.Step.Value;
            return Interval(pm, pm == 1 ? "every minute" : "every " + pm + " minutes");
        }

        // Hour interval: 0 */n * * *
        if (minute.Values != null && minute.Values.Count == 1 && !minute.Step.HasValue
            && hour.Step.HasValue && IsStar(dom) && IsStar(mon) && IsStar(dow))
        {
            var step = hour.Step.Value;
            return Interval(step * 60, step == 1 ? "every hour" : "every " + step + " hours");
        }

        // Hourly 0 * * * *
        if (minute.Values != null && minute.Values.Count == 1 && minute.Values[0] == 0
            && IsStar(hour) && IsStar(dom) && IsStar(mon) && IsStar(dow))
            return Interval(60, "every hour");

        // Minutely * * * * *
        if (IsStar(minute) && IsStar(hour) && IsStar(dom) && IsStar(mon) && IsStar(dow))
            return Interval(1, "every minute");

        // Nth weekday
        if (dow.Nth.HasValue && IsStar(dom) && IsStar(mon))
        {
            var day = DowNames[dow.Values != null && dow.Values.Count > 0 ? dow.Values[0] : 0];
            var label = "every " + Ordinal(dow.Nth.Value) + " " + day;
            return new Part
            {
                Kind = Kind.NthWeekday,
                Narrative = label,
                FiresPerMonth = 1,
                EstimateFires = _ => 1
            };
        }

        // Hours window every day
        if (IsStar(minute) && !IsStar(hour) && hour.Values != null && !hour.Step.HasValue
            && IsStar(dom) && IsStar(mon) && IsStar(dow))
        {
            return new Part
            {
                Kind = Kind.Window,
                Narrative = "hours " + hour.Raw + " every day",
                Countable = false,
                EstimateFires = _ => null
            };
        }

        // Weekly / weekday window
        if (IsStar(dom) && IsStar(mon) && !IsStar(dow) && !dow.Nth.HasValue)
        {
            var set = dow.Values ?? new List<int>();
            var names = new List<string>();
            foreach (var d in set) names.Add(d >= 0 && d < DowNames.Length ? DowNames[d] : d.ToString());
            string when;
            if (names.Count == 1) when = "once a week on " + names[0];
            else if (names.Count == 5 && set.Count > 0 && set[0] == 1 && set[set.Count - 1] == 5)
                when = "on weekdays";
            else when = "on " + string.Join(", ", names);

            var isWindow = IsStar(minute) && !IsStar(hour) && hour.Values != null && !hour.Step.HasValue;
            if (isWindow)
            {
                return new Part
                {
                    Kind = Kind.Window,
                    Narrative = when + " hours " + hour.Raw,
                    Countable = false,
                    EstimateFires = _ => null
                };
            }

            var weeks = Math.Max(1, set.Count);
            var label = when + TimeSuffix(minute, hour);
            return new Part
            {
                Kind = Kind.Weekly,
                Narrative = label,
                Weekdays = weeks,
                EstimateFires = days => (days / 7) * weeks
            };
        }

        // Monthly DOM
        if (!IsStar(dom) && IsStar(mon) && IsStar(dow) && !dom.Step.HasValue)
        {
            var days = dom.Values ?? new List<int>();
            var fires = Math.Max(1, days.Count);
            string domLabel;
            if (days.Count == 1 && days[0] == 1) domLabel = "per month";
            else if (days.Count == 1) domLabel = "on day " + days[0] + " of each month";
            else domLabel = "on days " + string.Join(", ", days) + " of each month";
            return new Part
            {
                Kind = Kind.Monthly,
                Narrative = domLabel,
                FiresPerMonth = fires,
                EstimateFires = _ => fires
            };
        }

        // Daily at time
        if (IsStar(dom) && IsStar(mon) && IsStar(dow) && !IsStar(hour) && !hour.Step.HasValue
            && minute.Values != null && minute.Values.Count == 1)
        {
            return new Part
            {
                Kind = Kind.Daily,
                Narrative = "daily" + TimeSuffix(minute, hour),
                EstimateFires = days => days
            };
        }

        return new Part
        {
            Kind = Kind.Custom,
            Narrative = "on `" + string.Join(" ", bits[0], bits[1], bits[2], bits[3], bits[4]) + "`",
            FiresPerMonth = 1,
            EstimateFires = _ => 1
        };
    }

    static Part Interval(int periodMinutes, string label)
    {
        return new Part
        {
            Kind = Kind.Interval,
            Narrative = label,
            PeriodMinutes = periodMinutes,
            EstimateFires = days => (days * 24 * 60) / periodMinutes
        };
    }

    static int Gcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            var t = b;
            b = a % b;
            a = t;
        }
        return a == 0 ? 1 : a;
    }

    static int? CombineFires(List<Part> analyzed, int dayCount)
    {
        var countable = analyzed.FindAll(a => a.Countable);
        if (countable.Count == 0) return null;

        var allInterval = countable.TrueForAll(a => a.Kind == Kind.Interval && a.PeriodMinutes.HasValue);
        if (allInterval && countable.Count > 1)
        {
            var g = countable[0].PeriodMinutes.Value;
            for (var i = 1; i < countable.Count; i++)
                g = Gcd(g, countable[i].PeriodMinutes.Value);
            return (dayCount * 24 * 60) / g;
        }

        var total = 0;
        foreach (var a in countable)
        {
            var f = a.EstimateFires(dayCount);
            if (f.HasValue) total += f.Value;
        }
        return total;
    }
}
