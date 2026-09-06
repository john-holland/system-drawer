using UnityEngine;

/// <summary>Sectional garage / carpentry door: rails, lock stiles, mullions, moulding.</summary>
[CreateAssetMenu(fileName = "DoorAssembly", menuName = "Locomotion/Civil/Door Assembly")]
public sealed class DoorAssemblySpec : ScriptableObject
{
    [Header("Opening")]
    public Vector2 openingSize = new Vector2(2.4f, 2.1f);
    public int sectionCount = 4;
    public float railThickness = 0.045f;
    public float stileWidth = 0.09f;
    public float mullionWidth = 0.06f;
    public float panelThickness = 0.028f;

    [Header("Pieces")]
    public bool topRail = true;
    public bool bottomRail = true;
    public bool lockStiles = true;
    public bool middleRail = true;
    public bool friezeRail;
    public bool mullion = true;
    public bool moulding = true;
    public int mouldingSides = 4;

    [Header("PixelLight")]
    public bool autoFitPixelLightGrid = true;
    public int pixelLightGridW = 9;
    public int pixelLightGridH = 9;
    public float pixelLightCellSize = 0.08f;

    [Header("Lemma / pack")]
    [Tooltip("e.g. pack=3d,placement=uniform,pad=0.04,sides=4")]
    public string lemmaPackFragment = "pack=3d,placement=uniform,pad=0.04,sides=4";

    public Vector2Int FittedGridSize
    {
        get
        {
            if (!autoFitPixelLightGrid)
                return new Vector2Int(Mathf.Max(1, pixelLightGridW), Mathf.Max(1, pixelLightGridH));
            int cols = Mathf.Max(3, sectionCount * 2 + 1);
            int rows = Mathf.Max(5, (middleRail ? 1 : 0) + (friezeRail ? 1 : 0) + 5);
            return new Vector2Int(cols, rows);
        }
    }

    public void ApplyAutoFit()
    {
        if (!autoFitPixelLightGrid) return;
        var s = FittedGridSize;
        pixelLightGridW = s.x;
        pixelLightGridH = s.y;
    }

    public int MouldingSideCount => Mathf.Max(3, mouldingSides);
}
