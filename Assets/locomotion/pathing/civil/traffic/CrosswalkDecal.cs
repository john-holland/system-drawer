using UnityEngine;

/// <summary>Zebra paint decal across lanes — not a Drive collider wall.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic/Crosswalk Decal")]
public sealed class CrosswalkDecal : MonoBehaviour
{
    public int barCount = 6;
    public float barWidthM = 0.4f;
    public bool acrossLanes = true;
    public RoadRepairDecal repair;

    public void Apply()
    {
        if (repair == null)
            repair = GetComponent<RoadRepairDecal>() ?? gameObject.AddComponent<RoadRepairDecal>();
        repair.patchColor = Color.white;
        repair.Apply();
        for (int i = 0; i < barCount; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "ZebraBar_" + i;
            q.transform.SetParent(transform, false);
            float x = (i - (barCount - 1) * 0.5f) * (barWidthM * 1.6f);
            q.transform.localPosition = new Vector3(x, 0.02f, 0f);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(barWidthM, acrossLanes ? 3.5f : 1.5f, 1f);
            Object.Destroy(q.GetComponent<Collider>());
        }
    }
}
