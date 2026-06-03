namespace Planetary
{
    public interface IPlanetaryPlanarSource
    {
        PlanetDataSourceMask Mask { get; }
        float SampleHeight(float latDeg, float lonDeg);
        float SampleSlope(float latDeg, float lonDeg);
        int SampleBiome(float latDeg, float lonDeg);
    }
}
