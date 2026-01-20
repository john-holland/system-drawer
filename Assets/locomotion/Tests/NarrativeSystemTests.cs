#if UNITY_EDITOR
using NUnit.Framework;
using Locomotion.Narrative;
using Locomotion.Narrative.Serialization;

public class NarrativeSystemTests
{
    [Test]
    public void NarrativeCalendarMath_BuildMonthGrid_Feb2025()
    {
        // Feb 2025 starts on Saturday (DayOfWeek.Saturday = 6)
        int[] grid = NarrativeCalendarMath.BuildMonthGrid(2025, 2);
        Assert.AreEqual(42, grid.Length);
        Assert.AreEqual(0, grid[0]); // Sun
        Assert.AreEqual(0, grid[5]); // Fri
        Assert.AreEqual(1, grid[6]); // Sat
        Assert.AreEqual(28, grid[6 + 27]); // should appear later in the grid
    }

    [Test]
    public void NarrativeCalendar_JsonRoundTrip_DtoParity()
    {
        var cal = UnityEngine.ScriptableObject.CreateInstance<NarrativeCalendarAsset>();
        cal.events.Add(new NarrativeCalendarEvent
        {
            title = "make apple pie",
            startDateTime = new NarrativeDateTime(2025, 2, 1, 10, 0, 0),
            actions = { new CallMethodAction { targetKey = "Oven", componentTypeName = "Oven", methodName = "Preheat" } }
        });

        string json = NarrativeExportUtility.ExportCalendarToJson(cal);
        var dto = NarrativeImportUtility.ImportCalendarFromJson(json);

        Assert.NotNull(dto);
        Assert.AreEqual(1, dto.events.Count);
        Assert.AreEqual("make apple pie", dto.events[0].title);
        Assert.AreEqual(2025, dto.events[0].startDateTime.year);
        Assert.AreEqual(2, dto.events[0].startDateTime.month);
        Assert.AreEqual(1, dto.events[0].startDateTime.day);
    }

    [Test]
    public void NarrativeCalendar_YamlRoundTrip_DtoParity()
    {
        var cal = UnityEngine.ScriptableObject.CreateInstance<NarrativeCalendarAsset>();
        cal.events.Add(new NarrativeCalendarEvent
        {
            title = "friend drops by",
            startDateTime = new NarrativeDateTime(2025, 2, 1, 18, 0, 0),
            actions = { new SetPropertyAction { targetKey = "Door", componentTypeName = "Door", memberName = "isOpen" } }
        });

        string yaml = NarrativeExportUtility.ExportCalendarToYaml(cal);
        var dto = NarrativeImportUtility.ImportCalendarFromYaml(yaml);

        Assert.NotNull(dto);
        Assert.AreEqual(1, dto.events.Count);
        Assert.AreEqual("friend drops by", dto.events[0].title);
    }
}
#endif

