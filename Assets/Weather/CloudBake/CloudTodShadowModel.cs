using UnityEngine;

namespace Weather.CloudBake
{
    public sealed class CloudTodShadowModel
    {
        public Vector3 SunDirection { get; private set; } = Vector3.down;
        public Color SunColor { get; private set; } = Color.white;
        public Color SkyTopTint { get; private set; } = new Color(0.53f, 0.81f, 0.98f);
        public Color CloudLitTint { get; private set; } = Color.white;
        public Color CloudShadowTint { get; private set; } = new Color(0.35f, 0.38f, 0.45f);

        public void RefreshFromScene()
        {
            var lighting = Object.FindAnyObjectByType<LightingContextComponent>();
            if (lighting != null)
            {
                SunDirection = lighting.sunDirectionVectorWorld.normalized;
                float elev = lighting.sunElevationDeg;
                SunColor = Color.Lerp(new Color(1f, 0.6f, 0.3f), Color.white, Mathf.Clamp01(elev / 45f));
                SkyTopTint = Color.Lerp(new Color(0.4f, 0.45f, 0.55f), new Color(0.53f, 0.81f, 0.98f), Mathf.Clamp01(elev / 30f));
                CloudLitTint = SunColor;
                CloudShadowTint = Color.Lerp(new Color(0.2f, 0.22f, 0.28f), new Color(0.45f, 0.48f, 0.52f), Mathf.Clamp01(elev / 20f));
                return;
            }

            var gi = Object.FindAnyObjectByType<GlobalIlluminationController>();
            if (gi != null && gi.directionalLight != null)
            {
                SunDirection = -gi.directionalLight.transform.forward;
                SunColor = gi.sunColor;
                CloudLitTint = gi.sunColor;
            }
        }

        public Color ExpectedBandColor(int bandIndex)
        {
            switch (bandIndex)
            {
                case 0: return SkyTopTint;
                case 1: return CloudLitTint;
                default: return CloudShadowTint;
            }
        }

        public float EvaluateShadowLoss(CloudHalfShellStack stack, CloudPerspectiveTarget target)
        {
            if (stack == null || target == null)
                return 0f;

            float loss = 0f;
            int count = 0;
            foreach (var sphere in stack.spheres)
            {
                float selfShadow = SelfShadowFactor(sphere, stack, SunDirection);
                Color expected = Color.Lerp(CloudLitTint, CloudShadowTint, selfShadow);
                Color band = target.gradientBands.mid;
                loss += ColorDeltaLab(expected, band);
                count++;
            }
            return count > 0 ? loss / count : 0f;
        }

        static float SelfShadowFactor(CloudSpherePrimitive sphere, CloudHalfShellStack stack, Vector3 sunDir)
        {
            sunDir = sunDir.normalized;
            int occluders = 0;
            foreach (var other in stack.spheres)
            {
                if (other == sphere)
                    continue;
                Vector3 toOther = other.center - sphere.center;
                if (Vector3.Dot(toOther.normalized, sunDir) > 0.85f && toOther.magnitude < other.radius + sphere.radius)
                    occluders++;
            }
            return Mathf.Clamp01(occluders * 0.35f + (1f - Mathf.Clamp01(Vector3.Dot(Vector3.up, sunDir))) * 0.2f);
        }

        public static float ColorDeltaLab(Color a, Color b)
        {
            ColorToLab(a, out float l1, out float aa1, out float bb1);
            ColorToLab(b, out float l2, out float aa2, out float bb2);
            float dl = l1 - l2;
            float da = aa1 - aa2;
            float db = bb1 - bb2;
            return dl * dl + da * da + db * db;
        }

        static void ColorToLab(Color c, out float l, out float a, out float b)
        {
            float r = c.r, g = c.g, bl = c.b;
            l = 0.2126f * r + 0.7152f * g + 0.0722f * bl;
            a = (r - g) * 0.5f;
            b = (g - bl) * 0.5f;
        }
    }
}
