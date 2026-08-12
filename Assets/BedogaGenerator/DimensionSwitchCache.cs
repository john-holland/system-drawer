using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-memory SG2D/SG3D/SG4D warm entries keyed by (game, dim).
/// Prefer cache apply over Clear/Generate when switching Continuuuum dimensions.
/// Integrates SharedDimensionalGenericCache, particle cleanup, and material cross-fade.
/// </summary>
public sealed class DimensionSwitchCache : MonoBehaviour
{
    [Serializable]
    public sealed class WarmEntry
    {
        public string gameSlug;
        public int dimIndex;
        public string sgKind;
        public string etag;
        public string payloadJson;
        public string mode;
        public bool enable4d;
    }

    public GameDimensionClient client;
    public SpatialGenerator4DOrchestrator orchestrator;
    public SpatialGeneratorSkinController skinController;
    public SpatialGenerator spatialGenerator;
    public Transform aestheticScopeRoot;
    public float crossFadeDuration = 0.35f;

    readonly Dictionary<string, WarmEntry> _cache = new Dictionary<string, WarmEntry>();
    public int ActiveDimIndex { get; private set; }
    public string ActiveGameSlug { get; private set; } = "main";
    public string LastStatus { get; private set; } = "";

    static string Key(string game, int dim, string kind) => $"{game}|{dim}|{kind}";

    void Awake()
    {
        if (client == null)
            client = GetComponent<GameDimensionClient>() ?? gameObject.AddComponent<GameDimensionClient>();
        if (orchestrator == null)
            orchestrator = FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
        if (skinController == null)
            skinController = FindAnyObjectByType<SpatialGeneratorSkinController>();
        if (spatialGenerator == null)
            spatialGenerator = FindAnyObjectByType<SpatialGenerator>();
        SharedDimensionalGenericCache.Ensure();
        DimensionMaterialCrossFader.Ensure();
    }

    public bool TryGet(string game, int dim, string kind, out WarmEntry entry) =>
        _cache.TryGetValue(Key(game, dim, kind), out entry);

