# Weather Event Wizard Window

## Overview

Add a new Editor window (Weather Event Wizard) that lets users pick weather presets (storm, rain, clear, overcast), fine-tune with rainier/clearer/colder/hotter buttons, control **wind** and **front direction / pressure gradients**, then save the configuration either as a new calendar event on a NarrativeCalendarAsset or as a NarrativeChangeWeatherAction on a NarrativeTreeAsset.

---

## Wind and Fronts / Pressure Gradients

### Wizard state (additions)

- **Wind speed** (float, e.g. 0–30 m/s) – maps to WindGust magnitude (and optionally base wind).
- **Wind direction** (float, 0–360°, meteorological: direction wind **comes from**) – used for front direction and wind field; maps to WeatherEvent.vectorData or a dedicated field so the runtime can set Wind.direction.
- **Pressure delta** (float, hPa; e.g. -40 to +40) – positive = high pressure (clear, stable), negative = low pressure (fronts, storms). Maps to PressureChange magnitude.

Fronts are boundaries between air masses; in the wizard, "direction of fronts" is represented by **wind direction** (fronts typically move with the wind flow) and **pressure gradient** (pressure delta + wind direction together imply where high/low pressure lie and how the front is oriented). No separate "front type" enum is required for MVP: low pressure + wind direction + precipitation/temperature already describe a front-like event.

### UI additions

- **Wind**: Presets set wind speed/direction (e.g. Storm: strong SW wind; Clear: light variable). Buttons: **Windier** / **Calmer** (nudge speed). Optional: **Wind direction** control (slider 0–360° or compass-style) or **Front direction** label (same value, "Front from NW").
- **Pressure**: Presets set pressure delta (Storm: low; Clear: high). Buttons or sliders: **Higher pressure** / **Lower pressure** (nudge pressure delta in hPa).

### Runtime support

- **NarrativeChangeWeatherAction** currently sets only `eventType`, `magnitude`, `isAdditive`, `affectedSystems`, `duration` on the created WeatherEvent. Weather.Runtime **WeatherEvent** has `vectorData` (e.g. wind direction); **Wind.ApplyWeatherEvent** for WindGust currently only applies magnitude to `gustSpeed`, not direction.
- **Plan**:
  1. **NarrativeChangeWeatherAction**: Add optional **windDirectionDegrees** (0–360) and/or **vectorData** (Vector3). When creating a WindGust event, set WeatherEvent.vectorData from wind direction (e.g. unit vector in the direction wind comes FROM: `Quaternion.Euler(0, windDirectionDegrees, 0) * Vector3.forward` or equivalent so Wind can interpret it).
  2. **Weather.Wind.ApplyWeatherEvent**: When handling WindGust, if `eventData.vectorData.magnitude > 0.01f`, set `wind.direction` from the vector (e.g. `Mathf.Atan2(vectorData.x, vectorData.z) * Mathf.Rad2Deg` or decode from vector). This ties front/wind direction from the wizard to the runtime Wind subsystem.
  3. **Pressure**: NarrativeChangeWeatherAction already supports **PressureChange**; wizard emits PressureChange with magnitude = pressure delta (hPa), affectedSystems = Meteorology.

### Preset table (updated)

| Preset   | Precipitation | Temperature | Cloud | Wind speed | Wind dir | Pressure delta |
|----------|---------------|-------------|-------|------------|----------|----------------|
| Storm    | High          | Cooler      | High  | High       | SW (225°) | Low (-25 hPa)  |
| Rain     | Medium        | Slight cool | Medium| Moderate   | W (270°)  | Slightly low    |
| Clear    | Zero          | Neutral     | Zero  | Light      | Variable  | High (+15 hPa) |
| Overcast | Low/none      | Neutral     | High  | Light      | Variable  | Near normal    |

### Adjustment buttons (updated)

| Button           | Effect                          |
|------------------|----------------------------------|
| Rainier          | Increase precipitation intensity |
| Clearer          | Decrease precipitation, decrease cloud |
| Colder           | Decrease temperature delta       |
| Hotter           | Increase temperature delta       |
| Windier          | Increase wind speed              |
| Calmer           | Decrease wind speed              |
| Higher pressure  | Increase pressure delta (e.g. +5 hPa) |
| Lower pressure   | Decrease pressure delta (e.g. -5 hPa) |

Optional: **Front direction** as a single control (0–360°) that sets wind direction and is shown as "Front from N/NE/E/..." for clarity.

---

## Goal (original)

- **Presets**: Storm, Rain, Clear, Overcast.
- **Adjustments**: Rainier, Clearer, Colder, Hotter (plus Windier, Calmer, Higher/Lower pressure above).
- **Save to calendar**: create a NarrativeCalendarEvent on an assigned NarrativeCalendarAsset (event runs in the narrative/behavior flow when scheduled).
- **Save to behavior tree**: plug in a **calendar** (add a calendar event) or a **behavior tree** (add a NarrativeChangeWeatherAction node to a NarrativeTreeAsset).

