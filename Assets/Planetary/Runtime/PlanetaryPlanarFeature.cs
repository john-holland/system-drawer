using UnityEngine;

namespace Planetary
{
    [CreateAssetMenu(fileName = "PlanetaryPlanarFeature", menuName = "Planetary/Planar Feature")]
    public sealed class PlanetaryPlanarFeature : ScriptableObject
    {
        public string featureId;
        public float latitudeDeg;
        public float longitudeDeg;
        public float tangentRotationDeg;
        [Min(1f)] public float footprintRadiusMeters = 500f;
        public Texture2D heightMap;
        public Texture2D albedo;
        [Range(0f, 1f)] public float strength = 1f;
        public float smoothRadius = 2f;
    }
}
