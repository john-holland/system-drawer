using Planetary.TimeTravel;
using Planetary.WeatherStreaming;
using UnityEngine;
using Weather.Executor;

namespace Planetary.Weather
{
    /// <summary>Bootstrap player weather eggs on teleport, streaming tiles, and time travel.</summary>
    [AddComponentMenu("Planetary/Weather Egg Bootstrapper")]
    public sealed class WeatherEggBootstrapper : MonoBehaviour
    {
        public PlanetaryWeatherStreamingService streamingService;
        public Transform focusTransform;
        public float teleportDistanceThreshold = 50f;
        public Vector3 defaultEggRadii = new Vector3(40f, 60f, 40f);

        WeatherExecutorService _executor;
        PlanetaryWeatherTimeTravelSystem _timeTravel;
        Vector3 _lastBootstrapPos;
        bool _hasLastPos;

        void Awake()
        {
            _executor = WeatherExecutorService.Instance ?? FindAnyObjectByType<WeatherExecutorService>();
            _timeTravel = FindAnyObjectByType<PlanetaryWeatherTimeTravelSystem>();
            if (streamingService == null)
                streamingService = FindAnyObjectByType<PlanetaryWeatherStreamingService>();
            if (focusTransform == null && _executor != null)
                focusTransform = _executor.focusTransform;
        }

        void OnEnable()
        {
            WeatherNetworkSink.OnRewindApplied += OnNarrativeRewindApplied;
            WeatherNetworkSink.OnSceneLoad += OnSceneLoaded;
        }

        void OnDisable()
        {
            WeatherNetworkSink.OnRewindApplied -= OnNarrativeRewindApplied;
            WeatherNetworkSink.OnSceneLoad -= OnSceneLoaded;
        }

        void OnSceneLoaded(string sceneName)
        {
            if (focusTransform != null)
                BootstrapAtPosition(focusTransform.position);
        }

        void Update()
        {
            if (focusTransform == null)
                return;

            if (!_hasLastPos || Vector3.Distance(focusTransform.position, _lastBootstrapPos) >= teleportDistanceThreshold)
                BootstrapAtPosition(focusTransform.position);
        }

        public void BootstrapAtPosition(Vector3 worldPosition)
        {
            _lastBootstrapPos = worldPosition;
            _hasLastPos = true;

            var payload = new WeatherEggBootstrapPayload
            {
                suggestedCenter = worldPosition,
                suggestedRadii = defaultEggRadii
            };

            if (_timeTravel != null)
            {
                var frame = _timeTravel.CaptureCurrentPublic();
                payload.weatherFrameJson = WeatherTimeTravelFrameSerializer.ToJson(frame);
            }

            if (_timeTravel != null && !string.IsNullOrEmpty(payload.weatherFrameJson))
            {
                var frame = WeatherTimeTravelFrameSerializer.FromJson(payload.weatherFrameJson);
                if (frame != null)
                    _timeTravel.ApplyFramePublic(frame);
            }

            _executor?.BootstrapEgg(payload);

            if (streamingService != null)
            {
                float lat = worldPosition.z * 0.01f;
                float lon = worldPosition.x * 0.01f;
                payload.latDeg = lat;
                payload.lonDeg = lon;
                streamingService.RequestWeatherTile(lat, lon, tile =>
                {
                    if (_executor == null)
                        return;
                    var egg = _executor.GetOrCreateEgg("local");
                    egg.transform.position = worldPosition;
                });
            }
        }

        public void OnNarrativeRewindApplied()
        {
            if (focusTransform != null)
                BootstrapAtPosition(focusTransform.position);
        }
    }
}
