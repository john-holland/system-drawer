using System;

using System.IO;

using UnityEngine;



/// <summary>

/// Runtime config for Continuuuum API URL (notifications, etc.).

/// Reads from Scripts/continuuuum_api_url.txt or uses http://localhost:5050.

/// </summary>

public static class ContinuuuumApiConfig

{

    private static string _cached;

    private static string _cachedTenant;

    private static bool? _disableReporting;



    public static string GetApiBaseUrl()

    {

        if (_cached != null)

            return _cached;

        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_api_url.txt");

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



    /// <summary>Tenant id from Scripts/continuuuum_tenant.txt or "default".</summary>

    public static string GetTenant()

    {

        if (_cachedTenant != null)

            return _cachedTenant;

        var p = Path.Combine(Application.dataPath, "..", "Scripts", "continuuuum_tenant.txt");

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



    /// <summary>

    /// When true, runtime component reports are suppressed (skunk-works / local-only mode).

    /// Set CONTINUUUUM_DISABLE_REPORTING=1 in the environment or PlayerPrefs ContinuuuumDisableReporting=1.

    /// </summary>

    public static bool DisableReportingForSkunkWorks

    {

        get

        {

            if (_disableReporting.HasValue)

                return _disableReporting.Value;

            try

            {

                var env = Environment.GetEnvironmentVariable("CONTINUUUUM_DISABLE_REPORTING");

                if (!string.IsNullOrEmpty(env) && env != "0" && !string.Equals(env, "false", StringComparison.OrdinalIgnoreCase))

                {

                    _disableReporting = true;

                    return true;

                }

            }

            catch { }

            _disableReporting = PlayerPrefs.GetInt("ContinuuuumDisableReporting", 0) == 1;

            return _disableReporting.Value;

        }

        set

        {

            _disableReporting = value;

            PlayerPrefs.SetInt("ContinuuuumDisableReporting", value ? 1 : 0);

            PlayerPrefs.Save();

        }

    }



    /// <summary>Runtime reports push in development builds unless skunk-works opt-out is set.</summary>

    public static bool ShouldPushRuntimeComponentReports =>

        !DisableReportingForSkunkWorks && (Debug.isDebugBuild || Application.isEditor);

}

