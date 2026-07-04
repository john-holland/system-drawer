using Locomotion.Liquid;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    sealed class StubLiquidVessel : ILiquidVessel
    {
        public float CurrentVolumeLiters { get; set; } = 0.2f;
        public float CapacityLiters { get; set; } = 0.2f;
        public float LastConsumed;

        public bool TryConsume(float liters)
        {
            if (liters <= 0f)
                return true;
            if (CurrentVolumeLiters < liters)
                return false;
            CurrentVolumeLiters -= liters;
            LastConsumed = liters;
            return true;
        }

        public void RefillToCapacity() => CurrentVolumeLiters = CapacityLiters;
    }

    public sealed class LiquidConsumptionLedgerTests
    {
        [Test]
        public void RecordDispense_SplitsMouthAndSpill_ByEfficacy()
        {
            var go = new GameObject("ledger");
            var ledger = go.AddComponent<LiquidConsumptionLedger>();
            ledger.turbulencePenalty = 0f;
            ledger.ResetBeat();
            ledger.RecordDispense(0.4f, 1f, 0.5f);
            Assert.AreEqual(0.4f, ledger.Snapshot.dispensedLiters, 0.0001f);
            Assert.AreEqual(0.2f, ledger.Snapshot.mouthReceivedLiters, 0.0001f);
            Assert.AreEqual(0.2f, ledger.Snapshot.spillLiters, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TurbulencePenalty_ReducesMouthDelivery()
        {
            var go = new GameObject("ledger");
            var ledger = go.AddComponent<LiquidConsumptionLedger>();
            ledger.turbulencePenalty = 0.5f;
            ledger.ResetBeat();
            ledger.RecordDispense(1f, 1f, 1f);
            Assert.AreEqual(0.5f, ledger.Snapshot.mouthReceivedLiters, 0.0001f);
            Assert.AreEqual(0.5f, ledger.Snapshot.spillLiters, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DispenseSuppressed_SkipsAccounting()
        {
            var go = new GameObject("ledger");
            var ledger = go.AddComponent<LiquidConsumptionLedger>();
            ledger.ResetBeat();
            ledger.SetDispenseSuppressed(true);
            ledger.RecordDispense(1f, 1f, 1f);
            Assert.AreEqual(0f, ledger.Snapshot.dispensedLiters, 0.0001f);
            Object.DestroyImmediate(go);
        }
    }

    public sealed class InfiniteDrainLedgerTests
    {
        [Test]
        public void InfiniteDrain_RefillsVesselInsteadOfDepleting()
        {
            var go = new GameObject("ledger");
            var ledger = go.AddComponent<LiquidConsumptionLedger>();
            var vessel = new StubLiquidVessel { CurrentVolumeLiters = 0.2f, CapacityLiters = 0.2f };
            ledger.vessel = vessel;
            ledger.infiniteDrain = true;
            ledger.ResetBeat();
            ledger.RecordDispense(0.5f, 1f, 0.5f);
            ledger.ApplyVesselDebit();
            Assert.AreEqual(0.2f, vessel.CurrentVolumeLiters, 0.0001f);
            Assert.AreEqual(0f, vessel.LastConsumed, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NormalDrain_ConsumesFromVessel()
        {
            var go = new GameObject("ledger");
            var ledger = go.AddComponent<LiquidConsumptionLedger>();
            var vessel = new StubLiquidVessel { CurrentVolumeLiters = 0.2f, CapacityLiters = 0.2f };
            ledger.vessel = vessel;
            ledger.infiniteDrain = false;
            ledger.ResetBeat();
            ledger.RecordDispense(0.1f, 1f, 1f);
            ledger.ApplyVesselDebit();
            Assert.AreEqual(0.1f, vessel.LastConsumed, 0.0001f);
            Assert.AreEqual(0.1f, vessel.CurrentVolumeLiters, 0.0001f);
            Object.DestroyImmediate(go);
        }
    }
}
