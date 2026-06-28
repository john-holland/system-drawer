using UnityEngine;

namespace Weather.CloudBake
{
    public static class CloudHalfShellConvexionUtility
    {
        public static void Apply(
            CloudHalfShellStack stack,
            CloudViewerSpec viewer,
            CloudHalfShellConvexion convexion)
        {
            if (stack == null || stack.spheres.Count == 0 || convexion == null || convexion.IsNeutral)
                return;

            stack.ComputeBounds();
            Vector3 centroid = stack.shellBounds.center;
            stack.shellCentroid = centroid;
            stack.appliedConvexion = new CloudHalfShellConvexion { bias = convexion.bias, size = convexion.size };

            Vector3 viewOrigin = viewer != null ? viewer.ResolveOrigin() : Vector3.zero;
            Vector3 viewDir = viewer != null ? viewer.ResolveForward().normalized : Vector3.forward;
            if (viewDir.sqrMagnitude < 0.0001f)
                viewDir = Vector3.forward;

            float maxDepth = convexion.size * (stack.cloudTopM - stack.cloudBaseM) * 0.5f;
            float horizontalRadius = convexion.size * Mathf.Max(
                stack.shellBounds.extents.x,
                stack.shellBounds.extents.z,
                1f);
            int spheresPerColumn = Mathf.Max(1, stack.spheresPerColumn);

            for (int i = 0; i < stack.spheres.Count; i++)
            {
                var sphere = stack.spheres[i];
                Vector3 offset = EvaluateOffset(
                    sphere.center,
                    sphere.stackIndex,
                    viewOrigin,
                    viewDir,
                    convexion,
                    maxDepth,
                    horizontalRadius,
                    spheresPerColumn);
                sphere.center += offset;
                float domeWeight = DomeWeight(sphere.center, viewOrigin, viewDir, horizontalRadius, sphere.stackIndex, spheresPerColumn);
                sphere.radius *= 1f + 0.15f * domeWeight * convexion.bias;
                stack.spheres[i] = sphere;
            }

            stack.ComputeBounds();
        }

        public static Vector3 EvaluateOffset(
            Vector3 sphereCenter,
            int stackIndex,
            Vector3 viewOrigin,
            Vector3 viewDir,
            CloudHalfShellConvexion convexion,
            float maxDepth,
            float horizontalRadius,
            int spheresPerColumn)
        {
            if (convexion == null || convexion.IsNeutral)
                return Vector3.zero;

            float domeWeight = DomeWeight(sphereCenter, viewOrigin, viewDir, horizontalRadius, stackIndex, spheresPerColumn);
            return viewDir * (convexion.bias * maxDepth * domeWeight);
        }

        public static float DomeWeight(
            Vector3 sphereCenter,
            Vector3 viewOrigin,
            Vector3 viewDir,
            float horizontalRadius,
            int stackIndex,
            int spheresPerColumn)
        {
            float horizontalDist = HorizontalDistanceFromViewAxis(sphereCenter, viewOrigin, viewDir);
            float radial = horizontalRadius > 0.0001f ? horizontalDist / horizontalRadius : 0f;
            float radialFalloff = Mathf.Clamp01(1f - radial * radial);
            float layerWeight = (stackIndex + 1f) / Mathf.Max(1, spheresPerColumn);
            return Mathf.Clamp01(radialFalloff * layerWeight);
        }

        static float HorizontalDistanceFromViewAxis(Vector3 sphereCenter, Vector3 viewOrigin, Vector3 viewDir)
        {
            Vector3 toSphere = sphereCenter - viewOrigin;
            float along = Vector3.Dot(toSphere, viewDir);
            Vector3 onAxis = viewOrigin + viewDir * along;
            Vector3 flat = sphereCenter - onAxis;
            flat.y = 0f;
            return flat.magnitude;
        }

#if UNITY_EDITOR
        public static void DrawGizmo(
            Vector3 centroid,
            Vector3 viewDir,
            CloudHalfShellConvexion convexion,
            float cloudBaseM,
            float cloudTopM,
            float horizontalRadius)
        {
            if (convexion == null || convexion.size <= 0.0001f)
                return;

            float maxDepth = convexion.size * (cloudTopM - cloudBaseM) * 0.5f;
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.6f);
            Vector3 forwardPt = centroid + viewDir * (convexion.bias * maxDepth);
            Vector3 backPt = centroid - viewDir * maxDepth;
            Gizmos.DrawLine(centroid, forwardPt);
            Gizmos.DrawWireSphere(forwardPt, horizontalRadius * 0.1f);
            Gizmos.DrawWireSphere(centroid, horizontalRadius);
            Gizmos.DrawLine(centroid, backPt);
        }
#endif
    }
}
