using UnityEngine;

namespace SdfMax
{
    /// <summary>Maps world sample points to bind space for skinned SDF evaluation.</summary>
    public sealed class SdfMaxBoneFieldContext
    {
        public Transform RootBone;
        public Transform[] Bones;
        public Matrix4x4[] Bindposes;
        public BoneWeight[] BindWeights;
        public Vector3[] BindVertices;
        public Matrix4x4 BindRootLocalToWorld = Matrix4x4.identity;

        public bool IsValid =>
            RootBone != null && Bones != null && Bones.Length > 0 &&
            Bindposes != null && Bindposes.Length == Bones.Length;

        public Vector3 WorldToBind(Vector3 worldPos)
        {
            if (RootBone == null)
                return worldPos;

            if (BindWeights == null || BindWeights.Length == 0 ||
                BindVertices == null || BindVertices.Length == 0)
            {
                return RootBone.worldToLocalMatrix.MultiplyPoint3x4(worldPos);
            }

            int nearest = FindNearestBindVertex(worldPos);
            return InverseSkinPoint(worldPos, BindWeights[nearest]);
        }

        Vector3 InverseSkinPoint(Vector3 worldPos, BoneWeight bw)
        {
            Vector3 acc = Vector3.zero;
            float total = 0f;
            Accumulate(bw.boneIndex0, bw.weight0);
            Accumulate(bw.boneIndex1, bw.weight1);
            Accumulate(bw.boneIndex2, bw.weight2);
            Accumulate(bw.boneIndex3, bw.weight3);

            if (total > 1e-6f)
                return acc / total;

            return RootBone.worldToLocalMatrix.MultiplyPoint3x4(worldPos);

            void Accumulate(int boneIndex, float weight)
            {
                if (weight < 1e-6f || boneIndex < 0 || boneIndex >= Bones.Length)
                    return;
                Transform bone = Bones[boneIndex];
                if (bone == null || boneIndex >= Bindposes.Length)
                    return;
                Matrix4x4 inv = Bindposes[boneIndex].inverse * bone.worldToLocalMatrix;
                acc += weight * inv.MultiplyPoint3x4(worldPos);
                total += weight;
            }
        }

        int FindNearestBindVertex(Vector3 worldPos)
        {
            int best = 0;
            float bestDist = float.MaxValue;
            Matrix4x4 rootLtw = RootBone.localToWorldMatrix;
            for (int i = 0; i < BindVertices.Length; i++)
            {
                Vector3 approxWorld = rootLtw.MultiplyPoint3x4(BindVertices[i]);
                float d = (approxWorld - worldPos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }
    }
}
