#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;

/// <summary>
/// Weather Event Wizard: presets (Storm, Rain, Clear, Overcast), adjustment buttons,
/// wind direction and pressure; Save to calendar or Save to behavior tree.
/// </summary>
public class WeatherEventWizardWindow : EditorWindow
{
    private float precipitation = 0f;
    private float temperatureDelta = 0f;
    private float cloud = 0f;
    private float windSpeed = 0f;
    private float windDirectionDegrees = 0f;
    private float pressureDelta = 0f;
    private float duration = 0f;

    private NarrativeCalendarAsset calendar;
    private NarrativeTreeAsset tree;

    private int eventYear = 2025;
    private int eventMonth = 1;
    private int eventDay = 1;
    private int eventHour = 9;
    private int eventMinute = 0;

    private const float PrecipMin = 0f;
    private const float PrecipMax = 1f;
    private const float TempDeltaMin = -30f;
    private const float TempDeltaMax = 30f;
    private const float CloudMin = 0f;
    private const float CloudMax = 1f;
    private const float WindSpeedMin = 0f;
    private const float WindSpeedMax = 30f;
    private const float PressureDeltaMin = -40f;
    private const float PressureDeltaMax = 40f;
    private const float PrecipStep = 0.1f;
    private const float CloudStep = 0.1f;
    private const float TempStep = 1f;
    private const float WindStep = 2f;
    private const float PressureStep = 5f;

