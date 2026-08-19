using UnityEngine;

/// <summary>Front walk / patio / grass / railing helpers for house yards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Yard Features")]
public sealed class HouseYardFeatures : MonoBehaviour
{
    public PlanarSplinePathLocomotion frontWalk;
    public StairwellTopologyAsset frontSteps;
    public RoadLot patioLot;
    public LotGrassGrowthController grass;
    public PixelLightGridMountGameObject railingLights;
    public int floorIndex = 1;

    public void BindRailingLights(HouseConstructionFloorParams floor)
    {
        if (railingLights == null)
            railingLights = GetComponent<PixelLightGridMountGameObject>()
                            ?? gameObject.AddComponent<PixelLightGridMountGameObject>();
        if (floor == null) return;
        railingLights.gridWidth = floor.pixelLightGridW;
        railingLights.gridHeight = floor.pixelLightGridH;
        railingLights.cellSize = floor.pixelLightCellSize;
    }
}
