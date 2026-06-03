using UnityEngine;

/// <summary>Tangent-aligned formation offsets along a path polyline.</summary>
public static class TravelFormationTangentOffset
{
    public static Vector3 ComputeAtWaypoint(
        TravelFormationAsset formation,
        int slotIndex,
        Vector3 waypoint,
        Vector3 tangentForward)
    {
        if (formation == null || !formation.HasSlots || slotIndex < 0 || slotIndex >= formation.SlotCount)
            return waypoint;
        Vector3 slotLocal = formation.slots[slotIndex].localOffset;
        Vector3 fwd = tangentForward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-8f)
            fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        if (right.sqrMagnitude < 1e-8f)
            right = Vector3.right;
        right.Normalize();
        return waypoint + right * slotLocal.x + Vector3.up * slotLocal.y + fwd * slotLocal.z;
    }
}
