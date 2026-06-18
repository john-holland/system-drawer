using System;

namespace Weather.Executor
{
    /// <summary>Runtime hooks registered by SystemDrawer.Networking (avoids Weather → Networking asmdef cycle).</summary>
    public static class WeatherNetworkSink
    {
        public static Action<WeatherEggClientPayload> SendPush;
        public static Action<WeatherEggApplyPayload> BroadcastApply;
    public static Action<WeatherEggBootstrapPayload> BroadcastBootstrap;
    public static Action OnRewindApplied;
    public static Action<string> OnSceneLoad;
}
}
