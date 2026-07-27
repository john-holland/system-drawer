using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Senses;

/// <summary>Clear/zero smell emitters by signature or body-region tags (greenfield clear API).</summary>
public static class HygieneSmellClearService
{
    public static void ClearAllOn(GameObject actor)
    {
        if (actor == null) return;
        var emitters = actor.GetComponentsInChildren<SmellEmitter>(true);
        for (int i = 0; i < emitters.Length; i++)
        {
            if (emitters[i] == null) continue;
            emitters[i].emissionMultiplier = 0f;
            emitters[i].intensity = 0f;
        }
    }

    public static void ClearSignatures(GameObject actor, IList<string> signatures)
    {
        if (actor == null || signatures == null) return;
        var emitters = actor.GetComponentsInChildren<SmellEmitter>(true);
        for (int i = 0; i < emitters.Length; i++)
        {
            var e = emitters[i];
            if (e == null) continue;
            for (int s = 0; s < signatures.Count; s++)
            {
                if (string.Equals(e.signature, signatures[s], StringComparison.OrdinalIgnoreCase))
                {
                    e.emissionMultiplier = 0f;
                    e.intensity = 0f;
                    break;
                }
            }
        }
    }

    public static void ClearHands(GameObject actor)
    {
        ClearSignatures(actor, new[] { "hand", "hands", "garlic", "cumin", "onion", "food_hand" });
    }
}
