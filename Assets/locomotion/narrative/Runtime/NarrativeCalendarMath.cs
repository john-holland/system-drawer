using System;

namespace Locomotion.Narrative
{
    public static class NarrativeCalendarMath
    {
        /// <summary>
        /// Returns a 6x7 month grid as a flat array (row-major), with 0 for empty cells.
        /// Sunday-first. Length is always 42.
        /// </summary>
        public static int[] BuildMonthGrid(int year, int month)
        {
            year = Math.Clamp(year, 1, 9999);
            month = Math.Clamp(month, 1, 12);

            int daysInMonth = DateTime.DaysInMonth(year, month);
            int firstDow = (int)new DateTime(year, month, 1).DayOfWeek; // 0=Sun

            int[] cells = new int[42];
            for (int i = 0; i < 42; i++)
            {
                int day = i - firstDow + 1;
                cells[i] = (day >= 1 && day <= daysInMonth) ? day : 0;
            }
            return cells;
        }
    }
}

