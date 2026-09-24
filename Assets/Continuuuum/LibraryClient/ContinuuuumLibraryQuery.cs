namespace Continuuuum.Library
{
    /// <summary>Shared search UI option tables for Continuuuum Library.</summary>
    public static class ContinuuuumLibraryQuery
    {
        public static readonly string[] DistanceOptionLabels =
        {
            "Infinite",
            "0 (same bucket)",
            "10 mi",
            "100 mi",
            "500 mi",
            "1000 mi",
            "5000 mi",
            "24000 mi",
        };

        /// <summary>Miles per <see cref="DistanceOptionLabels"/> index; -1 means infinite.</summary>
        public static readonly float[] DistanceMiles =
        {
            -1f, 0f, 10f, 100f, 500f, 1000f, 5000f, 24000f,
        };

        public static readonly string[] DocumentTypes =
        {
            "All", "video", "document", "audio", "image", "program", "data",
        };

        public static string DistanceMilesQueryValue(int distanceIndex)
        {
            if (distanceIndex < 0 || distanceIndex >= DistanceMiles.Length)
                return "infinite";
            float mi = DistanceMiles[distanceIndex];
            if (mi < 0f) return "infinite";
            if (mi == 0f) return "0";
            return mi.ToString("0");
        }
    }
}
