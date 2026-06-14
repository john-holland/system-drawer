using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>Axis-aligned <see cref="Bounds4"/> adapter for <see cref="ISpatiotemporalVolume"/>.</summary>
    public sealed class Bounds4AxisAlignedVolume : ISpatiotemporalVolume
    {
        public Bounds4 bounds4;

        public Bounds4AxisAlignedVolume() { }

        public Bounds4AxisAlignedVolume(Bounds4 b) => bounds4 = b;

        public bool Contains(Vector3 world, float narrativeTime) => bounds4.Contains(world, narrativeTime);

        public Bounds ApproximateBounds() => bounds4.ToBounds();

        public void ExportSamples(List<Vector3> surfacePoints, float narrativeTime)
        {
            if (surfacePoints == null || !bounds4.Contains(bounds4.center, narrativeTime))
                return;
            Bounds b = bounds4.ToBounds();
            Vector3 c = b.center;
            Vector3 e = b.extents;
            surfacePoints.Add(c + new Vector3(e.x, e.y, e.z));
            surfacePoints.Add(c + new Vector3(-e.x, e.y, e.z));
            surfacePoints.Add(c + new Vector3(e.x, -e.y, e.z));
            surfacePoints.Add(c + new Vector3(e.x, e.y, -e.z));
        }
    }
}
