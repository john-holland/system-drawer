using System;
using System.Globalization;

/// <summary>
/// Minimal cron due check (5-field: min hour dom month dow), port of submit_scheduler._cron_due spirit.
/// Null/empty cron → always due. Matches if previous scheduled minute is within 60s of truncated now.
/// </summary>
public static class CronDue
{
    /// <summary>True when cron would fire in the current minute (submit_scheduler-style).</summary>
    public static bool IsDue(string cronExpr, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cronExpr))
            return true;
        var truncated = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, 0, DateTimeKind.Utc);
        return Matches(cronExpr.Trim(), truncated);
    }

    /// <summary>True when current time matches cron as an open-hours mask (e.g. * 11-22 * * *).</summary>
    public static bool IsActiveSchedule(string cronExpr, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cronExpr))
            return true;
        return Matches(cronExpr.Trim(), utcNow.ToUniversalTime());
    }

    public static bool Matches(string cronExpr, DateTime t)
    {
        var parts = cronExpr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
            return false;
        return FieldMatches(parts[0], t.Minute, 0, 59)
               && FieldMatches(parts[1], t.Hour, 0, 23)
               && FieldMatches(parts[2], t.Day, 1, 31)
               && FieldMatches(parts[3], t.Month, 1, 12)
               && FieldMatches(parts[4], (int)t.DayOfWeek, 0, 6);
    }

    static bool FieldMatches(string field, int value, int min, int max)
    {
        if (field == "*")
            return true;
        // lists
        if (field.Contains(","))
        {
            var bits = field.Split(',');
            for (int i = 0; i < bits.Length; i++)
                if (FieldMatches(bits[i].Trim(), value, min, max))
                    return true;
            return false;
        }
        // step */n or a-b/n
        if (field.Contains("/"))
        {
            var slash = field.Split('/');
            if (slash.Length != 2 || !int.TryParse(slash[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int step) || step <= 0)
                return false;
            int start = min;
            int end = max;
            if (slash[0] != "*")
            {
                if (slash[0].Contains("-"))
                {
                    var r = slash[0].Split('-');
                    if (r.Length != 2 || !int.TryParse(r[0], out start) || !int.TryParse(r[1], out end))
                        return false;
                }
                else if (!int.TryParse(slash[0], out start))
                    return false;
            }
            if (value < start || value > end)
                return false;
            return (value - start) % step == 0;
        }
        // range a-b
        if (field.Contains("-"))
        {
            var r = field.Split('-');
            if (r.Length != 2 || !int.TryParse(r[0], out int a) || !int.TryParse(r[1], out int b))
                return false;
            return value >= a && value <= b;
        }
        return int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out int exact) && exact == value;
    }
}
