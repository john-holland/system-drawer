#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
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
        var go1 = new GameObject("TestCalendar");
        var cal = go1.AddComponent<NarrativeCalendarAsset>();
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
        
        UnityEngine.Object.DestroyImmediate(go1);
    }

    [Test]
    public void NarrativeCalendar_YamlRoundTrip_DtoParity()
    {
        var go2 = new GameObject("TestCalendar");
        var cal = go2.AddComponent<NarrativeCalendarAsset>();
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
        
        UnityEngine.Object.DestroyImmediate(go2);
    }

    [Test]
    public void NarrativeCalendarLightingAction_JsonRoundTrip_PreservesFields()
    {
        var go = new GameObject("LightingCalendar");
        var cal = go.AddComponent<NarrativeCalendarAsset>();
        cal.events.Add(new NarrativeCalendarEvent
        {
            title = "lighting update",
            startDateTime = new NarrativeDateTime(2026, 2, 21, 10, 15, 0),
            actions =
            {
                new NarrativeCalendarLightingAction
                {
                    lightingContextKey = "lightingContext",
                    sunAzimuthDeg = 120.5f,
                    sunElevationDeg = 35.25f,
                    sunVisible = true,
                    moonAzimuthDeg = 302.5f,
                    moonElevationDeg = 11.25f,
                    moonDirectionConfidence = 0.67f,
                    moonDirectionSource = "calculated",
                    moonIlluminationFraction = 0.41f,
                    moonVisible = true,
                    inferredSunDirectionVector = new Vector3(0.2f, 0.9f, 0.3f),
                    inferredSunDirectionConfidence = 0.78f,
                    lightingValidityScore = 0.88f,
                    weatherProvider = "open-meteo",
                    cloudCoverPct = 42f,
                    requireValidity = true,
                    minValidityScore = 0.6f,
                    year = 2026,
                    month = 2,
                    day = 21,
                    hour = 10
                }
            }
        });

        string json = NarrativeExportUtility.ExportCalendarToJson(cal);
        var dto = NarrativeImportUtility.ImportCalendarFromJson(json);
        Assert.NotNull(dto);
        Assert.AreEqual(1, dto.events.Count);
        Assert.AreEqual(1, dto.events[0].actions.Count);
        var a = dto.events[0].actions[0];
        Assert.AreEqual(nameof(NarrativeCalendarLightingAction), a.type);
        Assert.AreEqual("lightingContext", a.lightingContextKey);
        Assert.AreEqual(120.5f, a.sunAzimuthDeg, 0.001f);
        Assert.AreEqual(35.25f, a.sunElevationDeg, 0.001f);
        Assert.IsTrue(a.sunVisible);
        Assert.AreEqual(302.5f, a.moonAzimuthDeg, 0.001f);
        Assert.AreEqual(11.25f, a.moonElevationDeg, 0.001f);
        Assert.AreEqual(0.67f, a.moonDirectionConfidence, 0.001f);
        Assert.AreEqual("calculated", a.moonDirectionSource);
        Assert.AreEqual(0.41f, a.moonIlluminationFraction, 0.001f);
        Assert.IsTrue(a.moonVisible);
        Assert.AreEqual(0.78f, a.inferredSunDirectionConfidence, 0.001f);
        Assert.AreEqual(0.88f, a.lightingValidityScore, 0.001f);
        Assert.AreEqual("open-meteo", a.weatherProvider);
        Assert.IsTrue(a.requireValidity);

        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif

