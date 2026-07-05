#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Resolves live vs stub localization API client.</summary>
public static class ContinuuuumLocalizationServices
{
    const string UseLivePref = "Continuuuum.UseLiveLocalizationApi";

    static IContinuuuumLocalizationClient _cached;
    static IContinuuuumNotificationClient _notifCached;

    public static IContinuuuumLocalizationClient GetClient()
    {
        if (_cached != null)
            return _cached;

#if UNITY_EDITOR
        bool useLive = EditorPrefs.GetBool(UseLivePref, true);
#else
        bool useLive = true;
#endif
        _cached = useLive
            ? new ContinuuuumLocalizationClient()
            : new StubContinuuuumLocalizationClient();
        _notifCached = _cached as IContinuuuumNotificationClient;
        return _cached;
    }

    public static IContinuuuumNotificationClient GetNotificationClient()
    {
        GetClient();
        return _notifCached ?? new StubContinuuuumLocalizationClient();
    }

    public static void ResetClient()
    {
        _cached = null;
        _notifCached = null;
    }

    /// <summary>Apply Mayor Dog Mod lemma placeholders to expanded prompt text.</summary>
    public static string ApplyModPlaceholders(string expandedText) =>
        Continuuuum.Mods.MayorDogModApplicator.ResolveModPlaceholders(expandedText);
}
