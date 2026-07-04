using System;
using UnityEngine;

namespace Weather.Activation
{
    /// <summary>
    /// Gates weather subsystems so only emergence corridors and LOD eggs run fine simulation by default.
    /// </summary>
    [Serializable]
    public sealed class WeatherActivationGate
    {
        public bool emergenceOnlyMode = true;

        [Tooltip("Enter active corridor at this weight.")]
        public float enterThreshold = 0.35f;

        [Tooltip("Exit active corridor below this weight.")]
        public float exitThreshold = 0.25f;

        [Tooltip("Minimum weight for heavy subsystems (wind, cloud, precip).")]
        public float heavyFeatureMinWeight = 0.5f;

        bool _corridorActive;

        public bool OnlyEmergenceAndEggs => emergenceOnlyMode;

        public WeatherFeatureMask DefaultEnabled =>
            WeatherFeatureMask.Emergence | WeatherFeatureMask.LodEggs;

        public float ApplyHysteresis(float rawWeight)
        {
            if (rawWeight >= enterThreshold)
                _corridorActive = true;
            else if (rawWeight < exitThreshold)
                _corridorActive = false;
            return _corridorActive ? Mathf.Max(rawWeight, exitThreshold) : rawWeight;
        }

        public bool IsActive(WeatherFeatureMask feature, float activationWeight, bool insideEggShell)
        {
            if (!emergenceOnlyMode)
                return true;

            if ((feature & DefaultEnabled) != 0)
                return true;

            float w = ApplyHysteresis(activationWeight);
            if (insideEggShell)
                return w >= exitThreshold;

            switch (feature)
            {
                case WeatherFeatureMask.MeteorologyGuess:
                case WeatherFeatureMask.CoarseAdvection:
                    return w > 0f || insideEggShell;
                case WeatherFeatureMask.FullManifold:
                case WeatherFeatureMask.NearFieldGraph:
                    return insideEggShell && w >= enterThreshold;
                case WeatherFeatureMask.WindField:
                case WeatherFeatureMask.Precipitation:
                case WeatherFeatureMask.Water:
                case WeatherFeatureMask.Cloud:
                case WeatherFeatureMask.VisualClouds:
                    return w >= heavyFeatureMinWeight || insideEggShell;
                case WeatherFeatureMask.WeatherEvents:
                    return w >= enterThreshold || insideEggShell;
                default:
                    return w >= enterThreshold;
            }
        }

        public void SetEmergenceOnlyMode(bool enabled) => emergenceOnlyMode = enabled;
    }
}
