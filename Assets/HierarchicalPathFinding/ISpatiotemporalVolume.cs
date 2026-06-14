using System.Collections.Generic;
using UnityEngine;

/// <summary>Curved or axis-aligned spatiotemporal volume membership (world + narrative time).</summary>
public interface ISpatiotemporalVolume
{
    bool Contains(Vector3 world, float narrativeTime);
    Bounds ApproximateBounds();
    void ExportSamples(List<Vector3> surfacePoints, float narrativeTime);
}
