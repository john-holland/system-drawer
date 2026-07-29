using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One row in the hairdo designer list: precedence + enable + weight.</summary>
[Serializable]
public sealed class HairdoBlendSlot
{
    public HairdoCutKind kind;
    public int precedence;
    public bool enabled;
    [Range(0f, 1f)] public float weight = 1f;
}

/// <summary>
/// Multi-select haircut blend: normalize enabled weights, lerp continuous params,
/// discrete part from highest weight (lower precedence / catalog index on ties).
/// </summary>
[Serializable]
public sealed class HairdoBlend
{
    public List<HairdoBlendSlot> slots = new List<HairdoBlendSlot>();

    public static HairdoBlend CreateDefault()
    {
        var b = new HairdoBlend();
        b.ResetToCrewDefault();
        return b;
    }

    public void EnsureSlots()
    {
        slots ??= new List<HairdoBlendSlot>();
        var kinds = HairdoPresetCatalog.All;
        if (slots.Count == kinds.Length)
        {
            for (int i = 0; i < kinds.Length; i++)
                slots[i].kind = kinds[i];
            return;
        }

        var map = new Dictionary<HairdoCutKind, HairdoBlendSlot>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            map[slots[i].kind] = slots[i];
        }

        slots.Clear();
        for (int i = 0; i < kinds.Length; i++)
        {
            if (map.TryGetValue(kinds[i], out var existing) && existing != null)
            {
                existing.kind = kinds[i];
                slots.Add(existing);
            }
            else
            {
                slots.Add(new HairdoBlendSlot
                {
                    kind = kinds[i],
                    precedence = 0,
                    enabled = false,
                    weight = 0f
                });
            }
        }
    }

    public void ResetToCrewDefault()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].precedence = 0;
            slots[i].enabled = slots[i].kind == HairdoCutKind.Crew;
            slots[i].weight = slots[i].kind == HairdoCutKind.Crew ? 1f : 0f;
        }
    }

    /// <summary>Display order: precedence ascending, then catalog index.</summary>
    public List<int> SortedSlotIndices()
    {
        EnsureSlots();
        var order = new List<int>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
            order.Add(i);
        order.Sort((a, b) =>
        {
            int cmp = slots[a].precedence.CompareTo(slots[b].precedence);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });
        return order;
    }

    public bool TryEvaluate(out HairdoParams blended, out float front, out float side, out float back, out float length)
    {
        EnsureSlots();
        var enabled = new List<(int index, HairdoBlendSlot slot, HairdoParams preset)>();
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || !s.enabled || s.weight <= 1e-6f) continue;
            enabled.Add((i, s, HairdoPresetCatalog.Get(s.kind)));
        }

        if (enabled.Count == 0)
        {
            blended = new HairdoParams();
            front = side = back = length = 0f;
            return false;
        }

        float wSum = 0f;
        for (int i = 0; i < enabled.Count; i++)
            wSum += enabled[i].slot.weight;
        if (wSum < 1e-6f)
        {
            blended = new HairdoParams();
            front = side = back = length = 0f;
            return false;
        }

        var sources = new HairdoParams[enabled.Count];
        var weights = new float[enabled.Count];
        for (int i = 0; i < enabled.Count; i++)
        {
            sources[i] = enabled[i].preset;
            weights[i] = enabled[i].slot.weight / wSum;
        }

        HairdoParams partSource = PickDiscreteWinner(enabled);
        blended = HairdoParams.WeightedAverage(sources, weights, partSource);
        front = blended.DiamondFront01;
        side = blended.DiamondSide01;
        back = blended.DiamondBack01;
        length = blended.DiamondLength01;
        return true;
    }

    public HairdoParams EvaluateOrDefault()
    {
        if (TryEvaluate(out var p, out _, out _, out _, out _))
            return p;
        return HairdoPresetCatalog.Get(HairdoCutKind.Crew);
    }

    /// <summary>Normalized weights for enabled slots only (kind → weight01).</summary>
    public Dictionary<HairdoCutKind, float> NormalizedEnabledWeights()
    {
        EnsureSlots();
        var result = new Dictionary<HairdoCutKind, float>();
        float wSum = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || !s.enabled || s.weight <= 1e-6f) continue;
            wSum += s.weight;
        }

        if (wSum < 1e-6f) return result;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || !s.enabled || s.weight <= 1e-6f) continue;
            result[s.kind] = s.weight / wSum;
        }

        return result;
    }

    static HairdoParams PickDiscreteWinner(List<(int index, HairdoBlendSlot slot, HairdoParams preset)> enabled)
    {
        int best = 0;
        for (int i = 1; i < enabled.Count; i++)
        {
            var a = enabled[best];
            var b = enabled[i];
            if (b.slot.weight > a.slot.weight + 1e-6f)
            {
                best = i;
                continue;
            }

            if (Mathf.Abs(b.slot.weight - a.slot.weight) > 1e-6f)
                continue;

            if (b.slot.precedence < a.slot.precedence)
            {
                best = i;
                continue;
            }

            if (b.slot.precedence == a.slot.precedence && b.index < a.index)
                best = i;
        }

        return enabled[best].preset;
    }
}
