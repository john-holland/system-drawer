using System.Collections.Generic;
using UnityEngine;

public static class StreetWireIndex
{
    static readonly List<PowerLineSpan> Spans = new List<PowerLineSpan>();

    public static IReadOnlyList<PowerLineSpan> All => Spans;

    public static void Register(PowerLineSpan span)
    {
        if (span == null || Spans.Contains(span)) return;
        if (string.IsNullOrEmpty(span.wireId))
            span.wireId = span.gameObject.name;
        Spans.Add(span);
    }

    public static void Unregister(PowerLineSpan span)
    {
        if (span != null) Spans.Remove(span);
    }

    public static PowerLineSpan FindById(string wireId)
    {
        if (string.IsNullOrEmpty(wireId)) return null;
        for (int i = 0; i < Spans.Count; i++)
            if (Spans[i] != null && Spans[i].wireId == wireId)
                return Spans[i];
        return null;
    }

    public static List<PowerLineSpan> QueryNear(Vector3 world, float radius)
    {
        var list = new List<PowerLineSpan>();
        float r2 = radius * radius;
        for (int i = 0; i < Spans.Count; i++)
        {
            var s = Spans[i];
            if (s == null) continue;
            Vector3 mid = s.SampleWorld(0.5f);
            if ((mid - world).sqrMagnitude <= r2)
                list.Add(s);
        }
        return list;
    }

    public static List<PowerLineSpan> QueryByPole(string poleId)
    {
        var list = new List<PowerLineSpan>();
        if (string.IsNullOrEmpty(poleId)) return list;
        for (int i = 0; i < Spans.Count; i++)
        {
            var s = Spans[i];
            if (s == null) continue;
            if (s.fromPoleId == poleId || s.toPoleId == poleId)
                list.Add(s);
        }
        return list;
    }

    public static List<PowerLineSpan> QueryOverIntersection(IntersectionLot lot)
    {
        var list = new List<PowerLineSpan>();
        if (lot == null) return list;
        Vector3 c = lot.pad != null ? lot.pad.ArrivalWorld : lot.transform.position;
        return QueryNear(c, 24f);
    }
}
