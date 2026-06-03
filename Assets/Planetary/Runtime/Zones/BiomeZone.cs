using UnityEngine;

namespace Planetary
{
    public sealed class BiomeZone : MonoBehaviour
    {
        public PlanetaryPlanarBase planarBase;
        public float minLat = -90f;
        public float maxLat = 90f;
        public int biomeId;

        public bool Contains(float lat, float lon) => lat >= minLat && lat <= maxLat;
    }
}
