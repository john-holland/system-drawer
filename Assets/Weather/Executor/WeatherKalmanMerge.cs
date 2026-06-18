using UnityEngine;
using Weather.Lod;

namespace Weather.Executor
{
    public static class WeatherKalmanMerge
    {
        public static float ServerClientWeight(float confidence, int timeoutOrder)
        {
            float halving = Mathf.Pow(0.5f, Mathf.Max(0, timeoutOrder));
            return Mathf.Clamp01(confidence * halving);
        }

        public static float ClientRecoveryBlend(float elapsed, float recoverySeconds, float minBlend, float targetBlend)
        {
            float t = recoverySeconds > 0f ? Mathf.Clamp01(elapsed / recoverySeconds) : 1f;
            return Mathf.Lerp(minBlend, targetBlend, t);
        }

        public static ManifoldCellData BlendCells(ManifoldCellData client, ManifoldCellData server, float serverWeight)
        {
            serverWeight = Mathf.Clamp01(serverWeight);
            float clientWeight = 1f - serverWeight;
            return new ManifoldCellData
            {
                velocity = client.velocity * clientWeight + server.velocity * serverWeight,
                temperature = client.temperature * clientWeight + server.temperature * serverWeight,
                pressure = client.pressure * clientWeight + server.pressure * serverWeight,
                density = client.density * clientWeight + server.density * serverWeight,
                mode = serverWeight >= 0.5f ? server.mode : client.mode
            };
        }
    }
}
