using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Senses
{
    /// <summary>
    /// Smell sensing model:
    /// - samples nearby SmellEmitter(s)
    /// - computes perceived intensity with distance falloff
    /// - advects/extends range using CombinedWind (Weather.Wind + Unity WindZone)
    ///
    /// This is designed to be called by <see cref="Sensor"/> when SensorType == Smell.
    /// </summary>
    [RequireComponent(typeof(Sensor))]
    public class SmellSensor : MonoBehaviour
    {
        [Header("Wind Composition Weights")]
        public float weatherWindWeight = 1f;
        public float unityWindWeight = 1f;

        [Header("Advection Tuning")]
        [Tooltip("Wind speed (m/s) at which advection reaches maximum effect.")]
        public float windSpeedForMaxEffect = 10f;

        [Tooltip("Multiplier applied when fully downwind (at max wind effect).")]
        public float downwindRangeMultiplier = 2.5f;

        [Tooltip("Multiplier applied when fully upwind (at max wind effect).")]
        public float upwindRangeMultiplier = 0.35f;

        [Tooltip("Extra multiplier for perceived intensity downwind (at max wind effect).")]
        public float downwindIntensityMultiplier = 2.0f;

        [Tooltip("Extra multiplier for perceived intensity upwind (at max wind effect).")]
        public float upwindIntensityMultiplier = 0.5f;

        [Header("Distance Falloff")]
        [Tooltip("Additional distance exponent (0 = pure inverse-square-ish).")]
        public float extraFalloffExponent = 0f;

        [Tooltip("Minimum perceived intensity to report as a detection.")]
        public float minimumReportIntensity = 0.01f;

        private readonly List<SmellEmitter> nearbyEmitters = new List<SmellEmitter>(64);

        private void Awake()
        {
            // Convenience: if this component is present, assume this Sensor is intended to be a smell sensor.
            var s = GetComponent<Sensor>();
            if (s != null)
            {
                s.sensorType = SensorType.Smell;
            }
        }

        public SensorData DetectSmell(Sensor hostSensor)
        {
            var data = new SensorData
            {
                sensorType = SensorType.Smell.ToString(),
                timestamp = Time.time
            };

            if (hostSensor == null)
                return data;

            Vector3 sensorPos = hostSensor.transform.position;
            float sensorRange = Mathf.Max(0f, hostSensor.range);

            // todo: does this require an `out` parameter for nearby emitters?
            SmellEmitter.GetEmittersInRange(sensorPos, sensorRange, nearbyEmitters);
            if (nearbyEmitters.Count == 0)
                return data;

            var weights = new CombinedWind.Weights
            {
                weatherWindWeight = weatherWindWeight,
                unityWindWeight = unityWindWeight
            };

            for (int i = 0; i < nearbyEmitters.Count; i++)
            {
                SmellEmitter emitter = nearbyEmitters[i];
                if (emitter == null)
                    continue;

                Vector3 emitterPos = emitter.transform.position;
                Vector3 toSensor = sensorPos - emitterPos;
                float distance = toSensor.magnitude;
                if (distance <= 0.0001f)
                    continue;

                float baseRadius = emitter.GetEffectiveRadius();
                float baseIntensity = emitter.GetEffectiveIntensity();
                if (baseRadius <= 0f || baseIntensity <= 0f)
                    continue;

                // Sample combined wind around the mid-point of the plume path.
                Vector3 samplePos = Vector3.Lerp(emitterPos, sensorPos, 0.5f);
                Vector3 wind = CombinedWind.GetWindAtPosition(samplePos, weights);
                float windSpeed = wind.magnitude;

                float windEffect01 = (windSpeedForMaxEffect <= 0.0001f)
                    ? 1f
                    : Mathf.Clamp01(windSpeed / windSpeedForMaxEffect);

                Vector3 windDir = windSpeed > 0.0001f ? (wind / windSpeed) : Vector3.zero; // normalized
                Vector3 toSensorDir = toSensor / distance; // normalized

                // alignment: +1 means wind pushes plume from emitter -> sensor, -1 means pushes away.
                float alignment = (windDir == Vector3.zero) ? 0f : Vector3.Dot(windDir, toSensorDir);
                float alignment01 = (alignment + 1f) * 0.5f; // from -1..1 to 0..1

                // static linear application of coefficient lerping to the range and intensity multipliers
                float rangeMultiplierDir = Mathf.Lerp(upwindRangeMultiplier, downwindRangeMultiplier, alignment01);
                float intensityMultiplierDir = Mathf.Lerp(upwindIntensityMultiplier, downwindIntensityMultiplier, alignment01);

                float effectiveRangeMultiplier = Mathf.Lerp(1f, rangeMultiplierDir, windEffect01);
                float effectiveIntensityMultiplier = Mathf.Lerp(1f, intensityMultiplierDir, windEffect01);

                float effectiveRadius = baseRadius * effectiveRangeMultiplier;
                if (distance > effectiveRadius)
                    continue;

                // Base falloff (inverse square-ish), plus optional exponent.
                float falloff = 1f / (1f + (distance * distance));
                if (extraFalloffExponent > 0f)
                {
                    falloff *= 1f / Mathf.Pow(1f + distance, extraFalloffExponent);
                }

                float perceived = baseIntensity * falloff * effectiveIntensityMultiplier;
                if (perceived < minimumReportIntensity)
                    continue;

                data.detectedObjects.Add(emitter.gameObject);
                data.smellDetections.Add(new SensorData.SmellDetection
                {
                    emitter = emitter.gameObject,
                    signature = emitter.signature,
                    perceivedIntensity = perceived,
                    distance = distance,
                    downwindAlignment = alignment,
                    windVector = wind
                });
            }

            return data;
        }
    }
}

