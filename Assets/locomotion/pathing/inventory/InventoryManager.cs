using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InventoryItem
{
    public string id;
    public string name;
    public string iconAsset;
    public string prefabId;
    public bool useTakeoutAnimation;
    public bool usePutawayAnimation;
    public string ownedByActorId;
    public string heldByActorId;
    public Vector3? onGround;
    public string loadoutSetId = "default";
    public GameObject spawnedInstance;
}

[AddComponentMenu("Locomotion/Inventory/Actor Inventory")]
public sealed class ActorInventory : MonoBehaviour
{
    public string actorId;
    public List<InventoryItem> items = new List<InventoryItem>();

    void Awake()
    {
        if (string.IsNullOrEmpty(actorId))
            actorId = gameObject.name;
    }

    public InventoryItem FindByName(string name)
    {
        if (items == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < items.Count; i++)
            if (items[i] != null && string.Equals(items[i].name, name, StringComparison.OrdinalIgnoreCase))
                return items[i];
        return null;
    }
}

/// <summary>Manages loadouts sync and script-mention gating for inventory mutations.</summary>
[AddComponentMenu("Locomotion/Inventory/Inventory Manager")]
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Tooltip("When true, only mutate inventory when lemma/script explicitly mentions the item.")]
    public bool scriptMentionGate = true;
    public string apiBaseUrl = "http://127.0.0.1:5050";
    public string activeLoadoutSetId = "default";
    public readonly List<InventoryItem> allItems = new List<InventoryItem>();
    readonly HashSet<string> _scriptMentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void NoteScriptMention(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName))
            _scriptMentions.Add(itemName.Trim());
    }

    public bool IsMentioned(string itemName)
    {
        if (!scriptMentionGate) return true;
        return !string.IsNullOrEmpty(itemName) && _scriptMentions.Contains(itemName.Trim());
    }

    public void UpsertLocal(InventoryItem item)
    {
        if (item == null) return;
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] != null && allItems[i].id == item.id)
            {
                allItems[i] = item;
                return;
            }
        }
        allItems.Add(item);
    }

    public InventoryItem FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < allItems.Count; i++)
            if (allItems[i] != null && string.Equals(allItems[i].name, name, StringComparison.OrdinalIgnoreCase))
                return allItems[i];
        return null;
    }

    /// <summary>
    /// Possessive / transfer. If item name is not in loadouts, returns false silently (tool-use assumed).
    /// </summary>
    public bool TryPossessiveOrTransfer(string itemName, string fromActorId, string toActorId, bool requireMention = true)
    {
        var item = FindByName(itemName);
        if (item == null)
            return false; // silent — not a loadout item
        if (requireMention && !IsMentioned(itemName))
            return false;
        if (!string.IsNullOrEmpty(toActorId))
        {
            item.heldByActorId = toActorId;
            if (string.IsNullOrEmpty(item.ownedByActorId))
                item.ownedByActorId = toActorId;
            item.onGround = null;
            SyncActorComponent(toActorId, item);
        }
        else if (!string.IsNullOrEmpty(fromActorId))
        {
            item.heldByActorId = fromActorId;
            item.ownedByActorId = fromActorId;
            SyncActorComponent(fromActorId, item);
        }
        return true;
    }

    public bool TryPickup(string itemName, string actorId, Vector3 worldPos)
    {
        var item = FindByName(itemName);
        if (item == null) return false;
        if (!IsMentioned(itemName)) return false;
        item.heldByActorId = actorId;
        item.ownedByActorId = string.IsNullOrEmpty(item.ownedByActorId) ? actorId : item.ownedByActorId;
        item.onGround = null;
        SyncActorComponent(actorId, item);
        return true;
    }

    public void PlaceOnGround(InventoryItem item, Vector3 world)
    {
        if (item == null) return;
        item.heldByActorId = null;
        item.onGround = world;
        // Octree / world pose hook: move spawned instance if present
        if (item.spawnedInstance != null)
            item.spawnedInstance.transform.position = world;
    }

    void SyncActorComponent(string actorId, InventoryItem item)
    {
        var inventories = FindObjectsByType<ActorInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < inventories.Length; i++)
        {
            var inv = inventories[i];
            if (inv == null || !string.Equals(inv.actorId, actorId, StringComparison.OrdinalIgnoreCase))
                continue;
            var existing = inv.FindByName(item.name);
            if (existing != null)
            {
                existing.heldByActorId = item.heldByActorId;
                existing.ownedByActorId = item.ownedByActorId;
                existing.onGround = item.onGround;
            }
            else
                inv.items.Add(item);
        }
    }

    /// <summary>Apply a row dictionary from Continuuuum API JSON (scaffold sync).</summary>
    public void ApplyApiRow(Dictionary<string, object> row)
    {
        if (row == null) return;
        var item = new InventoryItem
        {
            id = Str(row, "id"),
            name = Str(row, "name"),
            iconAsset = Str(row, "icon_asset"),
            prefabId = Str(row, "prefab_id"),
            useTakeoutAnimation = Bool(row, "use_takeout_animation"),
            usePutawayAnimation = Bool(row, "use_putaway_animation"),
            ownedByActorId = Str(row, "ownedby_actor_id"),
            heldByActorId = Str(row, "heldby_actor_id"),
            loadoutSetId = Str(row, "loadout_set_id") ?? "default"
        };
        if (row.ContainsKey("onground_x") && row["onground_x"] != null)
        {
            float x = Float(row, "onground_x");
            float y = Float(row, "onground_y");
            float z = Float(row, "onground_z");
            item.onGround = new Vector3(x, y, z);
        }
        UpsertLocal(item);
    }

    static string Str(Dictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v != null ? v.ToString() : null;
    static bool Bool(Dictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && (v is bool b ? b : v?.ToString() == "1" || string.Equals(v?.ToString(), "true", StringComparison.OrdinalIgnoreCase));
    static float Float(Dictionary<string, object> d, string k)
    {
        if (!d.TryGetValue(k, out var v) || v == null) return 0f;
        float.TryParse(v.ToString(), out var f);
        return f;
    }
}
