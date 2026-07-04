using UnityEngine;

namespace Locomotion.Liquid
{
    /// <summary>Volume source for consumption ledger without Drink assembly dependency.</summary>
    public interface ILiquidVessel
    {
        float CurrentVolumeLiters { get; }
        float CapacityLiters { get; }
        bool TryConsume(float liters);
        void RefillToCapacity();
    }

    /// <summary>Driving instrument anchor for tool return and spawn pose.</summary>
    public sealed class LiquidDrivingInstrument : MonoBehaviour
    {
        public ILiquidVessel vessel;
        public Vector3 spawnReturnPosition;
        public int totalSquirtCount;
    }
}
