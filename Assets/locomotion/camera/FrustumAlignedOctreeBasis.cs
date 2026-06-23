using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Camera
{
    /// <summary>
    /// Rotates octree leaf centers into camera-local basis and builds a fixed topology vector for LSTM input.
    /// </summary>
    public static class FrustumAlignedOctreeBasis
    {
        public const int TopologyDim = 64;

        public static Quaternion CameraBasisRotation(Vector3 forward, Vector3 up)
        {
            if (forward.sqrMagnitude < 1e-6f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, up.sqrMagnitude > 1e-6f ? up : Vector3.up);
        }

        public static Vector3 ToCameraLocal(Vector3 worldPoint, Vector3 camPos, Quaternion basisRotation)
        {
            return Quaternion.Inverse(basisRotation) * (worldPoint - camPos);
        }

        public static bool IsInsideFrustumAabb(UnityEngine.Camera cam, Vector3 worldPoint, float margin = 1.15f)
        {
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(worldPoint);
            if (vp.z <= cam.nearClipPlane) return false;
            float m = margin;
            return vp.x >= -m && vp.x <= 1f + m && vp.y >= -m && vp.y <= 1f + m;
        }

        public static List<Vector3> CollectVisibleLeafCenters(
            UnityEngine.Camera cam,
            IReadOnlyList<HierarchicalPathingOctTree.Leaf> leaves)
        {
            var outList = new List<Vector3>();
            if (cam == null || leaves == null) return outList;
            foreach (var leaf in leaves)
            {
                if (leaf == null || leaf.blocked) continue;
                if (IsInsideFrustumAabb(cam, leaf.Center))
                    outList.Add(leaf.Center);
            }
            return outList;
        }

        public static float[] BuildTopologyVector(UnityEngine.Camera cam, IReadOnlyList<HierarchicalPathingOctTree.Leaf> leaves)
        {
            var vec = new float[TopologyDim];
            if (cam == null || leaves == null) return vec;

            Quaternion basis = CameraBasisRotation(cam.transform.forward, cam.transform.up);
            Vector3 camPos = cam.transform.position;
            var localPoints = new List<Vector3>();

            foreach (var leaf in leaves)
            {
                if (leaf == null || leaf.blocked) continue;
                if (!IsInsideFrustumAabb(cam, leaf.Center)) continue;
                localPoints.Add(ToCameraLocal(leaf.Center, camPos, basis));
            }

            if (localPoints.Count == 0) return vec;

            localPoints.Sort((a, b) => a.z.CompareTo(b.z));
            int bins = TopologyDim / 2;
            float zMin = localPoints[0].z;
            float zMax = localPoints[localPoints.Count - 1].z;
            float zRange = Mathf.Max(0.01f, zMax - zMin);

            for (int i = 0; i < localPoints.Count; i++)
            {
                var p = localPoints[i];
                int bin = Mathf.Clamp(Mathf.FloorToInt((p.z - zMin) / zRange * (bins - 1)), 0, bins - 1);
                vec[bin] += 1f;
                int radialBin = bins + Mathf.Clamp(Mathf.FloorToInt(Mathf.Sqrt(p.x * p.x + p.y * p.y) * 2f), 0, bins - 1);
                vec[radialBin] += 0.5f;
            }

            float max = 0f;
            for (int i = 0; i < vec.Length; i++)
                max = Mathf.Max(max, vec[i]);
            if (max > 1e-5f)
            {
                for (int i = 0; i < vec.Length; i++)
                    vec[i] /= max;
            }

            return vec;
        }
    }
}
