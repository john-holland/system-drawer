using System;
using Locomotion.Open.Nodes;

namespace Locomotion.Open
{
    /// <summary>Causality leaf hooks for open/close beat lifecycle.</summary>
    public static class OpenCloseCausalityBridge
    {
        public static event Action<OpenCloseClosureMode, string, string> OnLeafTransition;

        public static void NotifyActive(string nodeName)
        {
            OnLeafTransition?.Invoke(OpenCloseClosureMode.Auto, "open_beat_idle", $"open_beat_active_{nodeName}");
        }

        public static void NotifyOpened(string nodeName)
        {
            OnLeafTransition?.Invoke(OpenCloseClosureMode.OpenBeatClosed, $"open_beat_active_{nodeName}", "open_beat_opened");
        }

        public static void NotifyClosed(OpenCloseClosureMode mode, string nodeName = null)
        {
            string to = mode switch
            {
                OpenCloseClosureMode.LatchFailed => "latch_failed",
                OpenCloseClosureMode.Cancelled => "open_beat_cancelled",
                _ => "open_beat_closed",
            };
            string from = string.IsNullOrEmpty(nodeName) ? "open_beat_active" : $"open_beat_active_{nodeName}";
            OnLeafTransition?.Invoke(mode, from, to);
        }
    }
}
