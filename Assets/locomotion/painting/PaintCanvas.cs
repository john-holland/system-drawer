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