## Existing pieces to reuse

- **NarrativeChangeWeatherAction** – WeatherEventType (TemperatureChange, PrecipitationChange, CloudFormation, **PressureChange**, **WindGust**), intensity, duration, isAdditive, affectedSystems. Extend with windDirectionDegrees/vectorData for WindGust.
- **NarrativeCalendarEvent** – title, startDateTime, durationSeconds, tree, actions (List<NarrativeActionSpec>).
- **NarrativeCalendarWizardWindow** – CreateEventOnSelectedDate() pattern: Undo, calendar.events.Add, SetDirty, SaveAssets.
- **NarrativeTreeEditorWindow** – AddChildToSelected(NarrativeNodeType.Action), append to root sequence; create NarrativeActionNode with NarrativeChangeWeatherAction(s).
- **Weather.Wind** – speed, direction (0–360°), gustSpeed; ApplyWeatherEvent(WindGust) currently only updates gustSpeed; extend to set direction from eventData.vectorData.
- **Weather.Meteorology** – pressure (hPa); PressureChange event already applied in WeatherSystem.ApplyWeatherEvent.

## Data model (wizard state, full)

- **Precipitation intensity** (float) → PrecipitationChange magnitude.
- **Temperature delta** (float, °C) → TemperatureChange magnitude.
- **Cloud** (float) → CloudFormation magnitude.
- **Wind speed** (float, m/s) → WindGust magnitude.
- **Wind direction** (float, 0–360°) → WindGust vectorData / windDirectionDegrees.
- **Pressure delta** (float, hPa) → PressureChange magnitude.
- **Duration** (float, seconds; 0 = permanent).

## UI layout (EditorWindow)

- Title: "Weather Event Wizard"; menu: e.g. `Window > Locomotion > Narrative > Weather Event Wizard`.
- Presets row: Storm | Rain | Clear | Overcast.
- Adjustments row: Rainier | Clearer | Colder | Hotter | Windier | Calmer | Higher pressure | Lower pressure.
- Optional: Wind direction slider (0–360°) or "Front direction" dropdown (N, NE, E, SE, S, SW, W, NW).
- Optional: Summary (precipitation, temp, cloud, wind, pressure) and Duration field.
- Save to calendar: ObjectField Calendar, optional date/time, button "Save to calendar".
- Save to behavior tree: ObjectField for Calendar or Tree; if Calendar → add event; if Tree → add action node(s) to root.

## Mapping wizard state to actions (full)

- **Precipitation** → NarrativeChangeWeatherAction(PrecipitationChange, intensity, AffectedSystem.Precipitation).
- **Temperature** → NarrativeChangeWeatherAction(TemperatureChange, temperatureDelta, Meteorology).
- **Cloud** → NarrativeChangeWeatherAction(CloudFormation, cloudValue, Cloud).
- **Wind** → NarrativeChangeWeatherAction(WindGust, windSpeed, Wind; vectorData from windDirectionDegrees).
- **Pressure** → NarrativeChangeWeatherAction(PressureChange, pressureDelta, Meteorology).

When saving, add one action per non-default dimension (or always emit all five and use 0 where not used; prefer minimal set for clarity).

## Implementation order

1. Add **WeatherEventWizardWindow** with presets, adjustment buttons, and full state (precipitation, temperature, cloud, wind speed, wind direction, pressure delta, duration).
2. Implement preset logic (Storm/Rain/Clear/Overcast) and adjustment logic (all buttons, clamped).
3. **NarrativeChangeWeatherAction**: Add optional windDirectionDegrees (or vectorData); when creating WindGust event, set WeatherEvent.vectorData from direction (unit vector wind comes FROM).
4. **Weather.Wind.ApplyWeatherEvent**: For WindGust, if eventData.vectorData.magnitude > 0.01f, set wind.direction from vectorData (e.g. Atan2 and rad2deg).
5. Implement Save to calendar: create NarrativeCalendarEvent with one or more NarrativeChangeWeatherAction (including PressureChange and WindGust with direction); add to calendar.events.
6. Implement Save to behavior tree: Calendar → same as step 5; Tree → append NarrativeActionNode(s) with weather action(s) to root sequence.
7. Optional: links from Narrative Calendar Wizard and Weather Service Wizard for discoverability.

## Notes

- **Front direction** = wind direction (meteorological convention: direction wind comes from). Fronts move with the flow; low pressure + wind direction gives a coherent "front" feel.
- **Pressure gradient** is implied by pressure delta (low vs high) and can be visualized later (e.g. arrows on map); wizard only exports pressure change magnitude and wind direction.
- Locomotion.Narrative.WeatherEventType already has PressureChange and WindGust; use these. Extend NarrativeChangeWeatherAction and Weather.Wind as above for direction and pressure.
