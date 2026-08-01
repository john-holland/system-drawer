using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Permanent ToolUse IK runs for dish cleaning BT (sponge/spray/rack).</summary>
[CreateAssetMenu(fileName = "DishIkTrainingCatalog", menuName = "Locomotion/Kitchen/Dish IK Training Catalog")]
public sealed class DishIkTrainingCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string displayName;
        public PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.ToolUse;
        public string suggestedClipFolder;
        public PhysicsIKTrainingRunAsset trainingRun;
        public string notes;
    }

    public List<Entry> entries = new List<Entry>();

    public void EnsureDefaults()
    {
        if (entries != null && entries.Count > 0) return;
        entries = new List<Entry>
        {
            EntryOf("sponge_pick", "Sponge pick", PhysicsIKTrainingCategory.Pick, "Pick sponge from caddy"),
            EntryOf("sponge_put", "Sponge put", PhysicsIKTrainingCategory.ToolUse, "Return sponge to caddy"),
            EntryOf("spray", "Spray rinse", PhysicsIKTrainingCategory.ToolUse, "Spray nozzle aim"),
            EntryOf("scrub_stroke", "Scrub stroke", PhysicsIKTrainingCategory.ToolUse, "Circular scrub on plate"),
            EntryOf("place_dishwasher", "Place dishwasher", PhysicsIKTrainingCategory.Carry, "Rack into dishwasher"),
            EntryOf("place_drying_rack", "Place drying rack", PhysicsIKTrainingCategory.Carry, "Stand on drying rack")
        };
    }

    static Entry EntryOf(string id, string name, PhysicsIKTrainingCategory cat, string notes) => new Entry
    {
        id = id,
        displayName = name,
        category = cat,
        suggestedClipFolder = $"Assets/locomotion/pathing/kitchen/dishwashing/Animations/{id}",
        notes = notes
    };

    public Entry Find(string id)
    {
        EnsureDefaults();
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].id == id)
                return entries[i];
        return null;
    }
}
