using Locomotion.Liquid;
using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Cabin turbulence for in-flight drink comedy beats.</summary>
    public sealed class CabinTurbulenceDriver : MonoBehaviour
    {
        public Transform cabinRoot;
        public NervousSystem nervousSystem;
        public float shakeAmplitude = 0.08f;
        public float shakeFrequency = 2.5f;
        public float bumpIntervalSeconds = 1.2f;
        public float motorImpulseStrength = 0.35f;

        bool _active;
        float _bumpTimer;
        Vector3 _baseLocalPos;

        public float TurbulenceIntensity01 { get; private set; }

        void Awake()
        {
            if (cabinRoot == null)
                cabinRoot = transform;
            if (nervousSystem == null)
                nervousSystem = GetComponentInParent<NervousSystem>();
            _baseLocalPos = cabinRoot.localPosition;
        }

        public void SetActiveForBeat(bool active) => _active = active;

        void Update()
        {
            if (!_active || cabinRoot == null)
            {
                TurbulenceIntensity01 = 0f;
                return;
            }

            float t = Time.time;
            float nx = (Mathf.PerlinNoise(t * shakeFrequency, 0f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0f, t * shakeFrequency) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(t, t) - 0.5f) * 2f;
            cabinRoot.localPosition = _baseLocalPos + new Vector3(nx, ny, nz) * shakeAmplitude;
            TurbulenceIntensity01 = Mathf.Clamp01(new Vector3(nx, ny, nz).magnitude);

            _bumpTimer += Time.deltaTime;
            if (_bumpTimer >= bumpIntervalSeconds)
            {
                _bumpTimer = 0f;
                ApplyMotorBump();
            }
        }

        void ApplyMotorBump()
        {
            if (nervousSystem == null)
                return;
            var motorData = new MotorData("turbulence", motorImpulseStrength, 0.15f, null)
            {
                forceDirection = Random.insideUnitSphere
            };
            var impulse = new ImpulseData(ImpulseType.Motor, nameof(CabinTurbulenceDriver), "Limb", motorData, 0);
            nervousSystem.SendImpulseDown("Limb", impulse);
        }
    }
}
