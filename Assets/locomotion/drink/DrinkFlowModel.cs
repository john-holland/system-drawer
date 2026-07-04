using Locomotion.Liquid;
using Locomotion.Liquid.Flood;
using UnityEngine;
using Weather;

namespace Locomotion.Drink
{
    /// <summary>Hydraulic flow model for drink nozzle or open rim (weir-style).</summary>
    public sealed class DrinkFlowModel : MonoBehaviour
    {
        const float Gravity = 9.81f;
        const float DischargeCoeff = 0.62f;

        public DrinkVesselComponent vessel;
        public DrinkNozzleComponent nozzle;
        public OpenEdgeLoopSpoutSimulator openRim;
        public DrinkLiquidContent liquidContent;
        public PhysicsManifold legacyManifold;
        public WeatherPhysicsManifold weatherManifold;
        public LiquidWeatherManifoldBridge weatherBridge;
        public bool infiniteDrain;

        [Tooltip("Hand grip pressure multiplier (Pa scale).")]
        public float handPressurePa = 101325f;

        [Range(0f, 1f)]
        public float apertureAngleEfficiency = 1f;

        [Tooltip("Fallback rim outlet area when using openRim only.")]
        public float openRimAreaM2 = 0.0004f;

        public float ComputeInstantaneousFlowLitersPerSecond()
        {
            if (vessel == null)
                return 0f;

            float head = vessel.HeadMeters(0.12f);
            if (!infiniteDrain && head <= 0f && vessel.currentVolumeLiters <= 0f)
                return 0f;

            float area = EffectiveApertureArea();
            float q = DischargeCoeff * area * Mathf.Sqrt(2f * Gravity * Mathf.Max(head, 0.01f));
            q *= PressureFactor();
            q *= MassViscosityFactor();
            q *= HardnessFactor();
            float litersPerSecond = q * 1000f;
            float maxQ = nozzle != null ? nozzle.maxThroughputLitersPerSecond : 0.08f;
            return Mathf.Min(litersPerSecond, maxQ);
        }

        float EffectiveApertureArea()
        {
            if (nozzle != null)
                return nozzle.EffectiveApertureArea(apertureAngleEfficiency);
            if (openRim != null && openRim.effectiveOutletAreaM2 > 0f)
                return openRim.effectiveOutletAreaM2 * apertureAngleEfficiency;
            return openRimAreaM2 * apertureAngleEfficiency;
        }

        float PressureFactor()
        {
            float atm = 101325f;
            return Mathf.Clamp(handPressurePa / atm, 0.5f, 2f);
        }

        float MassViscosityFactor()
        {
            if (liquidContent == null)
                return 1f;
            return 1f / (1f + liquidContent.sloshMassKg * 0.15f);
        }

        float HardnessFactor()
        {
            if (liquidContent == null)
                return 1f;
            return 1f - liquidContent.materialHardness * 0.5f;
        }

        public Vector3 StreamTipPosition()
        {
            if (nozzle != null)
                return nozzle.TipPosition;
            if (openRim != null)
                return openRim.RimWorldPosition;
            return transform.position;
        }

        public Vector3 StreamTipForward()
        {
            if (nozzle != null)
                return nozzle.TipForward;
            if (openRim != null)
                return openRim.loopNormal;
            return Vector3.down;
        }

        public void SyncManifoldVelocity(Vector3 streamForce)
        {
            if (legacyManifold != null)
            {
                legacyManifold.material = MaterialType.Water;
                legacyManifold.state = MaterialState.Liquid;
                legacyManifold.velocity = streamForce;
                legacyManifold.pressure = handPressurePa;
            }

            var bridge = weatherBridge != null ? weatherBridge : GetComponentInParent<LiquidWeatherManifoldBridge>();
            if (bridge != null)
            {
                bridge.PaintWaterSphere(StreamTipPosition(), 0.02f, streamForce, handPressurePa);
                return;
            }

            if (weatherManifold != null)
            {
                var data = weatherManifold.GetDataAtPosition(StreamTipPosition());
                data.mode = WeatherMode.Water;
                data.velocity = streamForce;
                data.pressure = handPressurePa / 100f;
                weatherManifold.SetDataAtPosition(StreamTipPosition(), data);
            }
        }
    }
}
