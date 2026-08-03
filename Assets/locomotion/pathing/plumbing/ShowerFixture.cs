using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Shower Fixture")]
public sealed class ShowerFixture : MonoBehaviour
{
    public FixturePlumbingNode plumbing;
    public MonoBehaviour floodSimulator;
    [Range(0f, 1f)] public float valveOpen01;
    [Range(0f, 1f)] public float hotMix01 = 0.55f;
    public float maxLitersPerSec = 0.35f;

    void Awake()
    {
        if (plumbing == null) plumbing = GetComponent<FixturePlumbingNode>() ?? gameObject.AddComponent<FixturePlumbingNode>();
        plumbing.fixtureKind = FixtureKind.Shower;
        plumbing.pressureGaugeBlowEnabled = false;
        if (plumbing.branchIdCold == "cold_a")
            plumbing.branchIdCold = "cold_shower";
        if (plumbing.branchIdHot == "hot_a")
            plumbing.branchIdHot = "hot_shower";
    }

    void Update()
    {
        if (valveOpen01 <= 0f && !plumbing.nozzlePoppedOff)
        {
            plumbing.SetInflow(0f, 0f);
            plumbing.SetOutflow(0f);
            return;
        }
        float open = plumbing.nozzlePoppedOff ? 1f : valveOpen01;
        plumbing.SetInflow(open * (1f - hotMix01), open * hotMix01);
        plumbing.SetOutflow(open);
        Emit(plumbing.CombinedInflowLitersPerSec(maxLitersPerSec));
    }

    public void SetValve(float open01, float hotMix = -1f)
    {
        valveOpen01 = Mathf.Clamp01(open01);
        if (hotMix >= 0f) hotMix01 = Mathf.Clamp01(hotMix);
    }

    void Emit(float lps)
    {
        if (floodSimulator == null || lps <= 0f) return;
        floodSimulator.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) })
            ?.Invoke(floodSimulator, new object[] { lps });
    }
}
