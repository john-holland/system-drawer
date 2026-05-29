using System.Collections.Generic;
using UnityEngine;

/// <summary>Scriptable formation template: ordered local offsets + default wrap row spacing.</summary>
[CreateAssetMenu(fileName = "TravelFormation", menuName = "Locomotion/Travel/Formation Asset", order = 50)]
public class TravelFormationAsset : ScriptableObject
{
    [Min(1)]
    public int version = 1;

    [Tooltip("Ordered slot offsets in formation-local space (see TravelFormation.md).")]
    public List<TravelFormationSlot> slots = new List<TravelFormationSlot>();

    [Min(0.05f)]
    [Tooltip("Distance between wrap rows when cohort count exceeds slot count.")]
    public float defaultWrapRowSpacing = 1.2f;

    public int SlotCount => slots != null ? slots.Count : 0;

    public bool HasSlots => SlotCount > 0;
}
