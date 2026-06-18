/// <summary>Project-level multiplayer capability flags.</summary>
public static class NetworkCapabilities
{
    public static bool HasMultiplayer => NetworkSettings.Default.hasMultiplayer;
    public static bool AllowSaveLoadInMultiplayer => NetworkSettings.Default.allowSaveLoadInMultiplayer;
}
