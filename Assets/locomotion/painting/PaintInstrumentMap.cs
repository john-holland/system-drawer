using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paint studio instrument slots (proxy mouse → brush / tube / sealant channels).
/// </summary>
[CreateAssetMenu(fileName = "PaintInstrumentMap", menuName = "Locomotion/Painting/Instrument Map")]
public sealed class PaintInstrumentMap : ScriptableObject
{
    public const string BrushYaw = "brush.yaw";
    public const string BrushPitch = "brush.pitch";
    public const string BrushRoll = "brush.roll";
    public const string BrushPress = "brush.press";
    public const string BrushTwist = "brush.twist";
    public const string TubeSqueeze = "tube.squeeze";
    public const string SealantSpray = "sealant.spray";

    [Serializable]
    public struct Slot
    {
        public string id;
        public string impulseChannelKey;
    }

    [SerializeField] List<Slot> slots = new List<Slot>();

    public IReadOnlyList<Slot> Slots => slots;

    public void EnsureDefaults()
    {
        if (slots != null && slots.Count > 0) return;
        slots = new List<Slot>
        {
            new Slot { id = BrushYaw, impulseChannelKey = BrushYaw },
            new Slot { id = BrushPitch, impulseChannelKey = BrushPitch },
            new Slot { id = BrushRoll, impulseChannelKey = BrushRoll },
            new Slot { id = BrushPress, impulseChannelKey = BrushPress },
            new Slot { id = BrushTwist, impulseChannelKey = BrushTwist },
            new Slot { id = TubeSqueeze, impulseChannelKey = TubeSqueeze },
            new Slot { id = SealantSpray, impulseChannelKey = SealantSpray },
        };
    }

    public bool TryGetSlot(string id, out Slot slot)
    {
        slot = default;
        if (string.IsNullOrEmpty(id) || slots == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (string.Equals(slots[i].id, id, StringComparison.OrdinalIgnoreCase))
            {
                slot = slots[i];
                return true;
            }
        }
        return false;
    }

    public bool ChannelIsAllowed(string channel)
    {
        if (string.IsNullOrEmpty(channel) || slots == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (string.Equals(slots[i].impulseChannelKey, channel, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(slots[i].id, channel, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
