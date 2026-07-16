using System;
using UnityEngine;

/// <summary>Circadian-ish oscillator modulating HR, clear thought, attention, immune lightly.</summary>
[Serializable]
public sealed class BioRhythmClock
{
    public float phaseRadians;
    [Range(0f, 1f)] public float amplitude01 = 0.35f;
    public float cyclesPerSecond = 1f / 120f;

    public void Tick(float dt)
    {
        phaseRadians += dt * cyclesPerSecond * Mathf.PI * 2f;
        if (phaseRadians > Mathf.PI * 2f)
            phaseRadians -= Mathf.PI * 2f;
    }

    public void ApplyAmplitudeDelta(float delta)
    {
        amplitude01 = Mathf.Clamp01(amplitude01 + delta);
    }

    /// <summary>Returns ±amplitude contribution in 01 space.</summary>
    public float Modulation01()
    {
        return Mathf.Sin(phaseRadians) * amplitude01 * 0.05f;
    }
}
