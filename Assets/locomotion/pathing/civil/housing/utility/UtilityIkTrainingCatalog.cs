using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Utility IK catalog (clip discovery still uses RagdollIKAnimationManager). Catalog + categories only.</summary>
[CreateAssetMenu(fileName = "UtilityIkTrainingCatalog", menuName = "Locomotion/Utility/IK Training Catalog")]
public sealed class UtilityIkTrainingCatalog : ScriptableObject
{
    public const string PlugIn = "plug_in";
    public const string PlugOut = "plug_out";
    public const string BreakerFlipOn = "breaker_flip_on";
    public const string BreakerFlipOff = "breaker_flip_off";

    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string displayName;
        public PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.ToolUse;
        public string suggestedClipFolder;
        public string notes;
    }

    public List<Entry> entries = new List<Entry>();

    public void EnsureDefaults()
    {
        if (entries == null)
            entries = new List<Entry>();
        AddIfMissing(PlugIn, "Plug in", PhysicsIKTrainingCategory.Open,
            "Assets/locomotion/pathing/civil/housing/utility/Animations/PlugIn", "Tines into wall-plug slots");
        AddIfMissing(PlugOut, "Plug out", PhysicsIKTrainingCategory.Close,
            "Assets/locomotion/pathing/civil/housing/utility/Animations/PlugOut", "Withdraw tines");
        AddIfMissing(BreakerFlipOn, "Breaker flip on", PhysicsIKTrainingCategory.Open,
            "Assets/locomotion/pathing/civil/housing/utility/Animations/BreakerOn", "Service breaker to on");
        AddIfMissing(BreakerFlipOff, "Breaker flip off", PhysicsIKTrainingCategory.Close,
            "Assets/locomotion/pathing/civil/housing/utility/Animations/BreakerOff", "Service breaker to off");
    }

    void AddIfMissing(string id, string displayName, PhysicsIKTrainingCategory category, string folder, string notes)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].id == id)
                return;
        }
        entries.Add(new Entry
        {
            id = id,
            displayName = displayName,
            category = category,
            suggestedClipFolder = folder,
            notes = notes
        });
    }
}
