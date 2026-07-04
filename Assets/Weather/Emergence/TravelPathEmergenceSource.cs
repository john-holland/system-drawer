using System.Collections.Generic;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Travel polylines as emergence corridor vectors.</summary>
    public sealed class TravelPathEmergenceSource : MonoBehaviour, IWeatherEmergenceSource
    {
        [Range(0f, 1f)] public float previewWeight = 0.45f;

        void OnEnable() => WeatherEmergenceCollector.RegisterSource(this);
        void OnDisable() => WeatherEmergenceCollector.UnregisterSource(this);

        public void CollectEmergenceVectors(List<EmergenceVector> into) =>
            WeatherEmergenceReflection.CollectTravel(into, previewWeight);
    }
}
