using UnityEngine;

/// <summary>
/// Applies a tinted decal to a MeshRenderer at a collision point using sampled paint color.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Transfer Decal")]
public sealed class PaintTransferDecal : MonoBehaviour
{
    public Material decalMaterialTemplate;
    [Range(0f, 1f)] public float transferOpacity = 0.75f;
    [Min(0.01f)] public float decalSize = 0.08f;
    public float decalLifetime = 12f;

    public void TryApply(Collider source, Vector3 worldPoint, Vector3 normal, Color paintColor)
    {
        if (source == null) return;
        var renderer = source.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        var go = new GameObject("PaintTransferDecal");
        go.transform.SetParent(renderer.transform, true);
        go.transform.position = worldPoint + normal.normalized * 0.001f;
        go.transform.rotation = Quaternion.LookRotation(-normal);
        go.transform.localScale = Vector3.one * decalSize;

        var mr = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildQuad();

        Material mat;
        if (decalMaterialTemplate != null)
            mat = new Material(decalMaterialTemplate);
        else
            mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
        Color c = paintColor;
        c.a = transferOpacity;
        mat.color = c;
        mr.material = mat;

        var life = go.AddComponent<PaintDecalLifetime>();
        life.seconds = decalLifetime;
    }

    static Mesh BuildQuad()
    {
        var mesh = new Mesh { name = "PaintDecalQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}

public sealed class PaintDecalLifetime : MonoBehaviour
{
    public float seconds = 12f;
    void Update()
    {
        seconds -= Time.deltaTime;
        if (seconds <= 0f)
            Destroy(gameObject);
    }
}
