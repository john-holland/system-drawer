using System;

namespace Planetary
{
    [Flags]
    public enum PlanetDataSourceMask
    {
        None = 0,
        Procedural = 1 << 0,
        Authored = 1 << 1,
        Gpx = 1 << 2,
        Continuum = 1 << 3,
        GoogleMaps = 1 << 4
    }
}