    public void Put(WarmEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.sgKind))
            return;
        _cache[Key(entry.gameSlug ?? "main", entry.dimIndex, entry.sgKind)] = entry;
    }

    public IEnumerator PrewarmAsync(string game, int dim, Action<bool> done = null)
    {
        ActiveGameSlug = game ?? "main";
        if (client != null)
            client.gameSlug = ActiveGameSlug;
        bool ok = false;
        string json = null;
        yield return client.Prewarm(dim, (success, body) =>
        {
            ok = success;
            json = body;
        });
        if (ok)
            IngestPrewarmJson(game, dim, json);
        LastStatus = ok ? $"Prewarmed dim {dim}" : "Prewarm failed";
        done?.Invoke(ok);
    }

    public IEnumerator SwitchToDimension(int dim, Action<bool> done = null)
    {
        var game = ActiveGameSlug ?? (client != null ? client.gameSlug : "main");
        int fromDim = ActiveDimIndex;
        var shared = SharedDimensionalGenericCache.Ensure();
        var fader = DimensionMaterialCrossFader.Ensure();

        // 1. Capture shared slots + TravelAgent goals
        DimensionalTravelAgentKeepAlive.CaptureGoals();
        shared.CaptureFromScene(game, fromDim);
        shared.CopySharedPositional(fromDim, dim);

        // 2. Particle cleanup on outgoing aesthetic roots
        var roots = CollectOutgoingRoots();
        DimensionParticleCleanup.Run(roots);

        // 2b. Optional open/close topology BT (exit fromDim, enter toDim) — transformers/convertibles
        var scope = aestheticScopeRoot != null ? aestheticScopeRoot : transform;
        DimensionalShaderComponent.NotifyDimensionSwitch(scope, fromDim, dim);

        // 3. Cross-fade from DimensionalShaderComponent jobs
        if (fader != null)
            yield return fader.FadeInScope(scope);
        bool ok = false;
        string json = null;
        if (client != null)
        {
            yield return client.SwitchDimension(dim, (success, body) =>
            {
                ok = success;
                json = body;
            });
            if (ok)
                IngestSwitchJson(game, dim, json);
        }

        if (!HasAllKinds(game, dim))
            yield return PrewarmAsync(game, dim, null);

        // 4. Apply SG warm payload
        ApplyWarm(game, dim);

        // 5. KeepAlive / Replace bindings + restore pose/velocity
        shared.ApplyKeepAliveBindings(game, dim);
        DimensionalTravelAgentKeepAlive.ApplyAfterDimSwitch();

        // 6. Restart particles on KeepAlive roots
        DimensionParticleCleanup.RestartEmission(roots);

        ActiveDimIndex = dim;
        shared.ActiveDimIndex = dim;
        shared.ActiveGameSlug = game;
        LastStatus = $"Switched to dimension {dim}";
        StartCoroutine(PrewarmAsync(game, 0));
        if (dim != 1)
            StartCoroutine(PrewarmAsync(game, dim + 1));
        if (dim > 0)
            StartCoroutine(PrewarmAsync(game, dim - 1));
        done?.Invoke(true);
    }

    List<Transform> CollectOutgoingRoots()
    {
        var list = new List<Transform>();
        if (aestheticScopeRoot != null)
            list.Add(aestheticScopeRoot);
        foreach (var binding in SharedDimensionalGenericCache.Ensure().Bindings)
        {
            if (binding != null)
                list.Add(binding.transform);
        }
        if (list.Count == 0)
            list.Add(transform);
        return list;
    }

    bool HasAllKinds(string game, int dim)
    {
        return TryGet(game, dim, "sg2d", out _) &&
               TryGet(game, dim, "sg3d", out _) &&
               TryGet(game, dim, "sg4d", out _);
    }

    void IngestPrewarmJson(string game, int dim, string json)
    {
        if (string.IsNullOrEmpty(json))
            return;
        Put(new WarmEntry
        {
            gameSlug = game,
            dimIndex = dim,
            sgKind = "sg2d",
            mode = "TwoDimensional",
            enable4d = false,
            payloadJson = json,
            etag = Hash(json + "sg2d"),
        });
        Put(new WarmEntry
        {
            gameSlug = game,
            dimIndex = dim,
            sgKind = "sg3d",
            mode = "ThreeDimensional",
            enable4d = false,
            payloadJson = json,
            etag = Hash(json + "sg3d"),
        });
        Put(new WarmEntry
        {
            gameSlug = game,
            dimIndex = dim,
            sgKind = "sg4d",
            mode = "FourDimensional",
            enable4d = true,
            payloadJson = json,
            etag = Hash(json + "sg4d"),
        });
    }

    void IngestSwitchJson(string game, int dim, string json) => IngestPrewarmJson(game, dim, json);

    void ApplyWarm(string game, int dim)
    {
        bool has4d = TryGet(game, dim, "sg4d", out var e4) && e4.enable4d;
        if (orchestrator != null)
        {
            orchestrator.use4DPlacement = has4d || dim != 0;
            orchestrator.Apply();
        }
        if (spatialGenerator != null)
        {
            spatialGenerator.mode = TryGet(game, dim, "sg3d", out _)
                ? SpatialGenerator.GenerationMode.ThreeDimensional
                : SpatialGenerator.GenerationMode.TwoDimensional;
        }
        // SG skins keep scale/_Dissolve; world aesthetics use DimensionMaterialCrossFader
        if (skinController != null && skinController.skins != null && skinController.skins.Count > 0)
        {
            int idx = Mathf.Clamp(dim, 0, skinController.skins.Count - 1);
            if (skinController.useScaleTransition)
                skinController.ApplySkinWithTransition(idx, crossFadeDuration);
            else
                skinController.ApplySkin(idx);
        }
        Debug.Log($"[DimensionSwitchCache] Applied warm for {game} dim={dim}");
    }

    static string Hash(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "0";
        unchecked
        {
            int h = 23;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h.ToString("x");
        }
    }
}
