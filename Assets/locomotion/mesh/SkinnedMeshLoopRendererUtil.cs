using UnityEngine;

/// <summary>Resolves a skinned or static mesh renderer and its shared mesh for loop section tools.</summary>
public static class SkinnedMeshLoopRendererUtil
{
    public static Renderer Resolve(Component host)
    {
        if (host == null)
            return null;
        var smr = host.GetComponent<SkinnedMeshRenderer>()
                  ?? host.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null)
            return smr;
        return host.GetComponent<MeshRenderer>()
               ?? host.GetComponentInChildren<MeshRenderer>(true);
    }

    public static Mesh SharedMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer smr)
            return smr.sharedMesh;
        if (renderer == null)
            return null;
        var mf = renderer.GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    public static bool IsSkinned(Renderer renderer) => renderer is SkinnedMeshRenderer;

    public static bool TryBake(Renderer renderer, Mesh dest)
    {
        if (renderer == null || dest == null)
            return false;
        if (renderer is SkinnedMeshRenderer smr)
        {
            if (smr.sharedMesh == null)
                return false;
            smr.BakeMesh(dest, true);
            return true;
        }
        Mesh mesh = SharedMesh(renderer);
        if (mesh == null)
            return false;
        dest.Clear();
        dest.vertices = mesh.vertices;
        dest.normals = mesh.normals;
        dest.tangents = mesh.tangents;
        dest.uv = mesh.uv;
        dest.colors = mesh.colors;
        dest.boneWeights = mesh.boneWeights;
        dest.bindposes = mesh.bindposes;
        dest.subMeshCount = Mathf.Max(1, mesh.subMeshCount);
        for (int i = 0; i < dest.subMeshCount; i++)
            dest.SetTriangles(mesh.GetTriangles(i), i, true);
        dest.RecalculateBounds();
        return true;
    }
}