    [MenuItem("Window/Locomotion/Narrative/Weather Event Wizard")]
    [MenuItem("Window/Weather/Create Narrative Weather Event")]
    public static void ShowWindow()
    {
        var w = GetWindow<WeatherEventWizardWindow>("Weather Event Wizard");
        w.minSize = new Vector2(360, 520);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Weather Event Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Storm")) ApplyPreset(Preset.Storm);
        if (GUILayout.Button("Rain")) ApplyPreset(Preset.Rain);
        if (GUILayout.Button("Clear")) ApplyPreset(Preset.Clear);
        if (GUILayout.Button("Overcast")) ApplyPreset(Preset.Overcast);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Adjustments", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rainier")) { precipitation = Mathf.Clamp(precipitation + PrecipStep, PrecipMin, PrecipMax); }
        if (GUILayout.Button("Clearer")) { precipitation = Mathf.Clamp(precipitation - PrecipStep, PrecipMin, PrecipMax); cloud = Mathf.Clamp(cloud - CloudStep, CloudMin, CloudMax); }
        if (GUILayout.Button("Colder")) { temperatureDelta = Mathf.Clamp(temperatureDelta - TempStep, TempDeltaMin, TempDeltaMax); }
        if (GUILayout.Button("Hotter")) { temperatureDelta = Mathf.Clamp(temperatureDelta + TempStep, TempDeltaMin, TempDeltaMax); }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Windier")) { windSpeed = Mathf.Clamp(windSpeed + WindStep, WindSpeedMin, WindSpeedMax); }
        if (GUILayout.Button("Calmer")) { windSpeed = Mathf.Clamp(windSpeed - WindStep, WindSpeedMin, WindSpeedMax); }
        if (GUILayout.Button("Higher pressure")) { pressureDelta = Mathf.Clamp(pressureDelta + PressureStep, PressureDeltaMin, PressureDeltaMax); }
        if (GUILayout.Button("Lower pressure")) { pressureDelta = Mathf.Clamp(pressureDelta - PressureStep, PressureDeltaMin, PressureDeltaMax); }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
        precipitation = EditorGUILayout.Slider("Precipitation", precipitation, PrecipMin, PrecipMax);
        temperatureDelta = EditorGUILayout.Slider("Temperature delta (°C)", temperatureDelta, TempDeltaMin, TempDeltaMax);
        cloud = EditorGUILayout.Slider("Cloud", cloud, CloudMin, CloudMax);
        windSpeed = EditorGUILayout.Slider("Wind speed (m/s)", windSpeed, WindSpeedMin, WindSpeedMax);
        EditorGUILayout.BeginHorizontal();
        windDirectionDegrees = EditorGUILayout.Slider("Wind direction (°)", windDirectionDegrees, 0f, 360f);
        string windCardinal = DegreesToCardinal(windDirectionDegrees);
        EditorGUILayout.LabelField($"North is +Z, Wind: {windCardinal}", EditorStyles.miniLabel, GUILayout.Width(140));
        EditorGUILayout.EndHorizontal();
        pressureDelta = EditorGUILayout.Slider("Pressure delta (hPa)", pressureDelta, PressureDeltaMin, PressureDeltaMax);
        duration = EditorGUILayout.FloatField("Duration (s, 0=permanent)", duration);
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Save to calendar", EditorStyles.boldLabel);
        calendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField("Calendar", calendar, typeof(NarrativeCalendarAsset), true);
        EditorGUILayout.BeginHorizontal();
        eventYear = EditorGUILayout.IntField("Year", eventYear);
        eventMonth = Mathf.Clamp(EditorGUILayout.IntField("Month", eventMonth), 1, 12);
        eventDay = Mathf.Clamp(EditorGUILayout.IntField("Day", eventDay), 1, 31);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        eventHour = Mathf.Clamp(EditorGUILayout.IntField("Hour", eventHour), 0, 23);
        eventMinute = Mathf.Clamp(EditorGUILayout.IntField("Minute", eventMinute), 0, 59);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Save to calendar", GUILayout.Height(24)))
            SaveToCalendar();
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Save to behavior tree", EditorStyles.boldLabel);
        tree = (NarrativeTreeAsset)EditorGUILayout.ObjectField("Tree", tree, typeof(NarrativeTreeAsset), true);
        if (GUILayout.Button("Save to behavior tree (append to root)", GUILayout.Height(24)))
            SaveToBehaviorTree();
    }

    private enum Preset { Storm, Rain, Clear, Overcast }

    private void ApplyPreset(Preset p)
    {
        switch (p)
        {
            case Preset.Storm:
                precipitation = 0.8f;
                temperatureDelta = -5f;
                cloud = 0.8f;
                windSpeed = 15f;
                windDirectionDegrees = 225f;
                pressureDelta = -25f;
                break;
            case Preset.Rain:
                precipitation = 0.5f;
                temperatureDelta = -2f;
                cloud = 0.5f;
                windSpeed = 8f;
                windDirectionDegrees = 270f;
                pressureDelta = -10f;
                break;
            case Preset.Clear:
                precipitation = 0f;
                temperatureDelta = 0f;
                cloud = 0f;
                windSpeed = 2f;
                windDirectionDegrees = 0f;
                pressureDelta = 15f;
                break;
            case Preset.Overcast:
                precipitation = 0.1f;
                temperatureDelta = 0f;
                cloud = 0.9f;
                windSpeed = 3f;
                windDirectionDegrees = 0f;
                pressureDelta = 0f;
                break;
        }
    }

    private List<NarrativeActionSpec> BuildActionsFromState()
    {
        var list = new List<NarrativeActionSpec>();
        if (precipitation != 0f)
        {
            list.Add(new NarrativeChangeWeatherAction
            {
                weatherType = WeatherEventType.PrecipitationChange,
                intensity = precipitation,
                duration = duration,
                isAdditive = true,
                affectedSystems = AffectedSystem.Precipitation
            });
        }
        if (temperatureDelta != 0f)
        {
            list.Add(new NarrativeChangeWeatherAction
            {
                weatherType = WeatherEventType.TemperatureChange,
                intensity = temperatureDelta,
                duration = duration,
                isAdditive = true,
                affectedSystems = AffectedSystem.Meteorology
            });
        }
        if (cloud != 0f)
        {
            list.Add(new NarrativeChangeWeatherAction
            {
                weatherType = WeatherEventType.CloudFormation,
                intensity = cloud,
                duration = duration,
                isAdditive = true,
                affectedSystems = AffectedSystem.Cloud
            });
        }
        if (windSpeed != 0f)
        {
            list.Add(new NarrativeChangeWeatherAction
            {
                weatherType = WeatherEventType.WindGust,
                intensity = windSpeed,
                windDirectionDegrees = windDirectionDegrees,
                duration = duration,
                isAdditive = true,
                affectedSystems = AffectedSystem.Wind
            });
        }
        if (pressureDelta != 0f)
        {
            list.Add(new NarrativeChangeWeatherAction
            {
                weatherType = WeatherEventType.PressureChange,
                intensity = pressureDelta,
                duration = duration,
                isAdditive = true,
                affectedSystems = AffectedSystem.Meteorology
            });
        }
        return list;
    }

    private string GetPresetTitle()
    {
        if (precipitation >= 0.6f && pressureDelta <= -15f) return "Storm";
        if (precipitation >= 0.3f) return "Rain";
        if (cloud >= 0.7f && precipitation < 0.2f) return "Overcast";
        return "Clear";
    }

    private void SaveToCalendar()
    {
        if (calendar == null)
        {
            EditorUtility.DisplayDialog("Weather Event Wizard", "Assign a Calendar first.", "OK");
            return;
        }
        var actions = BuildActionsFromState();
        if (actions.Count == 0)
        {
            EditorUtility.DisplayDialog("Weather Event Wizard", "No weather changes (all values at default). Add at least one non-zero value.", "OK");
            return;
        }
        Undo.RecordObject(calendar, "Add Weather Event");
        var evt = new NarrativeCalendarEvent
        {
            title = "Weather: " + GetPresetTitle(),
            startDateTime = new NarrativeDateTime(eventYear, eventMonth, eventDay, eventHour, eventMinute, 0),
            actions = actions
        };
        calendar.events.Add(evt);
        EditorUtility.SetDirty(calendar);
        if (calendar.gameObject != null)
            PrefabUtility.RecordPrefabInstancePropertyModifications(calendar);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Weather Event Wizard", "Weather event added to calendar.", "OK");
    }

    private void SaveToBehaviorTree()
    {
        if (tree == null)
        {
            EditorUtility.DisplayDialog("Weather Event Wizard", "Assign a Tree first.", "OK");
            return;
        }
        var actions = BuildActionsFromState();
        if (actions.Count == 0)
        {
            EditorUtility.DisplayDialog("Weather Event Wizard", "No weather changes (all values at default). Add at least one non-zero value.", "OK");
            return;
        }
        Undo.RecordObject(tree, "Add Weather Action");
        NarrativeSequenceNode rootSeq = tree.root as NarrativeSequenceNode;
        if (rootSeq == null)
        {
            var wrap = new NarrativeSequenceNode { title = "Root" };
            wrap.children.Add(tree.root);
            tree.root = wrap;
            rootSeq = wrap;
        }
        if (rootSeq.children == null)
            rootSeq.children = new List<NarrativeNode>();
        foreach (var spec in actions)
        {
            var node = new NarrativeActionNode
            {
                title = "Weather: " + spec.GetType().Name,
                action = spec
            };
            rootSeq.children.Add(node);
        }
        EditorUtility.SetDirty(tree);
        if (tree.gameObject != null)
            PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Weather Event Wizard", "Weather action(s) appended to tree root.", "OK");
    }

    /// <summary>Convert degrees (0 = N, 90 = E) to cardinal/sub-cardinal label.</summary>
    private static string DegreesToCardinal(float degrees)
    {
        float d = ((degrees % 360f) + 360f) % 360f;
        if (d < 22.5f) return "N";
        if (d < 67.5f) return "NE";
        if (d < 112.5f) return "E";
        if (d < 157.5f) return "SE";
        if (d < 202.5f) return "S";
        if (d < 247.5f) return "SW";
        if (d < 292.5f) return "W";
        if (d < 337.5f) return "NW";
        return "N";
    }
}
#endif
