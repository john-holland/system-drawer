using UnityEngine;

public enum HouseWallBrushKind
{
    Electrical = 0,
    Plumbing = 1,
    Hvac = 2,
    Insulation = 3,
    Drywall = 4,
    Slats = 5,
    Studs = 6,
    Custom = 7
}

/// <summary>Discrete wall-piece brush: paints one construction layer and stamps a prefab.</summary>
[CreateAssetMenu(fileName = "WallBrush", menuName = "Locomotion/Civil/Wall Brush")]
public sealed class WallBrushSpec : ScriptableObject
{
    public const byte FirstCatalogPaintByte = 8;

    public string brushId = "electrical";
    public string displayName = "Electrical";
    public HouseWallBrushKind kind = HouseWallBrushKind.Electrical;
    public string targetLayerId = "rough_mep";
    public byte paintByte = FirstCatalogPaintByte;
    public Color color = new Color(0.99f, 0.85f, 0.21f, 1f);
    public GameObject prefab;
    public float thicknessM = 0.04f;
    public float bayWidthM = 0.406f;

    public static string DefaultLayerId(HouseWallBrushKind kind)
    {
        switch (kind)
        {
            case HouseWallBrushKind.Electrical:
            case HouseWallBrushKind.Plumbing:
            case HouseWallBrushKind.Hvac:
                return "rough_mep";
            case HouseWallBrushKind.Insulation:
                return "insulation";
            case HouseWallBrushKind.Drywall:
                return "sheathing";
            case HouseWallBrushKind.Slats:
            case HouseWallBrushKind.Studs:
                return "studs";
            default:
                return "sheathing";
        }
    }

    public static Color DefaultColor(HouseWallBrushKind kind)
    {
        switch (kind)
        {
            case HouseWallBrushKind.Electrical: return HouseFoundationPalette.Yellow;
            case HouseWallBrushKind.Plumbing: return HouseFoundationPalette.Purple;
            case HouseWallBrushKind.Hvac: return HouseFoundationPalette.Blue;
            case HouseWallBrushKind.Insulation: return HouseFoundationPalette.Red;
            case HouseWallBrushKind.Drywall: return HouseFoundationPalette.Construction;
            case HouseWallBrushKind.Slats: return new Color(0.72f, 0.62f, 0.42f, 1f);
            case HouseWallBrushKind.Studs: return new Color(0.62f, 0.48f, 0.28f, 1f);
            default: return HouseFoundationPalette.Construction;
        }
    }

    public static float DefaultThickness(HouseWallBrushKind kind)
    {
        switch (kind)
        {
            case HouseWallBrushKind.Electrical: return 0.04f;
            case HouseWallBrushKind.Plumbing: return 0.05f;
            case HouseWallBrushKind.Hvac: return 0.2f;
            case HouseWallBrushKind.Insulation: return 0.09f;
            case HouseWallBrushKind.Drywall: return 0.013f;
            case HouseWallBrushKind.Slats: return 0.02f;
            case HouseWallBrushKind.Studs: return 0.038f;
            default: return 0.02f;
        }
    }

    public void ApplyKindDefaults()
    {
        if (string.IsNullOrEmpty(targetLayerId) || kind != HouseWallBrushKind.Custom)
            targetLayerId = DefaultLayerId(kind);
        color = DefaultColor(kind);
        thicknessM = DefaultThickness(kind);
        if (string.IsNullOrEmpty(displayName) || displayName == "Custom" || displayName == kind.ToString())
            displayName = kind.ToString();
    }
}
