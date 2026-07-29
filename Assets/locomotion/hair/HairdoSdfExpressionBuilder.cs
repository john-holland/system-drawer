using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>
/// Intentionally overbuilt SDF Max hairdo composition + sexpr dump for hand editing.
/// </summary>
public static class HairdoSdfExpressionBuilder
{
    public const int RingSamples = 28;

    public struct BuildResult
    {
        public SdfMaxCompositionAsset asset;
        public string sexpr;
        public List<string> comments;
    }

    public static BuildResult Build(HairPlumeConfig config, HairdoBlend blend, HairdoParams blended)
    {
        config ??= ScriptableObject.CreateInstance<HairPlumeConfig>();
        blended ??= new HairdoParams();
        blend ??= HairdoBlend.CreateDefault();

        // Ensure curves match blended params for ring sampling
        blended.ApplyTo(config);

        float peak = Mathf.Max(0.01f, config.peakHeightM);
        float scalp = Mathf.Max(0.01f, config.scalpRadiusM);
        float tipHold = Mathf.Clamp01(config.plumeTipHold);
        float sigma = Mathf.Max(0.01f, config.gaussianSigma);
        float flux = Mathf.Max(0f, config.gaussianFluxGain);

        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.name = "HairdoObsceneSdf";
        asset.nodes = new List<SdfMaxNode>();
        var comments = new List<string>
        {
            $";; obscene hairdo sdf — tipHold={tipHold:0.##} sigma={sigma:0.##} flux={flux:0.##}",
            $";; length={config.maxStrandLengthM:0.###} scalpR={scalp:0.###}"
        };

        // Crown sphere
        int crown = AddSphere(asset, scalp * 0.55f, Vector3.up * (scalp * 0.2f));
        // Displaced plume
        int plume = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.DisplacedSphere,
            sphereRadius = scalp + peak * Mathf.Lerp(0.35f, 1f, tipHold),
            radius = scalp + peak,
            noiseFrequency = Mathf.Lerp(2.8f, 0.7f, tipHold) * Mathf.Lerp(1f, 1.4f, sigma),
            noiseOctaves = tipHold > 0.7f ? 3 : 5,
            noisePersistence = Mathf.Lerp(0.55f, 0.22f, tipHold),
            localPosition = Vector3.up * (peak * 0.35f * (1f - tipHold * 0.3f))
        });

        int root = AddSmoothMax(asset, crown, plume, 0.04f + tipHold * 0.06f);
        comments.Add(";; crown + displaced plume");

        // Dense hairline ring
        comments.Add($";; ring x{RingSamples}");
        Vector3 pate = config.centerPateLocal;
        for (int i = 0; i < RingSamples; i++)
        {
            float u = i / (float)RingSamples;
            float ang = u * Mathf.PI * 2f;
            float r01 = config.hairLineCurve != null ? config.hairLineCurve.Radius01(u) : 1f;
            float r = r01 * scalp;
            float h = config.hairLineCurve != null
                ? config.hairLineCurve.EmergenceHeight01(u) * peak * 0.15f
                : 0f;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, h + peak * 0.05f, Mathf.Sin(ang) * r);
            Vector3 axis = (pate - pos);
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.up;
            else axis.Normalize();
            int leaf = AddCapsule(
                asset,
                scalp * Mathf.Lerp(0.05f, 0.1f, r01),
                pos + axis * (peak * 0.12f),
                Quaternion.LookRotation(axis).eulerAngles);
            root = AddMax(asset, root, leaf);
        }

        // Per enabled haircut contribution branches
        var weights = blend.NormalizedEnabledWeights();
        foreach (var kv in weights)
        {
            float w = kv.Value;
            if (w < 1e-4f) continue;
            comments.Add($";; cut:{HairdoPresetCatalog.DisplayName(kv.Key)} w={w:0.##}");
            int branch = BuildCutBranch(asset, HairdoPresetCatalog.Get(kv.Key), w, peak, scalp);
            root = AddSmoothMax(asset, root, branch, 0.03f + w * 0.05f);
        }

        // Part valley subtract ribbon
        if (config.hairPartSpline != null && config.hairPartSpline.enabled &&
            blended.partMode != HairdoPartMode.None)
        {
            comments.Add(";; part valley");
            int partRibbon = BuildPartRibbon(asset, config, scalp, peak);
            root = AddSubtract(asset, root, partRibbon);
        }

        // Tip drip when hold low / flux high
        float tipDrop = peak * config.gravityTipGain * (1f - tipHold) * (0.5f + 0.5f * Mathf.Clamp01(flux));
        if (tipDrop > 0.01f)
        {
            comments.Add(";; tip drip / flux break");
            int drip = AddCapsule(
                asset,
                scalp * 0.35f,
                Vector3.down * tipDrop * 0.45f + Vector3.up * (scalp * 0.08f),
                new Vector3(90f, 0f, 0f));
            root = AddMax(asset, root, drip);
            int drip2 = AddSphere(asset, scalp * 0.22f, Vector3.down * tipDrop * 0.8f);
            root = AddSmoothMax(asset, root, drip2, 0.05f);
        }

        // Helix curl capsule chains
        float curlAmt = Mathf.Clamp01(blended.curlAmount);
        if (curlAmt > 0.02f)
        {
            comments.Add(
                $";; curls amount={curlAmt:0.##} freq={blended.curlFrequency:0.##} tight={blended.curlTightness:0.##}");
            int helix = BuildCurlHelices(asset, blended, scalp, peak);
            root = AddSmoothMax(asset, root, helix, 0.04f + curlAmt * 0.04f);
        }

        // Fringe bangs boxes near front
        if (blended.fringeHeight > 0.05f)
        {
            comments.Add(";; fringe bangs");
            float fh = blended.fringeHeight;
            for (int b = 0; b < 5; b++)
            {
                float t = (b - 2) / 2f;
                float az = 0.25f + t * 0.06f;
                float ang = az * Mathf.PI * 2f;
                float rr = scalp * Mathf.Lerp(0.7f, blended.hairlineFront, 0.5f);
                Vector3 p = new Vector3(Mathf.Cos(ang) * rr, peak * 0.08f * fh, Mathf.Sin(ang) * rr);
                int box = asset.nodes.Count;
                asset.nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.Box,
                    halfExtents = new Vector3(0.02f, peak * 0.12f * fh, 0.015f),
                    localPosition = p,
                    localRotationEuler = new Vector3(15f * fh, -t * 25f, 0f)
                });
                root = AddMax(asset, root, box);
            }
        }

        asset.rootNodeIndex = root;
        string sexpr = HairdoSdfSexpr.Format(asset, comments);
        return new BuildResult { asset = asset, sexpr = sexpr, comments = comments };
    }

    static int BuildCurlHelices(
        SdfMaxCompositionAsset asset,
        HairdoParams blended,
        float scalp,
        float peak)
    {
        float amount = Mathf.Clamp01(blended.curlAmount);
        float freq = Mathf.Clamp(blended.curlFrequency, 0.5f, 8f);
        float tight = Mathf.Clamp01(blended.curlTightness);
        float curlR = Mathf.Lerp(scalp * 0.18f, scalp * 0.06f, tight) * amount;
        float capR = Mathf.Lerp(scalp * 0.045f, scalp * 0.025f, tight) * Mathf.Lerp(0.6f, 1f, amount);
        int steps = Mathf.Clamp(Mathf.RoundToInt(6 + freq * 2f), 8, 18);
        const int chains = 3;
        int helixRoot = -1;

        for (int c = 0; c < chains; c++)
        {
            float az0 = (c + 0.5f) / chains;
            int chain = -1;
            for (int s = 0; s < steps; s++)
            {
                float length01 = (s + 0.5f) / steps;
                float phase = HairPlumeSdfComposer.CurlPhase(az0, length01, freq);
                float ang = az0 * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                Vector3 bitangent = Vector3.up;
                float baseR = scalp * Mathf.Lerp(0.85f, 1.05f, blended.hairlineCrown) * 0.9f;
                Vector3 p = radial * baseR
                            + bitangent * (length01 * peak * 0.85f)
                            + tangent * (Mathf.Sin(phase) * curlR)
                            + radial * (Mathf.Cos(phase) * curlR * 0.65f);
                int cap = AddCapsule(asset, capR, p, Quaternion.LookRotation(bitangent + tangent * 0.2f).eulerAngles);
                chain = chain < 0 ? cap : AddMax(asset, chain, cap);
            }

            helixRoot = helixRoot < 0 ? chain : AddSmoothMax(asset, helixRoot, chain, 0.03f);
        }

        return helixRoot < 0 ? AddSphere(asset, 0.01f, Vector3.zero) : helixRoot;
    }

    static int BuildCutBranch(
        SdfMaxCompositionAsset asset,
        HairdoParams cut,
        float weight,
        float peak,
        float scalp)
    {
        float wPeak = peak * Mathf.Lerp(0.4f, 1.1f, weight) * Mathf.Clamp01(cut.maxStrandLengthM / HairdoParams.CatalogMaxLengthM + 0.2f);
        int a = AddSphere(asset, scalp * (0.2f + 0.25f * weight), Vector3.up * (wPeak * 0.25f));
        int b = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.DisplacedSphere,
            sphereRadius = scalp * (0.5f + cut.hairlineCrown * 0.4f) * weight + wPeak * 0.3f,
            radius = scalp + wPeak,
            noiseFrequency = 1.2f + cut.gaussianSigma,
            noiseOctaves = 3,
            noisePersistence = 0.4f,
            localPosition = Vector3.up * (wPeak * 0.4f) + Vector3.forward * ((cut.hairlineFront - cut.hairlineBack) * 0.03f)
        });
        int branch = AddMax(asset, a, b);

        // A few azimuth capsules biased by cut hairline
        for (int i = 0; i < 6; i++)
        {
            float u = i / 6f;
            float ang = u * Mathf.PI * 2f;
            float r01 = HairlineAt(cut, u);
            float r = r01 * scalp * Mathf.Lerp(0.8f, 1.15f, weight);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, wPeak * 0.1f, Mathf.Sin(ang) * r);
            int cap = AddCapsule(asset, scalp * 0.06f * weight + 0.01f, pos, Quaternion.LookRotation(Vector3.up).eulerAngles);
            branch = AddMax(asset, branch, cap);
        }

        return branch;
    }

    static float HairlineAt(HairdoParams cut, float azimuth01)
    {
        float u = Mathf.Repeat(azimuth01, 1f);
        if (u < 0.125f || u >= 0.875f) return cut.hairlineSide;
        if (u < 0.375f) return Mathf.Lerp(cut.hairlineSide, cut.hairlineFront, (u - 0.125f) / 0.25f);
        if (u < 0.625f) return Mathf.Lerp(cut.hairlineFront, cut.hairlineSide, (u - 0.375f) / 0.25f);
        if (u < 0.875f) return Mathf.Lerp(cut.hairlineSide, cut.hairlineBack, (u - 0.625f) / 0.25f);
        return cut.hairlineBack;
    }

    static int BuildPartRibbon(SdfMaxCompositionAsset asset, HairPlumeConfig config, float scalp, float peak)
    {
        var part = config.hairPartSpline;
        part.EnsureDefaults();
        int ribbon = -1;
        const int samples = 10;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            Vector3 p = part.EvaluateLocal(t);
            p.y += peak * 0.05f;
            int cap = AddCapsule(asset, Mathf.Max(0.004f, part.partWidthM * 1.5f), p, new Vector3(0f, 0f, 90f));
            ribbon = ribbon < 0 ? cap : AddMax(asset, ribbon, cap);
        }

        return ribbon < 0 ? AddSphere(asset, 0.01f, Vector3.zero) : ribbon;
    }

    static int AddSphere(SdfMaxCompositionAsset asset, float r, Vector3 pos)
    {
        int i = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            sphereRadius = r,
            radius = r,
            localPosition = pos
        });
        return i;
    }

    static int AddCapsule(SdfMaxCompositionAsset asset, float r, Vector3 pos, Vector3 rotEuler)
    {
        int i = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Capsule,
            sphereRadius = r,
            radius = r,
            localPosition = pos,
            localRotationEuler = rotEuler
        });
        return i;
    }

    static int AddMax(SdfMaxCompositionAsset asset, int a, int b)
    {
        int i = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode { op = SdfMaxOp.Max, childIndexA = a, childIndexB = b });
        return i;
    }

    static int AddSmoothMax(SdfMaxCompositionAsset asset, int a, int b, float k)
    {
        int i = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.SmoothMax,
            smoothRadius = k,
            childIndexA = a,
            childIndexB = b
        });
        return i;
    }

    static int AddSubtract(SdfMaxCompositionAsset asset, int a, int b)
    {
        int i = asset.nodes.Count;
        asset.nodes.Add(new SdfMaxNode { op = SdfMaxOp.Subtract, childIndexA = a, childIndexB = b });
        return i;
    }
}
