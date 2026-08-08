using UnityEngine;

public enum RoadsideDecorMode
{
    GameObjectPrefab = 0,
    MeshRendererDecal = 1
}

/// <summary>Signage / shrubs / plants for SG3D print or mesh-decal roadside decoration.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roadside Decor Stamp")]
public sealed class RoadsideDecorStamp : MonoBehaviour
{
    public RoadsideDecorMode mode = RoadsideDecorMode.GameObjectPrefab;
    public GameObject prefab;
    public MeshRenderer decalRenderer;
    public Material decalMaterial;
    public bool faceOutFromStreet = true;
    public Vector3 streetOutward = Vector3.right;
    public string label = "signage";

    public GameObject Apply()
    {
        if (mode == RoadsideDecorMode.MeshRendererDecal)
        {
            if (decalRenderer != null && decalMaterial != null)
                decalRenderer.sharedMaterial = decalMaterial;
            if (faceOutFromStreet && decalRenderer != null)
                decalRenderer.transform.forward = streetOutward.sqrMagnitude > 1e-4f
                    ? streetOutward.normalized
                    : decalRenderer.transform.forward;
            return decalRenderer != null ? decalRenderer.gameObject : null;
        }

        if (prefab == null) return null;
        var go = Instantiate(prefab, transform.position, transform.rotation, transform);
        if (faceOutFromStreet)
            go.transform.forward = streetOutward.sqrMagnitude > 1e-4f ? streetOutward.normalized : go.transform.forward;
        go.name = label + "_decor";
        return go;
    }
}
