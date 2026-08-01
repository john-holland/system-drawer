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
    [Tooltip("Alternate inventory context space (station, pantry, vehicle).")]
    public GameObject contextGameObject;
    public string contextPath;
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

    /// <summary>
    /// Place item into a VehicleInterior marker context space (put-away).
    /// Transfers into the vehicle <see cref="ActorInventory"/> (created on interior if absent)
    /// and removes the item from any other actor bags. Lemma: {P:have|op=putaway|item=…|context=…}
    /// </summary>
    /// <param name="actorId">Previous holder id (audit only); transfer target is always the vehicle inventory.</param>
    public bool PutAwayToVehicleInterior(InventoryItem item, VehicleInterior interior, string actorId = null)
    {
        if (item == null || interior == null) return false;
        if (!string.IsNullOrEmpty(item.name) && !IsMentioned(item.name) && scriptMentionGate)
            return false;

        var vehicleInv = GetOrCreateVehicleInventory(interior);
        item.contextGameObject = interior.gameObject;
        item.contextPath = interior.gameObject.name;
        item.heldByActorId = null;
        item.ownedByActorId = vehicleInv.actorId;
        item.onGround = null;
        if (item.spawnedInstance != null)
        {
            item.spawnedInstance.transform.SetParent(interior.transform, true);
            item.spawnedInstance.transform.localPosition = Vector3.zero;
        }

        RemoveFromAllActorInventories(item);
        AddOrUpdateActorInventory(vehicleInv, item);
        UpsertLocal(item);
        return true;
    }

    public bool PutAwayToContext(InventoryItem item, GameObject context, string actorId = null)
    {
        if (item == null || context == null) return false;
        var interior = context.GetComponent<VehicleInterior>() ?? context.GetComponentInChildren<VehicleInterior>();
        if (interior != null)
            return PutAwayToVehicleInterior(item, interior, actorId);
        item.contextGameObject = context;
        item.contextPath = context.name;
        item.heldByActorId = null;
        if (item.spawnedInstance != null)
        {
            item.spawnedInstance.transform.SetParent(context.transform, true);
            item.spawnedInstance.transform.localPosition = Vector3.zero;
        }
        UpsertLocal(item);
        return true;
    }

    /// <summary>Find ActorInventory on interior or parents; create on interior if missing.</summary>
    public static ActorInventory GetOrCreateVehicleInventory(VehicleInterior interior)
    {
        if (interior == null) return null;
        var inv = interior.GetComponent<ActorInventory>()
                  ?? interior.GetComponentInParent<ActorInventory>();
        if (inv != null)
        {
            if (string.IsNullOrEmpty(inv.actorId))
                inv.actorId = inv.gameObject.name;
            return inv;
        }
        inv = interior.gameObject.AddComponent<ActorInventory>();
        inv.actorId = interior.gameObject.name;
        if (inv.items == null)
            inv.items = new List<InventoryItem>();
        return inv;
    }

    static bool ItemMatches(InventoryItem a, InventoryItem b)
    {
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id))
            return string.Equals(a.id, b.id, StringComparison.Ordinal);
        return !string.IsNullOrEmpty(a.name) &&
               string.Equals(a.name, b.name, StringComparison.OrdinalIgnoreCase);
    }

    void RemoveFromAllActorInventories(InventoryItem item)
    {
        if (item == null) return;
        var inventories = FindObjectsByType<ActorInventory>(FindObjectsSortMode.None);
        for (int i = 0; i < inventories.Length; i++)
        {
            var inv = inventories[i];
            if (inv?.items == null) continue;
            for (int j = inv.items.Count - 1; j >= 0; j--)
            {
                if (ItemMatches(inv.items[j], item))
                    inv.items.RemoveAt(j);
            }
        }
    }

    static void AddOrUpdateActorInventory(ActorInventory inv, InventoryItem item)
    {
        if (inv == null || item == null) return;
        if (inv.items == null)
            inv.items = new List<InventoryItem>();
        for (int i = 0; i < inv.items.Count; i++)
        {
            if (!ItemMatches(inv.items[i], item)) continue;
            inv.items[i] = item;
            return;
        }
        inv.items.Add(item);
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
                existing.contextGameObject = item.contextGameObject;
                existing.contextPath = item.contextPath;
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
            loadoutSetId = Str(row, "loadout_set_id") ?? "default",
            contextPath = Str(row, "context_path")
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
