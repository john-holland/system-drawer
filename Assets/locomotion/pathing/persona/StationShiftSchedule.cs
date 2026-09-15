using System;
using UnityEngine;

/// <summary>Extends hoursCron with N shifts (24h or 18h banana-stand).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Persona/Station Shift Schedule")]
public sealed class StationShiftSchedule : MonoBehaviour
{
    public int shiftCount = 3;
    public float hoursPerShift = 8f;
    public bool bananaStand18h;
    public PersonaShiftManager personaShifts;

    public int ResolvedShiftCount =>
        bananaStand18h ? 2 : Mathf.Max(1, shiftCount);

    public float ResolvedHoursPerShift =>
        bananaStand18h ? 9f : Mathf.Max(1f, hoursPerShift);

    public string ResolvedHoursCron(string fallback)
    {
        if (bananaStand18h)
            return "* 6-23 * * *";
        if (ResolvedShiftCount >= 3 && ResolvedHoursPerShift >= 8f)
            return "* * * * *";
        return string.IsNullOrEmpty(fallback) ? "* 8-20 * * *" : fallback;
    }

    public bool IsShiftActive(DateTime utcNow, int shiftIndex)
    {
        int n = ResolvedShiftCount;
        int idx = ((shiftIndex % n) + n) % n;
        float span = ResolvedHoursPerShift;
        float start = bananaStand18h ? 6f + idx * span : idx * span;
        float hour = utcNow.Hour + utcNow.Minute / 60f;
        float end = start + span;
        if (end <= 24f)
            return hour >= start && hour < end;
        return hour >= start || hour < (end - 24f);
    }
}
