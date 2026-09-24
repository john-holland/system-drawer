using UnityEngine;

/// <summary>
/// Canvas plane host for paint layers, viscosity cache, and stroke stamping.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Canvas")]
public sealed class PaintCanvas : MonoBehaviour
{
    public enum SurfaceKind
    {
        Plane,
        CurvedDecal
    }

    public PaintCanvasLayerStack layerStack;
    public Renderer canvasRenderer;
    public int viscosityWidth = 128;
    public int viscosityHeight = 128;
    [Range(0f, 2f)] public float totalViscosity = 1f;
    [Tooltip("Higher = less streaky deposit.")]
    [Range(0f, 1f)] public float streakiness = 0.35f;
    [Tooltip("Canvas film surface tension (hydro beads / gloss).")]
    [Range(0f, 1f)] public float surfaceTension = 0.85f;
    public InkMaterialProfile inkProfile;
    public SurfaceKind surfaceKind = SurfaceKind.Plane;

    PaintPlanarViscosityCache _visc;
    PaintCanvasHydroSolver _hydro;

    public PaintPlanarViscosityCache Viscosity
    {
        get
        {
            EnsureViscosity();
            return _visc;
        }
    }

    public PaintCanvasHydroSolver Hydro
    {
        get
        {
            EnsureHydro();
            return _hydro;
        }
    }

    public Plane WorldPlane => new Plane(transform.forward, transform.position);

    void Awake()
    {
        if (layerStack != null)
            layerStack.EnsureBaseLayer();
        ApplyInkProfile();
        EnsureViscosity();
        EnsureHydro();
    }

    public void ApplyInkProfile()
    {
        if (inkProfile == null || layerStack == null)
            return;
        layerStack.ApplyInkProfile(inkProfile);
        if (inkProfile.MixesIntoSingleLayer)
            streakiness = Mathf.Min(streakiness, 0.2f);
    }

    public void EnsureViscosity()
    {
        if (_visc == null)
            _visc = new PaintPlanarViscosityCache(viscosityWidth, viscosityHeight);
    }

    public void EnsureHydro()
    {
        _hydro = GetComponent<PaintCanvasHydroSolver>();
        if (_hydro == null)
            _hydro = gameObject.AddComponent<PaintCanvasHydroSolver>();
        _hydro.canvas = this;
        _hydro.surfaceTension = surfaceTension;
    }

    void OnDestroy() => _visc?.Dispose();
    public bool WorldToCanvasUv(Vector3 world, out Vector2 uv)
    {
        if (surfaceKind == SurfaceKind.CurvedDecal)
        {
            var curved = GetComponent<PaintCanvasCurvedDecal>();
            if (curved != null)
                return curved.WorldToUv(world, out uv);
        }
        Vector3 local = transform.InverseTransformPoint(world);
        uv = new Vector2(local.x + 0.5f, local.y + 0.5f);
        return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
    }

    public Color SamplePaintColor(Vector2 uv)
    {
        if (_visc != null)
        {
            _visc.SampleUv(uv, out Color c);
            if (c.b > 0.01f)
            {
                var wet = layerStack != null ? layerStack.TopWetLayer() : null;
                Color albedo = wet != null ? wet.albedo : Color.white;
                return Color.Lerp(albedo, new Color(c.r, c.g, c.b, 1f), 0.5f);
            }
        }
        var layer = layerStack != null ? layerStack.TopWetLayer() : null;
        return layer != null ? layer.albedo : Color.white;
    }

    public void BindMaterials()
    {
        if (canvasRenderer == null || _visc == null) return;
        var mat = canvasRenderer.material;
        _visc.Apply();
        mat.SetTexture("_PaintViscosityTex", _visc.Texture);
        mat.SetFloat("_PaintViscosity", totalViscosity);
        mat.SetFloat("_PaintStreakiness", streakiness);
        mat.SetFloat("_PaintSurfaceTension", surfaceTension);
        // Viscosity RGBA: R=wet G=dry B=mass A=caustic/spec film
        if (mat.HasProperty("_PaintWetDryCaustic"))
            mat.SetTexture("_PaintWetDryCaustic", _visc.Texture);
    }
}

// <auto-merged-from-InkMaterialProfile>
// ---- from Assets/locomotion/painting/InkMaterialProfile.cs ----
/// <summary>
/// Ink vs paintlike film: fast SPH dry, thin layers, high dilution, single-layer mix by default.
/// </summary>
[CreateAssetMenu(fileName = "InkMaterialProfile", menuName = "Locomotion/Painting/Ink Material Profile")]
public sealed class InkMaterialProfile : ScriptableObject
{
    [Tooltip("When false, each deposit dilutes into the top wet layer. Paintlike ink keeps stacked films.")]
    public bool paintlikeInk;
    public bool singleLayerMixing = true;
    [Range(0f, 1f)] public float dilution = 0.75f;
    [Min(0.00005f)] public float layerThicknessM = 0.0004f;
    [Tooltip("SPH wet decay scale. Paint hydro uses 0.02; ink defaults much faster.")]
    [Min(0.01f)] public float sphDryRate = 0.45f;
    [Range(0f, 1f)] public float specularWet = 0.85f;
    [Range(0f, 1f)] public float specularDry = 0.08f;
    [Tooltip("Seconds the film stays see-through while drying (whiteboard spy gag).")]
    [Min(0f)] public float seeThroughDrySeconds = 30f;
    [Range(0f, 1f)] public float seeThroughAlpha = 0.12f;
    public Color defaultInkColor = new Color(0.05f, 0.06f, 0.12f, 1f);

