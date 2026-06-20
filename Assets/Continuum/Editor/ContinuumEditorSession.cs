#if UNITY_EDITOR
using UnityEditor;

/// <summary>Editor session identity for Continuum API headers (no passwords).</summary>
public static class ContinuumEditorSession
{
    const string UserIdPref = "Continuum.EditorUserId";

    public static string UserId
    {
        get => EditorPrefs.GetString(UserIdPref, "anonymous");
        set => EditorPrefs.SetString(UserIdPref, value);
    }

    public static string TenantId => ContinuumApiConfig.GetTenant();
    public static string ApiBaseUrl => ContinuumApiConfig.GetApiBaseUrl().TrimEnd('/');
}

#endif
