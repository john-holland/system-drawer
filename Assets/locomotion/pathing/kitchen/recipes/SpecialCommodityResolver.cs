using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves special commodities: prefer exact match; if missing and supplementable,
/// materialize from specialOf base stock (e.g. nanas classic sauce from nanas).
/// </summary>
public static class SpecialCommodityResolver
{
    public sealed class ResolveResult
    {
        public bool ok;
        public InventoryItem item;
        public bool supplementedFromBase;
        public string error;
    }

    public static ResolveResult Resolve(RecipeCommoditySpec spec, InventoryManager inventory = null)
    {
        var result = new ResolveResult();
        if (spec == null || string.IsNullOrEmpty(spec.displayName) && string.IsNullOrEmpty(spec.inventoryItemName))
        {
            result.error = "commodity spec empty";
            return result;
        }

        inventory = inventory != null ? inventory : InventoryManager.Instance;
        string specialName = spec.ResolvedInventoryName;

        if (inventory != null)
        {
            var exact = inventory.FindByName(specialName);
            if (exact != null)
            {
                result.ok = true;
                result.item = exact;
                return result;
            }
        }

        if (!spec.supplementable)
        {
            result.error = $"special '{specialName}' missing and not supplementable";
            return result;
        }

        if (string.IsNullOrEmpty(spec.specialOf))
        {
            result.error = $"special '{specialName}' supplementable but specialOf empty";
            return result;
        }

        InventoryItem baseItem = inventory != null ? inventory.FindByName(spec.specialOf) : null;
        if (baseItem == null && !spec.createIfMissing)
        {
            result.error = $"base '{spec.specialOf}' missing for special '{specialName}'";
            return result;
        }

        var special = new InventoryItem
        {
            id = string.IsNullOrEmpty(spec.inventoryItemId)
                ? Guid.NewGuid().ToString("N")
                : spec.inventoryItemId,
            name = specialName,
            prefabId = baseItem != null ? baseItem.prefabId : null,
            loadoutSetId = baseItem != null ? baseItem.loadoutSetId : "default",
            contextGameObject = baseItem != null ? baseItem.contextGameObject : null,
            contextPath = baseItem != null
                ? baseItem.contextPath
                : $"specialOf:{spec.specialOf}"
        };

        if (inventory != null)
        {
            inventory.NoteScriptMention(specialName);
            inventory.NoteScriptMention(spec.specialOf);
            inventory.UpsertLocal(special);
        }

        result.ok = true;
        result.item = special;
        result.supplementedFromBase = true;
        return result;
    }

    public static int ResolveAll(IList<RecipeCommoditySpec> specs, InventoryManager inventory, List<InventoryItem> outItems = null)
    {
        int ok = 0;
        if (specs == null) return 0;
        for (int i = 0; i < specs.Count; i++)
        {
            var r = Resolve(specs[i], inventory);
            if (!r.ok) continue;
            ok++;
            outItems?.Add(r.item);
        }
        return ok;
    }
}
