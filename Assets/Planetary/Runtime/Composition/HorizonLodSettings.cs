using UnityEngine;

namespace Planetary.Composition
{
    [CreateAssetMenu(fileName = "HorizonLodSettings", menuName = "Horizon/LOD Settings")]
    public sealed class HorizonLodSettings : ScriptableObject
    {
        public float fullSimRadiusKm = 50f;
        public float fullSimAltitudeMaxM = 12000f;
        public float horizonDistanceKm = 500f;
        public float surfaceBandMaxM = 2000f;
        public float troposphereMaxM = 12000f;
        public float upperAtmosphereMaxM = 80000f;
    }
}
