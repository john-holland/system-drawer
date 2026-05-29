using System.Collections.Generic;
using UnityEngine;

namespace SpatialVolumes
{
    public interface ISpatialVolumeQuery
    {
        bool TrySample(Vector3 worldPos, float t, out float fieldValue, out bool inside);
        bool SearchLeaves(Bounds worldBounds, float t, List<SpatialVolumeLeaf> results);
        Bounds GetWorldBounds();
    }
}
