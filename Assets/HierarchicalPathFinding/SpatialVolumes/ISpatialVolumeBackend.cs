using System.Collections.Generic;
using UnityEngine;

namespace SpatialVolumes
{
    public interface ISpatialVolumeBackend
    {
        int BuildVersion { get; }
        Bounds WorldBounds { get; }
        bool EnsureBuilt(SpatialVolumeBuildContext ctx);
        void Invalidate();
        void CollectLeaves(Bounds query, float t, List<SpatialVolumeLeaf> outLeaves);
        float Sample(Vector3 worldPos, float t);
        bool IsInside(Vector3 worldPos, float t);
        void ExportVolumeBounds(List<SpatialVolumeBounds4> outVolumes, float tMin, float tMax, Matrix4x4 localToWorld);
    }
}
