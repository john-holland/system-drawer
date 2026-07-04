using System.Collections.Generic;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Quest objectives with map-layer=emergence boost corridor weight.</summary>
    public sealed class QuestEmergenceSource : MonoBehaviour, IWeatherEmergenceSource
    {
        public float boostWeight = 1.25f;

        void OnEnable() => WeatherEmergenceCollector.RegisterSource(this);
        void OnDisable() => WeatherEmergenceCollector.UnregisterSource(this);

        public void CollectEmergenceVectors(List<EmergenceVector> into) =>
            WeatherEmergenceReflection.CollectQuest(into, boostWeight);
    }
}