    public static InkMaterialProfile CreateInkDefaults()
    {
        var p = CreateInstance<InkMaterialProfile>();
        p.name = "InkDefaults";
        p.paintlikeInk = false;
        p.singleLayerMixing = true;
        p.dilution = 0.75f;
        p.layerThicknessM = 0.0004f;
        p.sphDryRate = 0.45f;
        p.specularWet = 0.85f;
        p.specularDry = 0.08f;
        p.seeThroughDrySeconds = 30f;
        p.seeThroughAlpha = 0.12f;
        return p;
    }

    public bool MixesIntoSingleLayer => singleLayerMixing && !paintlikeInk;
}

// ---- from Assets/locomotion/painting/QuillNibDefinition.cs ----
/// <summary>
/// Quill/pen nib: angle-limited Gaussian spread plus optional max bend against the page.
/// </summary>
[CreateAssetMenu(fileName = "QuillNibDefinition", menuName = "Locomotion/Painting/Quill Nib")]
public sealed class QuillNibDefinition : ScriptableObject
{
    [Tooltip("Procedural default: 10 degrees of nib flex against the page.")]
    [Range(0f, 45f)] public float maxBendDeg = 10f;
    [Range(0.05f, 2f)] public float gaussianSigma = 0.35f;
    [Range(1f, 40f)] public float maxSpreadAngleDeg = 18f;
    [Min(0.0001f)] public float apertureRadiusM = 0.0008f;
    [Min(0.001f)] public float nibLengthM = 0.018f;
    [Range(0f, 1f)] public float tipHold = 0.92f;

    public static QuillNibDefinition CreateDefaults()
    {
        var n = CreateInstance<QuillNibDefinition>();
        n.name = "QuillNibDefaults";
        n.maxBendDeg = 10f;
        n.gaussianSigma = 0.35f;
        n.maxSpreadAngleDeg = 18f;
        n.apertureRadiusM = 0.0008f;
        n.nibLengthM = 0.018f;
        n.tipHold = 0.92f;
        return n;
    }

    /// <summary>Spread weight in [0,1] for contact angle vs nib axis, clamped to maxSpreadAngleDeg.</summary>
    public float GaussianSpread01(float contactAngleDeg)
    {
        float limited = Mathf.Min(Mathf.Abs(contactAngleDeg), maxSpreadAngleDeg);
        float x = limited / Mathf.Max(1e-4f, maxSpreadAngleDeg);
        float s = Mathf.Max(1e-4f, gaussianSigma);
        return Mathf.Exp(-0.5f * (x * x) / (s * s));
    }

    public float ClampBendDeg(float requestedBendDeg) =>
        Mathf.Clamp(requestedBendDeg, -maxBendDeg, maxBendDeg);

    public float Stress01(float bendDeg, float contactForceN, float breakForceN)
    {
        float bend = Mathf.Abs(bendDeg) / Mathf.Max(1e-4f, maxBendDeg);
        float force = contactForceN / Mathf.Max(1e-4f, breakForceN);
        return Mathf.Max(bend, force);
    }
}

// ---- from Assets/locomotion/painting/PenInkInstrument.cs ----
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


// <auto-merged-ink-break-stub>
public struct InkNibBreakResult
{
    public bool broke;
    public float stress01;
    public float clampedBendDeg;
    public float requestedBendDeg;
    public Vector3 breakWorld;
    public GameObject debris;
    public int hydroSeeded;
}

public static class InkNibBreakAnalyzer
{
    public static InkNibBreakResult OnContact(PenInkInstrument instrument, PaintCanvas canvas,
        float requestedBendDeg, float contactForceN, Collider contactCollider = null, Vector3 contactNormal = default,
        bool splatter = true)
    {
        var result = new InkNibBreakResult { requestedBendDeg = requestedBendDeg };
        if (instrument == null) return result;
        var nib = instrument.ResolveNib();
        result.clampedBendDeg = nib.ClampBendDeg(requestedBendDeg);
        result.stress01 = nib.Stress01(requestedBendDeg, contactForceN, Mathf.Max(0.1f, instrument.breakForceN));
        result.breakWorld = instrument.TipWorld;
        return result;
    }
}
