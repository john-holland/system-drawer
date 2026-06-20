using System.IO;
using UnityEngine;

/// <summary>
/// Runtime config for Continuum API URL (notifications, etc.).
/// Reads from Scripts/continuum_api_url.txt or uses http://localhost:5050.
/// </summary>
public static class ContinuumApiConfig
{
    private static string _cached;
    private static string _cachedTenant;

    public static string GetApiBaseUrl()
    {
        if (_cached != null)
            return _cached;
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuum_api_url.txt");
        try
        {
            if (File.Exists(p))
            {
                var s = File.ReadAllText(p).Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    _cached = s;
                    return _cached;
                }
            }
        }
        catch { }
        _cached = "http://localhost:5050";
        return _cached;
    }

    /// <summary>Tenant id from Scripts/continuum_tenant.txt or "default".</summary>
    public static string GetTenant()
    {
        if (_cachedTenant != null)
            return _cachedTenant;
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuum_tenant.txt");
        try
        {
            if (File.Exists(p))
            {
                var s = File.ReadAllText(p).Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    _cachedTenant = s;
                    return _cachedTenant;
                }
            }
        }
        catch { }
        _cachedTenant = "default";
        return _cachedTenant;
    }
}
