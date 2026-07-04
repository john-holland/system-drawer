using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Liquid container with volume tracking in liters.</summary>
    public sealed class DrinkVesselComponent : MonoBehaviour
    {
        [Header("Volume (SI)")]
        public float capacityLiters = 0.5f;
        public float currentVolumeLiters = 0.5f;
        public float liquidDensityKgPerL = 1000f;

        [Header("Interior")]
        public MeshCollider interiorMeshCollider;

        public bool TryConsume(float liters)
        {
            if (liters <= 0f)
                return true;
            if (currentVolumeLiters < liters)
                return false;
            currentVolumeLiters -= liters;
            return true;
        }

        public float HeadMeters(float nozzleHeightM)
        {
            float fill = capacityLiters > 0f ? currentVolumeLiters / capacityLiters : 0f;
            return Mathf.Max(0f, fill * nozzleHeightM);
        }
    }
}
