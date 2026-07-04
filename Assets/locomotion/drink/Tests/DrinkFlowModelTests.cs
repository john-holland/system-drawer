using Locomotion.Drink;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class DrinkFlowModelTests
    {
        [Test]
        public void ComputeFlow_IsZeroWhenEmpty()
        {
            var go = new GameObject("vessel");
            var vessel = go.AddComponent<DrinkVesselComponent>();
            vessel.currentVolumeLiters = 0f;
            var nozzleGo = new GameObject("nozzle");
            nozzleGo.transform.SetParent(go.transform);
            var nozzle = nozzleGo.AddComponent<DrinkNozzleComponent>();
            var model = go.AddComponent<DrinkFlowModel>();
            model.vessel = vessel;
            model.nozzle = nozzle;
            Assert.AreEqual(0f, model.ComputeInstantaneousFlowLitersPerSecond());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Hardness_ReducesEffectiveFlow()
        {
            var go = new GameObject("vessel");
            var vessel = go.AddComponent<DrinkVesselComponent>();
            vessel.currentVolumeLiters = 0.5f;
            var nozzleGo = new GameObject("nozzle");
            nozzleGo.transform.SetParent(go.transform);
            var nozzle = nozzleGo.AddComponent<DrinkNozzleComponent>();
            var soft = go.AddComponent<DrinkLiquidContent>();
            soft.materialHardness = 0f;
            var modelSoft = go.AddComponent<DrinkFlowModel>();
            modelSoft.vessel = vessel;
            modelSoft.nozzle = nozzle;
            modelSoft.liquidContent = soft;
            float qSoft = modelSoft.ComputeInstantaneousFlowLitersPerSecond();

            var hardGo = Object.Instantiate(go);
            hardGo.GetComponent<DrinkLiquidContent>().materialHardness = 1f;
            var modelHard = hardGo.GetComponent<DrinkFlowModel>();
            float qHard = modelHard.ComputeInstantaneousFlowLitersPerSecond();

            Assert.Greater(qSoft, qHard);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(hardGo);
        }
    }
}
