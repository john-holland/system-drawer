using UnityEngine;

public enum JusticeSeatRole
{
    Prisoner = 0,
    Guard = 1
}

/// <summary>Bus seat role for justice transport — Guard vs Prisoner on BusVehicleRagdoll.seatAnchors.</summary>
[System.Serializable]
public class JusticeSeatCard : CommuterFindSeatCard
{
    public JusticeSeatRole seatRole = JusticeSeatRole.Prisoner;

    public static JusticeSeatCard Generate(GameObject actor, BusVehicleRagdoll vehicle, JusticeSeatRole role)
    {
        var c = new JusticeSeatCard();
        Fill(c, actor, vehicle, "justice_transport", "justice_seat");
        c.seatRole = role;
        if (vehicle != null && vehicle.seatAnchors != null && vehicle.seatAnchors.Count > 0)
        {
            int idx = role == JusticeSeatRole.Guard ? 0 : Mathf.Min(1, vehicle.seatAnchors.Count - 1);
            c.seatAnchor = vehicle.seatAnchors[idx];
        }
        c.sectionName = role == JusticeSeatRole.Guard ? "justice_seat_guard" : "justice_seat_prisoner";
        return c;
    }
}

[System.Serializable]
public class TAVehicleJusticeTransportCard : TATransitCard
{
    public JusticeSeatRole defaultPassengerRole = JusticeSeatRole.Prisoner;

    public static TAVehicleJusticeTransportCard Generate(DispatchRequest request, BusVehicleRagdoll vehicle = null)
    {
        var c = new TAVehicleJusticeTransportCard();
        Fill(c, request, "ta_vehicle_justice_transport");
        c.vehicle = vehicle;
        c.physicalPathingTag = "ta_justice_transport";
        return c;
    }
}
