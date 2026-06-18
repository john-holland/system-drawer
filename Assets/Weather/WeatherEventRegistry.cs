using System.Collections.Generic;

namespace Weather
{
    /// <summary>Tracks active WeatherEvent instances without per-frame scene scans.</summary>
    public static class WeatherEventRegistry
    {
        static readonly List<WeatherEvent> Active = new List<WeatherEvent>(32);

        public static void Register(WeatherEvent weatherEvent)
        {
            if (weatherEvent == null || Active.Contains(weatherEvent))
                return;
            Active.Add(weatherEvent);
        }

        public static void Unregister(WeatherEvent weatherEvent)
        {
            if (weatherEvent == null)
                return;
            Active.Remove(weatherEvent);
        }

        public static void CopyTo(List<WeatherEvent> destination)
        {
            destination.Clear();
            for (int i = 0; i < Active.Count; i++)
            {
                WeatherEvent e = Active[i];
                if (e != null)
                    destination.Add(e);
            }
        }

        public static int Count => Active.Count;
    }
}
