using UnityEngine;
using Weather;

/// <summary>Eave / gutter / awning runoff cache into Weather.Water. Overflow uses HousingBuildingRagdoll.overflowLayers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Eave Water Cache")]
public sealed class HouseEaveWaterCache : MonoBehaviour
{
    public HousingBuildingRagdoll house;
    public Water water;
    public float catchmentM2 = 40f;
    public float gutterFlowM3s;
    public float downspoutFlowM3s;
    public float lastRainM;

    public void Prebake(float rainMeters)
    {
        lastRainM = Mathf.Max(0f, rainMeters);
        float volume = catchmentM2 * lastRainM;
        gutterFlowM3s = volume * 0.1f;
        downspoutFlowM3s = gutterFlowM3s * 0.85f;
        if (water != null)
            water.volume += volume;
        if (house != null && house.overflowLayers != null && gutterFlowM3s > 2f)
        {
            for (int i = 0; i < house.overflowLayers.Count; i++)
            {
                var layer = house.overflowLayers[i];
                if (layer != null && layer.kind == DestructibleLayerKind.Roof && !layer.IsIntact())
                    downspoutFlowM3s *= 0.5f;
            }
        }
    }
}
