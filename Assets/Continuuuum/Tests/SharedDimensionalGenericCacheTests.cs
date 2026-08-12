using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SharedDimensionalGenericCacheTests
{
    [TearDown]
    public void TearDown()
    {
        var caches = Object.FindObjectsByType<SharedDimensionalGenericCache>(FindObjectsSortMode.None);
        for (int i = 0; i < caches.Length; i++)
            Object.DestroyImmediate(caches[i].gameObject);
        var faders = Object.FindObjectsByType<DimensionMaterialCrossFader>(FindObjectsSortMode.None);
        for (int i = 0; i < faders.Length; i++)
            Object.DestroyImmediate(faders[i].gameObject);
    }

    [Test]
    public void ParsePolicy_RecognizesVariants()
    {
        Assert.AreEqual(DimensionalActorPolicy.KeepAlive, SharedDimensionalGenericCache.ParsePolicy("keep-alive"));
        Assert.AreEqual(DimensionalActorPolicy.AestheticOnly, SharedDimensionalGenericCache.ParsePolicy("aesthetic-only"));
        Assert.AreEqual(DimensionalActorPolicy.ReplaceActor, SharedDimensionalGenericCache.ParsePolicy("replace"));
    }

    [Test]
    public void Upsert_And_CopySharedPositional_BridgesVelocity()
    {
        var cache = SharedDimensionalGenericCache.Ensure();
        cache.ActiveGameSlug = "main";
        var key0 = new DimensionalCacheKey("main", 0, "lemma-a", "inst-1");
        cache.Upsert(new DimensionalCacheEntry
        {
            key = key0,
            policy = DimensionalActorPolicy.KeepAlive,
            positional = new DimensionalPositionalSlot
            {
                worldPos = new Vector3(1, 2, 3),
                worldRot = Quaternion.identity,
                lossyScale = Vector3.one,
                hasVelocity = true,
                linearVelocity = new Vector3(4, 0, 0),
                angularVelocity = Vector3.up
            }
        });

        int n = cache.CopySharedPositional(0, 1);
        Assert.GreaterOrEqual(n, 1);
        Assert.IsTrue(cache.TryGet(new DimensionalCacheKey("main", 1, "lemma-a", "inst-1"), out var dest));
        Assert.AreEqual(new Vector3(1, 2, 3), dest.positional.worldPos);
        Assert.IsTrue(dest.positional.hasVelocity);
        Assert.AreEqual(new Vector3(4, 0, 0), dest.positional.linearVelocity);
    }

    [Test]
    public void VelocityBridge_RoundTrip_OnRigidbody()
    {
        var go = new GameObject("vel");
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(2, 0, 1);
        rb.angularVelocity = new Vector3(0, 3, 0);
        var bridge = go.AddComponent<DimensionalLemmaVelocityBridge>();
        var slot = new DimensionalPositionalSlot();
        bridge.WriteTo(slot);
        Assert.IsTrue(slot.hasVelocity);
        Assert.AreEqual(new Vector3(2, 0, 1), slot.linearVelocity);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        bridge.ApplyFrom(slot);
        Assert.AreEqual(new Vector3(2, 0, 1), rb.linearVelocity);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ParticleCleanup_StopsAndClears()
    {
        var root = new GameObject("ps-root");
        var psGo = new GameObject("DimLocalFx");
        psGo.transform.SetParent(root.transform);
        var ps = psGo.AddComponent<ParticleSystem>();
        ps.Play();
        DimensionParticleCleanup.RunOnRoot(root.transform, destroyDimLocalOrphans: false);
        Assert.IsFalse(ps.isPlaying);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ShaderComponent_TryBuildFadeJob_UsesExplicitRenderers()
    {
        var go = new GameObject("shader-root");
        var r = go.AddComponent<MeshRenderer>();
        var shader = Shader.Find("Continuuuum/Dimensions/CrossFadeMesh")
                     ?? Shader.Find("Hidden/Internal-Colored")
                     ?? Shader.Find("Sprites/Default");
        r.sharedMaterial = new Material(shader);
        if (r.sharedMaterial.HasProperty("_DimBlend") == false && shader != null && shader.name.Contains("CrossFade"))
            Assume.That(r.sharedMaterial.HasProperty("_DimBlend"));

        var comp = go.AddComponent<DimensionalShaderComponent>();
        comp.durationSeconds = 0.5f;
        comp.materialKind = DimensionalMaterialKind.MeshLit;
        comp.renderers.Add(r);
        comp.blendPropertyName = "_DimBlend";

        Assert.IsTrue(comp.TryBuildFadeJob(out var job));
        Assert.AreEqual(0.5f, job.durationSeconds, 1e-4f);
        Assert.AreEqual(1, job.renderers.Length);
        Assert.AreEqual(DimensionalMaterialKind.MeshLit, job.kind);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShaderComponent_FallbackMode_WhenNoBlendProp()
    {
        var go = new GameObject("shader-fallback");
        var r = go.AddComponent<MeshRenderer>();
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
        Assume.That(shader != null);
        r.sharedMaterial = new Material(shader);
        var comp = go.AddComponent<DimensionalShaderComponent>();
        comp.renderers.Add(r);
        comp.blendPropertyName = "_DimBlend";
        comp.fallbackMode = DimensionalShaderFallbackMode.HardCutAtHalf;
        Assert.IsTrue(comp.TryBuildFadeJob(out var job));
        Assert.IsFalse(job.hasBlendProperty);
        Assert.AreEqual(DimensionalShaderFallbackMode.HardCutAtHalf, job.fallbackMode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Binding_CaptureAndKeepAlive_RestoresPosition()
    {
        var cache = SharedDimensionalGenericCache.Ensure();
        var go = new GameObject("bound");
        go.transform.position = new Vector3(9, 8, 7);
        var binding = go.AddComponent<DimensionalLemmaBinding>();
        binding.lemmaEntryId = "L1";
        binding.instanceStableId = "stable-1";
        go.AddComponent<DimensionalLemmaPosition>();

        cache.Register(binding);
        cache.CaptureFromScene("main", 0);
        go.transform.position = Vector3.zero;
        cache.CopySharedPositional(0, 1);
        cache.ActiveDimIndex = 0;
        binding.ApplyKeepAlive("main", 1);
        Assert.AreEqual(new Vector3(9, 8, 7), go.transform.position);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OpenCloseBtMap_LookupAndFlags()
    {
        var go = new GameObject("oc-map");
        var comp = go.AddComponent<DimensionalShaderComponent>();
        var topo = ScriptableObject.CreateInstance<FakeOpenCloseTopologyStub>();
        comp.openCloseBtByDimension.Add(new DimensionalOpenCloseBtEntry
        {
            dimIndex = 1,
            topology = topo,
            runtimeMilliseconds = 1200,
            runOnEnter = true,
            runOnExit = false
        });

        Assert.IsFalse(comp.TryGetOpenCloseEntry(0, out _));
        Assert.IsTrue(comp.TryGetOpenCloseEntry(1, out var entry));
        Assert.AreEqual(1200, entry.runtimeMilliseconds);

        var stub = new StubOpenCloseRunner();
        DimensionalOpenCloseRunnerHost.Instance = stub;
        Assert.IsTrue(comp.BeginOpenCloseForDimension(1, entering: true));
        Assert.AreEqual(1200, stub.lastRuntimeMs);
        Assert.IsTrue(stub.lastEntering);

        Assert.IsFalse(comp.BeginOpenCloseForDimension(1, entering: false));
        entry.runOnExit = true;
        Assert.IsTrue(comp.BeginOpenCloseForDimension(1, entering: false));
        Assert.IsFalse(stub.lastEntering);

        entry.runtimeMilliseconds = -1;
        Assert.IsTrue(comp.BeginOpenCloseForDimension(1, entering: true));
        Assert.AreEqual(-1, stub.lastRuntimeMs);

        DimensionalOpenCloseRunnerHost.Instance = null;
        Object.DestroyImmediate(topo);
        Object.DestroyImmediate(go);
    }

    sealed class FakeOpenCloseTopologyStub : ScriptableObject { }

    sealed class StubOpenCloseRunner : IDimensionalOpenCloseRunner
    {
        public int lastRuntimeMs;
        public bool lastEntering;
        public void Begin(GameObject host, ScriptableObject topologyAsset, bool entering, int runtimeMilliseconds)
        {
            lastRuntimeMs = runtimeMilliseconds;
            lastEntering = entering;
        }
    }
}
