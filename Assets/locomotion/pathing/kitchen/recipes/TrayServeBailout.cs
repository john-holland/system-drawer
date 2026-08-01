using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects tray drop / already eaten / place waypoint covered and reduces load.
/// </summary>
public static class TrayServeBailout
{
    public sealed class BailResult
    {
        public TrayServeBailReason reason;
        public List<TrayBinAllocator.Batch> reducedBatches = new List<TrayBinAllocator.Batch>();
        public bool needsCleanup;
        public bool switchSansTray;
    }

    public static bool IsWaypointCovered(Transform waypoint, float radius = 0.35f, Collider ignore = null)
    {
        if (waypoint == null) return true;
        var hits = Physics.OverlapSphere(waypoint.position, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i] == ignore) continue;
            if (hits[i].transform.IsChildOf(waypoint)) continue;
            // Occupied if another solid body sits on the place target
            if (hits[i].attachedRigidbody != null || !hits[i].isTrigger)
                return true;
        }
        return false;
    }

    public static BailResult Evaluate(
        bool trayDropped,
        bool alreadyEaten,
        bool placeWaypointCovered,
        int remainingPlates,
        TrayBinSettings settings)
    {
        var result = new BailResult { reason = TrayServeBailReason.None };
        if (trayDropped) result.reason = TrayServeBailReason.TrayDropped;
        else if (alreadyEaten) result.reason = TrayServeBailReason.AlreadyEaten;
        else if (placeWaypointCovered) result.reason = TrayServeBailReason.PlaceWaypointCovered;
        else return result;

        result.needsCleanup = true;
        result.switchSansTray = settings == null || settings.allowSansTrayFallback;
        result.reducedBatches = TrayBinAllocator.ReduceAfterBailout(remainingPlates, settings);
        return result;
    }
}
