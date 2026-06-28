namespace Weather.CloudBake
{
    /// <summary>Active bake session flags for runtime integration (e.g. gate Cloud.ApplyWind).</summary>
    public static class CloudBakeSession
    {
        public static bool IsActive { get; private set; }
        public static bool AllowFloatAway { get; private set; }

        public static void Begin(bool allowFloatAway)
        {
            IsActive = true;
            AllowFloatAway = allowFloatAway;
        }

        public static void End()
        {
            IsActive = false;
            AllowFloatAway = false;
        }
    }
}
