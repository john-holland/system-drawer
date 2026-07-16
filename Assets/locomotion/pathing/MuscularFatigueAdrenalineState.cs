using UnityEngine;

/// <summary>Per-actor fatigue / adrenaline / strength for rail deflection success estimation.</summary>
public sealed class MuscularFatigueAdrenalineState : MonoBehaviour
{
    [Range(0f, 1f)] public float fatigue01;
    [Range(0f, 1f)] public float adrenaline01;
    [Range(0f, 1f)] public float strength01 = 0.65f;

    public float fatiguePerSwing = 0.08f;
    public float adrenalineDecayPerSecond = 0.15f;
    public float fatigueRecoveryPerSecond = 0.04f;
    public float adrenalineOnNearMiss = 0.35f;
    public float adrenalineOnKo = 0.55f;

    void Update()
    {
        float dt = Time.deltaTime;
        adrenaline01 = Mathf.Clamp01(adrenaline01 - adrenalineDecayPerSecond * dt);
        fatigue01 = Mathf.Clamp01(fatigue01 - fatigueRecoveryPerSecond * dt);
    }

    public void RegisterSwing()
    {
        fatigue01 = Mathf.Clamp01(fatigue01 + fatiguePerSwing);
    }

    public void RegisterNearMiss()
    {
        adrenaline01 = Mathf.Clamp01(adrenaline01 + adrenalineOnNearMiss);
    }

    public void RegisterKo()
    {
        adrenaline01 = Mathf.Clamp01(adrenaline01 + adrenalineOnKo);
    }

    public float EffectiveStrength => Mathf.Clamp01(strength01 * (1f - 0.55f * fatigue01) * (1f + 0.45f * adrenaline01));
}
