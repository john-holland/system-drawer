using System.Collections.Generic;
using UnityEngine;
using Weather.Emergence;

/// <summary>
/// Typed travel emergence bridge (Locomotion.Runtime). Add to scene alongside WeatherEmergenceCollector.
/// </summary>
public sealed class TravelPathWeatherEmergenceBridge : MonoBehaviour, IWeatherEmergenceSource
{
    [Range(0f, 1f)] public float previewWeight = 0.45f;

    void OnEnable() => WeatherEmergenceCollector.RegisterSource(this);
    void OnDisable() => WeatherEmergenceCollector.UnregisterSource(this);

    public void CollectEmergenceVectors(List<EmergenceVector> into)
    {
        foreach (TravelAgent agent in TravelAgentRegistry.All)
        {
            if (agent == null)
                continue;
            List<Vector3> poly = TravelMultibodyPathAdjuster.BuildEffectivePolyline(agent);
            if (poly == null || poly.Count < 2)
                continue;
            float radius = 4f;
            if (agent.multibody != null)
                radius = Mathf.Max(radius, agent.multibody.clearanceRadius);
            bool hasPlan = poly.Count > 2;
            float w = hasPlan ? 1f : previewWeight;
            for (int i = 0; i < poly.Count - 1; i++)
                into.Add(EmergenceVector.Segment(poly[i], poly[i + 1], radius, w, $"travel:{agent.GetInstanceID()}:{i}"));
        }
    }
}
