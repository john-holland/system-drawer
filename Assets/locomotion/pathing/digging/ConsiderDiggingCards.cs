using System.Collections.Generic;
using Locomotion.Rig;
using UnityEngine;

/// <summary>Overlap scan of DiggableVolume markers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Digging/Consider Digging Cards")]
public sealed class ConsiderDiggingCards : MonoBehaviour
{
    public float scanRadius = 4f;
    public bool stopAmbulation = true;
    public GameObject tool;
    public BoneMap boneMap;

    public List<DiggingCard> Scan(Vector3 origin)
    {
        var cards = new List<DiggingCard>();
        var cols = Physics.OverlapSphere(origin, scanRadius);
        if (cols != null)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                var vol = cols[i] != null ? cols[i].GetComponentInParent<DiggableVolume>() : null;
                if (vol == null || !vol.diggable) continue;
                var card = DiggingCard.Generate(tool, boneMap, stopAmbulation);
                card.contactWorld = vol.WorldBounds.center;
                cards.Add(card);
            }
        }

        var volumes = FindObjectsByType<DiggableVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            var vol = volumes[i];
            if (vol == null || !vol.diggable) continue;
            if ((vol.WorldBounds.center - origin).sqrMagnitude > scanRadius * scanRadius) continue;
            bool already = false;
            for (int c = 0; c < cards.Count; c++)
            {
                if ((cards[c].contactWorld - vol.WorldBounds.center).sqrMagnitude < 0.01f)
                {
                    already = true;
                    break;
                }
            }
            if (already) continue;
            var card = DiggingCard.Generate(tool, boneMap, stopAmbulation);
            card.contactWorld = vol.WorldBounds.center;
            cards.Add(card);
        }
        return cards;
    }
}
