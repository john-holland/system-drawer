using NUnit.Framework;

public sealed class CronHumanizeTests
{
    [Test]
    public void Compound_IntervalAndWeekly_FiftyTwoOnAvg30()
    {
        var n = CronHumanize.Describe("0 */15 * * *;0 8 * * 1", 30);
        StringAssert.Contains("every 15 hours", n);
        StringAssert.Contains("Monday", n);
        StringAssert.Contains("52 occurrences", n);
        StringAssert.Contains("avg 30", n);
    }

    [Test]
    public void Compound_MonthlyAndNthTuesday()
    {
        var n = CronHumanize.Describe("0 0 1 * *;0 0 * * 2#2", 30);
        StringAssert.Contains("per month", n);
        StringAssert.Contains("second Tuesday", n);
        StringAssert.Contains("2 occurrences", n);
    }

    [Test]
    public void HoursWindow_Weekdays()
    {
        var n = CronHumanize.Describe("* 6-22 * * 1-5", 30);
        StringAssert.Contains("weekdays", n);
        StringAssert.Contains("hours 6-22", n);
        StringAssert.Contains("active hours window", n);
    }

    [Test]
    public void HoursWindow_EveryDay()
    {
        var n = CronHumanize.Describe("* 5-23 * * *", 30);
        StringAssert.Contains("hours 5-23 every day", n);
        StringAssert.Contains("active hours window", n);
    }
}
