using UnityEngine;

/// <summary>
/// Attaches bowel/bladder + optional groin to vehicles (or any ambulating actor) via weak organ host refs.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Ingestion/Vehicle Organ Host")]
public sealed class VehicleOrganHost : MonoBehaviour, IOrganSystemHost
{
    public BowelBladderRuntime bowelBladder;
    public GroinAnatomyRuntime groin;
    public WeakOrganHostRef weakRef = new WeakOrganHostRef();

    public GameObject HostObject => gameObject;

    void Awake()
    {
        EnsureOrgans();
        weakRef.hostComponent = this;
        if (groin != null)
            groin.organHost = weakRef;
    }

    public void EnsureOrgans()
    {
        if (bowelBladder == null)
            bowelBladder = BowelBladderRuntime.FindOrCreate(gameObject);
        if (groin == null)
            groin = GetComponent<GroinAnatomyRuntime>() ?? gameObject.AddComponent<GroinAnatomyRuntime>();
        if (groin.urethraTip == null)
        {
            var tip = transform.Find("UrethraTip");
            if (tip == null)
            {
                var go = new GameObject("UrethraTip");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, -0.05f, 0.2f);
                tip = go.transform;
            }
            groin.urethraTip = tip;
        }
    }

    public bool TryGetOrganRuntime<T>(out T runtime) where T : class
    {
        if (bowelBladder is T b)
        {
            runtime = b;
            return true;
        }
        if (groin is T g)
        {
            runtime = g;
            return true;
        }
        runtime = null;
        return false;
    }

    /// <summary>Ensure a VehicleActor (or any host) has organ runtimes for free excrete.</summary>
    public static VehicleOrganHost FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        var existing = host.GetComponent<VehicleOrganHost>();
        if (existing != null)
        {
            existing.EnsureOrgans();
            return existing;
        }
        var vh = host.AddComponent<VehicleOrganHost>();
        vh.EnsureOrgans();
        return vh;
    }
}
