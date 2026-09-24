/// <summary>Property keys for {P:house|...} architecture / size lemma painting.</summary>
public static class HousingLemmaPropertyKeys
{
    public const string PlaceholderName = "house";
    public const string Size = "size";
    public const string Style = "style";
    public const string SpecSize = "house-size";
    public const string SpecStyle = "house-style";

    public static readonly string[] SizeTokens =
    {
        "quaint", "good_size", "mc_mansion", "mansion", "cabin", "cottage", "townhome"
    };

    public static readonly string[] AllKeys = { Size, Style };
}
