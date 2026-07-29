using System;
using UnityEngine;

/// <summary>
/// Parametric hairdo controls that rewrite HairPlumeConfig curves (no raw AnimationCurve UI).
/// Azimuth: 0=+X (side), 0.25=+Z (front), 0.5=-X (side), 0.75=-Z (back).
/// </summary>
[Serializable]
public sealed class HairdoParams
{
    public const float CatalogMaxLengthM = 0.55f;

    public float maxStrandLengthM = 0.22f;
    public float peakHeightM = 0.18f;
    public float gaussianSigma = 0.45f;
    public float plumeTipHold = 0.55f;
    public float gaussianFluxGain = 1f;

    [Range(0f, 1.5f)] public float hairlineFront = 1f;
    [Range(0f, 1.5f)] public float hairlineSide = 1f;
    [Range(0f, 1.5f)] public float hairlineBack = 1f;
    [Range(0f, 1.5f)] public float hairlineCrown = 1f;

    [Range(0f, 1f)] public float fringeHeight = 0f;
    public float sideTiltDeg = 0f;
    [Range(0.5f, 2f)] public float flare = 1.15f;

    public HairdoPartMode partMode = HairdoPartMode.None;
    [Min(0.001f)] public float partWidthM = 0.01f;
    [Range(0f, 1f)] public float partStrength = 1f;

    [Range(0f, 1f)] public float pateAngleBlend = 0.35f;
    [Range(0f, 1f)] public float authoredRadialBias = 0.35f;

    [Range(0f, 1f)] public float curlAmount = 0f;
    [Range(0.5f, 8f)] public float curlFrequency = 3f;
    [Range(0f, 1f)] public float curlTightness = 0.5f;

    public float DiamondFront01 =>
        Mathf.Clamp01((hairlineFront + fringeHeight * 0.5f) / 1.5f);

    public float DiamondSide01 => Mathf.Clamp01(hairlineSide / 1.5f);

    public float DiamondBack01 => Mathf.Clamp01(hairlineBack / 1.5f);

    public float DiamondLength01 =>
        Mathf.Clamp01(maxStrandLengthM / Mathf.Max(1e-3f, CatalogMaxLengthM));

    public static HairdoParams Lerp(HairdoParams a, HairdoParams b, float t)
    {
        a ??= new HairdoParams();
        b ??= new HairdoParams();
        t = Mathf.Clamp01(t);
        return new HairdoParams
        {
            maxStrandLengthM = Mathf.Lerp(a.maxStrandLengthM, b.maxStrandLengthM, t),
            peakHeightM = Mathf.Lerp(a.peakHeightM, b.peakHeightM, t),
            gaussianSigma = Mathf.Lerp(a.gaussianSigma, b.gaussianSigma, t),
            plumeTipHold = Mathf.Lerp(a.plumeTipHold, b.plumeTipHold, t),
            gaussianFluxGain = Mathf.Lerp(a.gaussianFluxGain, b.gaussianFluxGain, t),
            hairlineFront = Mathf.Lerp(a.hairlineFront, b.hairlineFront, t),
            hairlineSide = Mathf.Lerp(a.hairlineSide, b.hairlineSide, t),
            hairlineBack = Mathf.Lerp(a.hairlineBack, b.hairlineBack, t),
            hairlineCrown = Mathf.Lerp(a.hairlineCrown, b.hairlineCrown, t),
            fringeHeight = Mathf.Lerp(a.fringeHeight, b.fringeHeight, t),
            sideTiltDeg = Mathf.Lerp(a.sideTiltDeg, b.sideTiltDeg, t),
            flare = Mathf.Lerp(a.flare, b.flare, t),
            partMode = t < 0.5f ? a.partMode : b.partMode,
            partWidthM = Mathf.Lerp(a.partWidthM, b.partWidthM, t),
            partStrength = Mathf.Lerp(a.partStrength, b.partStrength, t),
            pateAngleBlend = Mathf.Lerp(a.pateAngleBlend, b.pateAngleBlend, t),
            authoredRadialBias = Mathf.Lerp(a.authoredRadialBias, b.authoredRadialBias, t),
            curlAmount = Mathf.Lerp(a.curlAmount, b.curlAmount, t),
            curlFrequency = Mathf.Lerp(a.curlFrequency, b.curlFrequency, t),
            curlTightness = Mathf.Lerp(a.curlTightness, b.curlTightness, t)
        };
    }

