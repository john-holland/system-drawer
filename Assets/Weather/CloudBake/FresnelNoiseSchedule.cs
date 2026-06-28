using UnityEngine;

namespace Weather.CloudBake
{
    public static class FresnelNoiseSchedule
    {
        public static float Sigma(int iteration, int maxIterations, float sigmaMax, float gamma)
        {
            if (maxIterations <= 1)
                return 0f;
            float t = iteration / (float)(maxIterations - 1);
            return sigmaMax * Mathf.Pow(1f - t, gamma);
        }

        public static float FresnelFactor(Vector3 viewDir, Vector3 normal)
        {
            viewDir = viewDir.normalized;
            normal = normal.normalized;
            float ndv = Mathf.Clamp01(Vector3.Dot(normal, viewDir));
            return Mathf.Pow(1f - ndv, 2f);
        }

        public static float Noise3D(Vector3 pos, float scale, int seed = 0)
        {
            pos *= scale;
            float n = Mathf.Sin(pos.x * 12.9898f + pos.y * 78.233f + pos.z * 37.719f + seed) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        public static void PerturbSphere(
            ref CloudSpherePrimitive sphere,
            Vector3 viewDir,
            int iteration,
            int maxIterations,
            float sigmaMax,
            float gamma,
            float noiseScale)
        {
            float sigma = Sigma(iteration, maxIterations, sigmaMax, gamma);
            if (sigma <= 0.0001f)
                return;

            Vector3 normal = (sphere.center - viewDir).normalized;
            float fresnel = FresnelFactor(viewDir, normal);
            float noise = Noise3D(sphere.center, noiseScale, iteration) * 2f - 1f;
            float delta = fresnel * noise * sigma;

            sphere.radius = Mathf.Max(1f, sphere.radius * (1f + delta * 0.25f));
            sphere.density = Mathf.Clamp(sphere.density + delta * 0.1f, 0.05f, 2f);
            sphere.moisture = Mathf.Clamp01(sphere.moisture + delta * 0.05f);
        }

        public static float KalmanBlendWeight(int iteration, int maxIterations, float sigmaMax, float gamma)
        {
            float sigma = Sigma(iteration, maxIterations, sigmaMax, gamma);
            return 1f / (1f + sigma);
        }
    }
}
