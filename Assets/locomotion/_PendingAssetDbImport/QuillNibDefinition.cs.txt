using UnityEngine;

/// <summary>
/// Quill/pen nib: angle-limited Gaussian spread plus optional max bend against the page.
/// </summary>
[CreateAssetMenu(fileName = "QuillNibDefinition", menuName = "Locomotion/Painting/Quill Nib")]
public sealed class QuillNibDefinition : ScriptableObject
{
    [Tooltip("Procedural default: 10 degrees of nib flex against the page.")]
    [Range(0f, 45f)] public float maxBendDeg = 10f;
    [Range(0.05f, 2f)] public float gaussianSigma = 0.35f;
    [Range(1f, 40f)] public float maxSpreadAngleDeg = 18f;
    [Min(0.0001f)] public float apertureRadiusM = 0.0008f;
    [Min(0.001f)] public float nibLengthM = 0.018f;
    [Range(0f, 1f)] public float tipHold = 0.92f;

    public static QuillNibDefinition CreateDefaults()
    {
        var n = CreateInstance<QuillNibDefinition>();
        n.name = "QuillNibDefaults";
        n.maxBendDeg = 10f;
        n.gaussianSigma = 0.35f;
        n.maxSpreadAngleDeg = 18f;
        n.apertureRadiusM = 0.0008f;
        n.nibLengthM = 0.018f;
        n.tipHold = 0.92f;
        return n;
    }

    /// <summary>Spread weight in [0,1] for contact angle vs nib axis, clamped to maxSpreadAngleDeg.</summary>
    public float GaussianSpread01(float contactAngleDeg)
    {
        float limited = Mathf.Min(Mathf.Abs(contactAngleDeg), maxSpreadAngleDeg);
        float x = limited / Mathf.Max(1e-4f, maxSpreadAngleDeg);
        float s = Mathf.Max(1e-4f, gaussianSigma);
        return Mathf.Exp(-0.5f * (x * x) / (s * s));
    }

    public float ClampBendDeg(float requestedBendDeg) =>
        Mathf.Clamp(requestedBendDeg, -maxBendDeg, maxBendDeg);

    public float Stress01(float bendDeg, float contactForceN, float breakForceN)
    {
        float bend = Mathf.Abs(bendDeg) / Mathf.Max(1e-4f, maxBendDeg);
        float force = contactForceN / Mathf.Max(1e-4f, breakForceN);
        return Mathf.Max(bend, force);
    }
}
