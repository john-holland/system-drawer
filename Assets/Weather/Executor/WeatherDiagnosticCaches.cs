using UnityEngine;

namespace Weather.Executor
{
    /// <summary>Editor/diagnostics hooks to drop sticky weather caches before a GC pass.</summary>
    public static class WeatherDiagnosticCaches
    {
        public static void ClearStickyCaches()
        {
            WeatherExecutorService exec = WeatherExecutorService.Instance
                ?? Object.FindAnyObjectByType<WeatherExecutorService>();
            exec?.ClearDiagnosticCaches();
        }
    }
}
