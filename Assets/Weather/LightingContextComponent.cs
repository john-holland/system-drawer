using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Runtime holder for lighting context values derived from GPS/date/weather analysis.
    /// Can optionally push its sun direction to a directional light.
    /// </summary>
    public class LightingContextComponent : MonoBehaviour
    {
        [Header("Sun Position")]
        public float sunAzimuthDeg;
        public float sunElevationDeg;
        public bool sunVisible = true;
        public Vector3 sunDirectionVectorWorld = new Vector3(0f, 1f, 0f);
        public string sunDirectionSource = "calculated";
        [Range(0f, 1f)] public float sunDirectionConfidence = 0.5f;

        [Header("Moon Position")]
        public float moonAzimuthDeg;
        public float moonElevationDeg;
        public Vector3 moonDirectionVectorWorld = new Vector3(0f, 1f, 0f);
        public string moonDirectionSource = "calculated";
        [Range(0f, 1f)] public float moonDirectionConfidence = 0.5f;
        [Range(0f, 1f)] public float moonIlluminationFraction;
        public bool moonVisible;

        [Header("Inferred Direction (2C)")]
        public Vector3 inferredSunDirectionVector = Vector3.zero;
        [Range(0f, 1f)] public float inferredSunDirectionConfidence = 0f;

        [Header("Validation")]
        [Range(0f, 1f)] public float lightingValidityScore = 0f;
        [TextArea] public string lightingValidationFlags;

        [Header("Date Time (UTC)")]
        public int year = 2026;
        [Range(1, 12)] public int month = 1;
        [Range(1, 31)] public int day = 1;
        [Range(0, 23)] public int hour = 12;
        [Range(0, 59)] public int minute = 0;
        [Range(0, 59)] public int second = 0;

        [Header("Weather Snapshot")]
        [Range(0f, 100f)] public float cloudCoverPct;
        public float visibilityM;
        public float precipitationMm;
        public float windSpeedMps;
        public string weatherProvider = "unknown";

        [Header("Multi-Body (generalized)")]
        [Tooltip("When non-empty, aggregate_direction is used for primary light when apply_aggregate_direction is true.")]
        public UnityEngine.Vector3[] lightSourceDirections = System.Array.Empty<UnityEngine.Vector3>();
        [Tooltip("Primary aggregate direction from multi-body solver; used when apply_aggregate_direction is true.")]
        public Vector3 aggregateDirection = new Vector3(0f, 1f, 0f);
        [Tooltip("Use aggregate_direction instead of sun when available from multi-body response.")]
        public bool applyAggregateDirection = false;
        [Tooltip("Eclipse events from query; empty when none.")]
        public string eclipsesJson = "";

        [Header("Light Binding")]
        public Light directionalLight;
        public bool applyOnValidate = false;

        public void ApplySunAngles(float azimuthDeg, float elevationDeg)
        {
            sunAzimuthDeg = azimuthDeg;
            sunElevationDeg = elevationDeg;
            sunDirectionVectorWorld = AzimuthElevationToDirection(azimuthDeg, elevationDeg);
        }

        public void ApplyMoonAngles(float azimuthDeg, float elevationDeg, float illuminationFraction, bool visible)
        {
            moonAzimuthDeg = azimuthDeg;
            moonElevationDeg = elevationDeg;
            moonDirectionVectorWorld = AzimuthElevationToDirection(azimuthDeg, elevationDeg);
            moonIlluminationFraction = Mathf.Clamp01(illuminationFraction);
            moonVisible = visible;
        }

        public void ApplySunDirection(Vector3 direction, string source, float confidence)
        {
            if (direction.sqrMagnitude <= 0.000001f)
                return;
            sunDirectionVectorWorld = direction.normalized;
            sunDirectionSource = source ?? "calculated";
            sunDirectionConfidence = Mathf.Clamp01(confidence);
        }

        public void ApplyToDirectionalLight()
        {
            if (directionalLight == null)
                return;
            Vector3 dir = sunDirectionVectorWorld;
            if (applyAggregateDirection && aggregateDirection.sqrMagnitude > 0.000001f)
                dir = aggregateDirection;
            if (dir.sqrMagnitude <= 0.000001f)
                return;
            dir = dir.normalized;
            directionalLight.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        }

        /// <summary>
        /// Apply generalized multi-body response: light_sources, aggregate_direction, eclipses.
        /// Preserves compatibility with sun/moon fields when not provided.
        /// </summary>
        public void ApplyMultiBodyResponse(Vector3[] lightSourceDirs, Vector3 aggregateDir, string eclipsesJsonPayload)
        {
            lightSourceDirections = lightSourceDirs ?? System.Array.Empty<Vector3>();
            aggregateDirection = aggregateDir.sqrMagnitude > 0.000001f ? aggregateDir.normalized : Vector3.up;
            eclipsesJson = eclipsesJsonPayload ?? "";
        }

        public static Vector3 AzimuthElevationToDirection(float azimuthDeg, float elevationDeg)
        {
            float az = azimuthDeg * Mathf.Deg2Rad;
            float el = elevationDeg * Mathf.Deg2Rad;
            // ENU: X east, Y up, Z north.
            Vector3 dir = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(el),
                Mathf.Sin(el),
                Mathf.Cos(az) * Mathf.Cos(el)
            );
            return dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.up;
        }

        private void OnValidate()
        {
            if (!applyOnValidate)
                return;
            ApplyToDirectionalLight();
        }
    }
}
