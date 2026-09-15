using UnityEngine;

[CreateAssetMenu(fileName = "Lathe", menuName = "Locomotion/Civil/Lathe")]
public sealed class LatheSpec : ScriptableObject
{
    public GearboxSpec headstockGearbox;
    public GearboxSpec quickChangeGearbox;
    public float carriageM;
    public float bedwaysLengthM = 1.8f;
    public float saddleM;
    public float crossSlideM;
    public float compoundRestDeg;
    public float toolPostM;
    public float apronLemma01 = 0.5f;
    public bool halfNutEngaged;
    public float leadScrewPitchMm = 3f;
    public float headstockRpm;
    public float tailstockM;
    public float knurl01;
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public Bounds4SdfInclusionPixelLightMount frameMount;
    public Bounds4SdfInclusionPixelLightMount shellMount;
    public PixelLightGridMountGameObject millKerfMount;
    public int millCutCount = 3;

    public float CarriageClamp01()
    {
        if (bedwaysLengthM <= 1e-4f) return 0f;
        return Mathf.Clamp01(carriageM / bedwaysLengthM);
    }

    public float LeadAdvanceM(float turns)
    {
        return turns * (leadScrewPitchMm * 0.001f);
    }
}

/// <summary>Odd kerf count with a center line; centroid + dual-crescent lifter defaults.</summary>
public static class MillKerfCuts
{
    public static int OddCutCount(int requested)
    {
        int n = Mathf.Max(1, requested);
        return (n & 1) == 0 ? n + 1 : n;
    }

    public static int CenterLineIndex(int oddCount)
    {
        int n = OddCutCount(oddCount);
        return n / 2;
    }

    /// <summary>n through-kerfs on a log yield n+1 planks (center line is one of the kerfs).</summary>
    public static int PlankCount(int oddKerfCount) => OddCutCount(oddKerfCount) + 1;

    public static Vector2 CentroidCell(int gridWidth, int gridHeight)
    {
        return new Vector2((gridWidth - 1) * 0.5f, (gridHeight - 1) * 0.5f);
    }

    public static void DualCrescentOffsets(float radius, out Vector2 left, out Vector2 right)
    {
        float r = Mathf.Max(0.01f, radius);
        left = new Vector2(-r * 0.55f, 0f);
        right = new Vector2(r * 0.55f, 0f);
    }
}
