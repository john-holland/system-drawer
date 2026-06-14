using UnityEngine;
using Weather;

namespace Planetary.Bridges
{
    /// <summary>
    /// Maps shell cell centers to <see cref="WeatherPhysicsManifold"/> read/write APIs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlanetShellToWeatherManifoldAdapter : MonoBehaviour
    {
        public PlanetShellManifoldGrid shellGrid;
        public WeatherPhysicsManifold weatherManifold;

        public ManifoldCellData ReadAtWorld(Vector3 world)
        {
            if (weatherManifold == null)
                return default;
            return weatherManifold.GetDataAtPosition(world);
        }

        public void WriteAtWorld(Vector3 world, ManifoldCellData data)
        {
            if (weatherManifold == null)
                return;
            weatherManifold.SetDataAtPosition(world, data);
        }

        public void SyncAllCells()
        {
            if (shellGrid == null || weatherManifold == null || shellGrid.planet == null)
                return;

            shellGrid.EnumerateAllCells(id =>
            {
                ManifoldCellData data = shellGrid.Sample(id);
                shellGrid.Stamp(id, data);
            });
        }
    }
}
