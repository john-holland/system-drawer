using UnityEngine;

/// <summary>Ubiquitous municipal water supply — auto-present; lemma/bespoke can scale pressure up or down.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Municipal Water Service")]
public sealed class MunicipalWaterService : MonoBehaviour
{
    public const string ServiceKey = "civil.municipalWater";

    static MunicipalWaterService _instance;
    public static MunicipalWaterService Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<MunicipalWaterService>();
            if (_instance == null)
            {
                var go = new GameObject("MunicipalWaterService");
                _instance = go.AddComponent<MunicipalWaterService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Range(0f, 2f)] public float supplyPressure01 = 1f;
    [Range(0f, 1f)] public float hotSupply01 = 0.85f;
    [Range(0f, 1f)] public float coldSupply01 = 1f;
    [Range(0f, 1f)] public float sewerCapacity01 = 1f;
    public MunicipalWaterLemmaBias lemmaBias;

    void Awake()
    {
        _instance = this;
        if (lemmaBias == null)
            lemmaBias = GetComponent<MunicipalWaterLemmaBias>();
        TryRegisterDrawer();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void TryRegisterDrawer()
    {
        try
        {
            var t = System.Type.GetType("SystemDrawerService");
            if (t == null) return;
            var inst = t.GetProperty("Instance")?.GetValue(null);
            if (inst == null) return;
            t.GetMethod("Register")?.Invoke(inst, new object[] { ServiceKey, this });
        }
        catch { /* optional */ }
    }

    public float EffectivePressure01()
    {
        float p = supplyPressure01;
        if (lemmaBias != null)
            p *= lemmaBias.PressureScale;
        return Mathf.Clamp(p, 0f, 2f);
    }

    public float EffectiveHot01()
    {
        float h = hotSupply01;
        if (lemmaBias != null)
            h = Mathf.Clamp01(h * lemmaBias.HotScale);
        return h;
    }

    public float EffectiveCold01()
    {
        float c = coldSupply01;
        if (lemmaBias != null)
            c = Mathf.Clamp01(c * lemmaBias.ColdScale);
        return c;
    }

    public float EffectiveSewer01()
    {
        float s = sewerCapacity01;
        if (lemmaBias != null)
            s = Mathf.Clamp01(s * lemmaBias.SewerScale);
        return s;
    }

    /// <summary>Publish capacity into SewerGraph tick (does not replace the graph).</summary>
    public void PublishToSewerGraph(SewerGraph graph, float dt)
    {
        if (graph == null) return;
        graph.municipalWater = this;
        sewerCapacity01 = EffectiveSewer01();
        graph.TickFlow(dt);
    }
}
