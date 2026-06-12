using UnityEngine;

namespace Roads.Features
{
    /// <summary>Capsule-shaped frost heave that stamps height displacement on road samples.</summary>
    [AddComponentMenu("Roads/Features/Frost Heave")]
    public class RoadFrostHeaveFeature : RoadFeatureBase
    {
        public float capsuleRadius = 1.5f;
        public float capsuleHeight = 0.3f;

        public override void ApplyToSamples(RoadSplineSample[] samples)
        {
            if (samples == null)
                return;
            for (int i = 0; i < samples.Length; i++)
            {
                float f = EvaluateFalloff(samples[i].distance);
                if (f <= 0f)
                    continue;
                float lift = capsuleHeight * f;
                if (lift > tolerance)
                    lift = tolerance + (lift - tolerance) * 0.25f;
                samples[i].position += samples[i].normal * lift;
                samples[i].heightOffset += lift;
            }
        }

        protected override void DrawFeatureGizmo(RoadSpline3D spline)
        {
            var s = spline.GetSampleAtDistance(distanceAlong);
            Gizmos.color = gizmoColor;
            Vector3 up = s.normal * capsuleHeight;
            DrawWireCapsule(s.position, s.position + up, capsuleRadius);
        }

        static void DrawWireCapsule(Vector3 bottom, Vector3 top, float radius)
        {
            Gizmos.DrawLine(bottom, top);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
        }
    }
}
