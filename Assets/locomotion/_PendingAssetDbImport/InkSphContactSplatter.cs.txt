using UnityEngine;

/// <summary>Always-on contact splatter: SPH seed + transfer decal using current drying / layer shader.</summary>
public static class InkSphContactSplatter
{
    public static int SeedHydro(PaintCanvas canvas, Vector3 worldPoint, Color pigment, float mass, float wet01, int count = 10)
    {
        if (canvas == null) return 0;
        var hydro = canvas.GetComponent<PaintCanvasHydroSolver>();
        if (hydro == null)
            hydro = canvas.Hydro;
        if (hydro == null) return 0;
        int before = hydro.ActiveCount;
        hydro.SeedFromStamp(worldPoint, pigment, mass, wet01, count);
        return Mathf.Max(0, hydro.ActiveCount - before);
    }

    public static void ApplyDecal(PaintCanvas canvas, Collider source, Vector3 worldPoint, Vector3 normal, Color pigment)
    {
        if (canvas == null) return;
        var decal = canvas.GetComponent<PaintTransferDecal>();
        if (decal == null)
            decal = canvas.gameObject.AddComponent<PaintTransferDecal>();
        var driver = canvas.GetComponent<InkDryingLayerDriver>();
        if (driver != null && driver.layerMaterial != null)
            decal.decalMaterialTemplate = driver.layerMaterial;
        if (source == null)
            source = canvas.GetComponent<Collider>();
        if (normal.sqrMagnitude < 1e-6f)
            normal = canvas.transform.forward;
        decal.TryApply(source, worldPoint, normal, pigment);
    }

    public static void Splatter(PenInkInstrument instrument, PaintCanvas canvas, Vector3 worldPoint, Vector3 normal,
        Collider source = null)
    {
        Color pigment = Color.black;
        if (instrument != null)
            pigment = instrument.ResolveInk().defaultInkColor;
        else if (canvas != null && canvas.inkProfile != null)
            pigment = canvas.inkProfile.defaultInkColor;
        SeedHydro(canvas, worldPoint, pigment, 0.08f, 1f, 12);
        ApplyDecal(canvas, source, worldPoint, normal, pigment);
    }
}
