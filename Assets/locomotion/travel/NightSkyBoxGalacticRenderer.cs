using System.Collections.Generic;
using Planetary.Celestial;
using Planetary.Composition;
using UnityEngine;

/// <summary>Blends baked night-sky caches; listens to TravelAgent galactic snapshots.</summary>
public sealed class NightSkyBoxGalacticRenderer : MonoBehaviour
{
    public Material blendMaterial;
    public GalacticSkyLatticeIndex latticeIndex = new GalacticSkyLatticeIndex();
    public float crossFadeSeconds = 1f;
    public bool useLatticeBlend = true;

    readonly Dictionary<string, Texture2D> _loaded = new Dictionary<string, Texture2D>();
    GalacticTravelSnapshot _current;
    GalacticTravelSnapshot _target;
    float _blendT;

    void OnEnable()
    {
        foreach (TravelAgent agent in TravelAgentRegistry.All)
            agent.GalacticPositionChanged += OnGalacticPositionChanged;
    }

    void OnDisable()
    {
        foreach (TravelAgent agent in TravelAgentRegistry.All)
            agent.GalacticPositionChanged -= OnGalacticPositionChanged;
    }

    void OnGalacticPositionChanged(GalacticTravelSnapshot snap)
    {
        _target = snap;
        _blendT = 0f;
    }

    void Update()
    {
        if (_blendT < 1f)
        {
            _blendT += Time.deltaTime / Mathf.Max(0.01f, crossFadeSeconds);
            _current = LerpSnapshot(_current, _target, Mathf.Clamp01(_blendT));
        }
        ApplyToMaterial(_current);
    }

    static GalacticTravelSnapshot LerpSnapshot(GalacticTravelSnapshot a, GalacticTravelSnapshot b, float t)
    {
        return new GalacticTravelSnapshot
        {
            worldPos = Vector3.Lerp(a.worldPos, b.worldPos, t),
            nearestBodyId = string.IsNullOrEmpty(b.nearestBodyId) ? a.nearestBodyId : b.nearestBodyId,
            surfaceAnchor = Vector3.Lerp(a.surfaceAnchor, b.surfaceAnchor, t),
            cellBlendWeight = Mathf.Lerp(a.cellBlendWeight, b.cellBlendWeight, t),
            altitudeBand = b.altitudeBand,
            activeLatticeCellId = b.activeLatticeCellId
        };
    }

    void ApplyToMaterial(GalacticTravelSnapshot snap)
    {
        if (blendMaterial == null)
            return;
        if (useLatticeBlend && latticeIndex.TrySampleBlend(snap.worldPos, out var cell, out float w))
        {
            blendMaterial.SetFloat("_BlendWeight", w * snap.cellBlendWeight);
            if (cell.cacheIds != null && cell.cacheIds.Length > 0)
                TryBindCache(cell.cacheIds[0], "_SkyTexA");
        }
        blendMaterial.SetVector("_ObserverWorld", snap.worldPos);
    }

    void TryBindCache(string cacheId, string prop)
    {
        if (string.IsNullOrEmpty(cacheId) || blendMaterial == null)
            return;
        if (!_loaded.TryGetValue(cacheId, out var tex))
            return;
        blendMaterial.SetTexture(prop, tex);
    }

    public void RegisterCacheTexture(string cacheId, Texture2D tex)
    {
        if (string.IsNullOrEmpty(cacheId) || tex == null)
            return;
        _loaded[cacheId] = tex;
    }
}
