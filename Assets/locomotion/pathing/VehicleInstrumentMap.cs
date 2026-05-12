using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoring asset: maps vehicle control surfaces to impulse channel names used in <see cref="ImpulseAction.muscleGroup"/>.
/// </summary>
[CreateAssetMenu(fileName = "VehicleInstrumentMap", menuName = "Locomotion/Vehicle Instrument Map", order = 100)]
public class VehicleInstrumentMap : ScriptableObject
{
    [SerializeField] List<VehicleInstrumentSlot> slots = new List<VehicleInstrumentSlot>();

    public IReadOnlyList<VehicleInstrumentSlot> Slots => slots;

    /// <summary>Replace all slots (authoring/tests).</summary>
    public void ReplaceSlots(IReadOnlyList<VehicleInstrumentSlot> newSlots)
    {
        slots = newSlots != null ? new List<VehicleInstrumentSlot>(newSlots) : new List<VehicleInstrumentSlot>();
    }

    public bool TryGetSlot(string instrumentId, out VehicleInstrumentSlot slot)
    {
        slot = default;
        if (string.IsNullOrEmpty(instrumentId) || slots == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].id == instrumentId)
            {
                slot = slots[i];
                return true;
            }
        }
        return false;
    }

    public bool ChannelIsAllowed(string muscleGroupKey)
    {
        if (string.IsNullOrEmpty(muscleGroupKey) || slots == null) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!string.IsNullOrEmpty(slots[i].impulseChannelKey) &&
                string.Equals(slots[i].impulseChannelKey, muscleGroupKey, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

[Serializable]
public struct VehicleInstrumentSlot
{
    public string id;
    [Tooltip("Must match ImpulseAction.muscleGroup for this instrument.")]
    public string impulseChannelKey;
    public DrivingAnimationServiceActionLimits defaultLimits;
}

/// <summary>
/// Ensures drive good sections only reference mapped instrument channels.
/// </summary>
public static class InstrumentImpulseValidator
{
    public static bool ValidateImpulseStack(IReadOnlyList<ImpulseAction> stack, VehicleInstrumentMap map)
    {
        if (map == null || stack == null) return false;
        for (int i = 0; i < stack.Count; i++)
        {
            var a = stack[i];
            if (a == null || string.IsNullOrEmpty(a.muscleGroup)) return false;
            if (!map.ChannelIsAllowed(a.muscleGroup)) return false;
        }
        return stack.Count > 0;
    }

    public static bool IsAllowedMuscleGroup(string muscleGroup, VehicleInstrumentMap map)
    {
        if (map == null || string.IsNullOrEmpty(muscleGroup)) return false;
        return map.ChannelIsAllowed(muscleGroup);
    }
}
