using UnityEngine;

/// <summary>Helical metamaterial wrap that traps iron / fiber flecks from the recoup axis.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Jacobs Ladder Gunk Freeer")]
public sealed class JacobsLadderGunkFreeer : MonoBehaviour
{
    public RecoupWheelAlternator wheel;
    public UtilityBioRhythm utilityBio;
    [Range(0f, 1f)] public float gunk01;
    public float trapRate = 0.02f;

    public void Tick(float dt)
    {
        if (wheel != null && wheel.spinning)
            gunk01 = Mathf.Clamp01(gunk01 + trapRate * dt);
        if (utilityBio != null)
            utilityBio.gunk01 = gunk01;
    }

    public void Flush()
    {
        gunk01 = 0f;
        if (utilityBio != null)
            utilityBio.gunk01 = 0f;
    }

    public SdfMax.SdfMaxCompositionAsset ComposeHelixWrap(string assetName = "JacobsLadderSdf")
    {
        return GlyphSdfMaxComposer.ComposeLegendSubtract(
            new Bounds(Vector3.zero, new Vector3(0.04f, 0.2f, 0.04f)),
            new Vector3(0.06f, 0.22f, 0.06f),
            assetName);
    }
}
