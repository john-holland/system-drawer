using System.Collections.Generic;
using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Aggregates emergence vectors from registered sources each tick.</summary>
    public sealed class WeatherEmergenceCollector : MonoBehaviour, IWeatherEmergenceSource
    {
        static readonly List<IWeatherEmergenceSource> s_registered = new List<IWeatherEmergenceSource>();

        public Transform playerFocus;
        public float playerFallbackRadius = 30f;
        public EmergenceVectorField Field { get; } = new EmergenceVectorField();

        readonly List<EmergenceVector> _scratch = new List<EmergenceVector>(64);

        public static void RegisterSource(IWeatherEmergenceSource source)
        {
            if (source != null && !s_registered.Contains(source))
                s_registered.Add(source);
        }

        public static void UnregisterSource(IWeatherEmergenceSource source)
        {
            if (source != null)
                s_registered.Remove(source);
        }

        void OnEnable()
        {
            WeatherEmergenceCollector.RegisterSource(this);
        }

        void OnDisable()
        {
            WeatherEmergenceCollector.UnregisterSource(this);
        }

        public void CollectEmergenceVectors(List<EmergenceVector> into)
        {
            Transform focus = playerFocus != null ? playerFocus : transform;
            into.Add(EmergenceVector.Point(focus.position, playerFallbackRadius, 1f, "player"));
        }

        public void Tick()
        {
            _scratch.Clear();
            for (int i = 0; i < s_registered.Count; i++)
            {
                if (s_registered[i] == null)
                    continue;
                s_registered[i].CollectEmergenceVectors(_scratch);
            }

            if (GetComponent<TravelPathEmergenceSource>() == null)
                WeatherEmergenceReflection.CollectTravel(_scratch);
            if (GetComponent<QuestEmergenceSource>() == null)
                WeatherEmergenceReflection.CollectQuest(_scratch);
            if (GetComponent<Spatial4DEmergenceSource>() == null)
            {
                Transform focus = playerFocus != null ? playerFocus : transform;
                WeatherEmergenceReflection.CollectSpatial4D(_scratch, focus);
            }

            Field.SetVectors(_scratch);
        }
    }
}
