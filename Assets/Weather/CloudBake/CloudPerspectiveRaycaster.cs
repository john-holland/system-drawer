using System.Collections.Generic;
using UnityEngine;

namespace Weather.CloudBake
{
    public static class CloudPerspectiveRaycaster
    {
        public static List<CloudColumnSample> SampleColumns(
            CloudViewerSpec viewer,
            CloudPerspectiveTarget target,
            float cloudBaseM,
            float cloudTopM,
            int maxRays = 4096)
        {
            var samples = new List<CloudColumnSample>();
            if (target == null || viewer == null)
                return samples;

            int w = target.rayWidth;
            int h = target.rayHeight;
            if (target.referenceTexture != null)
            {
                w = target.referenceTexture.width;
                h = target.referenceTexture.height;
            }
            if (w <= 0 || h <= 0)
            {
                w = 64;
                h = 64;
            }

            int stride = Mathf.Max(1, target.sampleStride);
            Vector3 origin = viewer.ResolveOrigin();
            Vector3 forward = viewer.ResolveForward();
            Vector3 up = viewer.ResolveUp();
            Vector3 right = Vector3.Cross(up, forward).normalized;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;
            up = Vector3.Cross(forward, right).normalized;

            int rayIndex = 0;
            for (int v = 0; v < h && rayIndex < maxRays; v += stride)
            {
                for (int u = 0; u < w && rayIndex < maxRays; u += stride)
                {
                    Color refColor = SampleReferenceColor(target, u, v, w, h);
                    float opacity = 1f - (refColor.r + refColor.g + refColor.b) / 3f;
                    if (opacity < 0.05f)
                        continue;

                    Vector3 dir = BuildRayDirection(viewer, u, v, w, h, forward, right, up);
                    Vector3 hit = MarchCloudLayer(origin, dir, cloudBaseM, cloudTopM, out float depth);

                    samples.Add(new CloudColumnSample
                    {
                        rayIndex = rayIndex++,
                        columnIndex = rayIndex - 1,
                        u = u,
                        v = v,
                        worldHit = hit,
                        columnDepth = depth,
                        referenceColor = refColor,
                        targetGradientBand = BandForV(v, h),
                        targetOpacity = opacity
                    });
                }
            }

            return samples;
        }

        static Vector3 BuildRayDirection(
            CloudViewerSpec viewer, int u, int v, int w, int h,
            Vector3 forward, Vector3 right, Vector3 up)
        {
            if (viewer.kind == CloudViewerKind.Camera && viewer.camera != null)
            {
                var cam = viewer.camera;
                float nx = (u + 0.5f) / w;
                float ny = (v + 0.5f) / h;
                var ray = cam.ViewportPointToRay(new Vector3(nx, ny, 0f));
                return ray.direction.normalized;
            }

            if (viewer.kind == CloudViewerKind.Bounds)
            {
                float fx = (u + 0.5f) / w - 0.5f;
                float fy = (v + 0.5f) / h - 0.5f;
                Vector3 local = new Vector3(
                    fx * viewer.bounds.size.x,
                    fy * viewer.bounds.size.y,
                    viewer.bounds.extents.z);
                return (viewer.bounds.center + local - viewer.ResolveOrigin()).normalized;
            }

            float tu = (u + 0.5f) / w * 2f - 1f;
            float tv = (v + 0.5f) / h * 2f - 1f;
            return (forward + right * tu * 0.5f + up * tv * 0.5f).normalized;
        }

        static Vector3 MarchCloudLayer(Vector3 origin, Vector3 dir, float cloudBaseM, float cloudTopM, out float depth)
        {
            depth = 0f;
            float tEnter = float.MaxValue;
            float tExit = float.MinValue;

            SolvePlaneIntersection(origin, dir, cloudBaseM, ref tEnter, ref tExit);
            SolvePlaneIntersection(origin, dir, cloudTopM, ref tEnter, ref tExit);

            if (tEnter == float.MaxValue)
            {
                float dist = 500f;
                depth = cloudTopM - cloudBaseM;
                return origin + dir * dist;
            }

            float tMid = (tEnter + tExit) * 0.5f;
            if (tMid < 0f)
                tMid = Mathf.Max(tEnter, 10f);
            depth = Mathf.Max(1f, tExit - tEnter);
            return origin + dir * tMid;
        }

        static void SolvePlaneIntersection(Vector3 origin, Vector3 dir, float altitudeY, ref float tEnter, ref float tExit)
        {
            if (Mathf.Abs(dir.y) < 0.0001f)
                return;
            float t = (altitudeY - origin.y) / dir.y;
            if (t < tEnter) tEnter = t;
            if (t > tExit) tExit = t;
        }

        static int BandForV(int v, int h)
        {
            if (v < h / 3) return 0;
            if (v < 2 * h / 3) return 1;
            return 2;
        }

        static Color SampleReferenceColor(CloudPerspectiveTarget target, int u, int v, int w, int h)
        {
            if (target.perRayColors != null && target.perRayColors.Length > 0)
            {
                int idx = Mathf.Clamp(v * w + u, 0, target.perRayColors.Length - 1);
                return target.perRayColors[idx];
            }
            if (target.referenceTexture != null)
            {
                int x = Mathf.Clamp(u, 0, target.referenceTexture.width - 1);
                int y = Mathf.Clamp(v, 0, target.referenceTexture.height - 1);
                return target.referenceTexture.GetPixel(x, y);
            }
            return target.gradientBands.mid;
        }
    }
}
