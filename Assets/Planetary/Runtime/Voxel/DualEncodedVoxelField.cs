using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Voxel
{
    public sealed class DualEncodedVoxelField
    {
        public int Resolution;
        public int InsideCount;
        readonly int[] _dualIndices;
        readonly bool[] _inside;

        public DualEncodedVoxelField(int resolution)
        {
            Resolution = Mathf.Max(4, resolution);
            int n = Resolution * Resolution * Resolution;
            _dualIndices = new int[n];
            _inside = new bool[n];
            for (int i = 0; i < n; i++)
                _dualIndices[i] = -1;
        }

        public int Index(int x, int y, int z) => x + Resolution * (y + Resolution * z);

        public void SetInside(int x, int y, int z, bool inside)
        {
            int i = Index(x, y, z);
            if (_inside[i] == inside)
                return;
            if (inside)
                InsideCount++;
            else
                InsideCount--;
            _inside[i] = inside;
            _dualIndices[i] = inside ? i : -1;
        }

        public bool IsInside(int x, int y, int z) => _inside[Index(x, y, z)];

        public int GetDualIndex(int x, int y, int z) => _dualIndices[Index(x, y, z)];
    }
}
