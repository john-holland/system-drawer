#if UNITY_EDITOR
using UnityEditor;

/// <summary>Editor session identity for Continuuuum API headers (no passwords).</summary>
public static class ContinuuuumEditorSession
{
    const string UserIdPref = "Continuuuum.EditorUserId";

    public static string UserId
    {
        get => EditorPrefs.GetString(UserIdPref, "anonymous");
        set => EditorPrefs.SetString(UserIdPref, value);
    }

    public static string TenantId => ContinuuuumApiConfig.GetTenant();
    public static string ApiBaseUrl => ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/');
}

#endif
