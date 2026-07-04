using Locomotion.Liquid;
using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Adapts DrinkVesselComponent to ILiquidVessel.</summary>
    public sealed class DrinkVesselLiquidAdapter : MonoBehaviour, ILiquidVessel
    {
        public DrinkVesselComponent vessel;

        void Awake()
        {
            if (vessel == null)
                vessel = GetComponent<DrinkVesselComponent>();
        }

        public float CurrentVolumeLiters => vessel != null ? vessel.currentVolumeLiters : 0f;
        public float CapacityLiters => vessel != null ? vessel.capacityLiters : 0f;

        public bool TryConsume(float liters) => vessel != null && vessel.TryConsume(liters);

        public void RefillToCapacity()
        {
            if (vessel != null)
                vessel.currentVolumeLiters = vessel.capacityLiters;
        }
    }
}
