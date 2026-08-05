using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DispatchRouteConfig
{
    public string routeId;
    public string label;
    public Transform pickup;
    public Transform dropoff;
    public string notes;
}

[Serializable]
public sealed class DispatchRequest
{
    public string requestId;
    public string fromServiceId;
    public string toServiceId;
    public string kind = "route";
    public Vector3 worldTarget;
    public string personaKey;
    public string notes;
    public float priority01 = 0.5f;
}

/// <summary>Base biorhythm for public/private dispatch services (fire, EMS, police, floating hub).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Dispatch Bio Rhythm")]
public class DispatchBioRhythm : MonoBehaviour
{
    public string serviceId;
    public CompanyRegistration company;
    public bool governmentAssigned;
    public string hoursCron = "* * * * *";
    public List<string> subscribedPeerIds = new List<string>();
    public List<DispatchRouteConfig> routes = new List<DispatchRouteConfig>();
    public List<RetinuePeckingEntry> agencyPeers = new List<RetinuePeckingEntry>();
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();

    [Range(0f, 1f)] public float queueDepth01;
    [Range(0f, 1f)] public float unitsAvailable01 = 1f;
    [Range(0f, 1f)] public float alert01;
    public CivilVenueBioRhythmService venueBio;

    readonly List<DispatchRequest> _pending = new List<DispatchRequest>();

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = gameObject.name;
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
    }

    protected virtual void OnDestroy()
    {
        CentralDispatchHub.Instance?.Unsubscribe(serviceId);
    }

    public virtual void Tick(DateTime utcNow, float dt)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = open ? Mathf.Clamp01(0.35f + alert01 * 0.4f + queueDepth01 * 0.2f) : 0.1f;
            venueBio.stress01 = alert01;
        }
        queueDepth01 = Mathf.MoveTowards(queueDepth01, _pending.Count > 0 ? 1f : 0f, dt * 0.05f);
    }

    public void Enqueue(DispatchRequest request)
    {
        if (request == null) return;
        if (string.IsNullOrEmpty(request.requestId))
            request.requestId = Guid.NewGuid().ToString("N");
        _pending.Add(request);
        alert01 = Mathf.Clamp01(alert01 + request.priority01 * 0.2f);
    }

    public bool TryDequeue(out DispatchRequest request)
    {
        if (_pending.Count == 0)
        {
            request = null;
            return false;
        }
        request = _pending[0];
        _pending.RemoveAt(0);
        return true;
    }

    public IReadOnlyList<DispatchRequest> Pending => _pending;

    public virtual List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        switch ((request.kind ?? "route").ToLowerInvariant())
        {
            case "pickup":
                cards.Add(DispatchRequestPickupCard.Generate(request));
                break;
            case "load":
                cards.Add(DispatchRequestLoadCard.Generate(request));
                break;
            case "unload":
                cards.Add(DispatchRequestUnloadCard.Generate(request));
                break;
            case "passenger_pickup":
                cards.Add(DispatchRequestPassengerPickupCard.Generate(request));
                break;
            case "passenger_dropoff":
                cards.Add(DispatchRequestPassengerDropoffCard.Generate(request));
                break;
            case "release_passenger":
                cards.Add(DispatchRequestReleasePassengerCard.Generate(request));
                break;
            default:
                cards.Add(DispatchRequestRouteCard.Generate(request));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
