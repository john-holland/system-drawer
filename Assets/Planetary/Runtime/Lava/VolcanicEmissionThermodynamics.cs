using UnityEngine;
using Weather;

namespace Planetary.Lava
{
    public static class VolcanicEmissionThermodynamics
    {
        public static void ApplyPhaseChangeAtVent(ref ManifoldCellData cell, float ventTempC)
        {
            if (ventTempC < 100f)
            {
                cell.mode = WeatherMode.Water;
                return;
            }
            if (ventTempC < 500f)
            {
                cell.mode = WeatherMode.Cloud;
                cell.gasPressure = Mathf.Max(cell.gasPressure, 1013f);
                return;
            }
            cell.mode = WeatherMode.MagmaPlume;
            cell.temperature = ventTempC;
            cell.gasPressure *= 1.5f;
        }

        public static void ApplyConvectionRadiation(ref ManifoldCellData cell, float deltaTime, float ambientC)
        {
            float dT = (cell.temperature - ambientC) * deltaTime * 0.01f;
            cell.temperature -= dT;
            cell.velocity += Vector3.up * dT * 0.001f;
        }
    }
}
