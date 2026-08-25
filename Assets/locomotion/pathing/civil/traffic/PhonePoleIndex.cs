using System.Collections.Generic;
using UnityEngine;

public static class PhonePoleIndex
{
    static readonly List<UtilityPoleAssembly> Poles = new List<UtilityPoleAssembly>();

    public static IReadOnlyList<UtilityPoleAssembly> All => Poles;

    public static void Register(UtilityPoleAssembly pole)
    {
        if (pole == null || Poles.Contains(pole)) return;
        if (string.IsNullOrEmpty(pole.poleId))
            pole.poleId = pole.gameObject.name;
        Poles.Add(pole);
    }

    public static void Unregister(UtilityPoleAssembly pole)
    {
        if (pole != null) Poles.Remove(pole);
    }

    public static UtilityPoleAssembly FindById(string poleId)
    {
        if (string.IsNullOrEmpty(poleId)) return null;
        for (int i = 0; i < Poles.Count; i++)
            if (Poles[i] != null && Poles[i].poleId == poleId)
                return Poles[i];
        return null;
    }

    public static List<UtilityPoleAssembly> QueryNear(Vector3 world, float radius)
    {
        var list = new List<UtilityPoleAssembly>();
        float r2 = radius * radius;
        for (int i = 0; i < Poles.Count; i++)
        {
            var p = Poles[i];
            if (p == null) continue;
            if ((p.transform.position - world).sqrMagnitude <= r2)
                list.Add(p);
        }
        return list;
    }
}
