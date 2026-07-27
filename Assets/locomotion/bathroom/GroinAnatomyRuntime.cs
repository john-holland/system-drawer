using UnityEngine;

public enum GroinAnatomyKind
{
    Penis,
    Vulva
}

/// <summary>Groin mesh/joints + urethra nozzle tip for pee (ragdoll or vehicle host).</summary>
[AddComponentMenu("Locomotion/Bathroom/Groin Anatomy Runtime")]
public sealed class GroinAnatomyRuntime : MonoBehaviour
{
    public GroinAnatomyKind kind = GroinAnatomyKind.Penis;
    public MeshFilter defaultMeshFilter;
    public Mesh penisMesh;
    public Mesh vulvaMesh;
    public ConfigurableJoint jointAssist;
    public Transform urethraTip;
    public float apertureRadiusM = 0.004f;
    public float maxThroughputLitersPerSecond = 0.05f;
    [Tooltip("Optional DrinkNozzleComponent.")]
    public MonoBehaviour urethraNozzle;
    public WeakOrganHostRef organHost = new WeakOrganHostRef();

    public Vector3 TipPosition =>
        urethraTip != null ? urethraTip.position :
        (urethraNozzle != null ? urethraNozzle.transform.position : transform.position);

    public Vector3 TipForward =>
        urethraTip != null ? urethraTip.forward :
        (urethraNozzle != null ? urethraNozzle.transform.forward : transform.forward);

    void Awake()
    {
        if (urethraTip == null && urethraNozzle != null)
            urethraTip = urethraNozzle.transform;
        ApplyDefaultMesh();
    }

    public void ApplyDefaultMesh()
    {
        if (defaultMeshFilter == null) return;
        if (kind == GroinAnatomyKind.Penis && penisMesh != null)
            defaultMeshFilter.sharedMesh = penisMesh;
        else if (kind == GroinAnatomyKind.Vulva && vulvaMesh != null)
            defaultMeshFilter.sharedMesh = vulvaMesh;
    }
}
