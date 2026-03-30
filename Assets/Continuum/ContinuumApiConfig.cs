using System.IO;
using UnityEngine;

/// <summary>
/// Runtime config for Continuum API URL (notifications, etc.).
/// Reads from Scripts/continuum_api_url.txt or uses http://localhost:5050.
/// </summary>
public static class ContinuumApiConfig
{
    private static string _cached;

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
}
