using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Links firehouse staff personas to HouseBioRhythm homes for off-shift PersonaDay lives.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fireman Home Binding")]
public sealed class FiremanHomeBinding : MonoBehaviour
{
    public FirehouseBioRhythm firehouse;
    public List<HouseBioRhythm> homes = new List<HouseBioRhythm>();

    void Awake()
    {
        if (firehouse == null)
            firehouse = GetComponent<FirehouseBioRhythm>();
    }

    /// <summary>Register home personas onto the firehouse bio for off-shift cron.</summary>
    public void SyncHomePersonaKeys()
    {
        if (firehouse == null) return;
        firehouse.homePersonaKeys.Clear();
        for (int i = 0; i < homes.Count; i++)
        {
            if (homes[i] == null) continue;
            firehouse.homePersonaKeys.Add(homes[i].gameObject.name);
        }
        if (firehouse.company == null) return;
        for (int i = 0; i < firehouse.company.staff.Count; i++)
        {
            var s = firehouse.company.staff[i];
            if (s == null || string.IsNullOrEmpty(s.personaKey)) continue;
            if (!firehouse.homePersonaKeys.Contains(s.personaKey))
                firehouse.homePersonaKeys.Add(s.personaKey);
        }
    }

    public FiremanDispatcherCallinRequestCard CallInOffShift(string personaKey)
    {
        return FiremanDispatcherCallinRequestCard.Generate(personaKey);
    }
}
