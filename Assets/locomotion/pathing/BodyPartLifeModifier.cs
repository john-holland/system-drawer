using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Regional multipliers/offsets for life-systems channels and organ trauma on a body part.</summary>
[AddComponentMenu("Locomotion/Body Part Life Modifier")]
public sealed class BodyPartLifeModifier : MonoBehaviour
{
    [Serializable]
    public sealed class ChannelMod
    {
        public string channelId;
        public float multiplier = 1f;
        public float offset;
    }

    public List<ChannelMod> channelMods = new List<ChannelMod>();
    [Tooltip("Multiplies incoming organ damage routed through this part.")]
    public float organTraumaMultiplier = 1f;
    public string[] hostedOrganIds = Array.Empty<string>();

    public float ApplyToChannel(string channelId, float systemic01)
    {
        if (channelMods == null || string.IsNullOrEmpty(channelId))
            return systemic01;
        for (int i = 0; i < channelMods.Count; i++)
        {
            var m = channelMods[i];
            if (m == null || !string.Equals(m.channelId, channelId, StringComparison.OrdinalIgnoreCase))
                continue;
            return Mathf.Clamp01(systemic01 * m.multiplier + m.offset);
        }
        return systemic01;
    }

    public float ScaleOrganDamage(float rawDelta)
    {
        if (rawDelta >= 0f) return rawDelta;
        return rawDelta * Mathf.Max(0f, organTraumaMultiplier);
    }

    public bool HostsOrgan(string organId)
    {
        if (hostedOrganIds == null || string.IsNullOrEmpty(organId))
            return false;
        for (int i = 0; i < hostedOrganIds.Length; i++)
        {
            if (string.Equals(hostedOrganIds[i], organId, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static BodyPartLifeModifier FindHost(LifeSystemsSheet sheet, OrganHostRegion region)
    {
        if (sheet == null) return null;
        var mods = sheet.GetComponentsInChildren<BodyPartLifeModifier>(true);
        string hint = region switch
        {
            OrganHostRegion.Head => "head",
            OrganHostRegion.Abdomen => "pelvis",
            OrganHostRegion.NeckTorso => "neck",
            _ => "torso"
        };
        BodyPartLifeModifier fallback = null;
        for (int i = 0; i < mods.Length; i++)
        {
            var m = mods[i];
            if (m == null) continue;
            fallback ??= m;
            string n = m.gameObject.name.ToLowerInvariant();
            if (n.Contains(hint))
                return m;
        }
        return fallback;
    }
}
