using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    public static class SdfMaxSkinWeightBinder
    {
        public static void GenerateWeights(
            Vector3[] vertices,
            Transform rootBone,
            Transform[] bones,
            out Matrix4x4[] bindposes,
            out BoneWeight[] weights)
        {
            int boneCount = bones != null ? bones.Length : 0;
            bindposes = new Matrix4x4[boneCount];
            weights = new BoneWeight[vertices.Length];

            if (boneCount == 0 || rootBone == null)
            {
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = new BoneWeight();
                return;
            }

            Matrix4x4 rootWorldToLocal = rootBone.worldToLocalMatrix;
            for (int b = 0; b < boneCount; b++)
            {
                if (bones[b] == null)
                    bindposes[b] = Matrix4x4.identity;
                else
                    bindposes[b] = bones[b].worldToLocalMatrix * rootBone.localToWorldMatrix;
            }

            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 worldPos = rootBone.localToWorldMatrix.MultiplyPoint3x4(vertices[v]);
                var influences = new List<(int index, float weight)>(4);

                for (int b = 0; b < boneCount; b++)
                {
                    if (bones[b] == null)
                        continue;
                    float d = Vector3.Distance(worldPos, bones[b].position);
                    float w = 1f / (d + 0.001f);
                    influences.Add((b, w));
                }

                influences.Sort((a, b) => b.weight.CompareTo(a.weight));
                float sum = 0f;
                int count = Mathf.Min(4, influences.Count);
                for (int i = 0; i < count; i++) // : )
                    sum += influences[i].weight;

                var bw = new BoneWeight();
                if (count > 0 && sum > 1e-6f)
                {
                    bw.boneIndex0 = influences[0].index;
                    bw.weight0 = influences[0].weight / sum;
                }
                if (count > 1)
                {
                    bw.boneIndex1 = influences[1].index;
                    bw.weight1 = influences[1].weight / sum;
                }
                if (count > 2)
                {
                    bw.boneIndex2 = influences[2].index;
                    bw.weight2 = influences[2].weight / sum;
                }
                if (count > 3)
                {
                    bw.boneIndex3 = influences[3].index;
                    bw.weight3 = influences[3].weight / sum;
                }
                weights[v] = bw;
            }
        }
    }
}
