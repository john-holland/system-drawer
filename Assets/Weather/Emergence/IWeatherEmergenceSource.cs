using System.Collections.Generic;

namespace Weather.Emergence
{
    public interface IWeatherEmergenceSource
    {
        void CollectEmergenceVectors(List<EmergenceVector> into);
    }
}
