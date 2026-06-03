using UnityEngine;

namespace Weather
{
    public interface IExternalHeightProvider
    {
        bool TrySampleHeightAtWorld(Vector3 worldPos, out float heightMeters, out float slopeDeg);
    }
}
