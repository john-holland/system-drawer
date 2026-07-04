using System.Collections.Generic;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>SpatialGenerator4D emergence SDF gradient adapter.</summary>
    public sealed class Spatial4DEmergenceSource : MonoBehaviour, IWeatherEmergenceSource
    {
        public Transform focusTransform;
        public float weight = 0.8f;

        void OnEnable() => WeatherEmergenceCollector.RegisterSource(this);
        void OnDisable() => WeatherEmergenceCollector.UnregisterSource(this);

        public void CollectEmergenceVectors(List<EmergenceVector> into)
        {
            Transform focus = focusTransform != null ? focusTransform : transform;
            WeatherEmergenceReflection.CollectSpatial4D(into, focus, weight);
        }
    }
}
