using System;

namespace Locomotion.Liquid
{
    /// <summary>Causality leaf hooks for liquid beat closure.</summary>
    public static class LiquidCausalityBridge
    {
        public static event Action<DrinkClosureMode, string, string> OnLeafTransition;

        public static void NotifyClosed(DrinkClosureMode mode)
        {
            string to = mode switch
            {
                DrinkClosureMode.Stalled => "stalled_beat_closed",
                DrinkClosureMode.SpillBeat => "spilled_beat_closed",
                DrinkClosureMode.InfiniteDrainBeat => "infinite_drain_beat_closed",
                DrinkClosureMode.Mouth => "drink_beat_closed",
                DrinkClosureMode.EmptyVessel => "empty_vessel_closed",
                _ => "drink_beat_closed",
            };
            OnLeafTransition?.Invoke(mode, "drink_beat_active", to);
        }
    }
}
