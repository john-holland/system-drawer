using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoilHorizonId
{
    O = 0,
    A = 1,
    E = 2,
    B = 3,
    C = 4,
    R = 5
}

public static class SoilHorizonOrder
{
    public static readonly SoilHorizonId[] Pedological =
    {
        SoilHorizonId.O,
        SoilHorizonId.A,
        SoilHorizonId.E,
        SoilHorizonId.B,
        SoilHorizonId.C,
        SoilHorizonId.R
    };
}

[Serializable]
public sealed class SoilHorizonLayer
{
    public SoilHorizonId id = SoilHorizonId.A;
    public string compositionId = "loam";
    [Min(0.01f)] public float thicknessM = 0.2f;
    [Range(0f, 1f)] public float destructibility01 = 0.5f;
}

/// <summary>Curvature of destructibility for one grid slot. Resilience applies only when GMO features are enabled.</summary>
[Serializable]
public sealed class StressAdapter
{
    public int slotX;
    public int slotY;
    [Min(0f)] public float curvature = 1f;
    [Range(0f, 1f)] public float resilience01;

    public float Destructibility(float base01, bool allowSpecialGmo)
    {
        float curved = Mathf.Clamp01(base01 * Mathf.Max(0f, curvature));
        if (!allowSpecialGmo)
            return curved;
        return Mathf.Clamp01(curved * (1f - Mathf.Clamp01(resilience01)));
    }
}

[Serializable]
public sealed class GrassBladeRecord
{
    public string id;
    public float shedSize;
    public float cutT = 1f;
    public float cutAngleDeg;
    public float radius;
    public float ribbonWidth;
    public bool splineCut;
    public bool cut;
    public bool shed;
    public float narrativeSeconds;
}

[Serializable]
public sealed class GroundCompositionSnapshot
{
    public float narrativeSeconds;
    public List<GrassBladeRecord> blades = new List<GrassBladeRecord>();
}

/// <summary>Working soil and blade save. Wind playback may quantize cut endpoints.</summary>
[CreateAssetMenu(fileName = "GroundCompositionSet", menuName = "Locomotion/Civil/Ground Composition Set")]
public sealed class GroundCompositionSet : ScriptableObject
{
    public List<SoilHorizonLayer> horizons = new List<SoilHorizonLayer>();
    public List<StressAdapter> stress = new List<StressAdapter>();
    public List<GrassBladeRecord> blades = new List<GrassBladeRecord>();
    public List<GroundCompositionSnapshot> snapshots = new List<GroundCompositionSnapshot>();
    public bool allowSPECIALGMOFeatures;
    public bool rampant;
    public Bounds rampantBounds;
    public float windQuantize = 0.05f;

    public void EnsureDefaultHorizons()
    {
        if (horizons == null)
            horizons = new List<SoilHorizonLayer>();
        if (horizons.Count > 0)
            return;
        SoilHorizonId[] order = SoilHorizonOrder.Pedological;
        string[] minerals = { "organic", "loam", "sand", "clay", "silt", "bedrock" };
        for (int i = 0; i < order.Length; i++)
        {
            horizons.Add(new SoilHorizonLayer
            {
                id = order[i],
                compositionId = minerals[i],
                thicknessM = 0.15f + i * 0.05f,
                destructibility01 = order[i] == SoilHorizonId.R ? 0.05f : 0.55f
            });
        }
    }

    public SoilHorizonLayer FindHorizon(SoilHorizonId id)
    {
        if (horizons == null)
            return null;
        for (int i = 0; i < horizons.Count; i++)
        {
            if (horizons[i] != null && horizons[i].id == id)
                return horizons[i];
        }
        return null;
    }

    public string CompositionId(SoilHorizonId id)
    {
        SoilHorizonLayer layer = FindHorizon(id);
        return layer != null && !string.IsNullOrEmpty(layer.compositionId) ? layer.compositionId : "loam";
    }

    public StressAdapter Slot(int x, int y)
    {
        if (stress != null)
        {
            for (int i = 0; i < stress.Count; i++)
            {
                StressAdapter slot = stress[i];
                if (slot != null && slot.slotX == x && slot.slotY == y)
                    return slot;
            }
        }
        return new StressAdapter { slotX = x, slotY = y, curvature = 1f };
    }

