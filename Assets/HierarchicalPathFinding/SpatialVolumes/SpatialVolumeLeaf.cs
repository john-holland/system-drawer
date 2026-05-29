using System.Collections.Generic;
using UnityEngine;

namespace SpatialVolumes
{
    public sealed class SpatialVolumeLeaf
    {
        public Bounds Bounds;
        public readonly List<int> TriangleIndices = new List<int>();
        public int SourceLeafIndex = -1;
    }
}
