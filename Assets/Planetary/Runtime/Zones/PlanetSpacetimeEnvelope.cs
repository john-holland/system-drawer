using UnityEngine;

namespace Planetary
{
    public sealed class PlanetSpacetimeEnvelope : MonoBehaviour
    {
        public PlanetBody planet;
        public float mass = 1e24f;
        public float atmosphereHeightMeters = 100000f;
        public PhysicalManifold manifold;

        void Awake()
        {
            if (manifold == null)
                manifold = GetComponent<PhysicalManifold>();
            float r = planet != null ? planet.PlanetRadius + atmosphereHeightMeters : atmosphereHeightMeters;
            transform.localScale = Vector3.one * r * 2f;
        }
    }
}
