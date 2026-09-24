using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Consent aggregator. Unused sources contribute 0 and remaining weights renormalize so a missing
/// legal warden is not a hole. Soft-clamps Love physicality to <see cref="maxPhysicality01"/>.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Consent Warden")]
public sealed class ConsentWarden : MonoBehaviour
{
    public ThreatWarden threatWarden;
    public TheocraticWarden theocraticWarden;
    public JusticeWarden justiceWarden;
    public RightsWarden rightsWarden;
    public LoveWarden loveWarden;

    [Range(0f, 1f)] public float wThreat = 1f / 3f;
    [Range(0f, 1f)] public float wTheo = 1f / 3f;
    [Range(0f, 1f)] public float wJust = 1f / 3f;
    [Range(0f, 1f)] public float wRights;
    [Range(0f, 1f)] public float lastScore01 = 1f;
    [Range(0f, 1f)] public float maxPhysicality01 = 0.95f;
    public List<WardenLimitKv> limits = new System.Collections.Generic.List<WardenLimitKv>();

    public float Allow01() => Evaluate();

    public float Evaluate()
    {
        float threat = threatWarden != null ? threatWarden.MaxThreat01() : 0f;
        float theo = theocraticWarden != null ? theocraticWarden.Allow01() : 0f;
        float just = justiceWarden != null ? justiceWarden.Allow01() : 0f;
        float rights = rightsWarden != null ? rightsWarden.Allow01() : 0f;
        lastScore01 = Blend01(
            wThreat, threat, threatWarden != null,
            wTheo, theo, theocraticWarden != null,
            wJust, just, justiceWarden != null,
            wRights, rights, rightsWarden != null);
        maxPhysicality01 = lastScore01;
        loveWarden?.CapPhysicality(maxPhysicality01);
        return lastScore01;
    }

    /// <summary>
    /// Weighted consent blend. Missing sources drop out and remaining weights renormalize.
    /// When Rights is present and <paramref name="wRights"/> is 0, remaining present sources
    /// (including Rights) share equally.
    /// </summary>
    public static float Blend01(
        float wThreat, float threat01, bool hasThreat,
        float wTheo, float theoAllow, bool hasTheo,
        float wJust, float justAllow, bool hasJust,
        float wRights, float rightsAllow, bool hasRights)
    {
        float tw = hasThreat ? wThreat : 0f;
        float thw = hasTheo ? wTheo : 0f;
        float jw = hasJust ? wJust : 0f;
        float rw = hasRights ? wRights : 0f;
        if (hasRights && wRights <= 1e-6f)
        {
            int n = (hasThreat ? 1 : 0) + (hasTheo ? 1 : 0) + (hasJust ? 1 : 0) + 1;
            float eq = 1f / n;
            tw = hasThreat ? eq : 0f;
            thw = hasTheo ? eq : 0f;
            jw = hasJust ? eq : 0f;
            rw = eq;
        }
        float sum = tw + thw + jw + rw;
        if (sum < 1e-6f) return 1f;
        tw /= sum;
        thw /= sum;
        jw /= sum;
        rw /= sum;
        return Mathf.Clamp01(
            tw * (1f - Mathf.Clamp01(threat01)) +
            thw * Mathf.Clamp01(theoAllow) +
            jw * Mathf.Clamp01(justAllow) +
            rw * Mathf.Clamp01(rightsAllow));
    }
}
