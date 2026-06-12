using UnityEngine;

namespace Roads.Features
{
    /// <summary>Base child feature under a road root; modifies bake samples along arc-length.</summary>
    public abstract class RoadFeatureBase : MonoBehaviour
    {
        [Header("Placement")]
        public float distanceAlong;
        public float length = 2f;
        public float intensity = 1f;
        public float tolerance = 0.5f;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("References")]
        public RoadSpline3D roadSpline;

        [Header("Gizmos")]
        public bool showGizmos = true;
        public Color gizmoColor = new Color(0.9f, 0.5f, 0.1f, 0.8f);

        protected RoadSpline3D ResolveSpline()
        {
            if (roadSpline != null)
                return roadSpline;
            roadSpline = GetComponentInParent<RoadSpline3D>();
            return roadSpline;
        }

        public float EvaluateFalloff(float distance)
        {
            float half = Mathf.Max(0.01f, length * 0.5f);
            float center = distanceAlong;
            if (distance < center - half || distance > center + half)
                return 0f;
            float t = (distance - (center - half)) / (half * 2f);
            return falloffCurve.Evaluate(t) * intensity;
        }

        public abstract void ApplyToSamples(RoadSplineSample[] samples);

        protected void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;
            var spline = ResolveSpline();
            if (spline == null || spline.controlPoints.Count < 2)
                return;
            DrawFeatureGizmo(spline);
        }

        protected abstract void DrawFeatureGizmo(RoadSpline3D spline);
    }
}
