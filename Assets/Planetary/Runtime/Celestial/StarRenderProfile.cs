using UnityEngine;

namespace Planetary.Celestial
{
    [CreateAssetMenu(fileName = "StarRenderProfile", menuName = "Planetary/Star Render Profile")]
    public sealed class StarRenderProfile : ScriptableObject
    {
        public Color color = new Color(1f, 0.95f, 0.85f);
        public float colorTemperatureK = 5778f;
        public float coronaRadiusMultiplier = 1.2f;
        public float intensity = 2f;
        [Tooltip("When true, sun disk bypasses cubemap bake and renders with super-saturation.")]
        public bool bypassBakeForNearbySun = true;
        public float superSaturation = 3f;
    }
}
