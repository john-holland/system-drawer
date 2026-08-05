using System.Collections.Generic;
using UnityEngine;

/// <summary>Central hub so hospital/police/fire dispatch can cross-request units.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Central Dispatch Hub")]
public sealed class CentralDispatchHub : MonoBehaviour
{
    public static CentralDispatchHub Instance { get; private set; }

    public string hubServiceId = "floating_dispatch";
    public bool floatingDeveloperHub = true;
    public CompanyRegistration hubCompany;

    readonly Dictionary<string, DispatchBioRhythm> _services = new Dictionary<string, DispatchBioRhythm>();

    void Awake()
    {
        Instance = this;
        if (hubCompany == null)
            hubCompany = GetComponent<CompanyRegistration>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Subscribe(string serviceId, DispatchBioRhythm bio)
    {
        if (string.IsNullOrEmpty(serviceId) || bio == null) return;
        _services[serviceId] = bio;
    }

    public void Unsubscribe(string serviceId)
    {
        if (string.IsNullOrEmpty(serviceId)) return;
        _services.Remove(serviceId);
    }

    public DispatchBioRhythm Get(string serviceId)
    {
        if (string.IsNullOrEmpty(serviceId)) return null;
        return _services.TryGetValue(serviceId, out var bio) ? bio : null;
    }

    public bool RequestCrossDispatch(string fromId, string toId, DispatchRequest request)
    {
        var target = Get(toId);
        if (target == null && floatingDeveloperHub)
        {
            // Floating hub accepts orphan requests onto first available peer or self-log.
            foreach (var kv in _services)
            {
                if (kv.Key == fromId) continue;
                target = kv.Value;
                break;
            }
        }
        if (target == null || request == null) return false;
        request.fromServiceId = fromId;
        request.toServiceId = toId;
        target.Enqueue(request);
        return true;
    }

    public IEnumerable<string> ServiceIds => _services.Keys;
}
