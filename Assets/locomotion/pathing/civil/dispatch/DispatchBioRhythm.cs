using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared dispatch peer: service id, hours, and the card queue venues override.</summary>
public class DispatchBioRhythm : MonoBehaviour
{
    public string serviceId = "dispatch";
    public bool governmentAssigned;
    public CompanyRegistration company;
    public string hoursCron = "* * * * *";
    public List<string> subscribedPeerIds = new List<string>();
    [Range(0f, 1f)] public float unitsAvailable01 = 1f;
    public CivilVenueBioRhythmService venueBio;
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();
    [Range(0f, 1f)] public float alert01;
    [Range(0f, 1f)] public float queueDepth01;

    readonly List<DispatchRequest> _pending = new List<DispatchRequest>();
    public IReadOnlyList<DispatchRequest> Pending => _pending;

    protected virtual void Awake()
    {
        if (subscribedPeerIds == null)
            subscribedPeerIds = new List<string>();
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>();
    }

    public virtual void Tick(DateTime utcNow, float dt)
    {
    }

    public void Enqueue(DispatchRequest request)
    {
        if (request != null)
            _pending.Add(request);
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
