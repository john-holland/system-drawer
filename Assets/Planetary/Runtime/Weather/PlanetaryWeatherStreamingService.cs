using System;
using System.Collections.Generic;
using Planetary.Composition;
using UnityEngine;
using UnityEngine.Networking;

namespace Planetary.WeatherStreaming
{
    [Serializable]
    public struct WeatherTilePayload
    {
        public float cloudBaseM;
        public float cloudTopM;
        public float cloudCover;
        public float pressureScaleHeight;
        public int altitudeBandMask;
    }

    public sealed class PlanetaryWeatherStreamingService : MonoBehaviour
    {
        public string continuumBaseUrl = "http://localhost:5050";
        public string planetId = "default";
        readonly Dictionary<string, WeatherTilePayload> _cache = new Dictionary<string, WeatherTilePayload>();

        public event Action<string, WeatherTilePayload> OnTileActivated;

        public void RequestWeatherTile(float latDeg, float lonDeg, Action<WeatherTilePayload> onDone)
        {
            string url = $"{continuumBaseUrl.TrimEnd('/')}/api/planet/weather_tiles?planet_id={planetId}&lat={latDeg:F2}&lon={lonDeg:F2}";
            StartCoroutine(GetWeather(url, latDeg, lonDeg, onDone));
        }

        System.Collections.IEnumerator GetWeather(string url, float lat, float lon, Action<WeatherTilePayload> onDone)
        {
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            WeatherTilePayload payload = default;
            if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
            {
                try
                {
                    var json = JsonUtility.FromJson<WeatherTileJson>(req.downloadHandler.text);
                    payload = new WeatherTilePayload
                    {
                        cloudBaseM = json.cloud_base_m,
                        cloudTopM = json.cloud_top_m,
                        cloudCover = json.cloud_cover,
                        pressureScaleHeight = json.pressure_scale_height,
                        altitudeBandMask = json.altitude_band_mask
                    };
                }
                catch
                {
                    payload = DefaultPayload();
                }
            }
            else
                payload = DefaultPayload();
            string key = $"{lat:F2}_{lon:F2}";
            _cache[key] = payload;
            OnTileActivated?.Invoke(key, payload);
            onDone?.Invoke(payload);
        }

        static WeatherTilePayload DefaultPayload() => new WeatherTilePayload
        {
            cloudBaseM = 1000f,
            cloudTopM = 3000f,
            cloudCover = 0.5f,
            pressureScaleHeight = 8500f,
            altitudeBandMask = 4
        };

        [Serializable]
        class WeatherTileJson
        {
            public float cloud_base_m;
            public float cloud_top_m;
            public float cloud_cover;
            public float pressure_scale_height;
            public int altitude_band_mask;
        }
    }
}
