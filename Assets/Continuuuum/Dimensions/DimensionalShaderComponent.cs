using System.Collections.Generic;
using UnityEngine;

/// <summary>Explicit per-root control parameters for DimensionMaterialCrossFader.</summary>
[AddComponentMenu("Continuuuum/Dimensions/Dimensional Shader Component")]
public sealed class DimensionalShaderComponent : MonoBehaviour
{
    public bool enabledForDimSwitch = true;
    public DimensionalMaterialKind materialKind = DimensionalMaterialKind.Auto;
    public string blendPropertyName = "_DimBlend";
    public float durationSeconds = 0.35f;
    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool includeChildren = true;
    public List<Renderer> renderers = new List<Renderer>();
    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    public bool useMaterialPropertyBlock = true;
    public bool commitOnComplete = true;
    public DimensionalShaderFallbackMode fallbackMode = DimensionalShaderFallbackMode.HardCutAtHalf;
    public string dissolvePropertyName = "";
    public List<DimensionalShaderGlobalFloat> shaderGlobals = new List<DimensionalShaderGlobalFloat>();
    public string lemmaEntryId;
    public bool allowLemmaOverride;

    /// <summary>
    /// Sparse map: dim index → optional open/close topology BT (transformers/convertibles).
    /// Missing dim → fade-only.
    /// </summary>
    public List<DimensionalOpenCloseBtEntry> openCloseBtByDimension = new List<DimensionalOpenCloseBtEntry>();

    Material[] _slotA;
    Material[] _slotB;
    float _lastT;

    void OnEnable()
    {
        DimensionMaterialCrossFader.Register(this);
    }

    void OnDisable()
    {
        DimensionMaterialCrossFader.Unregister(this);
    }

