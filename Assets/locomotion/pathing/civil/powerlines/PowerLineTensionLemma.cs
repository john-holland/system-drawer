using UnityEngine;

public enum PowerLineStandoffLemma
{
    Nominal = 0,
    Unbreakable = 1,
    FaultyStandoff = 2
}

/// <summary>Developer in-paint for power-line / pole break behavior.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Power Line Tension Lemma")]
public sealed class PowerLineTensionLemma : MonoBehaviour
{
    public PowerLineStandoffLemma lemma = PowerLineStandoffLemma.Nominal;
    [Range(0f, 1f)] public float leanBias01 = 0.7f;
    [Range(0f, 1f)] public float poleBreakChance01 = 0.08f;

    public void ApplyToken(string token)
    {
        var t = (token ?? "").ToLowerInvariant().Replace('-', '_');
        if (t.Contains("unbreakable")) lemma = PowerLineStandoffLemma.Unbreakable;
        else if (t.Contains("faulty")) lemma = PowerLineStandoffLemma.FaultyStandoff;
        else lemma = PowerLineStandoffLemma.Nominal;
    }

    public bool ShouldBreakPole(float tension01)
    {
        if (lemma == PowerLineStandoffLemma.Unbreakable) return false;
        float chance = poleBreakChance01;
        if (lemma == PowerLineStandoffLemma.FaultyStandoff)
            chance = Mathf.Clamp01(chance + 0.35f);
        return tension01 > leanBias01 && Random.value < chance * tension01;
    }

    public float LeanAmount(float tension01)
    {
        if (lemma == PowerLineStandoffLemma.Unbreakable)
            return tension01 * 0.05f;
        return tension01 * leanBias01;
    }
}
