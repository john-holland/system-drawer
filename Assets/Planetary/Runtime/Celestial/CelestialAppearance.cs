using UnityEngine;

namespace Planetary.Celestial
{
    public struct CelestialAppearance
    {
        public Color tint;
        public float intensity;
        public float stareBackWeight;
        public bool visible;

        public static CelestialAppearance Default => new CelestialAppearance
        {
            tint = Color.white,
            intensity = 1f,
            stareBackWeight = 0f,
            visible = true
        };
    }
}
