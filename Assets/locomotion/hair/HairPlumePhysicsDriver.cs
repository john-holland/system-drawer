using UnityEngine;
using SdfMax;

/// <summary>
/// Runtime plume driver: gravity + soft capsule tension into the radial cache,
/// optional PhysX materials, or shader-only bounce.
/// </summary>
[AddComponentMenu("Locomotion/Hair/Plume Physics Driver")]
public sealed class HairPlumePhysicsDriver : MonoBehaviour
{
    public HairPlumeConfig config;
    [InspectorName("Plume SDF Composition (reference)")]
    [Tooltip("Baked SDF Max composition used when rebaking the radial lattice. Optional spatial/volume host can share this asset.")]
    public SdfMaxCompositionAsset plumeSdfComposition;
    public Renderer hairRenderer;
    public Transform scalpRoot;
    public HairBodyCapsuleBinder bodyBinder;
    public HairColliderPrimitiveScanner colliderScanner;
    public HairHelmetSectionCache helmetSectionCache;

    [Tooltip("When false, skip physics materials and use shader bounce only.")]
    public bool usePhysicsMaterials = false;

    HairRadialTextureCache _cache;
    HairCapsuleBuffer _capsules;
    Color[] _bakeBase;
    MaterialPropertyBlock _block;
    Material _runtimeMat;
    bool _physicsEnabled = true;

    static readonly int TipHoldId = Shader.PropertyToID("_PlumeTipHold");
    static readonly int GravityTipId = Shader.PropertyToID("_GravityTipGain");
    static readonly int TensionPartId = Shader.PropertyToID("_TensionPartGain");
    static readonly int BounceId = Shader.PropertyToID("_ShaderBounceGain");
    static readonly int ExtrudeId = Shader.PropertyToID("_ExtrudeGain");
    static readonly int CurlAmountId = Shader.PropertyToID("_CurlAmount");
    static readonly int CurlFrequencyId = Shader.PropertyToID("_CurlFrequency");
    static readonly int CurlTightnessId = Shader.PropertyToID("_CurlTightness");

    public HairRadialTextureCache Cache => _cache;
    public HairCapsuleBuffer Capsules => _capsules;
    public bool PhysicsEnabled => _physicsEnabled;

    public void SetPhysicsEnabled(bool enabled) => _physicsEnabled = enabled;

    void Awake()
    {
        EnsureCache();
        _capsules = new HairCapsuleBuffer();
        _block = new MaterialPropertyBlock();
        if (config != null)
            usePhysicsMaterials = config.usePhysicsMaterials;
        if (bodyBinder == null)
            bodyBinder = GetComponent<HairBodyCapsuleBinder>();
        if (colliderScanner == null)
            colliderScanner = GetComponent<HairColliderPrimitiveScanner>();
        if (scalpRoot == null)
            scalpRoot = transform;
        EnsurePartGizmo();
    }

    public void EnsurePartGizmo()
    {
        var gizmo = GetComponent<HairLinePartGizmo>();
        if (gizmo == null)
            gizmo = gameObject.AddComponent<HairLinePartGizmo>();
        gizmo.config = config;
        gizmo.scalpRoot = scalpRoot;
    }

    void OnDestroy()
    {
        _cache?.Dispose();
        if (_runtimeMat != null)
            Destroy(_runtimeMat);
    }

    public void EnsureCache()
    {
        int az = config != null ? config.azimuthBins : 64;
        int len = config != null ? config.lengthBins : 32;
        if (_cache == null || _cache.AzimuthBins != az || _cache.LengthBins != len)
        {
            _cache?.Dispose();
            _cache = new HairRadialTextureCache(az, len);
        }
    }

    public void LoadBake(Color[] pixels)
    {
        EnsureCache();
        if (pixels == null || pixels.Length != _cache.AzimuthBins * _cache.LengthBins)
            return;
        _bakeBase = (Color[])pixels.Clone();
        _cache.CopyFrom(_bakeBase);
        _cache.Apply();
    }

    public void BakeFromConfig()
    {
        if (config == null) return;
        bool ownsComposition = false;
        SdfMaxCompositionAsset composition = plumeSdfComposition;
        if (composition == null)
        {
            composition = HairPlumeSdfComposer.ComposeGaussianPlume(config);
            ownsComposition = true;
            plumeSdfComposition = composition;
        }

        var bake = HairLatticeWaterfallBaker.Bake(config, composition, scalpRoot);
        LoadBake(bake.pixels);
        if (bake.texture != null)
        {
            if (Application.isPlaying) Destroy(bake.texture);
            else DestroyImmediate(bake.texture);
        }

        // Only destroy ephemeral compositions created this call that were never assigned as assets
        if (ownsComposition && plumeSdfComposition != composition)
        {
            if (Application.isPlaying) Destroy(composition);
            else DestroyImmediate(composition);
        }
    }

    /// <summary>Assign the baked SDF composition reference (editor bake / spatial hosts).</summary>
    public void SetPlumeSdfComposition(SdfMaxCompositionAsset composition)
    {
        plumeSdfComposition = composition;
    }

    void FixedUpdate()
    {
        if (config == null) return;
        EnsureCache();
        _capsules ??= new HairCapsuleBuffer();

        bodyBinder?.Bind(_capsules);
        colliderScanner?.ScanAndWrite(_capsules);

        float dt = Mathf.Max(1e-4f, Time.fixedDeltaTime);
        if (_physicsEnabled)
            IntegrateTensionAndGravity(dt);

        ApplyToRenderer();
    }

