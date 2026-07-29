using System;
using UnityEngine;

/// <summary>
/// Hairline radius (and optional emergence height) by azimuth — base of the conical emergence ring.
/// </summary>
[Serializable]
public sealed class HairLineCurve
{
    public AnimationCurve radiusByAzimuth01 = AnimationCurve.Constant(0f, 1f, 1f);
    public AnimationCurve emergenceHeightByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f);

    public float Radius01(float azimuth01)
    {
        float u = Mathf.Repeat(azimuth01, 1f);
        return Mathf.Max(0f, radiusByAzimuth01 != null ? radiusByAzimuth01.Evaluate(u) : 1f);
    }

    public float EmergenceHeight01(float azimuth01)
    {
        float u = Mathf.Repeat(azimuth01, 1f);
        return emergenceHeightByAzimuth01 != null ? emergenceHeightByAzimuth01.Evaluate(u) : 0f;
    }

    public static HairLineCurve Constant(float radius01 = 1f)
    {
        return new HairLineCurve
        {
            radiusByAzimuth01 = AnimationCurve.Constant(0f, 1f, radius01),
            emergenceHeightByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f)
        };
    }
}

/// <summary>
/// Local tilt of strands at the hairline (degrees), keyed by azimuth.
/// </summary>
[Serializable]
public sealed class HairLineAngleCurve
{
    public AnimationCurve emergenceAngleDegByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f);

    public float AngleDeg(float azimuth01)
    {
        float u = Mathf.Repeat(azimuth01, 1f);
        return emergenceAngleDegByAzimuth01 != null ? emergenceAngleDegByAzimuth01.Evaluate(u) : 0f;
    }

    public static HairLineAngleCurve Zero()
    {
        return new HairLineAngleCurve
        {
            emergenceAngleDegByAzimuth01 = AnimationCurve.Constant(0f, 1f, 0f)
        };
    }
}

/// <summary>
/// Samples hairline ring points and pate-averaged emergence directions.
/// </summary>
public static class HairLineSampler
{
    public static float Radius(HairPlumeConfig config, float azimuth01)
    {
        float def = config != null ? config.hairLineDefaultRadius : 1f;
        if (config?.hairLineCurve == null)
            return def * (config != null ? config.scalpRadiusM : 0.11f);
        return config.hairLineCurve.Radius01(azimuth01) * config.scalpRadiusM;
    }

    public static Vector3 EmergenceRingPoint(Transform scalpRoot, HairPlumeConfig config, float azimuth01)
    {
        float ang = Mathf.Repeat(azimuth01, 1f) * Mathf.PI * 2f;
        float r = Radius(config, azimuth01);
        float h = config?.hairLineCurve != null
            ? config.hairLineCurve.EmergenceHeight01(azimuth01) * (config.peakHeightM * 0.15f)
            : 0f;
        Vector3 local = new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r);
        return scalpRoot != null ? scalpRoot.TransformPoint(local) : local;
    }

    public static Vector3 CenterPateWorld(Transform scalpRoot, HairPlumeConfig config)
    {
        Vector3 local = config != null ? config.centerPateLocal : Vector3.up * 0.05f;
        return scalpRoot != null ? scalpRoot.TransformPoint(local) : local;
    }

    public static float ConicalRadius(HairPlumeConfig config, float azimuth01, float length01)
    {
        float baseR = Radius(config, azimuth01);
        float cone = 1f;
        if (config?.conicalEmergenceCurve != null)
            cone = Mathf.Max(0f, config.conicalEmergenceCurve.Evaluate(Mathf.Clamp01(length01)));
        return baseR * cone;
    }

    /// <summary>
    /// Final strand root direction: angle-tilted base lerped toward CenterPatePoint.
    /// </summary>
    public static Vector3 EmergenceDirection(Transform scalpRoot, HairPlumeConfig config, float azimuth01)
    {
        Vector3 pate = CenterPateWorld(scalpRoot, config);
        Vector3 p = EmergenceRingPoint(scalpRoot, config, azimuth01);
        Vector3 scalpNormal = scalpRoot != null ? scalpRoot.up : Vector3.up;
        Vector3 pateOnPlane = pate - Vector3.Project(pate - p, scalpNormal);
        Vector3 radialOut = p - pateOnPlane;
        if (radialOut.sqrMagnitude < 1e-8f)
            radialOut = p - (scalpRoot != null ? scalpRoot.position : Vector3.zero);
        radialOut = radialOut.sqrMagnitude > 1e-8f ? radialOut.normalized : Vector3.forward;

        float radialBias = config != null ? config.authoredRadialBias : 0.35f;
        Vector3 baseDir = Vector3.Slerp(scalpNormal, radialOut, Mathf.Clamp01(radialBias)).normalized;

        float angleDeg = config?.hairLineAngleCurve != null
            ? config.hairLineAngleCurve.AngleDeg(azimuth01)
            : 0f;
        if (Mathf.Abs(angleDeg) > 1e-4f)
        {
            Vector3 axis = Vector3.Cross(scalpNormal, radialOut);
            if (axis.sqrMagnitude < 1e-8f)
                axis = scalpRoot != null ? scalpRoot.right : Vector3.right;
            baseDir = Quaternion.AngleAxis(angleDeg, axis.normalized) * baseDir;
        }

        Vector3 toPate = pate - p;
        if (toPate.sqrMagnitude < 1e-8f)
            toPate = scalpNormal;
        else
            toPate.Normalize();

        float blend = config != null ? Mathf.Clamp01(config.pateAngleBlend) : 0.35f;
        Vector3 dir = Vector3.Lerp(baseDir, toPate, blend).normalized;

        // Optional flux-aware aim: density falls off with length from pate → tipward flux nudges emergence
        if (config != null && config.gaussianFluxGain > 0f)
        {
            float r = Mathf.Clamp01(Vector3.Distance(p, pate) / Mathf.Max(1e-3f, config.scalpRadiusM));
            float flux01 = Mathf.Clamp01(
                HairGaussianFlux.RadialFlux(r, config.gaussianSigma) *
                config.gaussianSigma * 1.64872127f * config.gaussianFluxGain);
            Vector3 tipward = (p - pate).sqrMagnitude > 1e-8f ? (p - pate).normalized : baseDir;
            dir = Vector3.Slerp(dir, tipward, flux01 * 0.25f * (1f - blend)).normalized;
        }

        return dir;
    }
}
