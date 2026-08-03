using UnityEngine;

/// <summary>Toilet plumbing: flush inflow/outflow, overflow jet, clog.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Toilet Fixture")]
public sealed class ToiletFixture : MonoBehaviour
{
    public ToiletStation station;
    public FixturePlumbingNode plumbing;
    public ToiletOverflowJetDriver overflowJet;
    public MonoBehaviour floodSimulator;
    public float flushLiters = 6f;
    public float flushDurationSec = 1.2f;
    float _flushRemaining;

    void Awake()
    {
        if (station == null) station = GetComponent<ToiletStation>();
        if (plumbing == null) plumbing = GetComponent<FixturePlumbingNode>() ?? gameObject.AddComponent<FixturePlumbingNode>();
        plumbing.fixtureKind = FixtureKind.Toilet;
        plumbing.overflowJetEnabled = true;
        if (overflowJet == null) overflowJet = GetComponent<ToiletOverflowJetDriver>();
    }

    void Update()
    {
        if (_flushRemaining <= 0f) return;
        _flushRemaining -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(_flushRemaining / flushDurationSec);
        plumbing.SetInflow(0.9f * (1f - t * 0.5f), 0f);
        float outMul = plumbing.SetOutflow(1f - plumbing.clog.EffectiveClog01());
        EmitFlood(plumbing.CombinedInflowLitersPerSec(flushLiters / flushDurationSec) * (0.3f + outMul));

        if (plumbing.clog.EffectiveClog01() > 0.55f || (plumbing.overflowJetEnabled && outMul < 0.35f))
            overflowJet?.ActivateJet(plumbing.AvailableCold01() * MunicipalWaterService.Instance.EffectivePressure01());

        if (_flushRemaining <= 0f)
        {
            plumbing.SetInflow(0f, 0f);
            plumbing.SetOutflow(0f);
        }
    }

    public void Flush()
    {
        _flushRemaining = flushDurationSec;
        plumbing.plumbingGroup?.NotifyToiletFlushed(1f);
        if (station?.options != null)
            station.options.autoFlush = true;
    }

    void EmitFlood(float lps)
    {
        if (floodSimulator == null || lps <= 0f) return;
        var m = floodSimulator.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) });
        m?.Invoke(floodSimulator, new object[] { lps });
    }
}
