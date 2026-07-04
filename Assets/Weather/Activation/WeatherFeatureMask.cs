using System;

namespace Weather.Activation
{
    [Flags]
    public enum WeatherFeatureMask
    {
        None = 0,
        Emergence = 1 << 0,
        LodEggs = 1 << 1,
        MeteorologyGuess = 1 << 2,
        CoarseAdvection = 1 << 3,
        WindField = 1 << 4,
        Precipitation = 1 << 5,
        Water = 1 << 6,
        Cloud = 1 << 7,
        FullManifold = 1 << 8,
        NearFieldGraph = 1 << 9,
        WeatherEvents = 1 << 10,
        VisualClouds = 1 << 11,
    }
}
