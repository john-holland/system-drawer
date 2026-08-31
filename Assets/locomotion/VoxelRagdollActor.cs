using UnityEngine;

/// <summary>
/// Minecraft-scale voxel ragdoll analog: 1 block = 1 m, 16 texels per meter.
/// Granulation fitting reads <see cref="GranularitySettings"/> (Minecraft preset or custom).
/// Voxel cells snap to the Minecraft grid; PixelLight faces bind as textures.
///
/// Minecraft® is a trademark of Mojang AB / Microsoft Corporation. All rights reserved.
/// This class is an unofficial analog and is not affiliated with Mojang or Microsoft.
/// No unofficial Minecraft assets are vendored.
/// </summary>
[AddComponentMenu("Locomotion/Actors/Voxel Ragdoll Actor")]
public sealed class VoxelRagdollActor : RagdollActor
{
    public GranularitySettings granularity = new GranularitySettings();
    public BonedSkinnedAnimateableMeshRenderer bonedMesh;

    [Header("PixelLight faces (bind as textures)")]
    public Texture2D north;
    public Texture2D south;
    public Texture2D east;
    public Texture2D west;
    public Texture2D up;
    public Texture2D down;

    public float BlockMeters => granularity != null ? granularity.blockMeters : 1f;
    public float TexelsPerMeter => granularity != null ? granularity.texelsPerMeter : 16f;

    public void ApplyBlockScale()
    {
        if (granularity == null)
            granularity = GranularitySettings.Minecraft();
        granularity.MarkCustomIfEdited();
        float meters = Mathf.Max(0.01f, granularity.blockMeters);
        transform.localScale = Vector3.one * meters;
    }

    /// <summary>Alias for <see cref="ApplyBlockScale"/> (Minecraft default: 1 block = 1 m).</summary>
    public void ApplyMinecraftScale() => ApplyBlockScale();

    public Vector3 SnapToVoxelGrid(Vector3 world)
    {
        if (granularity == null)
            granularity = GranularitySettings.Minecraft();
        return granularity.SnapWorld(world);
    }

    public Texture2D FaceTexture(string face)
    {
        switch ((face ?? "").ToLowerInvariant())
        {
            case "south": return south;
            case "east": return east;
            case "west": return west;
            case "up": return up;
            case "down": return down;
            default: return north;
        }
    }

    public void BindPixelLightToRenderer(Renderer renderer, string face = "north")
    {
        if (renderer == null)
            return;
        var tex = FaceTexture(face);
        if (tex == null)
            return;
        var mat = renderer.sharedMaterial;
        if (mat == null)
            return;
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", tex);
        else if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (granularity == null)
            return;
        float cell = granularity.VoxelCellMeters;
        Gizmos.color = new Color(0.4f, 0.9f, 0.3f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        int n = Mathf.Clamp(granularity.pixelGrid, 8, 32);
        for (int x = 0; x <= n; x++)
        {
            float u = (x / (float)n) - 0.5f;
            Gizmos.DrawLine(new Vector3(u, 0f, -0.5f), new Vector3(u, 0f, 0.5f));
            Gizmos.DrawLine(new Vector3(-0.5f, 0f, u), new Vector3(0.5f, 0f, u));
        }
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * Mathf.Max(cell * n, 0.1f));
    }
#endif
}
