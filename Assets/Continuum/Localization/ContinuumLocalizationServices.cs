#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Resolves live vs stub localization API client.</summary>
public static class ContinuumLocalizationServices
{
    const string UseLivePref = "Continuum.UseLiveLocalizationApi";

    static IContinuumLocalizationClient _cached;
    static IContinuumNotificationClient _notifCached;

    public static IContinuumLocalizationClient GetClient()
    {
        if (_cached != null)
            return _cached;

#if UNITY_EDITOR
        bool useLive = EditorPrefs.GetBool(UseLivePref, true);
#else
        bool useLive = true;
#endif
        _cached = useLive
            ? new ContinuumLocalizationClient()
            : new StubContinuumLocalizationClient();
        _notifCached = _cached as IContinuumNotificationClient;
        return _cached;
    }

    public static IContinuumNotificationClient GetNotificationClient()
    {
        GetClient();
        return _notifCached ?? new StubContinuumLocalizationClient();
    }

    public static void ResetClient()
    {
        _cached = null;
        _notifCached = null;
    }
}
