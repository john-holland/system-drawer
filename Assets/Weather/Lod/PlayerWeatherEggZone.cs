using UnityEngine;
using Weather.Executor;

namespace Weather.Lod
{
    /// <summary>Per-player prolate spheroid weather simulation zone.</summary>
    public sealed class PlayerWeatherEggZone : MonoBehaviour
    {
        [Header("Egg Shape")]
        public Vector3 radii = new Vector3(40f, 60f, 40f);

        [Header("Identity")]
        public string clientId = "local";

        [Header("Merge")]
        [Range(0f, 1f)] public float confidence = 1f;
        [Range(0f, 1f)] public float serverBlend = 0f;
        public float clientMinBlend = 0.1f;
        public float clientRecoverySeconds = 0.5f;

        public SphericalHyperplaneRegression Regression { get; } = new SphericalHyperplaneRegression();
        public WeatherDiffCircuitBreaker CircuitBreaker { get; } = new WeatherDiffCircuitBreaker();

        float _serverBlendStartTime = -999f;
        float _targetServerBlend;

        public Vector3 Center => transform.position;
        public Vector3 Radii => radii;

        public Bounds GetBounds() => WeatherEggBounds.GetAabb(Center, radii);

        public bool Contains(Vector3 world) => WeatherEggBounds.Contains(Center, radii, world);

        void OnEnable()
        {
            WeatherExecutorService.Instance?.Registry.Register(this);
        }

        void OnDisable()
        {
            WeatherExecutorService.Instance?.Registry.Unregister(this);
        }

        public void BeginServerBlend(float targetBlend)
        {
            _targetServerBlend = Mathf.Clamp01(targetBlend);
            _serverBlendStartTime = Time.time;
        }

        public void TickServerBlend()
        {
            if (_serverBlendStartTime < 0f)
                return;
            float t = clientRecoverySeconds > 0f
                ? Mathf.Clamp01((Time.time - _serverBlendStartTime) / clientRecoverySeconds)
                : 1f;
            serverBlend = Mathf.Lerp(clientMinBlend, _targetServerBlend, t);
        }

        public ManifoldCellData QueryLocal(Vector3 world, WeatherPhysicsManifold manifold)
        {
            if (Contains(world) && manifold != null)
                return manifold.GetDataAtPosition(world);
            return Regression.Evaluate(world);
        }
    }
}