    /// <summary>Weighted average of continuous fields; discrete part from <paramref name="partSource"/>.</summary>
    public static HairdoParams WeightedAverage(HairdoParams[] sources, float[] weights, HairdoParams partSource)
    {
        var r = new HairdoParams();
        if (sources == null || weights == null || sources.Length == 0)
            return partSource ?? r;

        float wSum = 0f;
        for (int i = 0; i < weights.Length && i < sources.Length; i++)
            wSum += Mathf.Max(0f, weights[i]);
        if (wSum < 1e-6f)
            return partSource ?? (sources[0] ?? r);

        float Acc(Func<HairdoParams, float> get)
        {
            float s = 0f;
            for (int i = 0; i < sources.Length && i < weights.Length; i++)
            {
                if (sources[i] == null) continue;
                s += get(sources[i]) * Mathf.Max(0f, weights[i]);
            }
            return s / wSum;
        }

        r.maxStrandLengthM = Acc(p => p.maxStrandLengthM);
        r.peakHeightM = Acc(p => p.peakHeightM);
        r.gaussianSigma = Acc(p => p.gaussianSigma);
        r.plumeTipHold = Acc(p => p.plumeTipHold);
        r.gaussianFluxGain = Acc(p => p.gaussianFluxGain);
        r.hairlineFront = Acc(p => p.hairlineFront);
        r.hairlineSide = Acc(p => p.hairlineSide);
        r.hairlineBack = Acc(p => p.hairlineBack);
        r.hairlineCrown = Acc(p => p.hairlineCrown);
        r.fringeHeight = Acc(p => p.fringeHeight);
        r.sideTiltDeg = Acc(p => p.sideTiltDeg);
        r.flare = Acc(p => p.flare);
        r.partWidthM = Acc(p => p.partWidthM);
        r.partStrength = Acc(p => p.partStrength);
        r.pateAngleBlend = Acc(p => p.pateAngleBlend);
        r.authoredRadialBias = Acc(p => p.authoredRadialBias);
        r.curlAmount = Acc(p => p.curlAmount);
        r.curlFrequency = Acc(p => p.curlFrequency);
        r.curlTightness = Acc(p => p.curlTightness);

        if (partSource != null)
        {
            r.partMode = partSource.partMode;
            r.partWidthM = partSource.partWidthM;
            r.partStrength = partSource.partStrength;
        }

        return r;
    }

    public void ApplyTo(HairPlumeConfig config)
    {
        if (config == null) return;
        config.ApplyLatticeBakeDefaults();

        config.maxStrandLengthM = Mathf.Max(0.05f, maxStrandLengthM);
        config.peakHeightM = Mathf.Max(0.01f, peakHeightM);
        config.gaussianSigma = Mathf.Max(0.01f, gaussianSigma);
        config.plumeTipHold = Mathf.Clamp01(plumeTipHold);
        config.gaussianFluxGain = Mathf.Max(0f, gaussianFluxGain);
        config.pateAngleBlend = Mathf.Clamp01(pateAngleBlend);
        config.authoredRadialBias = Mathf.Clamp01(authoredRadialBias);
        config.curlAmount = Mathf.Clamp01(curlAmount);
        config.curlFrequency = Mathf.Clamp(curlFrequency, 0.5f, 8f);
        config.curlTightness = Mathf.Clamp01(curlTightness);

        float side = hairlineSide;
        float crownLift = Mathf.Lerp(1f, hairlineCrown, 0.35f);
        config.hairLineCurve ??= HairLineCurve.Constant(1f);
        config.hairLineCurve.radiusByAzimuth01 = new AnimationCurve(
            new Keyframe(0f, side * crownLift),
            new Keyframe(0.25f, hairlineFront * crownLift),
            new Keyframe(0.5f, side * crownLift),
            new Keyframe(0.75f, hairlineBack * crownLift),
            new Keyframe(1f, side * crownLift));

        config.hairLineCurve.emergenceHeightByAzimuth01 = new AnimationCurve(
            new Keyframe(0f, fringeHeight * 0.25f),
            new Keyframe(0.2f, fringeHeight),
            new Keyframe(0.3f, fringeHeight),
            new Keyframe(0.5f, fringeHeight * 0.2f),
            new Keyframe(1f, fringeHeight * 0.25f));

        config.hairLineAngleCurve ??= HairLineAngleCurve.Zero();
        float tilt = sideTiltDeg;
        config.hairLineAngleCurve.emergenceAngleDegByAzimuth01 = new AnimationCurve(
            new Keyframe(0f, tilt),
            new Keyframe(0.25f, tilt * 0.35f),
            new Keyframe(0.5f, -tilt),
            new Keyframe(0.75f, 0f),
            new Keyframe(1f, tilt));

        config.conicalEmergenceCurve = AnimationCurve.Linear(0f, 1f, 1f, Mathf.Max(0.5f, flare));

        config.hairPartSpline ??= new HairPartSpline();
        ApplyPart(config.hairPartSpline);
    }

