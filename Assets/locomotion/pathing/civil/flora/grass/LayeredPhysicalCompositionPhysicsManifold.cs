using UnityEngine;
using Weather;

public struct LayeredCompositionSample
{
    public float temperatureC;
    public float moisture01;
    public Vector3 wind;
    public SoilHorizonId horizon;
    public string compositionId;
}

/// <summary>
/// One physical layer per soil horizon. Temperature, moisture, and wind come from the weather manifold.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Layered Physical Composition Physics Manifold")]
public sealed class LayeredPhysicalCompositionPhysicsManifold : MonoBehaviour
{
    public GroundCompositionSet composition;
    public WeatherPhysicsManifold weather;

    public LayeredCompositionSample SampleAt(Vector3 world, SoilHorizonId horizon)
    {
        float temperature = weather != null ? weather.GetTemperatureAtPosition(world) : 15f;
        float pressure = weather != null ? weather.GetPressureAtPosition(world) : 1013f;
        Vector3 wind = weather != null ? weather.GetVelocityAtPosition(world) : Vector3.zero;
        if (composition != null)
            composition.EnsureDefaultHorizons();
        return new LayeredCompositionSample
        {
            temperatureC = temperature,
            moisture01 = Mathf.InverseLerp(870f, 1080f, pressure),
            wind = wind,
            horizon = horizon,
            compositionId = composition != null ? composition.CompositionId(horizon) : "loam"
        };
    }
}
