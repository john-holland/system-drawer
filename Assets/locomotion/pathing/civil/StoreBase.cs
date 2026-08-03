using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StoreShelfSlot
{
    public string shelfId;
    public string commodityKey;
    public string displayName;
    public float quantity = 1f;
    public float price;
    public Vector3 localPosition;
}

/// <summary>Shared retail store: hours, shelves, commodities, staff pecking, open/close.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Store Base")]
public class StoreBase : MonoBehaviour
{
    public string storeStableId;
    public string storeType = "generic";
    public string hoursCron = "* 10-21 * * *";
    public bool isOpen;
    public List<StoreShelfSlot> shelves = new List<StoreShelfSlot>();
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();
    public BuildingRagdoll buildingRagdoll;
    [TextArea(2, 6)]
    public string shelfPromptOverride;
    public string builtinPromptKey;

    void Awake()
    {
        if (string.IsNullOrEmpty(storeStableId))
            storeStableId = gameObject.name;
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        buildingRagdoll?.bio?.NotifyOpen();
        if (!open)
            buildingRagdoll?.bio?.NotifyClosed();
        for (int i = 0; i < staff.Count; i++)
        {
            var a = staff[i]?.actor;
            if (a == null) continue;
            if (open && !a.activeSelf) a.SetActive(true);
        }
    }

    public void TickHours(DateTime utcNow)
    {
        bool due = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (due != isOpen)
            SetOpen(due);
    }

    /// <summary>Offline fallback: random spread from commodity keys.</summary>
    public void FillShelvesFromCatalog(IList<string> commodityKeys, int count = 8)
    {
        shelves.Clear();
        if (commodityKeys == null || commodityKeys.Count == 0) return;
        int n = Mathf.Clamp(count, 1, 64);
        for (int i = 0; i < n; i++)
        {
            string key = commodityKeys[UnityEngine.Random.Range(0, commodityKeys.Count)];
            shelves.Add(new StoreShelfSlot
            {
                shelfId = $"shelf-{i}",
                commodityKey = key,
                displayName = key,
                quantity = UnityEngine.Random.Range(1f, 12f),
                price = UnityEngine.Random.Range(1f, 40f),
                localPosition = new Vector3((i % 4) * 0.5f, (i / 4) * 0.4f, 0f)
            });
        }
    }

    public static string DefaultPromptForStoreType(string storeType)
    {
        // todo: once we implement each type, let's update these
        switch ((storeType ?? "").ToLowerInvariant())
        {
            case "liquor":
            case "liquor_store":
                return "Layout a liquor store with shelves for beer, wine, spirits, mixers, and snacks.";
            case "mall_kiosk":
                return "Layout a mall kiosk with seasonal goods and accessories.";
            case "convenience_store":
                return "Layout a convenience store with shelves for snacks, supplies, drinks, and sundries. Refrigerated sections for beer, snacks, and drinks. Coffee and kitchen + supplies.";
            default:
                return "Layout a general retail store with shelves, checkout, and stockroom.";
        }
    }

    public string ResolveShelfPrompt()
    {
        if (!string.IsNullOrWhiteSpace(shelfPromptOverride))
            return shelfPromptOverride;
        return DefaultPromptForStoreType(string.IsNullOrEmpty(builtinPromptKey) ? storeType : builtinPromptKey);
    }
}
