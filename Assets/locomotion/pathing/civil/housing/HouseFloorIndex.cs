using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Canonical house floor index: first floor = 1, basement = 0, sub-basement = -1 and below.
/// </summary>
public static class HouseFloorIndex
{
    public const int First = 1;
    public const int Basement = 0;
    public const int SubBasement = -1;

    public static bool TryParse(string text, out int floorIndex)
    {
        floorIndex = First;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string t = text.Trim();
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out floorIndex))
            return true;

        t = t.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        switch (t)
        {
            case "first":
            case "firstfloor":
            case "1st":
            case "ground":
                floorIndex = First;
                return true;
            case "basement":
            case "b":
            case "b0":
                floorIndex = Basement;
                return true;
            case "subbasement":
            case "sub":
            case "sb":
            case "b1":
                floorIndex = SubBasement;
                return true;
            default:
                if (t.StartsWith("b") && int.TryParse(t.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                {
                    floorIndex = n <= 0 ? n : -n;
                    return true;
                }
                return false;
        }
    }

    public static string Format(int floorIndex)
    {
        if (floorIndex == First)
            return "first";
        if (floorIndex == Basement)
            return "basement";
        if (floorIndex == SubBasement)
            return "subbasement";
        if (floorIndex < SubBasement)
            return "B" + Mathf.Abs(floorIndex);
        return floorIndex.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Y of the slab for this floor. Basement (0) sits at originY; first is +storyHeight; sub-basement is -storyHeight.</summary>
    public static float FloorY(int floorIndex, float storyHeightM, float originY = 0f)
    {
        float h = Mathf.Max(0.01f, storyHeightM);
        return originY + floorIndex * h;
    }
}
