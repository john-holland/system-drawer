using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Named commodity for recipes/meals. Specials derive from a base via <see cref="specialOf"/>
/// (e.g. display "nanas classic sauce", specialOf "nanas"). <see cref="supplementable"/> defaults true.
/// </summary>
[Serializable]
public sealed class RecipeCommoditySpec
{
    public string displayName = "nanas classic sauce";
    public string inventoryItemName;
    public string inventoryItemId;
    [Tooltip("Base commodity name used when supplementable and special stock is missing.")]
    public string specialOf = "nanas";
    [Tooltip("When true (default), missing special can be materialized from specialOf base stock.")]
    public bool supplementable = true;
    public bool createIfMissing = true;
    public List<TasteNoteEntry> tasteNotes = new List<TasteNoteEntry>();

    public string ResolvedInventoryName =>
        !string.IsNullOrEmpty(inventoryItemName) ? inventoryItemName : displayName;
}
