using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Sink Fixture")]
public sealed class SinkFixture : MonoBehaviour
{
    public FixturePlumbingNode plumbing;
    public MonoBehaviour floodSimulator;
    [Range(0f, 1f)] public float tapOpen01;
    [Range(0f, 1f)] public float hotMix01 = 0.4f;
    public float maxLitersPerSec = 0.25f;

    void Awake()
    {
        if (plumbing == null) plumbing = GetComponent<FixturePlumbingNode>() ?? gameObject.AddComponent<FixturePlumbingNode>();
        plumbing.fixtureKind = FixtureKind.Sink;
        plumbing.pressureGaugeBlowEnabled = false;
        // Unique cold branch from toilet by default
        if (plumbing.branchIdCold == "cold_a")
            plumbing.branchIdCold = "cold_sink";
        if (plumbing.branchIdHot == "hot_a")
            plumbing.branchIdHot = "hot_sink";
    }

    void Update()
    {
        if (tapOpen01 <= 0f || plumbing.nozzlePoppedOff)
        {
            if (plumbing.nozzlePoppedOff)
            {
                plumbing.SetInflow(1f, hotMix01);
                Emit(plumbing.CombinedInflowLitersPerSec(maxLitersPerSec * 1.5f));
            }
            else
            {
                plumbing.SetInflow(0f, 0f);
                plumbing.SetOutflow(0f);
            }
            return;
        }
        plumbing.SetInflow(tapOpen01 * (1f - hotMix01), tapOpen01 * hotMix01);
        float drained = plumbing.SetOutflow(tapOpen01);
        float spill = plumbing.CombinedInflowLitersPerSec(maxLitersPerSec) * (1f - drained);
        Emit(Mathf.Max(0f, spill) + plumbing.CombinedInflowLitersPerSec(maxLitersPerSec) * 0.1f);
    }

    public void SetTap(float open01, float hotMix = -1f)
    {
        tapOpen01 = Mathf.Clamp01(open01);
        if (hotMix >= 0f) hotMix01 = Mathf.Clamp01(hotMix);
    }

    void Emit(float lps)
    {
        if (floodSimulator == null || lps <= 0f) return;
        floodSimulator.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) })
            ?.Invoke(floodSimulator, new object[] { lps });
    }
}
