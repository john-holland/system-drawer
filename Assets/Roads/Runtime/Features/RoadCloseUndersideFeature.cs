using UnityEngine;

namespace Roads.Features
{
    [AddComponentMenu("Roads/Features/Close Underside")]
    public class RoadCloseUndersideFeature : RoadFeatureBase
    {
        public SplinePathMeshSampler undersideSampler;
        public AnimationCurve skirtCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float skirtDrop = 2f;

        public override void ApplyToSamples(RoadSplineSample[] samples)
        {
            if (undersideSampler == null)
                undersideSampler = GetComponentInParent<SplinePathMeshSampler>();
            if (undersideSampler != null)
                undersideSampler.closeUndersideWithLoop = true;
        }

        protected override void DrawFeatureGizmo(RoadSpline3D spline)
        {
            float half = length * 0.5f;
            Gizmos.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);
            int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float d = distanceAlong - half + t * length;
                var s = spline.GetSampleAtDistance(d);
                float drop = skirtDrop * skirtCurve.Evaluate(t);
                Vector3 left = s.position - s.binormal * s.width * 0.5f - s.normal * drop;
                Vector3 right = s.position + s.binormal * s.width * 0.5f - s.normal * drop;
                Gizmos.DrawLine(left, right);
            }
        }
    }
}
