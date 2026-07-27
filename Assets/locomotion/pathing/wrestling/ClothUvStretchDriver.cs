using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Samples relative skin/cloth motion under contact; writes ClothUvStretchCache; binds materials.
/// Additive to rope strain — does not replace RopeRadialStrainCache.
/// </summary>
[AddComponentMenu("Locomotion/Wrestling/Cloth UV Stretch Driver")]
public sealed class ClothUvStretchDriver : MonoBehaviour
{
    public List<ClothUvStretchLayer> layers = new List<ClothUvStretchLayer>();
    public int cacheWidth = 64;
    public int cacheHeight = 64;
    [Tooltip("Higher recovery for kayfabe / professionalStyle Play snaps fabric cleaner.")]
    public bool preferProfessionalRecovery;
    [Range(0f, 1f)] public float professionalRecoveryBoost = 0.1f;

    ClothUvStretchCache _cache;
    GameObject _contactOpponent;
    float _contactBoost01;
    Vector3 _lastOppPos;

    public ClothUvStretchCache Cache => _cache;
    public float ContactBoost01 => _contactBoost01;

    void Awake()
    {
        _cache = new ClothUvStretchCache(cacheWidth, cacheHeight);
    }

    void OnDestroy()
    {
        _cache?.Dispose();
    }

    public void NotifyContact(GameObject opponent, float weight01)
    {
        _contactOpponent = opponent;
        _contactBoost01 = Mathf.Clamp01(Mathf.Max(_contactBoost01, weight01));
        if (opponent != null)
            _lastOppPos = opponent.transform.position;
    }

    public void ClearContact()
    {
        _contactOpponent = null;
        _contactBoost01 = 0f;
    }

    void FixedUpdate()
    {
        float dt = Mathf.Max(1e-4f, Time.fixedDeltaTime);
        if (_cache == null)
            _cache = new ClothUvStretchCache(cacheWidth, cacheHeight);

        Vector3 shearWorld = Vector3.zero;
        if (_contactOpponent != null)
        {
            Vector3 p = _contactOpponent.transform.position;
            shearWorld = (p - _lastOppPos) / dt;
            _lastOppPos = p;
        }

        _cache.Clear();
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null) continue;
            IntegrateLayer(layer, shearWorld, dt);
            Vector2 uv = new Vector2(0.5f, 0.5f);
            _cache.WriteSample(uv, layer.strain01, layer.slipUv, layer.contactWeight01);
            BindLayer(layer);
        }
        _cache.Apply();

        // Decay contact boost when not refreshed.
        _contactBoost01 = Mathf.MoveTowards(_contactBoost01, 0f, dt * 1.5f);
    }

    void IntegrateLayer(ClothUvStretchLayer layer, Vector3 shearWorld, float dt)
    {
        var e = layer.elastic ?? new ClothElasticProperties();
        float slideMask = SampleMaskR(layer.slideMaskTex, 0.5f, 0.5f);
        float elasticMask = SampleMaskR(layer.elasticMaskTex, 0.5f, 0.5f);
        if (elasticMask <= 0f) elasticMask = 1f;

        float contact = Mathf.Clamp01(_contactBoost01);
        layer.contactWeight01 = contact;

        // Tangential shear → UV slip (approximate: world shear projected to plane).
        Vector2 shearUv = new Vector2(shearWorld.x, shearWorld.z) * 0.02f;
        float friction = Mathf.Clamp01(e.friction01);
        Vector2 slipDelta = shearUv * e.slideGain * slideMask * (1f - friction) * contact * dt;

        layer.slipVelocity += slipDelta / dt;
        // Spring toward 0
        float recovery = e.recovery01;
        if (preferProfessionalRecovery)
            recovery = Mathf.Clamp01(recovery + professionalRecoveryBoost);
        layer.slipVelocity += -layer.slipUv * e.stiffness * dt;
        layer.slipVelocity *= Mathf.Exp(-e.damping * dt);
        if (contact < 0.05f)
            layer.slipUv *= Mathf.Lerp(1f, recovery, dt * 4f);

        layer.slipUv += layer.slipVelocity * dt;
        layer.slipUv.x = Mathf.Clamp(layer.slipUv.x, -e.maxSlipUv, e.maxSlipUv);
        layer.slipUv.y = Mathf.Clamp(layer.slipUv.y, -e.maxSlipUv, e.maxSlipUv);

        float edgeStrain = layer.slipUv.magnitude / Mathf.Max(1e-4f, e.maxSlipUv);
        layer.strain01 = Mathf.Clamp01(edgeStrain * e.stretchGain * elasticMask * Mathf.Max(contact, 0.15f));

        // Mask stick zones: zero slip where slideMask ~ 0
        if (slideMask < 0.05f)
        {
            layer.slipUv = Vector2.zero;
            layer.slipVelocity = Vector2.zero;
        }
    }

    void BindLayer(ClothUvStretchLayer layer)
    {
        var mat = layer.ResolveMaterial();
        if (mat == null || _cache == null) return;
        _cache.BindToMaterial(mat, "_ClothStretchTex");
        if (layer.slideMaskTex != null)
            mat.SetTexture("_SlideMaskTex", layer.slideMaskTex);
        if (layer.elasticMaskTex != null)
            mat.SetTexture("_ElasticMaskTex", layer.elasticMaskTex);
        var e = layer.elastic ?? new ClothElasticProperties();
        mat.SetFloat("_StretchGain", e.stretchGain);
        mat.SetFloat("_SlideGain", e.slideGain);
    }

    static float SampleMaskR(Texture2D tex, float u, float v)
    {
        if (tex == null) return 1f;
        if (!tex.isReadable) return 1f;
        return Mathf.Clamp01(tex.GetPixelBilinear(u, v).r);
    }

    /// <summary>Test helper: apply shear without FixedUpdate.</summary>
    public void DebugIntegrate(float dt, Vector3 shearWorld)
    {
        if (_cache == null)
            _cache = new ClothUvStretchCache(cacheWidth, cacheHeight);
        _cache.Clear();
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null) continue;
            IntegrateLayer(layer, shearWorld, dt);
            _cache.WriteSample(new Vector2(0.5f, 0.5f), layer.strain01, layer.slipUv, layer.contactWeight01);
        }
        _cache.Apply();
    }
}
