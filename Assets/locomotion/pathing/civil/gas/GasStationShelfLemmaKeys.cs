/// <summary>Shelf position lemmas — top-shelf implies high / possibly high price for alcohol.</summary>
public static class GasStationShelfLemmaKeys
{
    public const string TopShelf = "top-shelf";
    public const string BottomShelf = "bottom-shelf";
    public const string EyeLevel = "eye-level";
    public const string HighPrice = "high_price";
    public const string Alcohol = "alcohol";

    public static bool IsShelfLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant();
        return k == TopShelf || k == BottomShelf || k == EyeLevel || k == HighPrice || k == Alcohol
               || k.StartsWith("shelf-") || k.StartsWith("shelf_");
    }

    /// <summary>Vertical band 0=bottom .. 1=top for placement.</summary>
    public static float VerticalBand01(string lemma)
    {
        if (string.IsNullOrEmpty(lemma)) return 0.5f;
        string k = lemma.ToLowerInvariant();
        if (k == TopShelf || k.Contains("top")) return 0.9f;
        if (k == BottomShelf || k.Contains("bottom")) return 0.15f;
        if (k == EyeLevel || k.Contains("eye")) return 0.55f;
        return 0.5f;
    }

    public static bool ImpliesHighPrice(string lemma, string commodityKey)
    {
        string k = (lemma ?? "").ToLowerInvariant();
        string c = (commodityKey ?? "").ToLowerInvariant();
        bool alcohol = c.Contains("alcohol") || c.Contains("beer") || c.Contains("liquor") || c.Contains("wine")
                       || k.Contains(Alcohol);
        return alcohol && (k == TopShelf || k.Contains("top") || k == HighPrice);
    }
}
