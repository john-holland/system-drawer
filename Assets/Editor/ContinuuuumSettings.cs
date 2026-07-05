#if UNITY_EDITOR
using System.IO;
using UnityEngine;

/// <summary>
/// Project-level Continuuuum settings (tenant per game). Read from Scripts/continuuuum_tenant.txt
/// so the value can be committed and shared by the team. Use "default" for local development.
/// </summary>
public static class ContinuuuumSettings
{
    private const string TenantFileName = "continuuuum_tenant.txt";
    private static string _cachedTenant;

    /// <summary>
    /// Tenant id for continuuuum library and explorer. Default "default" for local dev.
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
    /// Clear cached tenant (e.g. after editing continuuuum_tenant.txt).
    /// </summary>
    public static void ClearCache()
    {
        _cachedTenant = null;
        _cachedDbPath = null;
        _cachedPythonPath = null;
        _cachedApiBaseUrl = null;
    }

    private static string _cachedDbPath;
    private static string _cachedApiBaseUrl;
    private static string _cachedPythonPath;

    /// <summary>
    /// DB path for continuuuum.db. From continuuuum_db_path.txt in Scripts or empty.
    /// </summary>
    public static string GetDbPath()
    {
        if (_cachedDbPath != null)
            return _cachedDbPath;
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_db_path.txt");
        _cachedDbPath = File.Exists(p) ? File.ReadAllText(p).Trim() : "";
        return _cachedDbPath;
    }

    /// <summary>
    /// Python path for USC CLI. From continuuuum_python_path.txt in Scripts or empty (use PATH).
    /// </summary>
    public static string GetPythonPath()
    {
        if (_cachedPythonPath != null)
            return _cachedPythonPath;
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_python_path.txt");
        _cachedPythonPath = File.Exists(p) ? File.ReadAllText(p).Trim() : "";
        return _cachedPythonPath;
    }

    /// <summary>
    /// Continuuuum API base URL for notifications. From Scripts/continuuuum_api_url.txt or http://localhost:5050.
    /// </summary>
    public static string GetApiBaseUrl()
    {
        if (_cachedApiBaseUrl != null)
            return _cachedApiBaseUrl;
        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_api_url.txt");
        _cachedApiBaseUrl = File.Exists(p) ? File.ReadAllText(p).Trim() : "http://localhost:5050";
        if (string.IsNullOrEmpty(_cachedApiBaseUrl))
            _cachedApiBaseUrl = "http://localhost:5050";
        return _cachedApiBaseUrl;
    }
}
#endif
