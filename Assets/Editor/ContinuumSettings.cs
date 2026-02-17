#if UNITY_EDITOR
using System.IO;
using UnityEngine;

/// <summary>
/// Project-level Continuum settings (tenant per game). Read from Scripts/continuum_tenant.txt
/// so the value can be committed and shared by the team. Use "default" for local development.
/// </summary>
public static class ContinuumSettings
{
    private const string TenantFileName = "continuum_tenant.txt";
    private static string _cachedTenant;

    /// <summary>
    /// Tenant id for continuum library and explorer. Default "default" for local dev.
    /// </summary>
    public static string GetTenant()
    {
        if (_cachedTenant != null)
            return _cachedTenant;
        string scriptDir = Path.Combine(Application.dataPath, "..", "Scripts");
        string path = Path.Combine(scriptDir, TenantFileName);
        try
        {
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    _cachedTenant = content;
                    return _cachedTenant;
                }
            }
        }
        catch { }
        _cachedTenant = "default";
        return _cachedTenant;
    }

    /// <summary>
    /// Clear cached tenant (e.g. after editing continuum_tenant.txt).
    /// </summary>
    public static void ClearCache()
    {
        _cachedTenant = null;
    }
}
#endif
