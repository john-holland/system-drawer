using Locomotion.Narrative;
using UnityEngine;

/// <summary>Bridges SpatialGenerator4D slice data to QuestMapRenderer.</summary>
public class Spatial4DQuestSliceSource : QuestSpatialSliceSource
{
    public SpatialGenerator4D generator;

    void Awake()
    {
        if (generator == null)
            generator = GetComponent<SpatialGenerator4D>();
    }

    public override bool TryGetSliceAtT(
        float t,
        out Bounds bounds,
        out int resX,
        out int resY,
        out int resZ,
        out float[] occupancy,
        out float[] causal)
    {
        if (generator == null)
        {
            bounds = default;
            resX = resY = resZ = 0;
            occupancy = causal = null;
            return false;
        }
        return generator.TryGetSliceAtT(t, out bounds, out resX, out resY, out resZ, out occupancy, out causal);
    }

    public override float NarrativeTMin => generator != null ? generator.tMin : base.NarrativeTMin;
    public override float NarrativeTMax => generator != null ? generator.tMax : base.NarrativeTMax;
}
