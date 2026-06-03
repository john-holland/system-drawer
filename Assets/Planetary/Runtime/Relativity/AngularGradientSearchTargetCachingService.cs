using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public sealed class AngularGradientSearchTargetCachingService
    {
        readonly Dictionary<long, Vector3> _binGradients = new Dictionary<long, Vector3>();

        public void SetBinGradient(long binKey, Vector3 gradientTowardLanding)
        {
            _binGradients[binKey] = gradientTowardLanding;
        }

        public float SortKey(long binKey, Vector3 queryDirection)
        {
            if (!_binGradients.TryGetValue(binKey, out Vector3 g) || g.sqrMagnitude < 1e-6f)
                return 0f;
            return Vector3.Dot(queryDirection.normalized, g.normalized);
        }

        public static long BinKeyFromPosition(Vector3 world, float binSize)
        {
            binSize = Mathf.Max(1f, binSize);
            int x = Mathf.FloorToInt(world.x / binSize);
            int y = Mathf.FloorToInt(world.y / binSize);
            int z = Mathf.FloorToInt(world.z / binSize);
            return ((long)x * 73856093L) ^ ((long)y * 19349663L) ^ ((long)z * 83492791L);
        }
    }
}
