using System;

namespace Planetary
{
    [Flags]
    public enum PlanetFeatureFlags
    {
        None = 0,
        MagneticPoles = 1 << 0,
        StablePoles = 1 << 1
    }
}
