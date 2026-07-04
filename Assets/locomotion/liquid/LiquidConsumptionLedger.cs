using System;
using UnityEngine;

namespace Locomotion.Liquid
{
    public struct LiquidConsumptionSnapshot
    {
        public float dispensedLiters;
        public float mouthReceivedLiters;
        public float spillLiters;
        public float vesselRemainingLiters;
        public int sipsAttempted;
        public bool raiseCompleted;
        public bool dispenseSuppressed;
    }

    /// <summary>Per-beat liquid accounting with optional weather manifold publish.</summary>
    public sealed class LiquidConsumptionLedger : MonoBehaviour
    {
        public ILiquidVessel vessel;
        public LiquidWeatherManifoldBridge weatherBridge;
        public bool infiniteDrain;
        [Range(0f, 1f)] public float turbulencePenalty;

        LiquidConsumptionSnapshot _snapshot;
        float _pendingDebitLiters;

        public LiquidConsumptionSnapshot Snapshot => _snapshot;

        public void ResetBeat()
        {
            _snapshot = new LiquidConsumptionSnapshot
            {
                vesselRemainingLiters = vessel != null ? vessel.CurrentVolumeLiters : 0f,
            };
            _pendingDebitLiters = 0f;
        }

        public void MarkRaiseCompleted() => _snapshot.raiseCompleted = true;

        public void SetDispenseSuppressed(bool suppressed) => _snapshot.dispenseSuppressed = suppressed;

        public void RecordDispense(float flowLitersPerSecond, float deltaTime, float drinkEfficacy)
        {
            if (deltaTime <= 0f || flowLitersPerSecond <= 0f || _snapshot.dispenseSuppressed)
                return;

            float liters = flowLitersPerSecond * deltaTime;
            float eff = Mathf.Clamp01(drinkEfficacy) * (1f - Mathf.Clamp01(turbulencePenalty));
            float toMouth = liters * eff;
            float toSpill = liters - toMouth;

            _snapshot.dispensedLiters += liters;
            _snapshot.mouthReceivedLiters += toMouth;
            _snapshot.spillLiters += toSpill;
            _pendingDebitLiters += liters;

            if (vessel != null)
                _snapshot.vesselRemainingLiters = vessel.CurrentVolumeLiters;

            PublishSpill(toSpill);
        }

        public void RecordSipAttempt() => _snapshot.sipsAttempted++;

        public void ApplyVesselDebit()
        {
            if (vessel == null || _pendingDebitLiters <= 0f)
                return;

            if (infiniteDrain)
            {
                vessel.RefillToCapacity();
                _pendingDebitLiters = 0f;
                _snapshot.vesselRemainingLiters = vessel.CurrentVolumeLiters;
                return;
            }

            vessel.TryConsume(_pendingDebitLiters);
            _pendingDebitLiters = 0f;
            _snapshot.vesselRemainingLiters = vessel.CurrentVolumeLiters;
        }

        void PublishSpill(float spillLiters)
        {
            if (weatherBridge == null || spillLiters <= 0f)
                return;
            Vector3 origin = transform.position;
            weatherBridge.PaintSpillFootprint(origin, spillLiters, Vector3.down * 0.5f);
        }

        public void PublishStreamToManifold(Vector3 tip, Vector3 velocity, float pressurePa, float radius = 0.015f)
        {
            weatherBridge?.PaintWaterSphere(tip, radius, velocity, pressurePa);
        }
    }
}
