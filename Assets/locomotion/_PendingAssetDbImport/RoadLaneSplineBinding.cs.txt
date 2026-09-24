using UnityEngine;

/// <summary>Binds a <see cref="RoadLaneLayout"/> / config to a RoadSpline3D GameObject (no Roads asmdef cycle).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Road Lane Spline Binding")]
public sealed class RoadLaneSplineBinding : MonoBehaviour
{
    public RoadLaneConfigAsset config;
    public RoadLaneLayout layout = new RoadLaneLayout();
    public RoadLaneGridSettings grid = new RoadLaneGridSettings();

    public RoadLaneLayout ResolveLayout()
    {
        if (config != null && config.layout != null)
            return config.layout;
        return layout ?? new RoadLaneLayout();
    }

    public RoadLaneGridSettings ResolveGrid()
    {
        if (config != null && config.grid != null)
            return config.grid;
        return grid ?? new RoadLaneGridSettings();
    }
}
