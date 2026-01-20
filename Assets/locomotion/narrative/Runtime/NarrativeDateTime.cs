using System;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Serializable, calendar-friendly date/time for narrative scheduling.
    /// Stored as explicit fields (year/month/day/hour/minute/second) for easy JSON/YAML persistence.
    /// </summary>
    [Serializable]
    public struct NarrativeDateTime : IComparable<NarrativeDateTime>, IEquatable<NarrativeDateTime>
    {
        public int year;
        public int month;  // 1-12
        public int day;    // 1-31
        public int hour;   // 0-23
        public int minute; // 0-59
        public int second; // 0-59

        public NarrativeDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        public DateTime ToDateTimeUtc()
        {
            // Clamp to DateTime-supported ranges without throwing on invalid input.
            int y = Math.Clamp(year, 1, 9999);
            int m = Math.Clamp(month, 1, 12);
            int dMax = DateTime.DaysInMonth(y, m);
            int d = Math.Clamp(day, 1, dMax);
            int h = Math.Clamp(hour, 0, 23);
            int min = Math.Clamp(minute, 0, 59);
            int s = Math.Clamp(second, 0, 59);
            return new DateTime(y, m, d, h, min, s, DateTimeKind.Utc);
        }

        public static NarrativeDateTime FromDateTimeUtc(DateTime dtUtc)
        {
            DateTime u = dtUtc.Kind == DateTimeKind.Utc ? dtUtc : dtUtc.ToUniversalTime();
            return new NarrativeDateTime(u.Year, u.Month, u.Day, u.Hour, u.Minute, u.Second);
        }

        public NarrativeDateTime AddSeconds(double seconds)
        {
            DateTime dt = ToDateTimeUtc().AddSeconds(seconds);
            return FromDateTimeUtc(dt);
        }

        public int CompareTo(NarrativeDateTime other)
        {
            return ToDateTimeUtc().CompareTo(other.ToDateTimeUtc());
        }

        public bool Equals(NarrativeDateTime other)
        {
            return year == other.year &&
                   month == other.month &&
                   day == other.day &&
                   hour == other.hour &&
                   minute == other.minute &&
                   second == other.second;
        }

        public override bool Equals(object obj) => obj is NarrativeDateTime other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(year, month, day, hour, minute, second);

        public override string ToString()
        {
            return $"{year:D4}-{month:D2}-{day:D2} {hour:D2}:{minute:D2}:{second:D2}Z";
        }

        public static bool operator <(NarrativeDateTime a, NarrativeDateTime b) => a.CompareTo(b) < 0;
        public static bool operator >(NarrativeDateTime a, NarrativeDateTime b) => a.CompareTo(b) > 0;
        public static bool operator <=(NarrativeDateTime a, NarrativeDateTime b) => a.CompareTo(b) <= 0;
        public static bool operator >=(NarrativeDateTime a, NarrativeDateTime b) => a.CompareTo(b) >= 0;
    }
}

