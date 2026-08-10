using System.Collections.Generic;
using UnityEngine;

/// <summary>Road care crew — retinue RTS CallToArms on assigned road/lot routes.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Road Care Crew")]
public sealed class RoadCareCrewRuntime : MonoBehaviour
{
    public CompanyRegistration company;
    public SanitationFacilityRuntime facility;
    public List<Transform> pickupWaypoints = new List<Transform>();
    public List<string> assignedRoadSegmentIds = new List<string>();
    public List<RoadLot> assignedLots = new List<RoadLot>();
    public string troupeId = "road_care_crew";
    public bool gatherRecycling = true;
    public bool gatherPoop = false;

    void Awake()
    {
        if (facility == null)
            facility = GetComponentInParent<SanitationFacilityRuntime>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "crew_lead", peckingOrder = 5, personaKey = "road_crew_lead" });
            company.staff.Add(new RetinuePeckingEntry { role = "picker", peckingOrder = 15, personaKey = "trash_picker" });
        }
    }

    public void CallToArms(Vector3 goal)
    {
        SendMessage("OnRetinueRtsCallToArms", goal, SendMessageOptions.DontRequireReceiver);
        var facilitator = GetComponent<CombatRulesFacilitatorService>()
                          ?? FindFirstObjectByType<CombatRulesFacilitatorService>();
        facilitator?.CallToArms(troupeId, goal);
    }

    /// <summary>Resolve pickup points: waypoints → house trash → facility trash anchor.</summary>
    public List<Transform> ResolvePickupPoints()
    {
        var list = new List<Transform>();
        for (int i = 0; i < pickupWaypoints.Count; i++)
            if (pickupWaypoints[i] != null)
                list.Add(pickupWaypoints[i]);
        if (list.Count > 0) return list;

        var houses = FindObjectsByType<HouseBioRhythm>(FindObjectsSortMode.None);
        for (int i = 0; i < houses.Length; i++)
        {
            var h = houses[i];
            if (h == null || h.trashFill01 < 0.15f) continue;
            var bin = h.GetComponentInChildren<TrashBinRuntime>();
            list.Add(bin != null ? bin.transform : h.transform);
        }

        if (list.Count == 0 && facility != null && facility.trashAnchor != null)
            list.Add(facility.trashAnchor);
        return list;
    }
}
