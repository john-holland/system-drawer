using System.Collections.Generic;
using UnityEngine;

/// <summary>Indexes ParkingZoneVolume(s) for civil venues; TravelAgent seeds on wake.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Parking Lot")]
public sealed class ParkingLot : MonoBehaviour
{
    public string lotId;
    public List<ParkingZoneVolume> zones = new List<ParkingZoneVolume>();
    public Transform arrivalAnchor;
    public bool registerWithTravelAgentOnWake = true;

    void Awake()
    {
        if (string.IsNullOrEmpty(lotId))
            lotId = gameObject.name;
        if (zones.Count == 0)
            GetComponentsInChildren(true, zones);
        if (arrivalAnchor == null)
            arrivalAnchor = transform;
    }

    public Vector3 ArrivalWorld => arrivalAnchor != null ? arrivalAnchor.position : transform.position;

    public Bounds CombinedBounds()
    {
        var b = new Bounds(ArrivalWorld, Vector3.one);
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] == null) continue;
            b.Encapsulate(zones[i].GetWorldBounds());
        }
        return b;
    }

    /// <summary>Soft-seed TravelAgents near the lot (used on civil venue open).</summary>
    public void SeedTravelAgents(float radiusM = 40f)
    {
        if (!registerWithTravelAgentOnWake) return;
        var agents = FindObjectsByType<TravelAgent>(FindObjectsSortMode.None);
        Vector3 goal = ArrivalWorld;
        for (int i = 0; i < agents.Length; i++)
        {
            var ta = agents[i];
            if (ta == null) continue;
            if ((ta.transform.position - goal).sqrMagnitude > radiusM * radiusM) continue;
            ta.previewGoalWorld = goal;
            ta.SendMessage("OnParkingLotAvailable", this, SendMessageOptions.DontRequireReceiver);
        }
    }
}
