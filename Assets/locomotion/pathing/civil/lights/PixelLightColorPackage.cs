using UnityEngine;

[CreateAssetMenu(fileName = "PixelLightColorPackage", menuName = "Locomotion/Civil/Pixel Light Color Package")]
public sealed class PixelLightColorPackage : ScriptableObject
{
    public Color onColor = new Color(1f, 0.15f, 0.1f, 1f);
    public Color offColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    public Color emissionColor = new Color(1f, 0.2f, 0.05f, 1f);
    [Range(0f, 8f)] public float emissionIntensity = 2.5f;

    public static PixelLightColorPackage CreateEmergencyRed()
    {
        var p = CreateInstance<PixelLightColorPackage>();
        p.onColor = new Color(1f, 0.12f, 0.08f);
        p.emissionColor = new Color(1f, 0.2f, 0.05f);
        return p;
    }

    public static PixelLightColorPackage CreateSignal(Color on)
    {
        var p = CreateInstance<PixelLightColorPackage>();
        p.onColor = on;
        p.emissionColor = on * 1.2f;
        p.emissionIntensity = 3f;
        return p;
    }
}
