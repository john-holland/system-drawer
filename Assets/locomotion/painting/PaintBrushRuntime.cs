using UnityEngine;

/// <summary>
/// Runtime brush: hair bristle load field, steal from piles, deposit onto canvas.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Brush Runtime")]
public sealed class PaintBrushRuntime : MonoBehaviour
{
    public PaintBrushDefinition definition;
    public Transform ferrule;
    public Transform tip;
    public PaintCanvas canvas;
    public PaintPileLiquidDriver pileSource;
    public Renderer bristleRenderer;
    [Range(0f, 1f)] public float load01;
    public Color loadedColor = Color.white;

    HairPlumeConfig _bristleConfig;
    HairRadialTextureCache _cache;
    float _pressLatch;

    public HairRadialTextureCache Cache => _cache;
    public float Load01 => load01;

    void Awake()
    {
        EnsureCache();
    }

    void OnDestroy()
    {
        _cache?.Dispose();
        if (_bristleConfig != null)
        {
            if (Application.isPlaying) Destroy(_bristleConfig);
            else DestroyImmediate(_bristleConfig);
        }
    }

    public void EnsureCache()
    {
        if (definition == null) return;
        if (_bristleConfig == null)
            _bristleConfig = definition.BuildBristleConfig();
        int az = _bristleConfig.azimuthBins;
        int len = _bristleConfig.lengthBins;
        if (_cache == null || _cache.AzimuthBins != az || _cache.LengthBins != len)
        {
            _cache?.Dispose();
            _cache = new HairRadialTextureCache(az, len);
            var bake = HairLatticeWaterfallBaker.Bake(_bristleConfig, null, ferrule != null ? ferrule : transform);
            HairLatticeWaterfallBaker.ApplyToCache(bake, _cache);
            if (bake.texture != null)
            {
                if (Application.isPlaying) Destroy(bake.texture);
                else DestroyImmediate(bake.texture);
            }
        }
    }

    void FixedUpdate()
    {
        EnsureCache();
        float press = 0f;
        var proxy = GetComponentInParent<PaintInstrumentProxy>();
        if (proxy != null)
            press = proxy.GetChannel(PaintInstrumentMap.BrushPress);

        if (pileSource != null && tip != null)
        {
            StealFromPile(Time.fixedDeltaTime);
            // Also pull pile paint into canvas hydro when tip is near canvas + pile
            if (canvas != null && press >= 0.15f)
            {
                var hydro = canvas.Hydro;
                if (hydro != null)
                {
                    hydro.pileSource = pileSource;
                    hydro.surfaceTension = canvas.surfaceTension;
                    hydro.TryPullFromPile(tip.position, Time.fixedDeltaTime);
                }
            }
        }

        if (press < 0.15f && _pressLatch >= 0.15f && canvas != null)
        {
            DepositStroke();
            // Brush hairs pull away: surface flux → thin film gloss
            var hydro = canvas.Hydro;
            if (hydro != null && tip != null)
            {
                Vector3 n = tip.up;
                if (canvas.transform != null)
                    n = canvas.transform.forward;
                hydro.ApplyPullAwayFlux(tip.position, -n, strength: Mathf.Max(0.2f, load01));
            }
        }
        _pressLatch = press;

        BindBristles();
    }

    public void StealFromPile(float dt)
    {
        if (definition == null || pileSource == null || tip == null) return;
        if (!pileSource.TrySampleContact(tip.position, out float depth, out Color color, out float mass))
            return;
        if (mass <= 1e-5f || depth <= 1e-5f) return;

        float area = definition.ferruleRadiusM * definition.ferruleRadiusM * Mathf.PI;
        float take = definition.saturationSpeed * area * depth * dt;
        take = Mathf.Min(take, mass, 1f - load01);
        if (take <= 0f) return;

        pileSource.ConsumeMass(take);
        loadedColor = Color.Lerp(loadedColor, color, take / Mathf.Max(1e-4f, load01 + take));
        load01 = Mathf.Clamp01(load01 + take);

        // Fill bristle radial G/R
        _cache.WriteSoftBlob(0.5f, 0.35f, new Color(take, take, load01, 0f), 0.12f);
        _cache.Apply();
    }

    public void DepositStroke()
    {
        if (canvas == null || definition == null || load01 <= 1e-4f) return;
        var stamper = canvas.GetComponent<PaintStrokeStamper>();
        if (stamper == null)
            stamper = canvas.gameObject.AddComponent<PaintStrokeStamper>();
        stamper.StampFromBrush(this);
        load01 *= 0.35f; // retain some load
    }

    void BindBristles()
    {
        if (bristleRenderer == null || _cache == null) return;
        var mat = bristleRenderer.material;
        _cache.BindToMaterial(mat);
        mat.SetColor("_Color", Color.Lerp(Color.white, loadedColor, load01));
        mat.SetFloat("_PlumeTipHold", definition != null ? definition.plumeTipHold : 0.85f);
    }

    /// <summary>CPU pole of bristle contact for canvas stamp footprint.</summary>
    public float SampleLoadAt(float azimuth01, float length01)
    {
        if (_cache == null) return load01 * (definition != null ? definition.TipSilhouette(length01) : 1f);
        int u = Mathf.FloorToInt(Mathf.Repeat(azimuth01, 1f) * _cache.AzimuthBins);
        int v = Mathf.FloorToInt(Mathf.Clamp01(length01) * (_cache.LengthBins - 1));
        Color c = _cache.GetPixel(u, v);
        float sil = definition != null ? definition.TipSilhouette(length01) : 1f;
        return Mathf.Clamp01(Mathf.Max(c.r, c.g) * load01 * sil);
    }
}
