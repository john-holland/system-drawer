using UnityEngine;

/// <summary>Witcher-style life-force capacity / resilience track.</summary>
[Serializable]
public sealed class LifeForceChannel
{
    [Range(0f, 2f)] public float lifeForce01 = 0.85f;

    public void ApplyDelta(float delta)
    {
        lifeForce01 = Mathf.Max(0f, lifeForce01 + delta);
    }

    public void TickTowardSetpoint(float setpoint, float ratePerSecond, float dt)
    {
        lifeForce01 = Mathf.MoveTowards(lifeForce01, setpoint, ratePerSecond * dt);
    }
}
