using UnityEngine;

/// <summary>
/// Stamps a repair decal on the road MeshRenderer (PaintTransferDecal) or plants a baked SDF Max shape.
/// When road damage memory is desired, keep this component after geometry reset.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Road Repair Decal")]
public sealed class RoadRepairDecal : MonoBehaviour
{
    public MeshRenderer roadRenderer;
    public Material repairMaterial;
    public Color patchColor = new Color(0.45f, 0.45f, 0.42f, 1f);
    public bool usePaintTransfer = true;
    public bool useSdfMaxPlant;
    public GameObject sdfMaxPrefab;
    public bool applied;

    public void Apply()
    {
        if (roadRenderer == null)
            roadRenderer = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();

        if (usePaintTransfer && roadRenderer != null)
        {
            var transfer = GetComponent<PaintTransferDecal>() ?? gameObject.AddComponent<PaintTransferDecal>();
            // Soft apply — PaintTransferDecal APIs vary; message hook for runtime systems.
            SendMessage("OnPaintTransferRepairDecal", roadRenderer, SendMessageOptions.DontRequireReceiver);
            if (repairMaterial != null)
            {
                // Do not replace whole road material permanently; spawn a child patch quad.
                EnsurePatchQuad();
            }
        }

        if (useSdfMaxPlant && sdfMaxPrefab != null)
        {
            var go = Instantiate(sdfMaxPrefab, transform.position, transform.rotation, transform);
            go.name = "RoadRepairSdfMax";
        }

        applied = true;
        SendMessage("OnRoadRepairDecalApplied", this, SendMessageOptions.DontRequireReceiver);
    }

    void EnsurePatchQuad()
    {
        Transform existing = transform.Find("RoadRepairPatch");
        if (existing != null) return;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "RoadRepairPatch";
        quad.transform.SetParent(transform, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(2f, 2f, 1f);
        var mr = quad.GetComponent<MeshRenderer>();
        if (mr != null && repairMaterial != null)
            mr.sharedMaterial = repairMaterial;
        else if (mr != null)
            mr.material.color = patchColor;
        var col = quad.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }
}