    public float DestructibilityForHorizon(SoilHorizonLayer layer)
    {
        if (layer == null)
            return 0f;
        StressAdapter slot = Slot((int)layer.id, 0);
        return slot.Destructibility(layer.destructibility01, allowSPECIALGMOFeatures);
    }

    public GrassBladeRecord FindBlade(string id)
    {
        if (blades == null || string.IsNullOrEmpty(id))
            return null;
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null && blades[i].id == id)
                return blades[i];
        }
        return null;
    }

    public GrassBladeRecord RecordCut(
        string id, float angleDeg, float radius, float ribbonWidth, bool spline, float shedSize, float narrativeSeconds)
    {
        GrassBladeRecord blade = EnsureBlade(id);
        blade.cut = true;
        blade.cutAngleDeg = angleDeg;
        blade.radius = Mathf.Max(0f, radius);
        blade.ribbonWidth = Mathf.Max(0f, ribbonWidth);
        blade.splineCut = spline;
        blade.shedSize = Mathf.Max(0f, shedSize);
        blade.narrativeSeconds = narrativeSeconds;
        blade.cutT = Quantize(Mathf.Clamp01(1f - shedSize), windQuantize);
        return blade;
    }

    public GrassBladeRecord RecordShed(string id, float shedSize, float narrativeSeconds)
    {
        GrassBladeRecord blade = EnsureBlade(id);
        blade.shed = true;
        blade.shedSize = Mathf.Max(0f, shedSize);
        blade.narrativeSeconds = narrativeSeconds;
        blade.cutT = Quantize(Mathf.Clamp01(1f - blade.shedSize), windQuantize);
        return blade;
    }

    public void QuantizeCuts(float cell)
    {
        windQuantize = Mathf.Max(0f, cell);
        if (blades == null)
            return;
        for (int i = 0; i < blades.Count; i++)
        {
            GrassBladeRecord blade = blades[i];
            if (blade == null || !blade.cut)
                continue;
            blade.cutT = Quantize(blade.cutT, windQuantize);
        }
    }

    public GroundCompositionSnapshot CaptureSnapshot(float narrativeSeconds)
    {
        var snap = new GroundCompositionSnapshot
        {
            narrativeSeconds = narrativeSeconds,
            blades = CloneBlades(blades)
        };
        if (snapshots == null)
            snapshots = new List<GroundCompositionSnapshot>();
        snapshots.Add(snap);
        return snap;
    }

    public bool RestoreSnapshot(float narrativeSeconds)
    {
        GroundCompositionSnapshot best = null;
        if (snapshots != null)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                GroundCompositionSnapshot snap = snapshots[i];
                if (snap == null || snap.narrativeSeconds > narrativeSeconds + 1e-4f)
                    continue;
                if (best == null || snap.narrativeSeconds >= best.narrativeSeconds)
                    best = snap;
            }
        }
        blades = best != null ? CloneBlades(best.blades) : new List<GrassBladeRecord>();
        return best != null;
    }

    public void MarkRampant(Bounds bounds)
    {
        rampant = true;
        rampantBounds = bounds;
    }

    public void ClearRampant()
    {
        rampant = false;
        rampantBounds = new Bounds();
    }

    GrassBladeRecord EnsureBlade(string id)
    {
        if (blades == null)
            blades = new List<GrassBladeRecord>();
        GrassBladeRecord found = FindBlade(id);
        if (found != null)
            return found;
        found = new GrassBladeRecord { id = id, cutT = 1f };
        blades.Add(found);
        return found;
    }

    static float Quantize(float value, float cell)
    {
        if (cell <= 1e-5f)
            return Mathf.Clamp01(value);
        return Mathf.Clamp01(Mathf.Round(value / cell) * cell);
    }

    static List<GrassBladeRecord> CloneBlades(List<GrassBladeRecord> source)
    {
        var copy = new List<GrassBladeRecord>();
        if (source == null)
            return copy;
        for (int i = 0; i < source.Count; i++)
        {
            GrassBladeRecord blade = source[i];
            if (blade == null)
                continue;
            copy.Add(new GrassBladeRecord
            {
                id = blade.id,
                shedSize = blade.shedSize,
                cutT = blade.cutT,
                cutAngleDeg = blade.cutAngleDeg,
                radius = blade.radius,
                ribbonWidth = blade.ribbonWidth,
                splineCut = blade.splineCut,
                cut = blade.cut,
                shed = blade.shed,
                narrativeSeconds = blade.narrativeSeconds
            });
        }
        return copy;
    }
}
