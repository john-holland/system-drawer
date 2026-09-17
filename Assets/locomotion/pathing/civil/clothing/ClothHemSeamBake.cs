using UnityEngine;

public enum HemSeamApplyMode
{
    Decal = 0,
    Bake = 1
}

/// <summary>Hem / seam stitch maps on bolt UV. Decal material or bake onto fold mesh.</summary>
public static class ClothHemSeamBake
{
    public const string ShaderName = "Locomotion/ClothHemSeam";

    public static bool ApplyToStep(TayloringStep step, ClothBoltSpec bolt, ClothSplinePath path = null)
    {
        if (step == null || bolt == null) return false;
        step.bakedMesh = ClothFoldBake.BakeBoltRect(bolt);
        step.foldCacheId = bolt.name + "_" + step.kind + "_hem";
        step.bakeComplete = step.bakedMesh != null && step.bakedMesh.vertexCount > 0;
        if (step.bakeComplete && path != null && path.applyMode == HemSeamApplyMode.Bake)
            BakeMaps(step.bakedMesh, path);
        return step.bakeComplete;
    }

    public static Material CreateDecalMaterial(float gauge01)
    {
        var shader = Shader.Find(ShaderName) ?? Shader.Find("Standard");
        if (shader == null) return null;
        var mat = new Material(shader) { name = "ClothHemSeamDecal" };
        mat.SetFloat("_Gauge01", Mathf.Clamp01(gauge01));
        return mat;
    }

    public static void ApplyDecalToRenderer(Renderer renderer, float gauge01)
    {
        if (renderer == null) return;
        var mat = CreateDecalMaterial(gauge01);
        if (mat != null)
            renderer.sharedMaterial = mat;
    }

    static void BakeMaps(Mesh mesh, ClothSplinePath path)
    {
        if (mesh == null || path == null) return;
        var uvs = mesh.uv;
        if (uvs == null || uvs.Length == 0) return;
        float gauge = path.gauge01;
        for (int i = 0; i < uvs.Length; i++)
            uvs[i] = new Vector2(uvs[i].x, uvs[i].y + gauge * 0.01f);
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }
}
