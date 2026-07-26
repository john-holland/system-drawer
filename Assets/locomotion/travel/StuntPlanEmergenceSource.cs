using System.Collections.Generic;
using UnityEngine;
using Weather.Emergence;

/// <summary>
/// Broccoli-like branching plume from Stuntman / Safety Warden plan forks; fades with age and branch fade01.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Stunt Plan Emergence Source")]
public sealed class StuntPlanEmergenceSource : MonoBehaviour, IWeatherEmergenceSource
{
    [Range(0f, 2f)] public float baseWeight = 0.8f;
    [Min(0.5f)] public float fadeSeconds = 8f;
    [Min(0.5f)] public float influenceRadius = 6f;

    void OnEnable() => WeatherEmergenceCollector.RegisterSource(this);
    void OnDisable() => WeatherEmergenceCollector.UnregisterSource(this);

    public void CollectEmergenceVectors(List<EmergenceVector> into)
    {
        if (into == null) return;
        float age = StuntPlanEmergenceBuffer.AgeSeconds;
        float ageFade = fadeSeconds <= 1e-3f ? 0f : Mathf.Clamp01(1f - age / fadeSeconds);
        if (ageFade <= 1e-3f) return;

        var branches = StuntPlanEmergenceBuffer.Current;
        for (int i = 0; i < branches.Count; i++)
        {
            var b = branches[i];
            float w = baseWeight * b.weight * ageFade * (1f - b.fade01 * 0.85f);
            if (w <= 1e-4f) continue;
            string id = b.stuntmanPreferred ? "stuntman" : "safety_warden";
            into.Add(EmergenceVector.Segment(b.a, b.b, influenceRadius, w, id));
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var branches = StuntPlanEmergenceBuffer.Current;
        for (int i = 0; i < branches.Count; i++)
        {
            var b = branches[i];
            Gizmos.color = b.stuntmanPreferred
                ? new Color(1f, 0.45f, 0.15f, 0.7f * (1f - b.fade01))
                : new Color(0.25f, 0.75f, 1f, 0.55f * (1f - b.fade01));
            Gizmos.DrawLine(b.a, b.b);
        }
    }
#endif
}
