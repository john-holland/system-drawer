using System;

/// <summary>Property keys for house size / architecture lemmas.</summary>
public static class HousingArchitectureLemmaPropertyKeys
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

public enum HousingArchitectureSize
{
    Quaint = 0,
    GoodSize = 1,
    McMansion = 2,
    Mansion = 3,
    Cabin = 4,
    Cottage = 5,
    Townhome = 6
}

[Serializable]
public struct HousingArchitectureLemmaProperties
{
    public HousingArchitectureSize size;
    public string style;

    public float FootprintScale()
    {
        switch (size)
        {
            case HousingArchitectureSize.Quaint: return 0.65f;
            case HousingArchitectureSize.Cabin: return 0.7f;
            case HousingArchitectureSize.Cottage: return 0.75f;
            case HousingArchitectureSize.Townhome: return 0.9f;
            case HousingArchitectureSize.GoodSize: return 1f;
            case HousingArchitectureSize.McMansion: return 1.45f;
            case HousingArchitectureSize.Mansion: return 1.8f;
            default: return 1f;
        }
    }

    public int RoomCountHint()
    {
        switch (size)
        {
            case HousingArchitectureSize.Quaint:
            case HousingArchitectureSize.Cabin: return 3;
            case HousingArchitectureSize.Cottage:
            case HousingArchitectureSize.Townhome: return 5;
            case HousingArchitectureSize.GoodSize: return 6;
            case HousingArchitectureSize.McMansion: return 10;
            case HousingArchitectureSize.Mansion: return 14;
            default: return 6;
        }
    }

    public static HousingArchitectureSize ParseSize(string token)
    {
        if (string.IsNullOrEmpty(token)) return HousingArchitectureSize.GoodSize;
        var t = token.ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        if (t.Contains("mc_mansion") || t.Contains("mcmansion")) return HousingArchitectureSize.McMansion;
        if (t.Contains("mansion")) return HousingArchitectureSize.Mansion;
        if (t.Contains("quaint")) return HousingArchitectureSize.Quaint;
        if (t.Contains("cabin")) return HousingArchitectureSize.Cabin;
        if (t.Contains("cottage")) return HousingArchitectureSize.Cottage;
        if (t.Contains("town")) return HousingArchitectureSize.Townhome;
        if (t.Contains("good")) return HousingArchitectureSize.GoodSize;
        return HousingArchitectureSize.GoodSize;
    }
}
