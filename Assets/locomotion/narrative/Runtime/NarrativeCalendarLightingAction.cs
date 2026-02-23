using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Applies computed lighting context (sun/weather/validity) to a scene LightingContextComponent.
    /// Uses reflection so narrative runtime stays loosely coupled to Weather assembly.
    /// </summary>
    [Serializable]
    public class NarrativeCalendarLightingAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for a GameObject with LightingContextComponent.")]
        public string lightingContextKey = "lightingContext";

        [Tooltip("Allow fallback to FindAnyObjectByType when key lookup fails.")]
        public bool fallbackFindAny = true;

        [Tooltip("If true, fail when lighting validity is below minimum.")]
        public bool requireValidity;

        [Range(0f, 1f)]
        public float minValidityScore = 0.5f;

        [Tooltip("Prefer inferred sun direction when provided.")]
        public bool preferInferredDirection = true;

        [Tooltip("Apply the resolved direction immediately to directional light.")]
        public bool applyDirectionalLight = true;

        [Header("Sun Inputs")]
        public float sunAzimuthDeg;
        public float sunElevationDeg;
        public bool sunVisible = true;
        public float sunDirectionConfidence = 0.5f;
        public string sunDirectionSource = "calculated";

        [Header("Moon Inputs")]
        public float moonAzimuthDeg;
        public float moonElevationDeg;
        public float moonDirectionConfidence = 0.5f;
        public string moonDirectionSource = "calculated";
        public float moonIlluminationFraction;
        public bool moonVisible;

        [Header("Inferred Sun Direction (2C)")]
        public Vector3 inferredSunDirectionVector;
        public float inferredSunDirectionConfidence;

        [Header("Multi-Body (optional)")]
        public Vector3[] lightSourceDirections = System.Array.Empty<Vector3>();
        public Vector3 aggregateDirection = Vector3.zero;
        public bool applyAggregateDirection = false;
        public string eclipsesJson = "";

        [Header("Validity and Weather")]
        public float lightingValidityScore = 0.5f;
        public string lightingValidationFlags = "";
        public string weatherProvider = "unknown";
        public float cloudCoverPct;
        public float visibilityM;
        public float precipitationMm;
        public float windSpeedMps;

        [Header("Date Time (UTC)")]
        public int year = 2026;
        public int month = 1;
        public int day = 1;
        public int hour = 12;
        public int minute;
        public int second;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (requireValidity && lightingValidityScore < minValidityScore)
            {
                Debug.LogWarning($"[NarrativeCalendarLightingAction] validity gate failed ({lightingValidityScore:0.000} < {minValidityScore:0.000}).");
                return BehaviorTreeStatus.Failure;
            }

            var comp = ResolveLightingComponent(ctx);
            if (comp == null)
            {
                Debug.LogWarning("[NarrativeCalendarLightingAction] Could not resolve LightingContextComponent.");
                return BehaviorTreeStatus.Failure;
            }

            SetMember(comp, "sunAzimuthDeg", sunAzimuthDeg);
            SetMember(comp, "sunElevationDeg", sunElevationDeg);
            SetMember(comp, "sunVisible", sunVisible);
            SetMember(comp, "sunDirectionSource", sunDirectionSource ?? "calculated");
            SetMember(comp, "sunDirectionConfidence", Mathf.Clamp01(sunDirectionConfidence));
            SetMember(comp, "moonAzimuthDeg", moonAzimuthDeg);
            SetMember(comp, "moonElevationDeg", moonElevationDeg);
            SetMember(comp, "moonDirectionSource", moonDirectionSource ?? "calculated");
            SetMember(comp, "moonDirectionConfidence", Mathf.Clamp01(moonDirectionConfidence));
            SetMember(comp, "moonIlluminationFraction", Mathf.Clamp01(moonIlluminationFraction));
            SetMember(comp, "moonVisible", moonVisible);
            SetMember(comp, "inferredSunDirectionVector", inferredSunDirectionVector);
            SetMember(comp, "inferredSunDirectionConfidence", Mathf.Clamp01(inferredSunDirectionConfidence));
            SetMember(comp, "lightingValidityScore", Mathf.Clamp01(lightingValidityScore));
            SetMember(comp, "lightingValidationFlags", lightingValidationFlags ?? "");

            SetMember(comp, "weatherProvider", weatherProvider ?? "unknown");
            SetMember(comp, "cloudCoverPct", cloudCoverPct);
            SetMember(comp, "visibilityM", visibilityM);
            SetMember(comp, "precipitationMm", precipitationMm);
            SetMember(comp, "windSpeedMps", windSpeedMps);

            SetMember(comp, "year", year);
            SetMember(comp, "month", month);
            SetMember(comp, "day", day);
            SetMember(comp, "hour", hour);
            SetMember(comp, "minute", minute);
            SetMember(comp, "second", second);

            if (lightSourceDirections != null && lightSourceDirections.Length > 0)
                SetMember(comp, "lightSourceDirections", lightSourceDirections);
            if (aggregateDirection.sqrMagnitude > 0.000001f)
                SetMember(comp, "aggregateDirection", aggregateDirection.normalized);
            SetMember(comp, "applyAggregateDirection", applyAggregateDirection);
            SetMember(comp, "eclipsesJson", eclipsesJson ?? "");

            Vector3 chosenDirection = applyAggregateDirection && aggregateDirection.sqrMagnitude > 0.000001f
                ? aggregateDirection.normalized
                : NarrativeDirectionFromAzEl(sunAzimuthDeg, sunElevationDeg);
            if (preferInferredDirection && inferredSunDirectionVector.sqrMagnitude > 0.000001f)
                chosenDirection = inferredSunDirectionVector.normalized;
            SetMember(comp, "sunDirectionVectorWorld", chosenDirection);
            SetMember(comp, "moonDirectionVectorWorld", NarrativeDirectionFromAzEl(moonAzimuthDeg, moonElevationDeg));

            if (applyDirectionalLight)
                TryInvoke(comp, "ApplyToDirectionalLight");

            return BehaviorTreeStatus.Success;
        }

        private UnityEngine.Object ResolveLightingComponent(NarrativeExecutionContext ctx)
        {
            var t = LightingContextType();
            if (t == null)
                return null;
            if (ctx != null && !string.IsNullOrWhiteSpace(lightingContextKey))
            {
                if (ctx.TryResolveGameObject(lightingContextKey, out var targetGo) && targetGo != null)
                {
                    var comp = targetGo.GetComponent(t);
                    if (comp != null)
                        return comp;
                }
            }
            if (!fallbackFindAny)
                return null;
            return UnityEngine.Object.FindAnyObjectByType(t);
        }

        private static Type LightingContextType()
        {
            var t = Type.GetType("Weather.LightingContextComponent, Weather.Runtime");
            if (t != null) return t;
            t = Type.GetType("Weather.LightingContextComponent, Assembly-CSharp");
            return t;
        }

        private static void SetMember(object obj, string memberName, object value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(memberName))
                return;
            var type = obj.GetType();
            var prop = type.GetProperty(memberName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
                return;
            }
            var field = type.GetField(memberName);
            if (field != null)
                field.SetValue(obj, value);
        }

        private static void TryInvoke(object obj, string methodName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(methodName))
                return;
            var method = obj.GetType().GetMethod(methodName, Type.EmptyTypes);
            if (method != null)
                method.Invoke(obj, null);
        }

        private static Vector3 NarrativeDirectionFromAzEl(float azimuthDeg, float elevationDeg)
        {
            float az = azimuthDeg * Mathf.Deg2Rad;
            float el = elevationDeg * Mathf.Deg2Rad;
            var dir = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(el),
                Mathf.Sin(el),
                Mathf.Cos(az) * Mathf.Cos(el)
            );
            return dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.up;
        }

    }
}
