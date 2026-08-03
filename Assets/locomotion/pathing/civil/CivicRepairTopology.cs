using System.Collections.Generic;
using UnityEngine;

/// <summary>Topological open/close for repair zones — CivicCard staging (restaurant open/close analog).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Civic Repair Topology")]
public sealed class CivicRepairTopology : MonoBehaviour
{
    public BuildingRagdoll buildingRagdoll;
    public Transform repairZoneRoot;
    public string waypointGroup = "repair";
    public List<RetinuePeckingEntry> repairRetinue = new List<RetinuePeckingEntry>();
    public bool zoneOpen;

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
    }

    public void SetRepairZoneOpen(bool open)
    {
        zoneOpen = open;
        if (repairZoneRoot != null)
            repairZoneRoot.gameObject.SetActive(open);
        if (repairRetinue == null) return;
        for (int i = 0; i < repairRetinue.Count; i++)
        {
            var a = repairRetinue[i]?.actor;
            if (a == null) continue;
            if (open && !a.activeSelf) a.SetActive(true);
            a.SendMessage(open ? "OnCivicRepairOpen" : "OnCivicRepairClose", this, SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool TryCompleteRepair(string objectId)
    {
        var q = buildingRagdoll != null ? buildingRagdoll.damageQueue : DamagedObjectQueue.Instance;
        bool ok = q != null && q.TryDequeue(objectId, buildingRagdoll != null ? buildingRagdoll.buildingStableId : null);
        if (ok)
            buildingRagdoll?.ApplyRepair(0.15f);
        return ok;
    }
}
