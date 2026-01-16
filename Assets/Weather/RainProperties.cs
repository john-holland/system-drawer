using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Properties specific to rain rendering and particle effects.
    /// Used for configuring particle systems and visual effects.
    /// </summary>
    [CreateAssetMenu(fileName = "RainProperties", menuName = "Weather/Rain Properties")]
    public class RainProperties : ScriptableObject
    {
        [Header("Droplet Properties")]
        [Tooltip("Droplet size in mm")]
        [Range(0.1f, 10f)]
        public float dropletSize = 2f;

        [Tooltip("Fall speed in m/s")]
        [Range(1f, 20f)]
        public float fallSpeed = 9f; // Terminal velocity for raindrops

        [Tooltip("Wind drift factor (how much wind affects rain, 0-1)")]
        [Range(0f, 1f)]
        public float windDrift = 0.5f;

        [Header("Visual Properties")]
        [Tooltip("Rain color")]
        public Color rainColor = new Color(0.8f, 0.8f, 1f, 0.8f);

        [Tooltip("Splash effect enabled")]
        public bool enableSplash = true;

        [Tooltip("Splash size multiplier")]
        [Range(0.1f, 5f)]
        public float splashSize = 1f;

        [Header("Particle System")]
        [Tooltip("Emission rate multiplier")]
        [Range(0.1f, 10f)]
        public float emissionMultiplier = 1f;

        [Tooltip("Lifetime in seconds")]
        [Range(0.5f, 10f)]
        public float lifetime = 2f;

        [Tooltip("Gravity multiplier")]
        [Range(0f, 2f)]
        public float gravityMultiplier = 1f;
    }
}
