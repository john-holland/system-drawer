using UnityEngine;

namespace Locomotion.Camera
{
    public static class CameraPathingHeuristic
    {
        public static float ModeCostDelta(CameraFocusMode mode, in CameraPlannerHints hints)
        {
            int idx = (int)mode;
            float bias = 0f;
            if (hints.modeHintBias != null && idx >= 0 && idx < hints.modeHintBias.Length)
                bias = hints.modeHintBias[idx];

            float memFactor = 1f - Mathf.Clamp01(hints.memorabilityScore);
            float ratingFactor = 1f - Mathf.Clamp01(hints.userRatingMean / 5f);
            float delta = bias * hints.lstmWeight * (memFactor * 0.6f + ratingFactor * 0.4f);

            if (mode == hints.preferredMode)
                delta -= 0.25f;

            return delta;
        }

        public static void ApplyLstmHints(ref CameraPlannerHints hints, float[] lstmBias, float memorabilityScore)
        {
            hints.modeHintBias = lstmBias ?? hints.modeHintBias;
            hints.memorabilityScore = memorabilityScore;
        }
    }
}
