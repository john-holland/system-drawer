using System.Collections.Generic;
using Locomotion.Liquid;
using UnityEngine;

namespace Locomotion.Drink.Flow
{
    /// <summary>Bake session lifecycle for drink flow curves (mirrors CloudBakeSession).</summary>
    public static class DrinkFlowBakeSession
    {
        public static bool IsActive { get; private set; }

        public static void Begin() => IsActive = true;

        public static void End() => IsActive = false;
    }

    [CreateAssetMenu(fileName = "DrinkFlowBakeAsset", menuName = "Continuum/Drink/Flow Bake Asset")]
    public sealed class DrinkFlowBakeAsset : ScriptableObject
    {
        public AnimationCurve flowLitersPerSecond = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve streamForceX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve streamForceY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve streamForceZ = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    public sealed class DrinkFlowBakeSolver
    {
        public DrinkFlowBakeAsset Bake(DrinkFlowModel model, float durationSeconds, float stepSeconds,
            LiquidWeatherManifoldBridge bridge = null)
        {
            var asset = ScriptableObject.CreateInstance<DrinkFlowBakeAsset>();
            if (model == null || durationSeconds <= 0f || stepSeconds <= 0f)
                return asset;

            DrinkFlowBakeSession.Begin();
            try
            {
                var flowKeys = new List<Keyframe>();
                var fx = new List<Keyframe>();
                var fy = new List<Keyframe>();
                var fz = new List<Keyframe>();
                float t = 0f;
                while (t <= durationSeconds)
                {
                    float q = model.ComputeInstantaneousFlowLitersPerSecond();
                    Vector3 force = model.StreamTipForward() * q;
                    model.SyncManifoldVelocity(force);
                    bridge?.PaintWaterSphere(model.StreamTipPosition(), 0.02f, force, model.handPressurePa);
                    flowKeys.Add(new Keyframe(t, q));
                    fx.Add(new Keyframe(t, force.x));
                    fy.Add(new Keyframe(t, force.y));
                    fz.Add(new Keyframe(t, force.z));
                    t += stepSeconds;
                }
                asset.flowLitersPerSecond = new AnimationCurve(flowKeys.ToArray());
                asset.streamForceX = new AnimationCurve(fx.ToArray());
                asset.streamForceY = new AnimationCurve(fy.ToArray());
                asset.streamForceZ = new AnimationCurve(fz.ToArray());
            }
            finally
            {
                DrinkFlowBakeSession.End();
            }
            return asset;
        }
    }
}
