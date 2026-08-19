using System.Collections.Generic;
using UnityEngine;

/// <summary>Builtin + custom wall-piece brushes painted onto HouseConstructionPlan layers.</summary>
[CreateAssetMenu(fileName = "WallBrushCatalog", menuName = "Locomotion/Civil/Wall Brush Catalog")]
public sealed class WallBrushCatalog : ScriptableObject
{
    public List<WallBrushSpec> brushes = new List<WallBrushSpec>();

    static readonly HouseWallBrushKind[] BuiltinKinds =
    {
        HouseWallBrushKind.Electrical,
        HouseWallBrushKind.Plumbing,
        HouseWallBrushKind.Hvac,
        HouseWallBrushKind.Insulation,
        HouseWallBrushKind.Drywall,
        HouseWallBrushKind.Slats,
        HouseWallBrushKind.Studs
    };

    public WallBrushSpec FindByPaintByte(byte paintByte)
    {
        if (brushes == null) return null;
        for (int i = 0; i < brushes.Count; i++)
        {
            var b = brushes[i];
            if (b != null && b.paintByte == paintByte)
                return b;
        }
        return null;
    }

    public WallBrushSpec FindByKind(HouseWallBrushKind kind)
    {
        if (brushes == null) return null;
        for (int i = 0; i < brushes.Count; i++)
        {
            var b = brushes[i];
            if (b != null && b.kind == kind)
                return b;
        }
        return null;
    }

    public WallBrushSpec FindById(string brushId)
    {
        if (brushes == null || string.IsNullOrEmpty(brushId)) return null;
        for (int i = 0; i < brushes.Count; i++)
        {
            var b = brushes[i];
            if (b != null && b.brushId == brushId)
                return b;
        }
        return null;
    }

    public byte NextPaintByte()
    {
        int next = WallBrushSpec.FirstCatalogPaintByte;
        if (brushes == null) return (byte)next;
        for (int i = 0; i < brushes.Count; i++)
        {
            var b = brushes[i];
            if (b == null) continue;
            if (b.paintByte >= next)
                next = b.paintByte + 1;
        }
        return (byte)Mathf.Clamp(next, WallBrushSpec.FirstCatalogPaintByte, 255);
    }

    public void EnsureBuiltins()
    {
        if (brushes == null)
            brushes = new List<WallBrushSpec>();
        for (int i = 0; i < BuiltinKinds.Length; i++)
        {
            var kind = BuiltinKinds[i];
            if (FindByKind(kind) != null) continue;
            brushes.Add(CreateBuiltin(kind, NextPaintByte()));
        }
    }

    /// <summary>Add brush+! — new catalog entry targeting <paramref name="layerId"/> (Custom defaults to sheathing).</summary>
    public WallBrushSpec AddBrush(HouseWallBrushKind kind, string layerId)
    {
        EnsureBuiltins();
        var spec = CreateInstance<WallBrushSpec>();
        spec.kind = kind;
        spec.displayName = kind.ToString();
        spec.brushId = UniqueBrushId(kind.ToString().ToLowerInvariant());
        spec.targetLayerId = string.IsNullOrEmpty(layerId)
            ? WallBrushSpec.DefaultLayerId(kind)
            : layerId;
        spec.paintByte = NextPaintByte();
        spec.color = WallBrushSpec.DefaultColor(kind);
        spec.thicknessM = WallBrushSpec.DefaultThickness(kind);
        spec.bayWidthM = 0.406f;
        brushes.Add(spec);
        return spec;
    }

    public static WallBrushSpec CreateBuiltin(HouseWallBrushKind kind, byte paintByte)
    {
        var spec = CreateInstance<WallBrushSpec>();
        spec.kind = kind;
        spec.displayName = kind.ToString();
        spec.brushId = kind.ToString().ToLowerInvariant();
        spec.targetLayerId = WallBrushSpec.DefaultLayerId(kind);
        spec.paintByte = paintByte;
        spec.color = WallBrushSpec.DefaultColor(kind);
        spec.thicknessM = WallBrushSpec.DefaultThickness(kind);
        spec.bayWidthM = 0.406f;
        return spec;
    }

    string UniqueBrushId(string stem)
    {
        string id = stem;
        int n = 2;
        while (FindById(id) != null)
        {
            id = stem + "_" + n;
            n++;
        }
        return id;
    }
}
