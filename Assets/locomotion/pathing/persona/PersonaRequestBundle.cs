using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persona + biorhythm seed multiplexed from Continuuuum / gov-glove request.</summary>
[Serializable]
public sealed class PersonaRequestBundle
{
    public string personaKey;
    public string actorType;
    public string cityId;
    public string venueStableId;
    public CivilSystemKind civilKind = CivilSystemKind.Generic;
    public string dutyCron;
    public int peckingOrder = 100;
    public float biorhythmAmplitudeSeed = 0.5f;
    public float biorhythmPhase01;
    public Dictionary<string, float> societyFeatures = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, float> needSatisfied01 = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public static PersonaRequestBundle CreateDefault(string personaKey, CivilSystemKind kind)
    {
        return new PersonaRequestBundle
        {
            personaKey = personaKey ?? "persona",
            actorType = kind.ToString().ToLowerInvariant(),
            civilKind = kind,
            biorhythmAmplitudeSeed = 0.5f
        };
    }
}