    void IntegrateTensionAndGravity(float dt)
    {
        int az = _cache.AzimuthBins;
        int len = _cache.LengthBins;
        float tipHold = Mathf.Clamp01(config.plumeTipHold);
        float reseal = config.resealRate;
        float partGain = config.tensionPartGain;
        Vector3 g = config.gravity;
        float gGain = config.gravityTipGain;

        if (_bakeBase == null || _bakeBase.Length != az * len)
            _bakeBase = _cache.ClonePixels();

        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            bool sectorPhysics = helmetSectionCache == null || helmetSectionCache.IsPhysicsEnabledForLength(length01);
            for (int u = 0; u < az; u++)
            {
                float azimuth01 = u / (float)az;
                if (helmetSectionCache != null && !helmetSectionCache.IsPhysicsEnabledForAzimuth(azimuth01))
                    continue;
                if (!sectorPhysics)
                    continue;

                Color baked = _bakeBase[v * az + u];
                Color cur = _cache.GetPixel(u, v);

                // Capsule tension → G channel (toroidal part / reseal)
                float tension = SampleCapsuleTensionAtBin(azimuth01, length01) * partGain;
                if (usePhysicsMaterials)
                {
                    // Soft spring toward bake with tension impulse (cloth-like, no hard collision)
                    cur.g = Mathf.Lerp(cur.g, tension, 1f - Mathf.Exp(-dt * 8f));
                    cur.g = Mathf.Lerp(cur.g, 0f, reseal * tipHold * dt);
                }
                else
                {
                    // Shader bounce path: write tension for part mask; bounce done in shader
                    cur.g = Mathf.Lerp(cur.g, tension, dt * 10f);
                    cur.g = Mathf.Lerp(cur.g, 0f, reseal * dt * (0.25f + tipHold));
                }

                // Gravity tips into A (tip break energy from radial gaussian flux) and softens R when tipHold low
                float tipEnergy = HairGaussianFlux.TipBreakFromFlux(
                    length01,
                    config.gaussianSigma,
                    tipHold,
                    config.gaussianFluxGain);
                tipEnergy = Mathf.Clamp01(tipEnergy * (Mathf.Abs(g.y) / 9.81f) * gGain);
                float breakSpread = baked.r * (1f - length01 * 0.85f) * (1f + tipEnergy * (1f - tipHold));
                float held = Mathf.Max(baked.r, baked.b);
                cur.r = Mathf.Lerp(breakSpread, held, tipHold);
                cur.b = baked.b;
                cur.a = Mathf.Lerp(cur.a, tipEnergy * (1f - tipHold), dt * 4f);

                _cache.SetPixel(u, v, cur);
            }
        }

        _cache.Apply();
    }

    float SampleCapsuleTensionAtBin(float azimuth01, float length01)
    {
        if (scalpRoot == null || _capsules == null) return 0f;
        float ang = azimuth01 * Mathf.PI * 2f;
        float scalpR = config != null ? config.scalpRadiusM : 0.11f;
        float maxLen = config != null ? config.maxStrandLengthM : 0.35f;
        Vector3 local = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (scalpR * 0.5f)
                        + Vector3.up * (length01 * maxLen * 0.25f);
        Vector3 world = scalpRoot.TransformPoint(local);

        float acc = 0f;
        var slots = _capsules.Slots;
        int n = _capsules.Count;
        for (int i = 0; i < n && i < slots.Length; i++)
        {
            Vector4 c = slots[i];
            if (c.w <= 1e-5f) continue;
            float dist = Vector3.Distance(world, new Vector3(c.x, c.y, c.z));
            float falloff = config != null ? config.tensionFalloff : 0.65f;
            float inf = 1f - Mathf.Clamp01(dist / Mathf.Max(1e-3f, c.w * (2f + falloff * 2f)));
            acc += inf;
        }
        return Mathf.Clamp01(acc);
    }

    void ApplyToRenderer()
    {
        if (hairRenderer == null) return;
        EnsureRuntimeMaterial();
        var mat = _runtimeMat != null ? _runtimeMat : hairRenderer.sharedMaterial;
        if (mat == null) return;

        _cache.BindToMaterial(mat);
        _capsules.BindToMaterial(mat);

        float tipHold = config != null ? config.plumeTipHold : 0.55f;
        mat.SetFloat(TipHoldId, tipHold);
        mat.SetFloat(GravityTipId, config != null ? config.gravityTipGain : 0.35f);
        mat.SetFloat(TensionPartId, config != null ? config.tensionPartGain : 1f);
        mat.SetFloat(BounceId, usePhysicsMaterials ? 0f : (config != null ? config.shaderBounceGain : 0.35f));
        mat.SetFloat(ExtrudeId, config != null ? config.peakHeightM : 0.28f);
        mat.SetFloat(CurlAmountId, config != null ? config.curlAmount : 0f);
        mat.SetFloat(CurlFrequencyId, config != null ? config.curlFrequency : 3f);
        mat.SetFloat(CurlTightnessId, config != null ? config.curlTightness : 0.5f);

        helmetSectionCache?.BindToMaterial(mat);
    }

    void EnsureRuntimeMaterial()
    {
        if (hairRenderer == null) return;
        if (_runtimeMat != null) return;
        var shared = hairRenderer.sharedMaterial;
        if (shared == null) return;
        _runtimeMat = new Material(shared) { name = shared.name + " (HairPlume Runtime)" };
        hairRenderer.material = _runtimeMat;
    }
}
