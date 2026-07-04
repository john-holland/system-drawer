using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Liquid stream arc from nozzle to mouth with drag attenuation.</summary>
    public static class DrinkFlooshTrajectory
    {
        public const float LiquidDrag = 0.35f;

        public struct FlooshResult
        {
            public bool feasible;
            public Vector3 initialVelocity;
            public float timeOfFlight;
            public float volumeLitersDelivered;
        }

        public static FlooshResult Compute(
            Vector3 nozzleTip,
            Vector3 mouthTarget,
            float flowLitersPerSecond,
            float drinkEfficacy,
            float sipDurationSeconds,
            Vector3? gravity = null)
        {
            var traj = ThrowTrajectoryUtility.Compute(nozzleTip, mouthTarget, gravity);
            Vector3 g = gravity ?? Physics.gravity;
            Vector3 v = traj.initialVelocity * (1f - LiquidDrag);
            bool feasible = traj.feasible && drinkEfficacy > 0.05f;
            float delivered = flowLitersPerSecond * sipDurationSeconds * Mathf.Clamp01(drinkEfficacy);
            return new FlooshResult
            {
                feasible = feasible,
                initialVelocity = v,
                timeOfFlight = traj.timeOfFlight,
                volumeLitersDelivered = delivered,
            };
        }

        public static bool IsFeasible(Vector3 nozzleTip, Vector3 mouthTarget, float drinkEfficacy, float maxLaunchSpeed = 0f)
        {
            var traj = ThrowTrajectoryUtility.Compute(nozzleTip, mouthTarget, null, maxLaunchSpeed);
            float accuracy = Mathf.Clamp01(drinkEfficacy);
            return traj.feasible && accuracy >= 0.05f;
        }

        public static float VolumeForSip(DrinkLemmaProperties props, int sipIndex, float remainingLiters)
        {
            if (props.totalVolumeLiters <= 0f)
                return 0f;
            int count = Mathf.Max(1, props.sipCount);
            float perSip = props.totalVolumeLiters / count;
            if (sipIndex >= count - 1)
                return remainingLiters > 0f ? remainingLiters : perSip;
            return perSip;
        }
    }
}
