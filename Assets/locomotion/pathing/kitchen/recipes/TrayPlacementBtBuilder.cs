using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds short place-step ids for plates onto tray slots / table waypoints.</summary>
public static class TrayPlacementBtBuilder
{
    public static List<string> BuildPlaceStepIds(int plateCount, TrayBinSettings settings, string batchKey = "tray")
    {
        var ids = new List<string>();
        int n = Mathf.Max(0, plateCount);
        settings = settings ?? new TrayBinSettings();
        for (int i = 0; i < n; i++)
        {
            ids.Add($"FetchPlate_{batchKey}_{i}");
            if (settings.placeMode == TrayPlaceMode.HoldIk)
                ids.Add($"HoldIk_{batchKey}_{i}");
            else
                ids.Add($"PlaceOnTray_{batchKey}_{i}");
        }
        ids.Add($"ServeWaypoint_{batchKey}");
        return ids;
    }

    public static Transform ResolveServeWaypoint(TrayBinSettings settings, Transform fallback = null)
    {
        if (settings != null && settings.placeWaypoint != null)
            return settings.placeWaypoint;
        return fallback;
    }
}
