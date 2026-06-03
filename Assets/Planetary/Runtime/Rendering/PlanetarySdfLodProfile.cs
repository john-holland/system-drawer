using UnityEngine;

namespace Planetary.Rendering
{
    [CreateAssetMenu(fileName = "PlanetarySdfLodProfile", menuName = "Planetary/SDF LOD Profile")]
    public sealed class PlanetarySdfLodProfile : ScriptableObject
    {
        public int[] tierGridRes = { 12, 24, 48 };
        public float isoLevel = 0f;
        public float farFullSdfKm = 1000f;
        public float nearFullSdfKm = 10f;
        public float sdfHorizonMinAltM = 2000f;
        public float sdfHorizonFullAltM = 12000f;
        public float surfaceHorizonSdfScale = 0.2f;
        public float horizonStart = 0.35f;
        public float horizonEnd = 0.85f;
    }
}
