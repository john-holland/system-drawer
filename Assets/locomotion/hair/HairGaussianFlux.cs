using UnityEngine;

/// <summary>
/// Gaussian plume field helpers with explicit density, flux, and integral.
/// Density ρ = exp(-r²/(2σ²)). Radial flux magnitude |F| = (r/σ²)·ρ (from |∇ρ|).
/// Integral accumulates density along a strand (erf-style quadrature), not the flux itself.
/// </summary>
public static class HairGaussianFlux
{
    /// <summary>Normalized density ρ(r) in (0,1], r typically length01 along a strand.</summary>
    public static float Density(float r, float sigma)
    {
        float s = Mathf.Max(1e-4f, sigma);
        return Mathf.Exp(-(r * r) / (2f * s * s));
    }

    /// <summary>
    /// Outward radial flux magnitude of the 1D gaussian along strand length.
    /// F = (r/σ²)·ρ — peaks off-center; zero at r=0. This is the |derivative| scale of ρ.
    /// </summary>
    public static float RadialFlux(float r, float sigma)
    {
        float s = Mathf.Max(1e-4f, sigma);
        float rho = Density(r, s);
        return (Mathf.Abs(r) / (s * s)) * rho;
    }

    /// <summary>
    /// Signed radial flux: positive flows toward tip (increasing r), negative toward root.
    /// For a crown-centered plume we take tipward as +r.
    /// </summary>
    public static float SignedRadialFlux(float r, float sigma)
    {
        float s = Mathf.Max(1e-4f, sigma);
        float rho = Density(r, s);
        return (r / (s * s)) * rho;
    }

    /// <summary>
    /// Approximate ∫₀^{r} ρ(s) ds / ∫₀^{∞} ρ via erf (normalized cumulative mass along strand).
    /// Use for hold / load — integrated density, not flux.
    /// </summary>
    public static float CumulativeMass01(float r, float sigma)
    {
        float s = Mathf.Max(1e-4f, sigma);
        // ∫₀^r exp(-s²/(2σ²)) ds = σ√(π/2) · erf(r/(σ√2))
        float arg = r / (s * 1.41421356f);
        float erf = ApproxErf(arg);
        // Normalize by half-line mass σ√(π/2) → cumulative in [0,1) as r→∞
        return Mathf.Clamp01(erf);
    }

    /// <summary>
    /// Lateral flux weight from a part valley: |∇carve| · bisect — pushes density off the part line.
    /// </summary>
    public static float PartLateralFluxWeight(float signedDist, float partWidth, float bisectStrength)
    {
        float w = Mathf.Max(1e-4f, partWidth);
        float dist = Mathf.Abs(signedDist);
        float carve = Mathf.Exp(-(dist * dist) / (2f * w * w));
        // Derivative of carve w.r.t. dist: (dist/w²)·carve — lateral flux away from part
        float lateralFlux = (dist / (w * w)) * carve;
        float peak = 1f / (w * Mathf.Max(1e-3f, w)); // scale so mid-shoulder ~ O(1)
        float flux01 = Mathf.Clamp01(lateralFlux / Mathf.Max(1e-3f, peak));
        return Mathf.Clamp01(bisectStrength * flux01);
    }

    /// <summary>
    /// Tip-break energy from radial flux (spread) vs cumulative mass (hold).
    /// tipHold 0 → pure flux break; 1 → integral hold, flux suppressed.
    /// </summary>
    public static float TipBreakFromFlux(float length01, float sigma, float tipHold, float fluxGain)
    {
        float flux = RadialFlux(length01, sigma);
        // Normalize flux roughly: max of (r/σ²)ρ occurs near r=σ → ~1/(σ·e^{0.5})
        float s = Mathf.Max(1e-4f, sigma);
        float norm = s * 1.64872127f; // σ·√e
        float flux01 = Mathf.Clamp01(flux * norm * Mathf.Max(0f, fluxGain));
        float mass01 = CumulativeMass01(length01, sigma);
        float hold = Mathf.Clamp01(tipHold);
        return Mathf.Lerp(flux01, mass01 * (1f - hold) * 0.25f, hold);
    }

    /// <summary>
    /// Height sample: density shaped by tipHold, with flux modulating break spread.
    /// </summary>
    public static float Height01(float length01, float sigma, float tipHold, float fluxGain)
    {
        float rho = Density(length01, sigma);
        float flux = RadialFlux(length01, sigma);
        float s = Mathf.Max(1e-4f, sigma);
        float flux01 = Mathf.Clamp01(flux * s * 1.64872127f * Mathf.Max(0f, fluxGain));
        float hold = Mathf.Clamp01(tipHold);

        // Break: density thinned by flux (derivative / outflow along strand)
        float breakSpread = rho * (1f - length01 * 0.85f) * (1f - 0.35f * flux01);
        // Hold: density weighted by remaining mass capacity (1 - cumulative)
        float remain = 1f - CumulativeMass01(length01, sigma) * (1f - hold);
        float held = rho * Mathf.Lerp(1f - length01 * 0.15f, remain, hold);
        return Mathf.Clamp01(Mathf.Lerp(breakSpread, held, hold));
    }

    static float ApproxErf(float x)
    {
        // Abramowitz & Stegun 7.1.26
        float sign = x < 0f ? -1f : 1f;
        x = Mathf.Abs(x);
        const float a1 = 0.254829592f;
        const float a2 = -0.284496736f;
        const float a3 = 1.421413741f;
        const float a4 = -1.453152027f;
        const float a5 = 1.061405429f;
        const float p = 0.3275911f;
        float t = 1f / (1f + p * x);
        float y = 1f - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Mathf.Exp(-x * x);
        return sign * y;
    }
}
