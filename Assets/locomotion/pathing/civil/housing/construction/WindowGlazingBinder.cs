using UnityEngine;

/// <summary>Single or double-vacuum panes. Cavity has no collider / hit.</summary>
public static class WindowGlazingBinder
{
    public const string CavityName = "igu_cavity";

    public static int BindPanes(GameObject host, WindowAssemblySpec spec)
    {
        if (host == null || spec == null) return 0;
        spec.PaneMuntinWorldSizes(out var paneSize, out _);
        float thick = Mathf.Max(0.002f, spec.paneThickness);
        ClearOld(host);
        int n = 0;
        n += AddPane(host, "pane_outer", paneSize, thick, 0f);
        if (spec.glazing == WindowGlazingKind.DoubleVacuum)
        {
            float gap = Mathf.Max(0.004f, spec.vacuumGap);
            var cavity = new GameObject(CavityName);
            cavity.transform.SetParent(host.transform, false);
            cavity.transform.localPosition = new Vector3(0f, 0f, thick * 0.5f + gap * 0.5f);
            n += AddPane(host, "pane_inner", paneSize, thick, thick + gap);
            // todo: review: we should add advection properties for vacuum to the cavity, like thickness and density.
        }
        return n;
    }

    public static bool CavityHasCollider(GameObject host)
    {
        if (host == null) return false;
        var t = host.transform.Find(CavityName);
        return t != null && t.GetComponent<Collider>() != null;
    }

    static int AddPane(GameObject host, string name, Vector2 paneSize, float thick, float z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(host.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        go.transform.localScale = new Vector3(paneSize.x, paneSize.y, thick);
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
        return 1;
    }

    static void ClearOld(GameObject host)
    {
        for (int i = host.transform.childCount - 1; i >= 0; i--)
        {
            var c = host.transform.GetChild(i);
            if (c.name.StartsWith("pane_") || c.name == CavityName)
                UnityEngine.Object.DestroyImmediate(c.gameObject);
        }
    }
}
