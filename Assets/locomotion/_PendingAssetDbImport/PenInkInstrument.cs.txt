using UnityEngine;

/// <summary>Pen, optional quill, or brush writing with ink reservoir / nozzle.</summary>
[AddComponentMenu("Locomotion/Painting/Pen Ink Instrument")]
public sealed class PenInkInstrument : MonoBehaviour
{
    public enum Kind
    {
        Pen,
        Quill,
        Brush,
        Nib
    }

    public Kind kind = Kind.Quill;
    public InkMaterialProfile ink;
    public QuillNibDefinition nib;
    public PaintBrushDefinition brush;
    public PaintBrushRuntime brushRuntime;
    [Tooltip("Optional DrinkNozzleComponent (or any component with apertureRadiusM / loopPourActive).")]
    public Component nozzle;
    public Transform tip;
    public float reservoirLiters = 0.008f;
    public bool capOpen = true;
    [Min(0.1f)] public float breakForceN = 12f;
    public float lastRequestedBendDeg;
    public float lastClampedBendDeg;
    public float lastContactForceN;
    public bool nibBroken;
    public float lastHydroRidgeHeightM;
    public Vector3 lastHydroWorldForce;

    public Vector3 TipWorld => tip != null ? tip.position : transform.position;
    public Vector3 TipForward => tip != null ? tip.forward : transform.forward;

    public InkMaterialProfile ResolveInk()
    {
        if (ink == null)
            ink = InkMaterialProfile.CreateInkDefaults();
        return ink;
    }

    public QuillNibDefinition ResolveNib()
    {
        if (nib == null)
            nib = QuillNibDefinition.CreateDefaults();
        return nib;
    }

    public float EffectiveApertureRadiusM()
    {
        float r = ResolveNib().apertureRadiusM;
        r = Mathf.Max(r, PenInkNozzleAccess.GetApertureRadiusM(nozzle));
        return r;
    }

    public void ExpandAperture(float newRadiusM)
    {
        var n = ResolveNib();
        n.apertureRadiusM = Mathf.Max(n.apertureRadiusM, newRadiusM);
        PenInkNozzleAccess.SetApertureRadiusM(nozzle, newRadiusM);
    }

    public void OnPenCapOpen(bool open)
    {
        capOpen = open;
    }

    public InkNibBreakResult ContactCanvas(PaintCanvas canvas, float requestedBendDeg, float contactForceN,
        Collider contactCollider = null, Vector3 contactNormal = default, bool splatter = true)
    {
        var n = ResolveNib();
        lastRequestedBendDeg = requestedBendDeg;
        lastClampedBendDeg = n.ClampBendDeg(requestedBendDeg);
        lastContactForceN = contactForceN;
        return InkNibBreakAnalyzer.OnContact(this, canvas, requestedBendDeg, contactForceN, contactCollider, contactNormal, splatter);
    }
}

/// <summary>Reads Drink nozzle fields without referencing Locomotion.Drink.Runtime (cycle).</summary>
public static class PenInkNozzleAccess
{
    public static float GetApertureRadiusM(Component nozzle)
    {
        if (nozzle == null) return 0f;
        var f = nozzle.GetType().GetField("apertureRadiusM");
        if (f != null && f.FieldType == typeof(float))
            return (float)f.GetValue(nozzle);
        return 0f;
    }

    public static void SetApertureRadiusM(Component nozzle, float radiusM)
    {
        if (nozzle == null) return;
        var f = nozzle.GetType().GetField("apertureRadiusM");
        if (f != null && f.FieldType == typeof(float))
            f.SetValue(nozzle, Mathf.Max(GetApertureRadiusM(nozzle), radiusM));
    }

    public static bool GetLoopPourActive(Component nozzle)
    {
        if (nozzle == null) return false;
        var f = nozzle.GetType().GetField("loopPourActive");
        return f != null && f.FieldType == typeof(bool) && (bool)f.GetValue(nozzle);
    }

    public static Vector3 GetStreamTip(Component flowOrNozzle)
    {
        if (flowOrNozzle == null) return Vector3.zero;
        var m = flowOrNozzle.GetType().GetMethod("StreamTipPosition", System.Type.EmptyTypes);
        if (m != null && m.ReturnType == typeof(Vector3))
            return (Vector3)m.Invoke(flowOrNozzle, null);
        var tipF = flowOrNozzle.GetType().GetProperty("TipPosition");
        if (tipF != null && tipF.PropertyType == typeof(Vector3))
            return (Vector3)tipF.GetValue(flowOrNozzle);
        return flowOrNozzle.transform.position;
    }
}
