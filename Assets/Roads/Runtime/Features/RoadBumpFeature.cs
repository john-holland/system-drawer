using UnityEngine;

namespace Roads.Features
{
    [AddComponentMenu("Roads/Features/Road Bump")]
    public class RoadBumpFeature : RoadFeatureBase
    {
        public AnimationCurve bumpCurve = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);
        public float maxDisplacement = 0.15f;

        public override void ApplyToSamples(RoadSplineSample[] samples)
        {
            if (samples == null)
                return;
            float half = length * 0.5f;
            for (int i = 0; i < samples.Length; i++)
            {
                float d = samples[i].distance;
                if (d < distanceAlong - half || d > distanceAlong + half)
                    continue;
                float t = (d - (distanceAlong - half)) / Mathf.Max(0.01f, length);
                float disp = bumpCurve.Evaluate(t) * maxDisplacement * intensity;
                samples[i].position += samples[i].normal * disp;
            }
        }

        protected override void DrawFeatureGizmo(RoadSpline3D spline)
        {
            int steps = 8;
            float half = length * 0.5f;
            Gizmos.color = gizmoColor;
            Vector3 prev = spline.GetSampleAtDistance(distanceAlong - half).position;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float d = distanceAlong - half + t * length;
                var s = spline.GetSampleAtDistance(d);
                float disp = bumpCurve.Evaluate(t) * maxDisplacement * intensity;
                Vector3 p = s.position + s.normal * disp;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
