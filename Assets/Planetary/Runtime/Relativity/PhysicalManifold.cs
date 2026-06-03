using UnityEngine;
using Weather;

namespace Planetary
{
    /// <summary>Gameplay manifold: gravity, radiation, lensing atop weather scalar field.</summary>
    public sealed class PhysicalManifold : PhysicsManifold
    {
        public float gravityWellStrength = 1f;
        public float radiationLevel;
        public float lensingStrength;
        public float fastMoverHazard;

        public float SampleRadiation(Vector3 world) => radiationLevel;
        public float SampleLensing(Vector3 world) => lensingStrength;
    }
}
