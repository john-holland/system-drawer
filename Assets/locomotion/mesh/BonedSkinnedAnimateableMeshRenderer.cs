using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skinned mesh with named loop bounds, bone weights, and assignment source
/// (mediapipe | mocapanything | custom). Authored by Image to Model.
/// </summary>
[AddComponentMenu("Locomotion/Mesh/Boned Skinned Animateable Mesh Renderer")]
[DisallowMultipleComponent]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public sealed class BonedSkinnedAnimateableMeshRenderer : MonoBehaviour
{
    public SkinnedMeshRenderer skinned;
    public string artworkId;
    public string assignmentSource = "custom";
    public GranularitySettings granularity = new GranularitySettings();
    public List<string> loopBoundNames = new List<string>();
    public List<string> boneNames = new List<string>();

    [Serializable]
    public sealed class BoneVertexList
    {
        public string boneName;
        public int[] vertexIndices;
    }

    public List<BoneVertexList> verticesPerBone = new List<BoneVertexList>();

    public SkinnedMeshRenderer Renderer =>
        skinned != null ? skinned : GetComponent<SkinnedMeshRenderer>();

    public void CaptureFromRenderer(string source)
    {
        var smr = Renderer;
        if (smr == null)
            return;
        skinned = smr;
        assignmentSource = string.IsNullOrEmpty(source) ? "custom" : source;
        boneNames.Clear();
        verticesPerBone.Clear();
        loopBoundNames.Clear();
        var bones = smr.bones;
        var mesh = smr.sharedMesh;
        if (bones == null || mesh == null)
            return;
        var weights = mesh.boneWeights;
        var perBone = new List<int>[bones.Length];
        for (int b = 0; b < bones.Length; b++)
            perBone[b] = new List<int>();
        for (int v = 0; v < weights.Length; v++)
        {
            AddWeight(perBone, weights[v].boneIndex0, weights[v].weight0, v);
            AddWeight(perBone, weights[v].boneIndex1, weights[v].weight1, v);
            AddWeight(perBone, weights[v].boneIndex2, weights[v].weight2, v);
            AddWeight(perBone, weights[v].boneIndex3, weights[v].weight3, v);
        }
        for (int b = 0; b < bones.Length; b++)
        {
            string name = bones[b] != null ? bones[b].name : ("bone_" + b);
            boneNames.Add(name);
            verticesPerBone.Add(new BoneVertexList
            {
                boneName = name,
                vertexIndices = perBone[b].ToArray()
            });
        }
        var boxes = GetComponentsInChildren<SkinnedMeshLoopSplitBounds>(true);
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null && !string.IsNullOrEmpty(boxes[i].loopName))
                loopBoundNames.Add(boxes[i].loopName);
        }
    }

    static void AddWeight(List<int>[] perBone, int index, float w, int vert)
    {
        if (w <= 0.01f || index < 0 || index >= perBone.Length)
            return;
        var list = perBone[index];
        if (list.Count == 0 || list[list.Count - 1] != vert)
            list.Add(vert);
    }

    void OnDrawGizmosSelected()
    {
        var smr = Renderer;
        if (smr == null || smr.sharedMesh == null || smr.bones == null)
            return;
        var mesh = smr.sharedMesh;
        var verts = mesh.vertices;
        var weights = mesh.boneWeights;
        var bones = smr.bones;
        Gizmos.color = new Color(0.2f, 0.85f, 0.7f, 0.35f);
        int drawn = 0;
        for (int v = 0; v < weights.Length && drawn < 256; v++)
        {
            int bi = weights[v].boneIndex0;
            if (bi < 0 || bi >= bones.Length || bones[bi] == null || weights[v].weight0 < 0.4f)
                continue;
            Vector3 worldV = smr.transform.TransformPoint(verts[v]);
            Gizmos.DrawLine(bones[bi].position, worldV);
            drawn++;
        }
    }
}