    public bool TryBuildFadeJob(out DimensionalShaderFadeJob job)
    {
        job = null;
        if (!enabledForDimSwitch || !isActiveAndEnabled)
            return false;

        var rends = ResolveRenderers();
        var ps = ResolveParticleSystems();
        var kind = materialKind;
        if (kind == DimensionalMaterialKind.Auto)
            kind = InferKind(rends, ps);

        string blend = string.IsNullOrEmpty(blendPropertyName)
            ? (kind == DimensionalMaterialKind.SkyCubemap ? "_BlendWeight" : "_DimBlend")
            : blendPropertyName;

        bool hasBlend = false;
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null || r.sharedMaterial == null)
                continue;
            if (r.sharedMaterial.HasProperty(blend))
            {
                hasBlend = true;
                break;
            }
        }

        job = new DimensionalShaderFadeJob
        {
            source = this,
            kind = kind,
            blendPropertyName = blend,
            durationSeconds = Mathf.Max(0.01f, durationSeconds),
            blendCurve = blendCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f),
            renderers = rends,
            particleSystems = ps,
            useMaterialPropertyBlock = useMaterialPropertyBlock,
            commitOnComplete = commitOnComplete,
            fallbackMode = fallbackMode,
            dissolvePropertyName = dissolvePropertyName,
            shaderGlobals = shaderGlobals != null ? shaderGlobals.ToArray() : System.Array.Empty<DimensionalShaderGlobalFloat>(),
            block = useMaterialPropertyBlock ? new MaterialPropertyBlock() : null,
            hasBlendProperty = hasBlend
        };

        CacheSlotMaterials(rends);
        return true;
    }

    public void ApplyBlend(float t)
    {
        _lastT = Mathf.Clamp01(t);
        if (TryBuildFadeJob(out var job) && job != null)
            ApplyBlendInternal(job, _lastT);
    }

    public void ApplyBlend(DimensionalShaderFadeJob job, float t)
    {
        _lastT = Mathf.Clamp01(t);
        ApplyBlendInternal(job, _lastT);
    }

    public void CommitB()
    {
        // Slot B becomes active: leave blend at 1
        ApplyBlend(1f);
        _slotA = _slotB;
    }

    public void AbortRestoreA()
    {
        ApplyBlend(0f);
    }

    public bool TryGetOpenCloseEntry(int dimIndex, out DimensionalOpenCloseBtEntry entry)
    {
        entry = null;
        if (openCloseBtByDimension == null)
            return false;
        for (int i = 0; i < openCloseBtByDimension.Count; i++)
        {
            var e = openCloseBtByDimension[i];
            if (e != null && e.dimIndex == dimIndex)
            {
                entry = e;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Start open (entering=true) or close (entering=false) topology BT for a dim entry.
    /// Forwards runtimeMilliseconds to the registered Locomotion runner.
    /// </summary>
    public bool BeginOpenCloseForDimension(int dimIndex, bool entering)
    {
        if (!TryGetOpenCloseEntry(dimIndex, out var entry) || entry == null)
            return false;
        if (entering && !entry.runOnEnter)
            return false;
        if (!entering && !entry.runOnExit)
            return false;
        if (entry.topology == null)
            return false;
        return DimensionalOpenCloseRunnerHost.TryBegin(
            gameObject,
            entry.topology,
            entering,
            entry.runtimeMilliseconds);
    }

    /// <summary>
    /// Invoke exit BT for fromDim then enter BT for toDim across all components under root.
    /// </summary>
    public static void NotifyDimensionSwitch(Transform root, int fromDim, int toDim)
    {
        if (root == null)
            return;
        var comps = root.GetComponentsInChildren<DimensionalShaderComponent>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null || !c.enabledForDimSwitch)
                continue;
            if (fromDim != toDim)
                c.BeginOpenCloseForDimension(fromDim, entering: false);
            c.BeginOpenCloseForDimension(toDim, entering: true);
        }
    }

    void ApplyBlendInternal(DimensionalShaderFadeJob job, float t)
    {
        if (job == null)
            return;
        float curved = job.blendCurve != null ? job.blendCurve.Evaluate(t) : t;
        int blendId = Shader.PropertyToID(job.blendPropertyName);
        int dissolveId = string.IsNullOrEmpty(job.dissolvePropertyName)
            ? -1
            : Shader.PropertyToID(job.dissolvePropertyName);

        if (!job.hasBlendProperty)
        {
            ApplyFallback(job, curved);
            return;
        }

        var rends = job.renderers;
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null)
                continue;
            if (job.useMaterialPropertyBlock)
            {
                r.GetPropertyBlock(job.block);
                job.block.SetFloat(blendId, curved);
                if (dissolveId >= 0)
                    job.block.SetFloat(dissolveId, curved);
                r.SetPropertyBlock(job.block);
            }
            else if (r.material != null && r.material.HasProperty(blendId))
            {
                r.material.SetFloat(blendId, curved);
                if (dissolveId >= 0 && r.material.HasProperty(dissolveId))
                    r.material.SetFloat(dissolveId, curved);
            }
        }

        if (job.particleSystems != null)
        {
            for (int i = 0; i < job.particleSystems.Length; i++)
            {
                var ps = job.particleSystems[i];
                if (ps == null)
                    continue;
                var main = ps.main;
                var c = main.startColor.color;
                c.a = Mathf.Lerp(1f, 0f, curved);
                main.startColor = c;
            }
        }

        if (job.shaderGlobals != null)
        {
            for (int i = 0; i < job.shaderGlobals.Length; i++)
            {
                var g = job.shaderGlobals[i];
                if (g == null || string.IsNullOrEmpty(g.name))
                    continue;
                Shader.SetGlobalFloat(g.name, Mathf.Lerp(g.from, g.to, curved));
            }
        }
    }

    void ApplyFallback(DimensionalShaderFadeJob job, float curved)
    {
        if (job.fallbackMode == DimensionalShaderFallbackMode.Skip)
            return;
        if (job.fallbackMode == DimensionalShaderFallbackMode.HardCutAtHalf)
        {
            bool showB = curved >= 0.5f;
            // No dual materials without blend prop — toggle renderer enabled as soft cut
            for (int i = 0; i < job.renderers.Length; i++)
            {
                if (job.renderers[i] != null)
                    job.renderers[i].enabled = true;
            }
            _ = showB;
            return;
        }
        // AlphaDither: fade color alpha when possible
        for (int i = 0; i < job.renderers.Length; i++)
        {
            var r = job.renderers[i];
            if (r == null || r.material == null)
                continue;
            if (r.material.HasProperty("_Color"))
            {
                var c = r.material.GetColor("_Color");
                c.a = 1f - curved;
                r.material.SetColor("_Color", c);
            }
        }
    }

    Renderer[] ResolveRenderers()
    {
        if (renderers != null && renderers.Count > 0)
        {
            var list = new List<Renderer>();
            for (int i = 0; i < renderers.Count; i++)
                if (renderers[i] != null)
                    list.Add(renderers[i]);
            return list.ToArray();
        }
        return includeChildren
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }

    ParticleSystem[] ResolveParticleSystems()
    {
        if (particleSystems != null && particleSystems.Count > 0)
        {
            var list = new List<ParticleSystem>();
            for (int i = 0; i < particleSystems.Count; i++)
                if (particleSystems[i] != null)
                    list.Add(particleSystems[i]);
            return list.ToArray();
        }
        return includeChildren
            ? GetComponentsInChildren<ParticleSystem>(true)
            : GetComponents<ParticleSystem>();
    }

    static DimensionalMaterialKind InferKind(Renderer[] rends, ParticleSystem[] ps)
    {
        if (ps != null && ps.Length > 0)
            return DimensionalMaterialKind.Particle;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] is SkinnedMeshRenderer)
                return DimensionalMaterialKind.SkinnedMesh;
            if (rends[i] is ParticleSystemRenderer)
                return DimensionalMaterialKind.Particle;
        }
        return DimensionalMaterialKind.MeshLit;
    }

    void CacheSlotMaterials(Renderer[] rends)
    {
        _slotA = new Material[rends.Length];
        for (int i = 0; i < rends.Length; i++)
            _slotA[i] = rends[i] != null ? rends[i].sharedMaterial : null;
        _slotB = _slotA;
    }

}
