using System.Collections.Generic;
using UnityEngine;
using Weather;

/// <summary>Whitelist/blacklist clear of weather physics manifold cells near hygiene fixtures.</summary>
public static class HygieneManifoldClearService
{
    public const string ChannelWater = "water";
    public const string ChannelHumidity = "humidity";
    public const string ChannelOdor = "odor";
    public const string ChannelSkin = "skin";

    public static void ClearSphere(
        WeatherPhysicsManifold manifold,
        Vector3 center,
        float radius,
        IList<string> whitelistChannels,
        IList<string> blacklistChannels)
    {
        if (manifold == null || radius <= 0f) return;
        float step = Mathf.Max(0.15f, radius * 0.3f);
        for (float x = -radius; x <= radius; x += step)
        for (float y = -radius; y <= radius; y += step)
        for (float z = -radius; z <= radius; z += step)
        {
            Vector3 p = center + new Vector3(x, y, z);
            if ((p - center).sqrMagnitude > radius * radius) continue;
            ClearPoint(manifold, p, whitelistChannels, blacklistChannels);
        }
    }

    public static void ClearPoint(
        WeatherPhysicsManifold manifold,
        Vector3 p,
        IList<string> whitelist,
        IList<string> blacklist)
    {
        if (manifold == null) return;
        var data = new ManifoldCellData();
        bool clearWater = ShouldClear(ChannelWater, whitelist, blacklist);
        bool clearOdor = ShouldClear(ChannelOdor, whitelist, blacklist);
        // Never clear "skin" unless explicitly whitelisted.
        if (blacklist != null)
        {
            for (int i = 0; i < blacklist.Count; i++)
                if (string.Equals(blacklist[i], ChannelSkin, System.StringComparison.OrdinalIgnoreCase))
                    return;
        }

        if (clearWater)
        {
            data.density = 0f;
            data.surfacePorosity = 0f;
            data.mode = WeatherMode.Air;
        }
        if (clearOdor)
            data.gasPressure = 0f;

        manifold.SetDataAtPosition(p, data);
    }

    static bool ShouldClear(string channel, IList<string> whitelist, IList<string> blacklist)
    {
        if (blacklist != null)
        {
            for (int i = 0; i < blacklist.Count; i++)
                if (string.Equals(blacklist[i], channel, System.StringComparison.OrdinalIgnoreCase))
                    return false;
        }
        if (whitelist == null || whitelist.Count == 0)
            return true;
        for (int i = 0; i < whitelist.Count; i++)
            if (string.Equals(whitelist[i], channel, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
