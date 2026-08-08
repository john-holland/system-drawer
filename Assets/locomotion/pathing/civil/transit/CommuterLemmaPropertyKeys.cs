/// <summary>Lemma property keys for commuter wait / find-seat behaviors.</summary>
public static class CommuterLemmaPropertyKeys
{
    public const string CheckTime = "check_time";
    public const string Impatiently = "impatiently";
    public const string Scans = "scans";
    public const string Find = "find";
    public const string BusStationPack = "bus_station";

    public static bool IsCommuterLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant();
        return k == CheckTime || k == Impatiently || k == Scans || k == Find || k == BusStationPack;
    }
}