    public void ReadFrom(HairPlumeConfig config)
    {
        if (config == null) return;
        maxStrandLengthM = config.maxStrandLengthM;
        peakHeightM = config.peakHeightM;
        gaussianSigma = config.gaussianSigma;
        plumeTipHold = config.plumeTipHold;
        gaussianFluxGain = config.gaussianFluxGain;
        pateAngleBlend = config.pateAngleBlend;
        authoredRadialBias = config.authoredRadialBias;
        curlAmount = config.curlAmount;
        curlFrequency = config.curlFrequency;
        curlTightness = config.curlTightness;

        var hl = config.hairLineCurve;
        if (hl?.radiusByAzimuth01 != null)
        {
            hairlineSide = hl.Radius01(0f);
            hairlineFront = hl.Radius01(0.25f);
            hairlineBack = hl.Radius01(0.75f);
            hairlineCrown = Mathf.Clamp((hairlineFront + hairlineSide + hairlineBack) / 3f, 0f, 1.5f);
        }

        if (hl?.emergenceHeightByAzimuth01 != null)
            fringeHeight = Mathf.Clamp01(hl.EmergenceHeight01(0.25f));

        if (config.hairLineAngleCurve != null)
            sideTiltDeg = config.hairLineAngleCurve.AngleDeg(0f);

        if (config.conicalEmergenceCurve != null && config.conicalEmergenceCurve.length > 0)
            flare = config.conicalEmergenceCurve.Evaluate(1f);

        var part = config.hairPartSpline;
        if (part == null || !part.enabled)
        {
            partMode = HairdoPartMode.None;
            return;
        }

        partWidthM = part.partWidthM;
        partStrength = part.bisectStrength;
        part.EnsureDefaults();
        float avgX = 0f;
        int n = part.localControlPoints.Count;
        for (int i = 0; i < n; i++)
            avgX += part.localControlPoints[i].x;
        avgX /= Mathf.Max(1, n);
        if (Mathf.Abs(avgX) < 0.008f)
            partMode = HairdoPartMode.Center;
        else if (avgX < 0f)
            partMode = HairdoPartMode.Left;
        else
            partMode = HairdoPartMode.Right;
    }

    void ApplyPart(HairPartSpline part)
    {
        if (partMode == HairdoPartMode.None)
        {
            part.enabled = false;
            return;
        }

        part.enabled = true;
        part.partWidthM = Mathf.Max(0.001f, partWidthM);
        part.bisectStrength = Mathf.Clamp01(partStrength);
        float x = partMode switch
        {
            HairdoPartMode.Left => -0.025f,
            HairdoPartMode.Right => 0.025f,
            _ => 0f
        };
        part.localControlPoints = new System.Collections.Generic.List<Vector3>
        {
            new Vector3(x, 0.02f, 0.09f),
            new Vector3(x, 0.05f, 0.03f),
            new Vector3(x * 0.5f, 0.06f, -0.02f)
        };
    }
}

public enum HairdoPartMode
{
    None = 0,
    Center = 1,
    Left = 2,
    Right = 3
}
