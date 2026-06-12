using UnityEngine;

namespace Roads.Features
{
    [AddComponentMenu("Roads/Features/Surface Break")]
    public class RoadSurfaceBreakFeature : RoadFeatureBase
    {
        public AnimationCurve tearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float frictionReduction = 0.5f;
        public Vector3 tearDirection = Vector3.forward;

        public override void ApplyToSamples(RoadSplineSample[] samples)
        {
            // Mark samples near break for erosion / manifold weakening (stored via heightOffset sign)
            if (samples == null)
                return;
            for (int i = 0; i < samples.Length; i++)
            {
                float f = EvaluateFalloff(samples[i].distance);
                if (f <= 0f)
                    continue;
                samples[i].heightOffset -= frictionReduction * f * 0.01f;
            }
        }

        protected override void DrawFeatureGizmo(RoadSpline3D spline)
        {
            var s = spline.GetSampleAtDistance(distanceAlong);
            Vector3 dir = tearDirection.sqrMagnitude > 0.01f
                ? tearDirection.normalized
                : s.tangent;
            Gizmos.color = Color.red;
            float half = length * 0.5f;
            Gizmos.DrawLine(s.position - dir * half, s.position + dir * half);
            int steps = 6;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float d = distanceAlong - half + t * length;
                var p = spline.GetSampleAtDistance(d);
                float w = tearCurve.Evaluate(t) * 0.5f;
                Gizmos.DrawLine(p.position - p.binormal * w, p.position + p.binormal * w);
            }
        }
    }
}
