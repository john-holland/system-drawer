using UnityEngine;

namespace Planetary
{
    [CreateAssetMenu(fileName = "RadiationEmissionStandard", menuName = "Planetary/Radiation Emission Standard")]
    public sealed class RadiationEmissionStandard : ScriptableObject
    {
        public float baselineSievertsPerHour = 0.01f;
        public float maxExposureSieverts = 1f;
    }
}
