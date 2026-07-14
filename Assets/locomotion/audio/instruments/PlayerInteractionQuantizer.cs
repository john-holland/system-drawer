using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Quantizes player interaction time/value. 0 = free continuous, 1 = hard grid.
    /// </summary>
    public static class PlayerInteractionQuantizer
    {
        public static float QuantizeTime(float timeSec, float bpm, float quantize01, int subdivision = 4)
        {
            quantize01 = Mathf.Clamp01(quantize01);
            if (quantize01 <= 0f || bpm <= 0f)
                return timeSec;
            float beatSec = 60f / bpm;
            float grid = beatSec / Mathf.Max(1, subdivision);
            float snapped = Mathf.Round(timeSec / grid) * grid;
            return Mathf.Lerp(timeSec, snapped, quantize01);
        }

        public static float QuantizeValue01(float value01, float quantize01, int steps = 12)
        {
            value01 = Mathf.Clamp01(value01);
            quantize01 = Mathf.Clamp01(quantize01);
            if (quantize01 <= 0f || steps <= 1)
                return value01;
            float step = 1f / steps;
            float snapped = Mathf.Round(value01 / step) * step;
            return Mathf.Lerp(value01, snapped, quantize01);
        }
    }
}
