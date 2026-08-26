using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Standard brush set: fan, pointed, square, round, angle, flat liner.
/// </summary>
[CreateAssetMenu(fileName = "PaintBrushCatalog", menuName = "Locomotion/Painting/Brush Catalog")]
public sealed class PaintBrushCatalog : ScriptableObject
{
    public List<PaintBrushDefinition> brushes = new List<PaintBrushDefinition>();

    public PaintBrushDefinition Get(PaintBrushDefinition.BrushKind kind)
    {
        for (int i = 0; i < brushes.Count; i++)
        {
            if (brushes[i] != null && brushes[i].kind == kind)
                return brushes[i];
        }
        return null;
    }

    public static PaintBrushDefinition CreateBuiltin(PaintBrushDefinition.BrushKind kind)
    {
        var d = ScriptableObject.CreateInstance<PaintBrushDefinition>();
        d.kind = kind;
        d.displayName = kind.ToString();
        switch (kind)
        {
            case PaintBrushDefinition.BrushKind.Fan:
                d.ferruleHairLine = new HairLineCurve
                {
                    radiusByAzimuth01 = new AnimationCurve(
                        new Keyframe(0f, 1.2f), new Keyframe(0.25f, 0.35f),
                        new Keyframe(0.5f, 1.2f), new Keyframe(0.75f, 0.35f), new Keyframe(1f, 1.2f))
                };
                d.endCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.85f);
                d.conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.6f);
                break;
            case PaintBrushDefinition.BrushKind.Pointed:
                d.endCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.05f);
                d.conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
                break;
            case PaintBrushDefinition.BrushKind.Square:
                d.endCurve = AnimationCurve.Constant(0f, 1f, 1f);
                d.conicalCurve = AnimationCurve.Constant(0f, 1f, 1f);
                d.ferruleHairLine = HairLineCurve.Constant(1.05f);
                break;
            case PaintBrushDefinition.BrushKind.Angle:
                d.ferruleAngle = new HairLineAngleCurve
                {
                    emergenceAngleDegByAzimuth01 = AnimationCurve.Linear(0f, -25f, 1f, 25f)
                };
                d.endCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
                break;
            case PaintBrushDefinition.BrushKind.Quill:
                d.displayName = "Quill";
                d.endCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.02f);
                d.conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.22f);
                d.bristleLengthM = 0.018f;
                d.ferruleRadiusM = 0.005f;
                d.plumeTipHold = 0.92f;
                d.ferruleAngle = new HairLineAngleCurve
                {
                    emergenceAngleDegByAzimuth01 = AnimationCurve.Linear(0f, -10f, 1f, 10f)
                };
                d.defaultPaintColor = new Color(0.05f, 0.06f, 0.12f, 1f);
                break;
            case PaintBrushDefinition.BrushKind.Nib:
                d.displayName = "Nib";
                d.endCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.04f);
                d.conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.28f);
                d.bristleLengthM = 0.014f;
                d.ferruleRadiusM = 0.004f;
                d.plumeTipHold = 0.9f;
                d.ferruleAngle = new HairLineAngleCurve
                {
                    emergenceAngleDegByAzimuth01 = AnimationCurve.Linear(0f, -8f, 1f, 8f)
                };
                d.defaultPaintColor = new Color(0.05f, 0.06f, 0.12f, 1f);
                break;
            case PaintBrushDefinition.BrushKind.FlatLiner:
                d.ferruleHairLine = new HairLineCurve
                {
                    radiusByAzimuth01 = new AnimationCurve(
                        new Keyframe(0f, 1.4f), new Keyframe(0.5f, 0.2f), new Keyframe(1f, 1.4f))
                };
                d.endCurve = AnimationCurve.Constant(0f, 1f, 0.9f);
                d.bristleLengthM = 0.04f;
                break;
            default: // Round
                d.endCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f);
                d.conicalCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.05f);
                break;
        }
        return d;
    }

    public void EnsureBuiltins()
    {
        if (brushes == null)
            brushes = new List<PaintBrushDefinition>();
        foreach (PaintBrushDefinition.BrushKind k in System.Enum.GetValues(typeof(PaintBrushDefinition.BrushKind)))
        {
            if (Get(k) != null) continue;
            brushes.Add(CreateBuiltin(k));
        }
    }
}
